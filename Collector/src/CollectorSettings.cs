namespace SecondMonitor.Collector
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Réglages du collecteur (« collecteur.json » dans le dossier de données de l'utilisateur, créé au premier lancement).
    /// Tout a une valeur par défaut : il n'y a normalement rien à régler.
    /// </summary>
    public sealed class CollectorSettings
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        /// <summary>Port réseau attendu par le hub (identique à SecondMonitor).</summary>
        public int Port { get; set; } = 52642;

        /// <summary>
        /// « econome » : le moins de ressources (10 lectures/s, adversaires 1×/s) ;
        /// « equilibre » (défaut) : 20 lectures/s, joueur 10×/s, adversaires 4×/s ;
        /// « precis » : 30 lectures/s, adversaires 10×/s (Vmax et trajectoires les plus fines).
        /// </summary>
        public string Qualite { get; set; } = "equilibre";

        /// <summary>Jeux à ne pas surveiller (noms affichés au démarrage), pour économiser encore un peu.</summary>
        public List<string> JeuxDesactives { get; set; } = new();

        /// <summary>Arrêter de lire le jeu quand aucun hub n'est connecté depuis ce nombre de secondes (0 = jamais).</summary>
        public int PauseSansHubSecondes { get; set; } = 20;

        /// <summary>Touche ou bouton du volant de ce PC qui déclenche la radio de l'ingénieur (null = aucun).</summary>
        public BoutonRadio BoutonRadio { get; set; }

        /// <summary>Port de l'interface de SecondMonitor (pour lui transmettre les appuis radio).</summary>
        public int PortSecondMonitor { get; set; } = 8686;

        /// <summary>Radio de l'ingénieur sur ce PC : sa voix dans le casque du pilote, le micro de ce PC (fenêtre › « Radio de l'ingénieur »).</summary>
        public RadioIngenieur RadioIngenieur { get; set; } = new();

        /// <summary>Priorité basse : le jeu passe toujours avant le collecteur.</summary>
        public bool PrioriteBasse { get; set; } = true;

        /// <summary>Mode diagnostic : CPU, mémoire, débit réseau et lectures du jeu affichés dans la fenêtre (ou la console).</summary>
        public bool Diagnostic { get; set; }

        /// <summary>
        /// SecondMonitor peut régler le menu des stands de Le Mans Ultimate depuis ce PC (API locale du jeu, port 6397), quand l'ingénieur
        /// l'a activé et que le pilote le demande (« softs neufs », « la réparation »). Rien n'est envoyé au jeu sans demande.
        /// </summary>
        public bool MenuStandsLmu { get; set; } = true;

        /// <summary>
        /// SecondMonitor fait jouer sa musique Deezer sur ce PC (application Deezer de Windows), donc dans le casque du pilote :
        /// recherche et commandes restent sur l'ordinateur de SecondMonitor (réglage « Musique sur : le PC de jeu »).
        /// </summary>
        public bool MusiqueDeezer { get; set; }

        [JsonIgnore]
        public string FilePath { get; private set; }

        [JsonIgnore]
        public (int TickMs, int MinPackageMs, int PlayerMs, int OthersMs) Rates => Qualite?.ToLowerInvariant() switch
        {
            "econome" or "économe" => (100, 100, 200, 1000),
            "precis" or "précis" => (33, 33, 66, 100),
            _ => (50, 50, 100, 250),
        };

        /// <summary>
        /// Dossier des réglages et du journal : celui de l'utilisateur (l'application installée peut être en lecture seule).
        /// Une ancienne version « portable » qui avait son collecteur.json à côté du programme le garde.
        /// </summary>
        public static string DataDirectory(string programDirectory)
        {
            if (File.Exists(Path.Combine(programDirectory, "collecteur.json")))
            {
                return programDirectory;
            }

            string root = OperatingSystem.IsMacOS()
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support")
                : Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string dir = Path.Combine(root, "SecondMonitor Collecteur");
            try
            {
                Directory.CreateDirectory(dir);
                return dir;
            }
            catch (Exception)
            {
                return programDirectory;
            }
        }

        public void Save()
        {
            try
            {
                File.WriteAllText(FilePath, JsonSerializer.Serialize(this, Options));
            }
            catch (Exception)
            {
                // Dossier en lecture seule : le réglage vaut jusqu'à la fermeture.
            }
        }

        public static CollectorSettings LoadOrCreate(string directory)
        {
            string path = Path.Combine(directory, "collecteur.json");
            CollectorSettings settings = null;
            try
            {
                if (File.Exists(path))
                {
                    settings = JsonSerializer.Deserialize<CollectorSettings>(File.ReadAllText(path), Options);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ! collecteur.json illisible ({ex.Message}) : valeurs par défaut utilisées.");
            }

            settings ??= new CollectorSettings();
            settings.RadioIngenieur ??= new RadioIngenieur();
            settings.FilePath = path;
            if (!File.Exists(path))
            {
                try
                {
                    File.WriteAllText(path, JsonSerializer.Serialize(settings, Options));
                }
                catch (Exception)
                {
                    // Dossier en lecture seule : on garde les valeurs par défaut en mémoire.
                }
            }

            return settings;
        }
    }

    /// <summary>Radio de l'ingénieur sur ce PC (collecteur.json). Les appareils sont ceux que la fenêtre du collecteur liste.</summary>
    public sealed class RadioIngenieur
    {
        /// <summary>Entendre l'ingénieur sur ce PC (sinon, sa voix reste sur les écrans de SecondMonitor).</summary>
        public bool Entendre { get; set; }

        /// <summary>Sortie audio choisie (identifiant donné par la vue web ; vide = sortie par défaut du système).</summary>
        public string Sortie { get; set; } = string.Empty;

        /// <summary>Nom de la sortie, pour la retrouver si son identifiant change.</summary>
        public string SortieNom { get; set; } = string.Empty;

        /// <summary>Parler avec le micro de ce PC : le bouton radio enregistre ici au lieu de l'écran de SecondMonitor.</summary>
        public bool Parler { get; set; }

        /// <summary>Micro choisi (vide = micro par défaut du système).</summary>
        public string Micro { get; set; } = string.Empty;

        /// <summary>Nom du micro, pour le retrouver si son identifiant change.</summary>
        public string MicroNom { get; set; } = string.Empty;
    }
}
