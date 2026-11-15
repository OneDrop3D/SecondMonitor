namespace SecondMonitor.Collector
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Encodings.Web;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Menu des stands de Le Mans Ultimate, réglé à la demande de l'ingénieur de SecondMonitor (pneus, réparation, énergie, carburant).
    /// LMU l'expose par l'API web locale de son interface (port 6397, non documentée) :
    ///   GET  /rest/garage/PitMenu/receivePitMenu → liste des lignes { "PMC Value", name, currentSetting, default, settings: [{ text, type?, isUsed? }] }
    ///   POST /rest/garage/PitMenu/loadPitMenu    ← la même liste COMPLÈTE (tableau nu), seuls les currentSetting changés.
    /// Ce que font les outils éprouvés, et que l'on suit ici :
    /// - CrewChief (LMU/LMU_REST_API.cs, LMUPitMenuController.cs) et Simulator Controller (Plugins/Libraries/LMURESTProvider.ahk) :
    ///   lecture, changement de currentSetting (un INDEX dans settings), renvoi du menu entier ; lignes repérées par leur nom anglais
    ///   (« TIRES: », « FL TIRE: », « DAMAGE: », « VIRTUAL ENERGY: », « FUEL RATIO: », « FUEL: »), aussi en jeu traduit (KoDriver, japonais).
    /// - VIRTUAL ENERGY : currentSetting = pourcentage visé après l'arrêt (TinyPedal process/vehicle.py, CrewChief SetVirtualEnergy),
    ///   textes « 50% 20 laps ». Litres → % par le ratio (« FUEL RATIO: », texte « 0.86 ») comme Simulator Controller et CrewChief.
    /// - FUEL : textes « 41L/15 laps » ; niveau visé après l'arrêt, sauf option « Relative Fuel Strategy » du jeu où les textes
    ///   commencent par « + » et comptent les litres à AJOUTER (CrewChief LMUPitMenuController.RelativeFuelStrategy).
    /// - Pneus : le jeu ignore « TIRES: » seul, ce sont les quatre lignes de roue qui comptent (Apexandchilloverlaysystem, vérifié en jeu) ;
    ///   chaque roue a sa propre liste, on choisit par gomme (« type ») et non par index ; « INVALID » et « Mixed Tyres » ne sont pas des gommes.
    /// - Le corps envoyé doit être un tableau : un objet nu fait planter le jeu (HellsCrimson/telemetry-handler, game/lmu/rest/write.go).
    /// - Adresse 127.0.0.1 et non « localhost » : depuis LMU 1.3.3, « localhost » (IPv6 essayé d'abord) échoue ou coûte ~2 s
    ///   (TinyPedal 2.47.0, LMULapTime docs/LMU_REST_API.md) ; « localhost » reste en secours pour les anciennes versions.
    /// - Réponses en text/plain sans charset mais en UTF-8 (KoDriver docs/lmu-windows-rest-api.md).
    /// Précautions : aucune lecture en boucle (seulement sur demande), le menu reçu est renvoyé tel quel sauf les lignes visées,
    /// rien n'est envoyé si tout est déjà réglé, et il est relu après l'envoi pour vérifier ce que le jeu a gardé.
    /// Chaque échange est écrit dans le journal du collecteur (tronqué) et en entier dans <see cref="DumpFile"/>.
    /// </summary>
    internal static class LmuPitMenu
    {
        public const string MenuPath = "/rest/garage/PitMenu/receivePitMenu";
        public const string LoadPath = "/rest/garage/PitMenu/loadPitMenu";

        /// <summary>Adresses sûres en lecture seule, interrogées seulement par le test (état du jeu, demande d'arrêt).</summary>
        public const string NavigationPath = "/navigation/state";
        public const string GameStatePath = "/rest/sessions/GetGameState";

        private static readonly string[] Corners = { "FL", "FR", "RL", "RR" };
        private static readonly string[] CornerNames = { "avant gauche", "avant droit", "arrière gauche", "arrière droit" };
        private static readonly JsonSerializerOptions AsSent = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        private static readonly SemaphoreSlim Gate = new(1, 1);
        private static readonly object DumpLock = new();
        private static readonly HttpClient Http = new(new SocketsHttpHandler
        {
            UseProxy = false, // jamais de proxy pour le PC local
            ConnectTimeout = TimeSpan.FromSeconds(3),
            PooledConnectionIdleTimeout = TimeSpan.FromSeconds(15),
        })
        {
            Timeout = TimeSpan.FromSeconds(6), // un jeu en chargement répond lentement (LMULapTime)
        };

        private static string _host;

        /// <summary>Port de l'API de LMU (changé seulement par les tests).</summary>
        internal static int Port { get; set; } = 6397;

        /// <summary>Adresses essayées dans l'ordre ; celle qui a répondu est reprise en premier la fois suivante.</summary>
        internal static string[] Hosts { get; set; } = { "127.0.0.1", "localhost" };

        /// <summary>Journal du collecteur (une ligne par échange, réponses tronquées).</summary>
        public static Action<string> Log { get; set; }

        /// <summary>Fichier où chaque échange est gardé en entier (requête envoyée, réponse brute), pour le diagnostic.</summary>
        public static string DumpFile { get; set; }

        /// <summary>Adresse de l'API (tests) : port et adresses à essayer, en oubliant celle qui avait répondu.</summary>
        internal static void UseAddress(int port, params string[] hosts)
        {
            Port = port;
            Hosts = hosts;
            _host = null;
        }

        /// <summary>Lecture seule : le menu répond-il, et que contient-il ? Phrase courte (radio).</summary>
        public static async Task<(bool Ok, string Text)> TestAsync(CancellationToken ct)
        {
            Inspection r = await InspectAsync(ct).ConfigureAwait(false);
            return (r.Ok, r.Text);
        }

        /// <summary>
        /// Lecture seule et détaillée (bouton « Tester l'API du menu des stands ») : toutes les lignes du menu et leurs choix,
        /// ce que l'ingénieur saura régler, l'état du jeu (navigation, demande d'arrêt) et la voiture vue par la télémétrie.
        /// </summary>
        public static async Task<Inspection> InspectAsync(CancellationToken ct, CarNow car = null)
        {
            Begin("Test du menu des stands (lecture seule)", car == null ? null : "voiture : " + car);
            JsonArray menu;
            try
            {
                menu = await ReadAsync(ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                string why = Why(ex);
                // Le jeu répond mais pas le menu (404, vide…) : son état dit souvent pourquoi.
                string state = ex is LmuException ? await StateAsync(ct).ConfigureAwait(false) : null;
                string detail = why + (state != null ? Environment.NewLine + state : string.Empty) + DumpHint();
                Write("Résultat du test : " + detail.Replace(Environment.NewLine, " | "));
                return new Inspection(false, why, detail);
            }

            string gameState = await StateAsync(ct).ConfigureAwait(false);
            var lines = new List<string> { $"Menu des stands lu sur {_host}:{Port} : {menu.Count} lignes." };
            if (gameState != null)
            {
                lines.Add(gameState);
            }

            lines.AddRange(Describe(menu));
            lines.Add("Reconnu : " + Recognised(menu) + ".");
            if (car != null)
            {
                lines.Add("Voiture (télémétrie du collecteur) : " + car + ".");
            }

            string detailText = string.Join(Environment.NewLine, lines) + DumpHint();
            Write("Résultat du test : " + string.Join(" | ", lines));
            return new Inspection(true, Summary(menu), detailText);
        }

        /// <summary>Règle le menu selon la demande { tyres, noTyres, repair, energyTargetPct, fuelTargetLitres }. Retourne une phrase pour la radio.</summary>
        public static async Task<(bool Ok, string Text)> ApplyAsync(JsonElement order, CancellationToken ct, CarNow car = null)
        {
            if (!await Gate.WaitAsync(TimeSpan.FromSeconds(10), ct).ConfigureAwait(false))
            {
                return (false, "un autre réglage du menu des stands est en cours");
            }

            try
            {
                Begin("Réglage du menu des stands", $"demande : {(order.ValueKind == JsonValueKind.Undefined ? "(vide)" : order.GetRawText())} ; voiture : {car?.ToString() ?? "inconnue"}");
                JsonArray before = await ReadAsync(ct).ConfigureAwait(false);
                var menu = (JsonArray)before.DeepClone();
                var (done, missing) = Plan(menu, order, car);
                List<Change> changes = Changes(before, menu);
                Write(changes.Count == 0 ? "Menu des stands : rien à changer dans le menu." : "Menu des stands, changements : " + string.Join(" ; ", changes.Select(c => c.ToString())));
                if (done.Count == 0)
                {
                    return (false, missing.Count > 0 ? "Rien à régler : " + string.Join(", ", missing) + "." : "Rien à régler.");
                }

                string text = (changes.Count == 0 ? "Menu des stands déjà réglé : " : "Menu des stands réglé : ") + string.Join(", ", done) + ".";
                if (missing.Count > 0)
                {
                    text += " Pas possible : " + string.Join(", ", missing) + ".";
                }

                if (changes.Count == 0)
                {
                    return (true, text);
                }

                await PostAsync(menu, ct).ConfigureAwait(false);
                await Task.Delay(400, ct).ConfigureAwait(false); // le temps que le jeu prenne le menu (pit-wall, Apex Radio)
                JsonArray after = await ReadAsync(ct).ConfigureAwait(false);
                var refused = changes.Select(c => (c, now: Same(after, before, c.Index, c.Name))).Where(x => Current(x.now) != x.c.To).ToList();
                // Le jeu recalcule « TIRES: » d'après les roues : quand les quatre roues ont été prises, seules elles comptent.
                static bool IsCorner(Change c) => Key(c.Name).EndsWith("TIRE", StringComparison.Ordinal);
                if (changes.Any(IsCorner) && !refused.Any(x => IsCorner(x.c)))
                {
                    refused.RemoveAll(x => Key(x.c.Name) == "TIRES");
                }

                var kept = refused.Select(x => x.now == null ? $"{Short(x.c.Name)} (ligne disparue)" : $"{Short(x.c.Name)} ({OptionText(x.now, Current(x.now))} au lieu de {x.c.ToText})").ToList();
                if (kept.Count > 0)
                {
                    text += $" Le jeu a gardé sa valeur pour : {string.Join(", ", kept)}.";
                }

                Write(kept.Count == 0 ? "Menu des stands : relu après l'envoi, le jeu a tout gardé." : "Menu des stands : relu après l'envoi, refusé : " + string.Join(", ", kept));
                return (kept.Count == 0, text);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                return (false, Why(ex));
            }
            finally
            {
                Gate.Release();
            }
        }

        /// <summary>Change les lignes visées dans <paramref name="menu"/> (sans appel réseau) : ce qui est réglé et ce qui ne l'est pas.</summary>
        internal static (List<string> Done, List<string> Missing) Plan(JsonArray menu, JsonElement order, CarNow car = null)
        {
            var done = new List<string>();
            var missing = new List<string>();
            PlanTyres(menu, order, car, done, missing);

            if (Bool(order, "repair"))
            {
                JsonNode d = Row(menu, "DAMAGE");
                if (d != null && Settings(d).Count >= 2)
                {
                    d["currentSetting"] = Settings(d).Count - 1; // dernier choix : tout réparer (Simulator Controller setRepairs)
                    done.Add("réparation");
                }
                else
                {
                    missing.Add(d == null ? "réparation (ligne absente du menu)" : "réparation (rien à réparer)");
                }
            }

            PlanRatio(menu, order, done, missing);
            PlanEnergy(menu, order, car, done, missing);
            return (done, missing);
        }

        /// <summary>« New Soft » → « soft neufs », « Used Medium -1% » → « medium usés », « New Wet » → « pluie neufs » (libellés du jeu, dits en français).</summary>
        internal static string Pretty(string text)
        {
            string t = Regex.Replace(text ?? string.Empty, @"\s+-?\d+\s*%\s*$", string.Empty).Trim();
            Match m = Regex.Match(t, @"^(new|neufs?|nouveaux?|used|usés?|usagés?)\s+(.+)$", RegexOptions.IgnoreCase);
            string compound = (m.Success ? m.Groups[2].Value : t).ToLowerInvariant();
            compound = Regex.Replace(compound, @"\b(wet|rain|mouillés?|mouillées?)\b", "pluie");
            if (!m.Success)
            {
                return compound;
            }

            return compound + (m.Groups[1].Value.StartsWith("n", StringComparison.OrdinalIgnoreCase) ? " neufs" : " usés");
        }

        /// <summary>Une ligne par ligne du menu : choix courant et choix possibles (pneus en entier, longues listes abrégées).</summary>
        internal static List<string> Describe(JsonArray menu)
        {
            var lines = new List<string>();
            foreach (JsonNode r in menu)
            {
                List<JsonNode> opts = Settings(r);
                int cur = Current(r);
                string name = Name(r);
                if (name.Length == 0)
                {
                    lines.Add("• (ligne sans nom) " + Cut(r?.ToJsonString(AsSent), 120));
                    continue;
                }

                bool tyre = opts.Any(o => Has(o, "type") || Has(o, "isUsed")) || Key(name).EndsWith("TIRE", StringComparison.Ordinal) || Key(name) == "TIRES";
                IEnumerable<string> shown = tyre || opts.Count <= 6
                    ? opts.Select((o, i) => $"{i} {Label(o)}")
                    : opts.Take(2).Select((o, i) => $"{i} {Label(o)}").Append("…").Append($"{opts.Count - 1} {Label(opts[^1])}");
                string pmc = r is JsonObject ro && ro["PMC Value"] is JsonValue p ? $" [PMC {p.ToJsonString()}]" : string.Empty;
                lines.Add($"• {name}{pmc} = {cur} « {OptionText(r, cur)} » ({opts.Count} choix : {string.Join(" · ", shown)})");
            }

            return lines;
        }

        private static string Label(JsonNode o)
        {
            string text = Text(o);
            var extra = new List<string>();
            if (Str(o, "type") is string type && type.Length > 0)
            {
                extra.Add(type);
            }

            if (Bool(o, "isUsed"))
            {
                extra.Add("usé");
            }

            return extra.Count == 0 ? text : $"{text} ({string.Join(", ", extra)})";
        }

        /// <summary>Ce que l'ingénieur saura régler avec ce menu.</summary>
        private static string Recognised(JsonArray menu)
        {
            var parts = new List<string>();
            var (all, corners) = TyreRows(menu);
            int wheels = corners.Count(c => c != null);
            parts.Add(all == null && wheels == 0 ? "pneus : non (aucune ligne de pneus)" : $"pneus : oui ({(all != null ? Name(all) : "pas de ligne TIRES")} + {wheels} roue{(wheels > 1 ? "s" : string.Empty)})");
            JsonNode d = Row(menu, "DAMAGE");
            parts.Add(d == null ? "réparation : pas de ligne DAMAGE" : Settings(d).Count >= 2 ? $"réparation : oui (« {OptionText(d, Settings(d).Count - 1)} »)" : "réparation : rien à réparer pour l'instant");
            JsonNode ve = EnergyRow(menu);
            JsonNode ratio = Row(menu, "FUELRATIO");
            JsonNode fuel = FuelRow(menu);
            parts.Add(ve == null ? "énergie virtuelle : pas de ligne" : $"énergie virtuelle : oui ({OptionText(ve, Current(ve))})");
            if (ratio != null)
            {
                parts.Add($"ratio carburant {OptionText(ratio, Current(ratio))}");
            }

            parts.Add(fuel == null ? "carburant : pas de ligne FUEL" : $"carburant : oui ({(Relative(fuel) ? "litres à ajouter, option Relative Fuel Strategy" : "niveau après l'arrêt")})");
            return string.Join(" · ", parts);
        }

        /// <summary>Phrase courte du test, pour la radio.</summary>
        private static string Summary(JsonArray menu)
        {
            var can = new List<string>();
            var (all, corners) = TyreRows(menu);
            JsonNode refRow = all ?? corners.FirstOrDefault(c => c != null);
            if (refRow != null)
            {
                List<JsonNode> opts = Settings(refRow);
                var news = Enumerable.Range(1, Math.Max(0, opts.Count - 1)).Where(i => Selectable(opts[i]) && !Used(opts[i]))
                    .Select(i => Pretty(Text(opts[i])).Replace(" neufs", string.Empty)).Distinct().ToList();
                can.Add(news.Count > 0 ? $"pneus ({string.Join(", ", news)})" : "pneus (aucun train neuf)");
            }

            if (EnergyRow(menu) is JsonNode ve)
            {
                can.Add($"énergie (aujourd'hui {Format(Percent(ve, Current(ve)) ?? Current(ve))} %)");
            }

            if (Row(menu, "FUELRATIO") is JsonNode ratioRow)
            {
                can.Add($"ratio (aujourd'hui {OptionText(ratioRow, Current(ratioRow))})");
            }
            else if (FuelRow(menu) is JsonNode f)
            {
                can.Add($"carburant (aujourd'hui {OptionText(f, Current(f))})");
            }

            JsonNode d = Row(menu, "DAMAGE");
            string repair = d == null ? " Pas de ligne réparation." : Settings(d).Count >= 2 ? string.Empty : " Réparation : rien à réparer pour l'instant.";
            if (d != null && Settings(d).Count >= 2)
            {
                can.Add("réparation");
            }

            return $"Menu des stands lu : {menu.Count} lignes. Je peux régler : {(can.Count > 0 ? string.Join(", ", can) : "rien de connu")}.{repair}";
        }

        private static void PlanTyres(JsonArray menu, JsonElement order, CarNow car, List<string> done, List<string> missing)
        {
            string tyres = Str(order, "tyres");
            bool none = Bool(order, "noTyres");
            if (!none && tyres == null)
            {
                return;
            }

            var (all, corners) = TyreRows(menu);
            if (all == null && corners.All(c => c == null))
            {
                missing.Add("pneus (ligne absente du menu)");
                return;
            }

            if (none)
            {
                // Premier choix de chaque ligne : « No Change ».
                Set(all, 0);
                foreach (JsonNode c in corners)
                {
                    Set(c, 0);
                }

                done.Add("pas de pneus");
                return;
            }

            // Gomme voulue pour chaque roue : « neufs » = la gomme montée (télémétrie), roue par roue (avant / arrière).
            JsonNode refRow = all ?? corners.First(c => c != null);
            var spec = new string[4];
            if (tyres == "neufs")
            {
                string front = SpecFor(refRow, "neufs", car?.FrontCompound);
                string rear = car?.RearCompound != null ? SpecFor(refRow, "neufs", car.RearCompound) : front;
                if (front == null)
                {
                    missing.Add("pneus neufs (gomme montée inconnue : précise soft, medium ou hard)");
                    return;
                }

                spec = new[] { front, front, rear ?? front, rear ?? front };
            }
            else
            {
                string s = SpecFor(refRow, tyres, null);
                spec = new[] { s, s, s, s };
            }

            var picks = new int[4];
            var lacking = new List<string>();
            for (int i = 0; i < 4; i++)
            {
                picks[i] = corners[i] == null ? -1 : PickFor(corners[i], spec[i]);
                if (corners[i] != null && picks[i] < 1)
                {
                    lacking.Add(CornerNames[i]);
                }
            }

            bool sameAll = spec.Distinct(StringComparer.OrdinalIgnoreCase).Count() == 1;
            int allPick = all == null ? -1 : sameAll ? PickFor(all, spec[0]) : MixedIndex(all);
            string say = SayTyres(tyres, spec, all, allPick, corners, picks);
            if (lacking.Count > 0 || (corners.All(c => c == null) && allPick < 1))
            {
                // Pas de train incomplet : rien n'est changé.
                missing.Add($"{say} (pas de train neuf disponible{(lacking.Count is > 0 and < 4 ? " pour " + string.Join(", ", lacking) : string.Empty)})");
                return;
            }

            if (allPick >= 1)
            {
                Set(all, allPick);
            }

            for (int i = 0; i < 4; i++)
            {
                Set(corners[i], picks[i]);
            }

            done.Add(say);
        }

        private static string SayTyres(string wanted, string[] spec, JsonNode all, int allPick, JsonNode[] corners, int[] picks)
        {
            if (spec.Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
            {
                return $"pneus neufs ({Pretty(spec[0])} avant, {Pretty(spec[2])} arrière)";
            }

            int k = Array.FindIndex(corners, c => c != null);
            string text = allPick >= 1 ? Text(Settings(all)[allPick]) : k >= 0 && picks[k] >= 1 ? Text(Settings(corners[k])[picks[k]]) : null;
            return text != null ? "pneus " + Pretty(text) : wanted == "neufs" ? $"pneus {Pretty(spec[0])} neufs" : $"pneus {Pretty(wanted)} neufs";
        }

        /// <summary>
        /// Ratio carburant / énergie virtuelle (ligne « FUEL RATIO: », textes « 0.86 ») : le choix le plus proche du ratio demandé,
        /// à égalité le plus haut (une goutte d'essence en plus plutôt qu'en moins).
        /// </summary>
        private static void PlanRatio(JsonArray menu, JsonElement order, List<string> done, List<string> missing)
        {
            if (Num(order, "fuelRatio") is not double want || want <= 0)
            {
                return;
            }

            JsonNode row = Row(menu, "FUELRATIO");
            if (row == null)
            {
                missing.Add("ratio (pas de ligne FUEL RATIO dans le menu)");
                return;
            }

            var opts = Settings(row).Select((o, i) => (i, v: Number(Text(o)))).Where(o => o.v is > 0).Select(o => (o.i, v: o.v.Value)).ToList();
            if (opts.Count == 0)
            {
                missing.Add("ratio (choix du menu illisibles)");
                return;
            }

            var pick = opts.OrderBy(o => Math.Abs(o.v - want)).ThenByDescending(o => o.v).First();
            Set(row, pick.i);
            done.Add($"ratio {pick.v.ToString("0.00", CultureInfo.InvariantCulture).Replace('.', ',')}");
        }

        private static void PlanEnergy(JsonArray menu, JsonElement order, CarNow car, List<string> done, List<string> missing)
        {
            double? energy = Num(order, "energyTargetPct");
            double? fuel = Num(order, "fuelTargetLitres");
            if (!energy.HasValue && !fuel.HasValue)
            {
                return;
            }

            JsonNode ve = EnergyRow(menu);
            JsonNode f = FuelRow(menu);
            if (ve != null)
            {
                // Énergie virtuelle (hypercar, LMGT3) : pourcentage visé après l'arrêt. Des litres seuls passent par le ratio carburant.
                double? pct = energy;
                if (!pct.HasValue && fuel.HasValue && Number(OptionText(Row(menu, "FUELRATIO"), Current(Row(menu, "FUELRATIO")))) is double ratio && ratio > 0)
                {
                    pct = fuel.Value / ratio;
                }

                if (!pct.HasValue)
                {
                    missing.Add("carburant (voiture à énergie virtuelle, ratio carburant illisible)");
                    return;
                }

                int k = AtLeast(ve, Math.Clamp(pct.Value, 0, 100), i => Percent(ve, i));
                Set(ve, k);
                done.Add($"énergie {Format(Percent(ve, k) ?? k)} %");
                return;
            }

            if (f == null || !fuel.HasValue)
            {
                missing.Add(f == null ? "carburant (ligne absente du menu)" : "carburant (demande en énergie, menu en litres)");
                return;
            }

            double want = fuel.Value;
            bool relative = Relative(f);
            if (relative)
            {
                // Option « Relative Fuel Strategy » : le menu compte les litres à ajouter.
                if (car?.FuelLitres is not double now)
                {
                    missing.Add("carburant (le menu compte les litres à ajouter et le niveau actuel est inconnu)");
                    return;
                }

                want = Math.Max(0, want - now);
            }

            List<(int i, double l)> opts = Settings(f).Select((s, i) => (i, l: Litres(Text(s)))).Where(o => o.l.HasValue).Select(o => (o.i, o.l.Value)).ToList();
            if (opts.Count == 0)
            {
                missing.Add("carburant (choix du menu illisibles)");
                return;
            }

            var enough = opts.Where(o => o.l >= want - 0.5).ToList();
            var pick = enough.Count > 0 ? enough.OrderBy(o => o.l).First() : opts.OrderByDescending(o => o.l).First();
            Set(f, pick.i);
            done.Add($"carburant {Format(pick.l)} L{(relative ? " ajoutés" : string.Empty)}");
        }

        /// <summary>Gomme cherchée dans le menu : « wet », « inter », la gomme demandée ou la gomme montée.</summary>
        private static string SpecFor(JsonNode row, string wanted, string fitted)
        {
            switch (wanted)
            {
                case null:
                    return null;
                case "Pluie":
                    return "wet";
                case "Intermédiaires":
                    return "inter";
                case "neufs":
                    if (!string.IsNullOrWhiteSpace(fitted))
                    {
                        return fitted.Trim();
                    }

                    // Télémétrie absente : la gomme déjà choisie dans le menu, sinon la seule gomme sèche proposée.
                    List<JsonNode> opts = Settings(row);
                    int cur = Current(row);
                    if (cur >= 1 && cur < opts.Count && Selectable(opts[cur]) && Str(opts[cur], "type") is string chosen && chosen.Length > 0)
                    {
                        return chosen;
                    }

                    var dry = opts.Skip(1).Where(o => Selectable(o) && !Wet(o)).Select(o => Str(o, "type") ?? Pretty(Text(o)).Replace(" neufs", string.Empty).Replace(" usés", string.Empty))
                        .Where(t => t.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                    return dry.Count == 1 ? dry[0] : null;
                default:
                    return wanted;
            }
        }

        /// <summary>Train NEUF d'une ligne de pneus pour une gomme (« wet », « inter », « Soft », « Medium »…), ou -1.</summary>
        private static int PickFor(JsonNode row, string spec)
        {
            if (row == null || string.IsNullOrWhiteSpace(spec))
            {
                return -1;
            }

            List<JsonNode> opts = Settings(row);
            var ok = Enumerable.Range(1, Math.Max(0, opts.Count - 1)).Where(i => Selectable(opts[i]) && !Used(opts[i])).ToList();
            string s = spec.Trim().ToLowerInvariant();
            if (s == "inter" || s.StartsWith("inter", StringComparison.Ordinal))
            {
                return ok.Where(i => Inter(opts[i])).DefaultIfEmpty(-1).First();
            }

            if (WetWord(s))
            {
                return ok.Where(i => Wet(opts[i]) && !Inter(opts[i])).Concat(ok.Where(i => Wet(opts[i]))).DefaultIfEmpty(-1).First();
            }

            return ok.Where(i => !Wet(opts[i]) && Matches(opts[i], s)).DefaultIfEmpty(-1).First();
        }

        private static bool Matches(JsonNode option, string spec)
        {
            string type = (Str(option, "type") ?? string.Empty).Trim().ToLowerInvariant();
            if (type.Length > 0)
            {
                return type == spec || type.Contains(spec, StringComparison.Ordinal) || spec.Contains(type, StringComparison.Ordinal);
            }

            return Text(option).ToLowerInvariant().Contains(spec, StringComparison.Ordinal);
        }

        /// <summary>Choix « Mixed Tyres » de la ligne TIRES (gommes différentes avant / arrière), ou -1.</summary>
        private static int MixedIndex(JsonNode row) =>
            Settings(row).Select((o, i) => (o, i)).Where(x => Regex.IsMatch(Text(x.o), @"^(mixed|mixte)", RegexOptions.IgnoreCase)).Select(x => x.i).DefaultIfEmpty(-1).First();

        /// <summary>Un vrai train de pneus (pas « No Change », « INVALID » ni « Mixed Tyres »).</summary>
        private static bool Selectable(JsonNode o)
        {
            string t = Text(o).ToLowerInvariant();
            return t.Length > 0 && !t.StartsWith("invalid", StringComparison.Ordinal) && !Regex.IsMatch(t, @"^(mixed|mixte)") && !t.Contains("no change", StringComparison.Ordinal) && !t.StartsWith("aucun", StringComparison.Ordinal);
        }

        private static bool Used(JsonNode o) => Bool(o, "isUsed") || Regex.IsMatch(Text(o), @"^(used|usés?|usagés?)\b", RegexOptions.IgnoreCase);

        private static bool Wet(JsonNode o) => WetWord(((Str(o, "type") ?? string.Empty) + " " + Text(o)).ToLowerInvariant());

        private static bool WetWord(string s) => s.Contains("wet", StringComparison.Ordinal) || s.Contains("rain", StringComparison.Ordinal) || s.Contains("pluie", StringComparison.Ordinal) || s.Contains("mouill", StringComparison.Ordinal);

        private static bool Inter(JsonNode o) => ((Str(o, "type") ?? string.Empty) + " " + Text(o)).ToLowerInvariant().Contains("inter", StringComparison.Ordinal);

        /// <summary>Ligne « TIRES: » et lignes des quatre roues (par nom ; sinon les lignes dont les choix portent une gomme « type »).</summary>
        private static (JsonNode All, JsonNode[] Corners) TyreRows(JsonArray menu)
        {
            JsonNode all = Row(menu, "TIRES");
            JsonNode[] corners = Corners.Select(c => Row(menu, c + "TIRE")).ToArray();
            if (all == null && corners.All(c => c == null))
            {
                var typed = menu.Where(r => Settings(r).Any(o => Has(o, "type"))).ToList();
                if (typed.Count >= 5)
                {
                    all = typed[0];
                    corners = typed.Skip(1).Take(4).ToArray();
                }
                else if (typed.Count == 4)
                {
                    corners = typed.ToArray();
                }
                else if (typed.Count > 0)
                {
                    all = typed[0];
                }
            }

            return (all, corners);
        }

        /// <summary>Ligne « VIRTUAL ENERGY: » (ou, faute de nom, la longue liste « 50% 20 laps »).</summary>
        private static JsonNode EnergyRow(JsonArray menu) =>
            Row(menu, "VIRTUALENERGY") ?? menu.FirstOrDefault(r => Settings(r).Count >= 50 && Settings(r).Count(o => Regex.IsMatch(Text(o), @"^\d+(?:[.,]\d+)?\s*%")) >= Settings(r).Count * 9 / 10);

        /// <summary>Ligne « FUEL: » (ou, faute de nom, la liste « 41L/15 laps »).</summary>
        private static JsonNode FuelRow(JsonArray menu) =>
            Row(menu, "FUEL") ?? menu.FirstOrDefault(r => Settings(r).Count >= 3 && Settings(r).Count(o => Regex.IsMatch(Text(o), @"^\+?\d+(?:[.,]\d+)?\s*(l|gal)\b", RegexOptions.IgnoreCase)) >= Settings(r).Count * 9 / 10);

        private static bool Relative(JsonNode fuelRow) => Settings(fuelRow).Any(o => Text(o).StartsWith("+", StringComparison.Ordinal));

        private static double? Percent(JsonNode ve, int i)
        {
            List<JsonNode> opts = Settings(ve);
            if (i < 0 || i >= opts.Count)
            {
                return null;
            }

            Match m = Regex.Match(Text(opts[i]), @"^(\d+(?:[.,]\d+)?)\s*%");
            return m.Success ? double.Parse(m.Groups[1].Value.Replace(',', '.'), CultureInfo.InvariantCulture) : opts.Count >= 101 ? i : null;
        }

        /// <summary>Plus petit choix qui atteint la valeur voulue (au demi-point près), sinon le plus grand.</summary>
        private static int AtLeast(JsonNode r, double want, Func<int, double?> value)
        {
            var opts = Enumerable.Range(0, Settings(r).Count).Select(i => (i, v: value(i))).Where(o => o.v.HasValue).ToList();
            if (opts.Count == 0)
            {
                return Math.Clamp((int)Math.Round(want), 0, Math.Max(0, Settings(r).Count - 1));
            }

            var enough = opts.Where(o => o.v >= want - 0.5).ToList();
            return enough.Count > 0 ? enough.OrderBy(o => o.v).First().i : opts.OrderByDescending(o => o.v).First().i;
        }

        private sealed record Change(int Index, string Name, int From, int To, string FromText, string ToText)
        {
            public override string ToString() => $"{Name} {From} « {FromText} » → {To} « {ToText} »";
        }

        private static List<Change> Changes(JsonArray before, JsonArray after)
        {
            var list = new List<Change>();
            for (int i = 0; i < Math.Min(before.Count, after.Count); i++)
            {
                int from = Current(before[i]);
                int to = Current(after[i]);
                if (from != to)
                {
                    list.Add(new Change(i, Name(after[i]), from, to, OptionText(before[i], from), OptionText(after[i], to)));
                }
            }

            return list;
        }

        /// <summary>La même ligne dans le menu relu : même position et même nom, sinon le premier du même nom.</summary>
        private static JsonNode Same(JsonArray after, JsonArray before, int index, string name)
        {
            if (after.Count == before.Count && index < after.Count && Name(after[index]) == name)
            {
                return after[index];
            }

            return after.FirstOrDefault(r => Name(r) == name);
        }

        private static string Why(Exception ex) => ex switch
        {
            LmuException => ex.Message,
            HttpRequestException h when h.HttpRequestError == HttpRequestError.ConnectionError =>
                $"Le Mans Ultimate ne répond pas sur son API ({string.Join(" ni ", Hosts.Select(x => $"{x}:{Port}"))}, connexion refusée) : le jeu est-il lancé ?",
            HttpRequestException h => $"Le Mans Ultimate : échange impossible avec son API sur le port {Port} ({h.InnerException?.Message ?? h.Message}).",
            TaskCanceledException or TimeoutException => $"Le Mans Ultimate n'a pas répondu à temps sur le port {Port} (jeu en chargement ou occupé ?).",
            JsonException => "le menu des stands envoyé par Le Mans Ultimate n'est pas lisible (format changé ?)",
            _ => ex.Message,
        };

        private static async Task<JsonArray> ReadAsync(CancellationToken ct)
        {
            Reply r = await CallAsync(HttpMethod.Get, MenuPath, null, ct).ConfigureAwait(false);
            if (r.Status is < 200 or >= 300)
            {
                throw new LmuException($"Le Mans Ultimate a répondu {r.Status} au menu des stands (« {Cut(r.Body, 120)} ») : la voiture est-elle en session ?");
            }

            JsonNode node;
            try
            {
                node = JsonNode.Parse(r.Body);
            }
            catch (JsonException)
            {
                throw new LmuException($"Le menu des stands envoyé par Le Mans Ultimate n'est pas du JSON : « {Cut(r.Body, 160)} »");
            }

            if (node is not JsonArray menu)
            {
                throw new LmuException($"Le menu des stands envoyé par Le Mans Ultimate n'est pas une liste de lignes : « {Cut(r.Body, 160)} »");
            }

            if (menu.Count == 0)
            {
                throw new LmuException("Le menu des stands de Le Mans Ultimate est vide : la voiture n'est pas en session (menu principal, chargement ou rejeu ?).");
            }

            if (menu.All(row => row is not JsonObject o || Name(o).Length == 0 || o["settings"] is not JsonArray))
            {
                throw new LmuException($"Le menu des stands envoyé par Le Mans Ultimate n'a aucune ligne lisible (format changé ?) : « {Cut(r.Body, 160)} »");
            }

            return menu;
        }

        private static async Task PostAsync(JsonArray menu, CancellationToken ct)
        {
            // Le menu entier, en tableau nu, octets UTF-8 tels quels (pas d'échappement é ni +), Content-Type: application/json.
            Reply r = await CallAsync(HttpMethod.Post, LoadPath, menu.ToJsonString(AsSent), ct).ConfigureAwait(false);
            if (r.Status is < 200 or >= 300)
            {
                throw new LmuException($"Le Mans Ultimate a refusé le menu des stands (réponse {r.Status} : « {Cut(r.Body, 120)} »).");
            }
        }

        /// <summary>État du jeu, une seule lecture de chaque adresse (navigation, demande d'arrêt), pour le test. Jamais d'exception.</summary>
        private static async Task<string> StateAsync(CancellationToken ct)
        {
            var parts = new List<string>();
            foreach (var (path, keys, label) in new[]
            {
                (NavigationPath, new[] { "navigationState", "gameState", "gameSession", "gamePhase", "settingMode", "appBuild" }, "navigation"),
                (GameStatePath, new[] { "PitState", "gamePhase", "inControlOfVehicle", "inMonitor", "isReplayActive", "MultiStintState", "status", "gameState" }, "GetGameState"),
            })
            {
                try
                {
                    Reply r = await CallAsync(HttpMethod.Get, path, null, ct).ConfigureAwait(false);
                    if (r.Status is < 200 or >= 300)
                    {
                        parts.Add($"{label} : réponse {r.Status}");
                        continue;
                    }

                    JsonNode node = JsonNode.Parse(r.Body);
                    JsonObject o = node?["state"] as JsonObject ?? node as JsonObject;
                    var found = keys.Where(k => o?[k] is JsonValue).Select(k => $"{k}={Scalar(o[k])}").ToList();
                    parts.Add($"{label} : {(found.Count > 0 ? string.Join(", ", found) : Cut(r.Body, 120))}");
                }
                catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
                {
                    parts.Add($"{label} : {(ex is LmuException ? ex.Message : ex.GetType().Name)}");
                }
            }

            return parts.Count == 0 ? null : "État du jeu : " + string.Join(" ; ", parts) + ".";
        }

        private static string Scalar(JsonNode v) => v is JsonValue j && j.TryGetValue(out string s) ? s : v?.ToJsonString() ?? "null";

        private sealed record Reply(int Status, string Body, string Url, long Ms);

        /// <summary>
        /// Un échange avec LMU. Adresse suivante seulement si la connexion n'a pas pu s'établir (rien n'est arrivé au jeu,
        /// donc sans risque même pour un envoi). Chaque échange est journalisé (tronqué) et gardé en entier dans le fichier.
        /// </summary>
        private static async Task<Reply> CallAsync(HttpMethod method, string path, string body, CancellationToken ct)
        {
            string first = _host;
            IEnumerable<string> order = first != null && Hosts.Contains(first) ? Hosts.Where(h => h == first).Concat(Hosts.Where(h => h != first)) : Hosts;
            Exception last = null;
            foreach (string host in order)
            {
                string url = $"http://{host}:{Port}{path}";
                var watch = Stopwatch.StartNew();
                try
                {
                    using var request = new HttpRequestMessage(method, url);
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    if (body != null)
                    {
                        request.Content = new ByteArrayContent(Encoding.UTF8.GetBytes(body));
                        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    }

                    using HttpResponseMessage response = await Http.SendAsync(request, ct).ConfigureAwait(false);
                    byte[] bytes = await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
                    var reply = new Reply((int)response.StatusCode, Encoding.UTF8.GetString(bytes), url, watch.ElapsedMilliseconds);
                    _host = host;
                    Trace(method, reply, body, response.Content.Headers.ContentType?.ToString());
                    return reply;
                }
                catch (HttpRequestException ex) when (!ct.IsCancellationRequested && (ex.HttpRequestError == HttpRequestError.ConnectionError || body == null))
                {
                    // Connexion impossible (ou lecture en échec) : l'adresse suivante. Un délai dépassé n'est pas réessayé (jeu occupé).
                    last = ex;
                    Write($"LMU {method} {url} : échec en {watch.ElapsedMilliseconds} ms ({ex.InnerException?.Message ?? ex.Message}).");
                    Dump($"{method} {url} : échec en {watch.ElapsedMilliseconds} ms : {ex}");
                }
                catch (Exception ex) when (!ct.IsCancellationRequested)
                {
                    string why = ex is TaskCanceledException ? $"pas de réponse en {Http.Timeout.TotalSeconds:0} s" : ex.InnerException?.Message ?? ex.Message;
                    Write($"LMU {method} {url} : échec en {watch.ElapsedMilliseconds} ms ({why}).");
                    Dump($"{method} {url} : échec en {watch.ElapsedMilliseconds} ms : {ex}");
                    throw;
                }
            }

            throw last ?? new LmuException("aucune adresse de Le Mans Ultimate à essayer");
        }

        private static void Trace(HttpMethod method, Reply r, string sent, string contentType)
        {
            string head = $"LMU {method} {r.Url} → {r.Status} en {r.Ms} ms ({contentType ?? "sans type"}, {r.Body.Length} car.)";
            Write(sent == null
                ? $"{head} : {Cut(r.Body, 1500)}"
                : $"{head}. Envoyé ({sent.Length} car.) : {Cut(sent, 700)} — réponse : « {Cut(r.Body, 300)} »");
            Dump(head + Environment.NewLine + (sent != null ? "ENVOYÉ :" + Environment.NewLine + sent + Environment.NewLine : string.Empty) + "REÇU :" + Environment.NewLine + r.Body);
        }

        /// <summary>Lignes de journal de l'échange en cours (demande, menu lu, changements, relecture), renvoyées à SecondMonitor.</summary>
        private static readonly List<string> Exchange = new();

        /// <summary>Journal du dernier échange (tronqué à 6 000 caractères) : SecondMonitor le garde sur le Mac pour le diagnostic.</summary>
        public static string LastExchange
        {
            get
            {
                lock (Exchange)
                {
                    string all = string.Join(Environment.NewLine, Exchange);
                    return all.Length > 6000 ? all[..6000] + " …" : all;
                }
            }
        }

        private static void Begin(string what, string extra = null)
        {
            lock (Exchange)
            {
                Exchange.Clear();
            }

            Write($"Menu des stands LMU : {what}{(extra != null ? " — " + extra : string.Empty)}.");
            Dump($"===== {DateTime.Now:dd/MM/yyyy HH:mm:ss} {what} =====" + (extra != null ? Environment.NewLine + extra : string.Empty));
        }

        private static string DumpHint() => DumpFile != null ? Environment.NewLine + "Réponses complètes de LMU : " + DumpFile : string.Empty;

        private static void Write(string line)
        {
            lock (Exchange)
            {
                if (Exchange.Count < 200)
                {
                    Exchange.Add(line);
                }
            }

            try
            {
                Log?.Invoke(line);
            }
            catch (Exception)
            {
                // Journal indisponible : sans importance.
            }
        }

        /// <summary>Ajoute au fichier de diagnostic (2 Mo au plus ; l'ancien est gardé à côté).</summary>
        private static void Dump(string text)
        {
            string file = DumpFile;
            if (file == null)
            {
                return;
            }

            try
            {
                lock (DumpLock)
                {
                    var info = new FileInfo(file);
                    if (info.Exists && info.Length > 2 * 1024 * 1024)
                    {
                        File.Move(file, Path.ChangeExtension(file, ".ancien.txt"), overwrite: true);
                    }

                    File.AppendAllText(file, text + Environment.NewLine + Environment.NewLine, new UTF8Encoding(false));
                }
            }
            catch (Exception)
            {
                // Diagnostic seulement : jamais bloquant.
            }
        }

        /// <summary>Texte sur une ligne, tronqué (« … (+N car.) »).</summary>
        private static string Cut(string text, int max)
        {
            string t = Regex.Replace(text ?? string.Empty, @"\s*[\r\n]+\s*", " ").Trim();
            return t.Length <= max ? t : t[..max] + $"… (+{t.Length - max} car.)";
        }

        /// <summary>Nombre à la française (« 47 », « 62,3 ») ; le collecteur tourne en culture invariante.</summary>
        private static string Format(double v) => v.ToString(v == Math.Floor(v) ? "0" : "0.#", CultureInfo.InvariantCulture).Replace('.', ',');

        private static bool Has(JsonNode o, string key) => o is JsonObject jo && jo.ContainsKey(key);

        /// <summary>Nom réduit pour comparer : « FL TIRE: » → « FLTIRE », « VIRTUAL ENERGY: » → « VIRTUALENERGY ».</summary>
        private static string Key(string name) => Regex.Replace(name ?? string.Empty, "[^A-Za-z]", string.Empty).ToUpperInvariant();

        private static string Short(string name) => name.TrimEnd(':', ' ').ToLowerInvariant();

        private static JsonNode Row(JsonArray menu, string key) => menu?.FirstOrDefault(r => Key(Name(r)) == key);

        private static string Name(JsonNode r) => r is JsonObject && r["name"] is JsonValue v && v.TryGetValue(out string s) ? s.Trim() : string.Empty;

        /// <summary>Index choisi (entier ; un nombre décimal est arrondi), ou -1.</summary>
        private static int Current(JsonNode r)
        {
            if (r is not JsonObject || r["currentSetting"] is not JsonValue v)
            {
                return -1;
            }

            if (v.TryGetValue(out int i))
            {
                return i;
            }

            return v.TryGetValue(out double d) ? (int)Math.Round(d) : -1;
        }

        private static string OptionText(JsonNode r, int i)
        {
            List<JsonNode> opts = Settings(r);
            return i >= 0 && i < opts.Count ? Text(opts[i]) : $"choix {i}";
        }

        private static List<JsonNode> Settings(JsonNode r) => (r is JsonObject ? r["settings"] as JsonArray : null)?.ToList() ?? new List<JsonNode>();

        private static string Text(JsonNode s) => Str(s, "text")?.Trim() ?? string.Empty;

        private static string Str(JsonNode s, string key) => s is JsonObject && s[key] is JsonValue v && v.TryGetValue(out string x) ? x : null;

        private static bool Bool(JsonNode s, string key) => s is JsonObject && s[key] is JsonValue v && v.TryGetValue(out bool b) && b;

        private static void Set(JsonNode r, int k)
        {
            if (r != null && k >= 0 && k < Settings(r).Count)
            {
                r["currentSetting"] = k;
            }
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

        /// <summary>Ce que le collecteur sait de la voiture par la télémétrie du jeu : gommes montées (avant, arrière) et carburant à bord.</summary>
        public sealed record CarNow(string FrontCompound, string RearCompound, double? FuelLitres)
        {
            public override string ToString() =>
                $"gomme {FrontCompound ?? "?"}{(RearCompound != null && !string.Equals(RearCompound, FrontCompound, StringComparison.OrdinalIgnoreCase) ? " / " + RearCompound : string.Empty)}, " +
                (FuelLitres is double l ? $"{Format(Math.Round(l, 1))} L à bord" : "carburant inconnu");
        }

        /// <summary>Résultat du test : phrase courte (radio) et détail (fenêtre du collecteur).</summary>
        public sealed record Inspection(bool Ok, string Text, string Detail);

        /// <summary>Erreur expliquée en français (réponse inattendue de LMU).</summary>
        private sealed class LmuException : Exception
        {
            public LmuException(string message)
                : base(message)
            {
            }
        }
    }
}
