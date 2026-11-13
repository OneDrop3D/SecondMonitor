namespace SecondMonitor.Hub.Core.Ai
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Text.RegularExpressions;
    using SecondMonitor.Hub.Core.Race;
    using static SecondMonitor.Hub.Core.Ai.RadioFormat;

    /// <summary>
    /// Carnet de session : toutes les radios (avec leur catégorie), les notes et retours du pilote, les réglages proposés
    /// (acceptés / refusés), les tours, le résultat et l'état des pneus. Enregistré automatiquement dans &lt;dossier de données&gt;/carnets.
    /// </summary>
    public sealed class CarnetSession
    {
        public string Id { get; set; }

        public string Game { get; set; }

        public string Track { get; set; }

        public string SessionType { get; set; }

        public string Car { get; set; }

        public string ClassName { get; set; }

        public DateTime StartedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public DateTime? EndedAt { get; set; }

        /// <summary>Course terminée (drapeau à damier franchi par le joueur).</summary>
        public bool Finished { get; set; }

        public List<CarnetRadio> Radios { get; set; } = new();

        public List<CarnetNote> Notes { get; set; } = new();

        public List<ProposalDecision> Decisions { get; set; } = new();

        public List<CarnetLap> Laps { get; set; } = new();

        public CarnetResult Result { get; set; }

        public CarnetTyres Tyres { get; set; }

        public CarnetRecap Recap { get; set; }

        public bool HasContent => Radios.Count > 0 || Laps.Count > 0 || Notes.Count > 0;
    }

    public sealed class CarnetRadio
    {
        public DateTime At { get; set; }

        public int? Lap { get; set; }

        public string From { get; set; }

        public string Question { get; set; }

        public string Answer { get; set; }

        public string Kind { get; set; }

        public string Category { get; set; }

        /// <summary>« règles », « modèle » ou « défaut ».</summary>
        public string CategorySource { get; set; }

        public bool Proactive { get; set; }
    }

    public sealed class CarnetNote
    {
        public int Id { get; set; }

        public DateTime At { get; set; }

        public int? Lap { get; set; }

        public string Source { get; set; }

        public string Topic { get; set; }

        public string Text { get; set; }

        public string Symptom { get; set; }

        public string Tyres { get; set; }
    }

    public sealed class CarnetLap
    {
        public int Lap { get; set; }

        public double? Time { get; set; }

        public bool Pit { get; set; }

        public bool Valid { get; set; }
    }

    public sealed class CarnetResult
    {
        public int Position { get; set; }

        public int ClassPosition { get; set; }

        public int ClassCount { get; set; }

        public int PositionsGained { get; set; }

        public int Laps { get; set; }

        public double? BestLap { get; set; }

        public int? BestLapNumber { get; set; }

        public double? ClassBestLap { get; set; }

        public int? ClassBestNumber { get; set; }

        public double? GapToClassLeader { get; set; }

        public int GapToClassLeaderLaps { get; set; }

        public int PitStops { get; set; }

        public List<int> PitLaps { get; set; } = new();

        public bool IsMultiClass { get; set; }
    }

    public sealed class CarnetTyres
    {
        public string Compound { get; set; }

        public int StintLaps { get; set; }

        public double[] Wear { get; set; }

        public double[] Temps { get; set; }

        public string Alert { get; set; }
    }

    public sealed class CarnetRecap
    {
        public DateTime GeneratedAt { get; set; } = DateTime.Now;

        /// <summary>Récapitulatif calculé sans IA (toujours présent).</summary>
        public List<RecapSection> Sections { get; set; } = new();

        /// <summary>Résumé rédigé par l'IA (null si Ollama n'est pas disponible).</summary>
        public string Ai { get; set; }

        public string Model { get; set; }

        public string Note { get; set; }
    }

    public sealed class RecapSection
    {
        public RecapSection()
        {
        }

        public RecapSection(string title, string text)
        {
            Title = title;
            Text = text;
        }

        public string Title { get; set; }

        public string Text { get; set; }
    }

    /// <summary>Résumé d'un carnet pour la liste des sessions.</summary>
    public sealed class CarnetSummary
    {
        public string Id { get; set; }

        public string Game { get; set; }

        public string Track { get; set; }

        public string SessionType { get; set; }

        public string Car { get; set; }

        public DateTime StartedAt { get; set; }

        public DateTime? EndedAt { get; set; }

        public int Radios { get; set; }

        public int Laps { get; set; }

        public string Result { get; set; }

        public bool HasRecap { get; set; }

        public bool Current { get; set; }
    }

    /// <summary>Remplit le carnet à partir de l'état de course, et calcule le récapitulatif sans IA.</summary>
    public static class CarnetBuilder
    {
        public static string SessionKey(RaceSnapshot snap) => $"{snap?.Source}|{snap?.Track?.Name}|{snap?.SessionType}";

        public static CarnetSession Create(RaceSnapshot snap, DateTime now)
        {
            DriverRow me = snap?.Drivers?.FirstOrDefault(d => d.IsPlayer);
            string slug = Regex.Replace(RadioCommands.Normalize($"{snap?.Track?.Name} {snap?.SessionType}"), @"\s+", "-");
            return new CarnetSession
            {
                Id = $"{now:yyyyMMdd-HHmmss}-{(slug.Length > 40 ? slug[..40] : slug)}".TrimEnd('-'),
                Game = GameName(snap?.Source),
                Track = snap?.Track?.Name,
                SessionType = snap?.SessionType,
                Car = me?.Car,
                ClassName = me?.ClassName,
                StartedAt = now,
                UpdatedAt = now,
            };
        }

        public static string GameName(string source) => source switch
        {
            null or "" => "jeu inconnu",
            "Demo" => "démo",
            "LM Ultimate" => "Le Mans Ultimate",
            "ACC" => "Assetto Corsa Competizione",
            _ => source,
        };

        public static string SessionName(string type) => type switch
        {
            "Race" => "Course",
            "Qualification" => "Qualification",
            "Practice" => "Essais",
            "WarmUp" => "Warm-up",
            _ => "Session",
        };

        /// <summary>Met à jour tours, résultat, pneus, notes et décisions (appelé régulièrement pendant la session).</summary>
        public static void Update(CarnetSession c, RaceSnapshot snap, IEnumerable<LapDto> laps, RaceMemory memory)
        {
            DriverRow me = snap?.Drivers?.FirstOrDefault(d => d.IsPlayer);
            if (me != null)
            {
                c.Car ??= me.Car;
                c.ClassName ??= me.ClassName;
                var cls = snap.Drivers.Where(d => d.ClassId == me.ClassId).ToList();
                DriverRow classBest = cls.Where(d => d.BestLap > 0).OrderBy(d => d.BestLap).FirstOrDefault();
                c.Result = new CarnetResult
                {
                    Position = me.Position,
                    ClassPosition = me.ClassPosition,
                    ClassCount = cls.Count,
                    PositionsGained = me.PositionsGained,
                    Laps = me.Laps,
                    BestLap = me.BestLap,
                    BestLapNumber = me.BestLapNumber,
                    ClassBestLap = classBest?.BestLap,
                    ClassBestNumber = classBest?.Number,
                    GapToClassLeader = me.GapToClassLeader,
                    GapToClassLeaderLaps = me.GapToClassLeaderLaps,
                    PitStops = me.PitStops,
                    PitLaps = me.Stints.Where(s => s.EndLap.HasValue).Select(s => s.EndLap.Value).ToList(),
                    IsMultiClass = snap.IsMultiClass,
                };
                c.Finished |= snap.SessionType == "Race" && me.FinishStatus == "Finished";
            }

            if (laps != null)
            {
                var list = laps.Where(l => l.Time is > 0).Select(l => new CarnetLap { Lap = l.Lap, Time = l.Time.HasValue ? Math.Round(l.Time.Value, 3) : null, Pit = l.Pit, Valid = l.Valid }).ToList();
                if (list.Count >= c.Laps.Count)
                {
                    c.Laps = list;
                }
            }

            PlayerStrategy p = snap?.Player;
            if (p?.Wheels?.Count == 4)
            {
                c.Tyres = new CarnetTyres
                {
                    Compound = p.Wheels[0].Compound ?? me?.Tyres?.Compound,
                    StintLaps = p.StintLaps,
                    Wear = p.Wheels.Select(w => Math.Round(w.WearPct, 1)).ToArray(),
                    Temps = p.Wheels.All(w => w.TempMiddle.HasValue) ? p.Wheels.Select(w => Math.Round(w.TempMiddle.Value, 0)).ToArray() : null,
                    Alert = p.Alerts?.FirstOrDefault(),
                };
            }

            if (memory != null)
            {
                foreach (MemoryEntry e in memory.Entries)
                {
                    if (!c.Notes.Any(n => n.Id == e.Id && n.At == e.At))
                    {
                        c.Notes.Add(new CarnetNote { Id = e.Id, At = e.At, Lap = e.Lap, Source = e.Source, Topic = e.Topic, Text = e.Text, Symptom = e.Symptom, Tyres = e.Tyres });
                    }
                }

                foreach (ProposalDecision d in memory.Thread.Decisions)
                {
                    if (!c.Decisions.Any(x => x.At == d.At && x.Key == d.Key && x.Decision == d.Decision))
                    {
                        c.Decisions.Add(d);
                    }
                }
            }

            c.UpdatedAt = DateTime.Now;
        }

        private static readonly Dictionary<string, string> SymptomNames = new()
        {
            ["understeer"] = "sous-virage",
            ["oversteer"] = "survirage",
            ["lock-front"] = "blocage de l'avant",
            ["lock-inside"] = "blocage de la roue intérieure",
            ["lock-rear"] = "blocage de l'arrière",
            ["brake-unstable"] = "instabilité au freinage",
            ["wheelspin"] = "patinage en sortie",
            ["bouncing"] = "rebonds",
            ["brakes-hot"] = "freins trop chauds",
            ["tyres-hot"] = "pneus trop chauds",
            ["tyres-cold"] = "pneus froids",
            ["rain"] = "manque de grip sous la pluie",
            ["no-grip"] = "manque de grip",
        };

        /// <summary>Récapitulatif calculé sans IA : déroulé, radios, pneus, freinage, comportement, setup, conclusion.</summary>
        public static CarnetRecap Recap(CarnetSession c)
        {
            var r = new CarnetRecap();
            CarnetResult res = c.Result;
            bool race = c.SessionType == "Race";
            var clean = c.Laps.Where(l => !l.Pit && l.Time > 0 && !(race && l.Lap <= 1)).ToList();
            double? avg = clean.Count >= 2 ? clean.Average(l => l.Time.Value) : null;
            double? sd = clean.Count >= 3 ? Math.Sqrt(clean.Sum(l => Math.Pow(l.Time.Value - avg.Value, 2)) / clean.Count) : null;

            // ── Déroulé ──
            var story = new StringBuilder();
            story.Append($"{SessionName(c.SessionType)} à {c.Track ?? "circuit inconnu"} ({c.Game}{(string.IsNullOrEmpty(c.Car) ? string.Empty : ", " + c.Car)})");
            if (res != null)
            {
                story.Append($" : {res.Laps} tours");
                int pos = res.IsMultiClass ? res.ClassPosition : res.Position;
                string where = res.IsMultiClass ? $"P{pos} sur {res.ClassCount} dans la catégorie" : $"P{pos}";
                if (race)
                {
                    story.Append($", {(c.Finished ? "arrivée" : "dernière position connue")} {where}");
                    if (res.PositionsGained != 0)
                    {
                        story.Append(res.PositionsGained > 0 ? $" ({res.PositionsGained} place(s) gagnée(s))" : $" ({-res.PositionsGained} place(s) perdue(s))");
                    }
                }
                else
                {
                    story.Append($", {where} au classement des temps");
                }

                story.Append('.');
                if (res.BestLap is > 0)
                {
                    story.Append($" Meilleur tour {Lap(res.BestLap)}{(res.BestLapNumber.HasValue ? $" (tour {res.BestLapNumber})" : string.Empty)}");
                    story.Append(res.ClassBestLap is double cb && cb < res.BestLap - 0.0005 ? $", à {Sec(res.BestLap.Value - cb)} du meilleur de la catégorie (numéro {res.ClassBestNumber})." : ", meilleur temps de la catégorie.");
                }

                if (race && res.PitStops > 0)
                {
                    story.Append($" {res.PitStops} arrêt{(res.PitStops > 1 ? "s" : string.Empty)}{(res.PitLaps.Count > 0 ? $" (tour {string.Join(", ", res.PitLaps)})" : string.Empty)}.");
                }

                if (race && pos > 1)
                {
                    story.Append(res.GapToClassLeaderLaps > 0 ? $" À {res.GapToClassLeaderLaps} tour(s) du premier." : res.GapToClassLeader is double g && g > 0 ? $" À {Sec(g)} du premier." : string.Empty);
                }
            }
            else
            {
                story.Append('.');
            }

            if (avg is double a)
            {
                story.Append($" Rythme moyen {Lap(a)} sur {clean.Count} tours propres");
                story.Append(sd is double s ? $", régularité plus ou moins {Sec(s)}." : ".");
            }

            r.Sections.Add(new RecapSection("Déroulé", story.ToString()));

            // ── Radios ──
            if (c.Radios.Count > 0)
            {
                var byCat = c.Radios.GroupBy(x => x.Category ?? ExchangeCategorizer.RaceInfo).ToDictionary(g => g.Key, g => g.Count());
                string counts = string.Join(", ", ExchangeCategorizer.All.Where(byCat.ContainsKey).Select(k => $"{k} {byCat[k]}"));
                int proactive = c.Radios.Count(x => x.Proactive);
                r.Sections.Add(new RecapSection("Radios", $"{c.Radios.Count} échange{(c.Radios.Count > 1 ? "s" : string.Empty)} ({counts}){(proactive > 0 ? $", dont {proactive} intervention(s) spontanée(s) de l'ingénieur" : string.Empty)}."));
            }

            // ── Pneus ──
            if (c.Tyres?.Wear != null)
            {
                int worst = Array.IndexOf(c.Tyres.Wear, c.Tyres.Wear.Max());
                string[] wheels = { "avant gauche", "avant droit", "arrière gauche", "arrière droit" };
                string temps = c.Tyres.Temps == null ? string.Empty : $" Températures en fin de session : {string.Join(", ", c.Tyres.Temps.Select((t, i) => $"{wheels[i]} {N(t, 0)}°"))}.";
                int tyreNotes = c.Notes.Count(n => n.Symptom is "tyres-hot" or "tyres-cold" or "no-grip" || (n.Text ?? string.Empty).Contains("pneu", StringComparison.OrdinalIgnoreCase));
                r.Sections.Add(new RecapSection("Pneus", $"{(string.IsNullOrEmpty(c.Tyres.Compound) ? "Pneus" : c.Tyres.Compound)}, {c.Tyres.StintLaps} tours sur le dernier relais, usure maximale {N(c.Tyres.Wear[worst], 0)} % ({wheels[worst]}).{temps}{(tyreNotes > 0 ? $" Le pilote en a parlé {tyreNotes} fois." : string.Empty)}{(c.Tyres.Alert != null ? " " + c.Tyres.Alert : string.Empty)}"));
            }

            var feedback = c.Notes.Where(n => n.Symptom != null && n.Topic == "retour").ToList(); // retours du pilote (pas ses réponses aux propositions)

            // ── Freinage ──
            var brakes = feedback.Where(n => n.Symptom is "lock-front" or "lock-inside" or "lock-rear" or "brake-unstable" or "brakes-hot").ToList();
            var brakeDecisions = c.Decisions.Where(d => d.Symptom is "lock-front" or "lock-inside" or "lock-rear" or "brake-unstable" or "brakes-hot").ToList();
            r.Sections.Add(new RecapSection("Freinage", brakes.Count == 0
                ? "Aucun problème de freinage signalé par le pilote."
                : string.Join(" ; ", brakes.GroupBy(n => n.Symptom).Select(g => $"{Cap(SymptomNames[g.Key])} signalé {g.Count()} fois ({Laps(g)})")) + "." + Decided(brakeDecisions)));

            // ── Comportement (selon l'état des pneus) ──
            var balance = feedback.Where(n => n.Symptom is "understeer" or "oversteer" or "wheelspin" or "bouncing" or "rain" or "no-grip").ToList();
            if (balance.Count == 0)
            {
                r.Sections.Add(new RecapSection("Comportement", "Pas de remarque du pilote sur l'équilibre de la voiture."));
            }
            else
            {
                var parts = balance.GroupBy(n => (n.Symptom, n.Tyres)).Select(g =>
                {
                    string what = g.Key.Symptom switch { "understeer" => "sous-vireuse", "oversteer" => "survireuse", _ => SymptomNames[g.Key.Symptom] };
                    string tyres = g.Key.Tyres switch { "neufs" => " en pneus neufs", "usés" => " en pneus usés", "mi-usure" => " à mi-relais", _ => string.Empty };
                    return $"{what}{tyres} ({Laps(g)})";
                });
                r.Sections.Add(new RecapSection("Comportement", Cap(string.Join(", ", parts)) + "." + Decided(c.Decisions.Where(d => d.Symptom is "understeer" or "oversteer" or "wheelspin" or "bouncing").ToList())));
            }

            // ── Setup ──
            var setup = new List<string>();
            setup.AddRange(c.Decisions.Where(d => d.Kind == "setup" && d.Decision == "accepté").Select(d => d.Text));
            foreach (var g in feedback.GroupBy(n => n.Symptom))
            {
                bool solved = c.Decisions.Any(d => d.Symptom == g.Key && d.Decision == "efficace");
                string idea = solved ? null : SetupAdvisor.SetupIdea(g.Key, c.Decisions.Where(d => d.Decision == "refusé").Select(d => d.Key));
                if (idea != null && !setup.Any(x => x.StartsWith(idea, StringComparison.Ordinal)))
                {
                    setup.Add($"{idea} (contre le {SymptomNames[g.Key]})");
                }
            }

            r.Sections.Add(new RecapSection("Setup à essayer", setup.Count == 0 ? "Rien de particulier à changer d'après les retours du pilote." : Cap(string.Join(" ; ", setup.Distinct())) + "."));

            // ── Conclusion ──
            var conclusion = new List<string>();
            if (res != null && race)
            {
                int pos = res.IsMultiClass ? res.ClassPosition : res.Position;
                conclusion.Add(pos == 1 ? "Victoire" : pos <= 3 ? $"Podium (P{pos})" : $"P{pos}" + (res.PositionsGained > 0 ? $", {res.PositionsGained} place(s) gagnée(s)" : res.PositionsGained < 0 ? $", {-res.PositionsGained} place(s) perdue(s)" : string.Empty));
            }

            if (sd is double s2)
            {
                conclusion.Add(s2 < 0.3 ? "rythme très régulier" : s2 < 0.6 ? "rythme régulier" : "rythme irrégulier, à travailler");
            }

            var work = feedback.GroupBy(n => n.Symptom).OrderByDescending(g => g.Count()).Take(2).Select(g => SymptomNames[g.Key]).ToList();
            if (work.Count > 0)
            {
                conclusion.Add("à travailler : " + string.Join(" et ", work));
            }

            r.Sections.Add(new RecapSection("Conclusion", conclusion.Count == 0 ? "Session sans incident notable." : Cap(string.Join(" ; ", conclusion)) + "."));
            return r;
        }

        /// <summary>Texte envoyé au modèle pour rédiger le récapitulatif.</summary>
        public static string AiPrompt(CarnetSession c, CarnetRecap computed)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Tu es l'ingénieur de course. Rédige en français le récapitulatif de la session pour le pilote, en 6 à 10 phrases simples, sans liste ni markdown :");
            sb.AppendLine("le déroulé, le résumé des radios, la conclusion, l'état des pneus, le ressenti du pilote sur le freinage, le comportement de la voiture selon l'état des pneus (neufs ou usés), et des notes pour améliorer le setup.");
            sb.AppendLine("Utilise uniquement les informations ci-dessous, sans inventer de chiffre.");
            sb.AppendLine();
            sb.AppendLine("RÉCAPITULATIF CALCULÉ :");
            foreach (RecapSection s in computed.Sections)
            {
                sb.AppendLine($"- {s.Title} : {s.Text}");
            }

            sb.AppendLine();
            sb.AppendLine("RADIOS (tour, catégorie, échange) :");
            foreach (CarnetRadio x in c.Radios.TakeLast(60))
            {
                sb.AppendLine($"- {(x.Lap.HasValue ? $"T{x.Lap}" : x.At.ToString("HH:mm"))} [{x.Category}] {(string.IsNullOrEmpty(x.Question) ? string.Empty : $"{x.From} : « {x.Question} » → ")}ingénieur : « {x.Answer} »");
            }

            var notes = c.Notes.Where(n => n.Source != "IA").TakeLast(30).ToList();
            if (notes.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("NOTES ET RETOURS DU PILOTE :");
                foreach (CarnetNote n in notes)
                {
                    sb.AppendLine($"- {(n.Lap.HasValue ? $"T{n.Lap}" : n.At.ToString("HH:mm"))} {n.Source} : {n.Text}{(n.Tyres != null ? $" (pneus {n.Tyres})" : string.Empty)}");
                }
            }

            return sb.ToString();
        }

        private static string Laps(IEnumerable<CarnetNote> notes)
        {
            var laps = notes.Where(n => n.Lap.HasValue).Select(n => n.Lap.Value).Distinct().OrderBy(x => x).ToList();
            return laps.Count == 0 ? "tour inconnu" : (laps.Count == 1 ? "tour " : "tours ") + string.Join(", ", laps);
        }

        private static string Decided(List<ProposalDecision> decisions)
        {
            if (decisions.Count == 0)
            {
                return string.Empty;
            }

            return " Réglages : " + string.Join(" ; ", decisions.GroupBy(d => d.Key).Select(g => g.Last()).Select(d => $"{d.Text} ({d.Decision})")) + ".";
        }
    }

    /// <summary>Lecture et écriture des carnets (un fichier JSON par session).</summary>
    public sealed class CarnetStore
    {
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };
        private readonly object _lock = new();

        public CarnetStore(string directory)
        {
            Directory = directory;
        }

        public string Directory { get; }

        public void Save(CarnetSession c)
        {
            if (c == null || string.IsNullOrEmpty(c.Id))
            {
                return;
            }

            lock (_lock)
            {
                System.IO.Directory.CreateDirectory(Directory);
                string path = PathOf(c.Id);
                string tmp = path + ".tmp";
                File.WriteAllText(tmp, JsonSerializer.Serialize(c, Json));
                File.Move(tmp, path, overwrite: true);
            }
        }

        public CarnetSession Load(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || id.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || id.Contains(".."))
            {
                return null;
            }

            lock (_lock)
            {
                string path = PathOf(id);
                if (!File.Exists(path))
                {
                    return null;
                }

                try
                {
                    return JsonSerializer.Deserialize<CarnetSession>(File.ReadAllText(path), Json);
                }
                catch (Exception)
                {
                    return null; // fichier abîmé : ignoré
                }
            }
        }

        public bool Delete(string id)
        {
            CarnetSession c = Load(id);
            if (c == null)
            {
                return false;
            }

            lock (_lock)
            {
                File.Delete(PathOf(id));
                return true;
            }
        }

        public List<CarnetSummary> List()
        {
            if (!System.IO.Directory.Exists(Directory))
            {
                return new List<CarnetSummary>();
            }

            var list = new List<CarnetSummary>();
            foreach (string file in System.IO.Directory.GetFiles(Directory, "*.json"))
            {
                CarnetSession c = Load(Path.GetFileNameWithoutExtension(file));
                if (c != null)
                {
                    list.Add(Summarize(c));
                }
            }

            return list.OrderByDescending(s => s.StartedAt).ToList();
        }

        public static CarnetSummary Summarize(CarnetSession c)
        {
            CarnetResult r = c.Result;
            string result = r == null ? null
                : c.SessionType == "Race" ? $"P{(r.IsMultiClass ? r.ClassPosition : r.Position)}{(c.Finished ? string.Empty : " (en cours)")}"
                : r.BestLap is > 0 ? Lap(r.BestLap) : null;
            return new CarnetSummary
            {
                Id = c.Id,
                Game = c.Game,
                Track = c.Track,
                SessionType = c.SessionType,
                Car = c.Car,
                StartedAt = c.StartedAt,
                EndedAt = c.EndedAt,
                Radios = c.Radios.Count,
                Laps = r?.Laps ?? c.Laps.Count,
                Result = result,
                HasRecap = c.Recap != null,
            };
        }

        private string PathOf(string id) => Path.Combine(Directory, id + ".json");
    }
}
