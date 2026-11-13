namespace SecondMonitor.Collector
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Menu des stands de Le Mans Ultimate, réglé à la demande de l'ingénieur de SecondMonitor (pneus, réparation, énergie, carburant).
    /// LMU l'expose par l'API web locale de son interface (port 6397, non documentée, la même que CrewChief et Simulator Controller) :
    ///   GET  /rest/garage/PitMenu/receivePitMenu  → liste des lignes { name, currentSetting, settings: [{ text, type, isUsed }] }
    ///   POST /rest/garage/PitMenu/loadPitMenu     ← la même liste avec les nouveaux choix.
    /// Précautions : aucune lecture en boucle (seulement sur demande), le menu reçu est renvoyé tel quel sauf les lignes visées,
    /// et il est relu après l'envoi pour vérifier ce que le jeu a gardé.
    /// </summary>
    internal static class LmuPitMenu
    {
        private const string Base = "http://localhost:6397/rest/garage/PitMenu/";
        private static readonly string[] Corners = { "FL", "FR", "RL", "RR" };
        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(3) };
        private static readonly SemaphoreSlim Gate = new(1, 1);

        /// <summary>Lecture seule : le menu répond-il, et que contient-il ? (bouton « Tester » du collecteur).</summary>
        public static async Task<(bool Ok, string Text)> TestAsync(CancellationToken ct)
        {
            try
            {
                JsonArray menu = await ReadAsync(ct).ConfigureAwait(false);
                var names = menu.Select(Name).Where(n => n.Length > 0).ToList();
                string tyres = Row(menu, "TIRES:") is JsonNode t
                    ? $" Pneus proposés : {string.Join(", ", Settings(t).Select(Text).Where(x => x.Length > 0).Distinct().Take(6))}."
                    : string.Empty;
                return (true, $"Menu des stands lu : {names.Count} lignes ({string.Join(", ", names.Take(8).Select(n => n.TrimEnd(':').ToLowerInvariant()))}{(names.Count > 8 ? "…" : string.Empty)}).{tyres}");
            }
            catch (Exception ex)
            {
                return (false, Why(ex));
            }
        }

        /// <summary>Règle le menu selon la demande { tyres, noTyres, repair, energyTargetPct, fuelTargetLitres }. Retourne une phrase pour la radio.</summary>
        public static async Task<(bool Ok, string Text)> ApplyAsync(JsonElement order, CancellationToken ct)
        {
            if (!await Gate.WaitAsync(TimeSpan.FromSeconds(10), ct).ConfigureAwait(false))
            {
                return (false, "un autre réglage du menu des stands est en cours");
            }

            try
            {
                JsonArray before = await ReadAsync(ct).ConfigureAwait(false);
                var menu = (JsonArray)before.DeepClone();
                var (done, missing) = Plan(menu, order);
                if (done.Count == 0)
                {
                    return (false, missing.Count > 0 ? "Rien à régler : " + string.Join(", ", missing) + "." : "Rien à régler.");
                }

                await PostAsync(menu, ct).ConfigureAwait(false);
                await Task.Delay(400, ct).ConfigureAwait(false);
                JsonArray after = await ReadAsync(ct).ConfigureAwait(false);
                var kept = menu.Select((r, i) => (r, i))
                    .Where(x => Current(x.r) != Current(before[x.i]) && Current(Row(after, Name(x.r))) != Current(x.r))
                    .Select(x => Name(x.r).TrimEnd(':').ToLowerInvariant()).ToList();
                string text = "Menu des stands réglé : " + string.Join(", ", done) + ".";
                if (missing.Count > 0)
                {
                    text += " Pas possible : " + string.Join(", ", missing) + ".";
                }

                if (kept.Count > 0)
                {
                    text += $" Le jeu a gardé sa valeur pour : {string.Join(", ", kept)}.";
                }

                return (kept.Count == 0, text);
            }
            catch (Exception ex)
            {
                return (false, Why(ex));
            }
            finally
            {
                Gate.Release();
            }
        }

        /// <summary>Change les lignes visées dans <paramref name="menu"/> (sans appel réseau) : ce qui est réglé et ce qui ne l'est pas.</summary>
        internal static (List<string> Done, List<string> Missing) Plan(JsonArray menu, JsonElement order)
        {
            var done = new List<string>();
            var missing = new List<string>();
            string tyres = Str(order, "tyres");
            if (Bool(order, "noTyres"))
            {
                if (Set(menu, "TIRES:", 0))
                {
                    foreach (string c in Corners)
                    {
                        Set(menu, $"{c} TIRE:", 0);
                    }

                    done.Add("pas de pneus");
                }
                else
                {
                    missing.Add("pneus (ligne absente du menu)");
                }
            }
            else if (tyres != null)
            {
                JsonNode row = Row(menu, "TIRES:");
                int k = row == null ? -1 : PickTyre(row, tyres);
                if (k > 0)
                {
                    row["currentSetting"] = k;
                    string type = Str(Settings(row)[k], "type");
                    foreach (string c in Corners)
                    {
                        if (Row(menu, $"{c} TIRE:") is JsonNode r)
                        {
                            var opts = Settings(r);
                            int j = Enumerable.Range(1, Math.Max(0, opts.Count - 1)).Where(i => !Bool(opts[i], "isUsed") && Same(Str(opts[i], "type"), type)).DefaultIfEmpty(-1).First();
                            if (j > 0)
                            {
                                r["currentSetting"] = j;
                            }
                        }
                    }

                    done.Add("pneus " + Pretty(Text(Settings(row)[k])));
                }
                else
                {
                    missing.Add(row == null ? "pneus (ligne absente du menu)" : $"pneus {tyres.ToLowerInvariant()} neufs (pas de train disponible)");
                }
            }

            if (Bool(order, "repair"))
            {
                JsonNode d = Row(menu, "DAMAGE:");
                if (d != null && Settings(d).Count >= 2)
                {
                    d["currentSetting"] = Settings(d).Count - 1; // tout réparer
                    done.Add("réparation");
                }
                else
                {
                    missing.Add("réparation (rien à réparer)");
                }
            }

            // Énergie virtuelle (hypercar, LMGT3) ou carburant.
            double? energy = Num(order, "energyTargetPct");
            double? fuel = Num(order, "fuelTargetLitres");
            if (energy.HasValue && Row(menu, "VIRTUAL ENERGY:") is JsonNode ve)
            {
                int k = Closest(ve, energy.Value, Number);
                ve["currentSetting"] = k;
                done.Add($"énergie {Text(Settings(ve)[k])}");
            }
            else if (fuel.HasValue && Row(menu, "FUEL:") is JsonNode f)
            {
                var opts = Settings(f).Select((s, i) => (i, l: Litres(Text(s)))).Where(o => o.l.HasValue).ToList();
                if (opts.Count > 0)
                {
                    var enough = opts.Where(o => o.l >= fuel.Value - 0.5).ToList();
                    var pick = enough.Count > 0 ? enough.OrderBy(o => o.l).First() : opts.OrderByDescending(o => o.l).First();
                    f["currentSetting"] = pick.i;
                    done.Add($"carburant {Text(Settings(f)[pick.i])}");
                }
            }
            else if (energy.HasValue || fuel.HasValue)
            {
                missing.Add("carburant (ligne absente du menu)");
            }

            return (done, missing);
        }

        /// <summary>Train neuf de la gomme demandée (« Soft », « Medium », « Hard », « Pluie », « Intermédiaires ») ; « neufs » = même gomme qu'aujourd'hui.</summary>
        internal static int PickTyre(JsonNode row, string wanted)
        {
            var opts = Settings(row);
            bool Fits(int i)
            {
                JsonNode s = opts[i];
                string text = Text(s).ToLowerInvariant();
                string type = (Str(s, "type") ?? string.Empty).ToLowerInvariant();
                if (Bool(s, "isUsed") || text.Contains("invalid") || text.Contains("used") || text.Contains("no change"))
                {
                    return false;
                }

                bool wet = type.Contains("wet") || type.Contains("rain") || text.Contains("wet") || text.Contains("rain");
                return wanted switch
                {
                    "Pluie" => wet,
                    "Intermédiaires" => type.Contains("inter") || text.Contains("inter"),
                    "neufs" => !wet,
                    _ => !wet && (type.Contains(wanted.ToLowerInvariant()) || text.Contains(wanted.ToLowerInvariant())),
                };
            }

            var ok = Enumerable.Range(1, Math.Max(0, opts.Count - 1)).Where(Fits).ToList();
            if (ok.Count == 0)
            {
                return -1;
            }

            if (wanted == "neufs" && opts.Count > 0)
            {
                string now = Str(opts[Math.Clamp(Current(row), 0, opts.Count - 1)], "type");
                return ok.Where(i => now != null && Same(Str(opts[i], "type"), now)).DefaultIfEmpty(ok[0]).First();
            }

            return ok[0];
        }

        /// <summary>« New Soft » → « soft neufs », « Used Medium » → « medium usés » (libellés anglais du jeu, dits en français).</summary>
        internal static string Pretty(string text)
        {
            string t = text.Trim();
            Match m = Regex.Match(t, @"^(new|used)\s+(.+)$", RegexOptions.IgnoreCase);
            if (!m.Success)
            {
                return t.ToLowerInvariant();
            }

            string compound = m.Groups[2].Value.ToLowerInvariant().Replace("wet", "pluie");
            return compound + (m.Groups[1].Value.Equals("new", StringComparison.OrdinalIgnoreCase) ? " neufs" : " usés");
        }

        private static string Why(Exception ex) => ex switch
        {
            HttpRequestException or TaskCanceledException => "Le Mans Ultimate ne répond pas sur son API (port 6397) : le jeu est-il lancé, en session ?",
            JsonException or InvalidOperationException => "le menu des stands envoyé par Le Mans Ultimate n'est pas lisible (format changé ?)",
            _ => ex.Message,
        };

        private static async Task<JsonArray> ReadAsync(CancellationToken ct)
        {
            string json = await Http.GetStringAsync(Base + "receivePitMenu", ct).ConfigureAwait(false);
            if (JsonNode.Parse(json) is not JsonArray menu || menu.Any(r => r is not JsonObject o || !o.ContainsKey("name") || !o.ContainsKey("currentSetting") || o["settings"] is not JsonArray))
            {
                throw new InvalidOperationException("menu illisible");
            }

            return menu;
        }

        private static async Task PostAsync(JsonArray menu, CancellationToken ct)
        {
            using var content = new StringContent(menu.ToJsonString(), Encoding.UTF8, "application/json");
            using HttpResponseMessage r = await Http.PostAsync(Base + "loadPitMenu", content, ct).ConfigureAwait(false);
            r.EnsureSuccessStatusCode();
        }

        private static JsonNode Row(JsonArray menu, string name) => menu?.FirstOrDefault(r => Name(r).Equals(name, StringComparison.OrdinalIgnoreCase));

        private static string Name(JsonNode r) => r?["name"] is JsonValue v && v.TryGetValue(out string s) ? s.Trim() : string.Empty;

        private static int Current(JsonNode r) => r?["currentSetting"] is JsonValue v && v.TryGetValue(out int i) ? i : -1;

        private static List<JsonNode> Settings(JsonNode r) => (r?["settings"] as JsonArray)?.ToList() ?? new List<JsonNode>();

        private static string Text(JsonNode s) => Str(s, "text")?.Trim() ?? string.Empty;

        private static string Str(JsonNode s, string key) => s?[key] is JsonValue v && v.TryGetValue(out string x) ? x : null;

        private static bool Bool(JsonNode s, string key) => s?[key] is JsonValue v && v.TryGetValue(out bool b) && b;

        private static bool Same(string a, string b) => a != null && b != null && a.Equals(b, StringComparison.OrdinalIgnoreCase);

        private static bool Set(JsonArray menu, string name, int k)
        {
            JsonNode r = Row(menu, name);
            if (r == null || k < 0 || k >= Settings(r).Count)
            {
                return false;
            }

            r["currentSetting"] = k;
            return true;
        }

        private static int Closest(JsonNode r, double want, Func<string, double?> value)
        {
            int best = Current(r);
            double gap = double.MaxValue;
            var opts = Settings(r);
            for (int i = 0; i < opts.Count; i++)
            {
                if (value(Text(opts[i])) is double v && Math.Abs(v - want) < gap)
                {
                    gap = Math.Abs(v - want);
                    best = i;
                }
            }

            return best;
        }

        private static double? Number(string text)
        {
            Match m = Regex.Match(text ?? string.Empty, @"-?\d+(?:[.,]\d+)?");
            return m.Success ? double.Parse(m.Value.Replace(',', '.'), CultureInfo.InvariantCulture) : null;
        }

        private static double? Litres(string text)
        {
            Match m = Regex.Match(text ?? string.Empty, @"(\d+(?:[.,]\d+)?)\s*(l|gal)\b", RegexOptions.IgnoreCase);
            return m.Success ? double.Parse(m.Groups[1].Value.Replace(',', '.'), CultureInfo.InvariantCulture) * (m.Groups[2].Value.Equals("gal", StringComparison.OrdinalIgnoreCase) ? 3.785411 : 1) : null;
        }

        private static string Str(JsonElement e, string key) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(key, out JsonElement v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

        private static bool Bool(JsonElement e, string key) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(key, out JsonElement v) && v.ValueKind == JsonValueKind.True;

        private static double? Num(JsonElement e, string key) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(key, out JsonElement v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : null;
    }
}
