namespace SecondMonitor.Hub;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>Réglages persistants du hub (~/Library/Application Support/SecondMonitorHub/settings.json sur Mac).</summary>
public sealed class HubSettings
{
    public int WebPort { get; set; } = 8686;

    /// <summary>Port HTTPS (certificat auto-signé) : nécessaire au micro et au volant depuis un autre appareil que le Mac.</summary>
    public int HttpsPort { get; set; } = 8687;

    public bool EnableHttps { get; set; } = true;

    public int GamePort { get; set; } = Core.Network.RemoteReceiver.DefaultPort;

    /// <summary>Adresses du PC de jeu à essayer en plus de la découverte automatique (ex. « 192.168.1.20 », « pc-jeu.tailnet.ts.net »).</summary>
    public List<string> GameHosts { get; set; } = new();

    public bool DemoMode { get; set; }

    public double DemoSpeed { get; set; } = 1.0;

    public AiSettings Ai { get; set; } = new();

    /// <summary>Fenêtre sans accélération graphique (comme l'option « --sans-gpu ») : Windows, fenêtre noire avec certains pilotes.</summary>
    public bool FenetreSansGpu { get; set; }

    /// <summary>
    /// Où se fait entendre la voix de l'ingénieur quand le PC de jeu la demande (collecteur, « Entendre l'ingénieur sur ce PC ») :
    /// « pc » (le PC de jeu seul, par défaut), « ecrans » (les écrans de SecondMonitor seuls) ou « deux ».
    /// </summary>
    public string VoiceOutput { get; set; } = "pc";
    /// <summary>Plages de température (bleu = froid, vert = bon, rouge = trop chaud) par jeu et par voiture.</summary>
    public List<Core.Race.ThermalProfile> ThermalProfiles { get; set; } = new();
}

/// <summary>Autre ordinateur du réseau qui fait tourner Ollama (Raspberry Pi, PC, autre Mac…).</summary>
public sealed class AiNode
{
    public string Name { get; set; } = "";

    public string Url { get; set; } = "";
}

public sealed class AiSettings
{
    public string OllamaUrl { get; set; } = "http://127.0.0.1:11434";

    /// <summary>Nom de la machine locale dans les listes (« Mac »).</summary>
    public string LocalNodeName { get; set; } = "Mac";

    /// <summary>Machines IA supplémentaires. Un modèle s'écrit « nom@machine » (sans « @ » = ce Mac).</summary>
    public List<AiNode> Nodes { get; set; } = new();

    /// <summary>Surnoms de modèles pour les changer à la voix (« omniaudio » → « lfm3-omni:2b »).</summary>
    public Dictionary<string, string> Aliases { get; set; } = new();

    /// <summary>Quand le hub a calculé la réponse, la dire telle quelle (fiable, rapide) au lieu de la faire reformuler.</summary>
    public bool StrictAnswers { get; set; } = true;

    /// <summary>Humidité de piste (%) à partir de laquelle l'IA conseille les pneus pluie.</summary>
    public int WetThresholdPct { get; set; } = 25;

    /// <summary>Délai minimal entre deux interventions spontanées de l'IA (s).</summary>
    public int ProactiveMinIntervalSec { get; set; } = 120;

    /// <summary>Modèle qui comprend la voix (transcription). Vide = même que le modèle de raisonnement.</summary>
    public string ListenModel { get; set; } = "";

    /// <summary>Modèle qui raisonne et répond (texte + appels d'outils).</summary>
    public string ThinkModel { get; set; } = "";

    /// <summary>Modèle qui parle (synthèse vocale). Vide = même que le modèle de raisonnement.</summary>
    public string SpeakModel { get; set; } = "";

    public string Voice { get; set; } = "default";

    public string Language { get; set; } = "fr";

    /// <summary>L'IA prend la parole d'elle-même (pluie, relais à allonger, arrêt proche, menace derrière, surchauffe).</summary>
    public bool ProactiveRadio { get; set; } = true;

    /// <summary>Alertes de pilotage dites tout de suite : limites de piste, temps annulé, pénalité, usure brutale, départ.</summary>
    public bool DrivingAlerts { get; set; } = true;

    /// <summary>
    /// Mode « thinking » des modèles qui l'ont (Qwen 3, DeepSeek R1, gpt-oss…) : l'IA raisonne avant de répondre, plus juste mais plus lent.
    /// Activable à la voix (« active le mode thinking », « réfléchis plus ») ; sans effet sur les modèles qui ne l'ont pas.
    /// </summary>
    public bool ThinkingMode { get; set; }

    /// <summary>Annonces de limites de piste et de temps annulé (désactivables à la voix : « coupe les limites de piste »).</summary>
    public bool TrackLimitAlerts { get; set; } = true;

    /// <summary>
    /// L'ingénieur règle lui-même le menu des stands de Le Mans Ultimate (par le collecteur du PC de jeu) quand le pilote demande
    /// des pneus, la réparation ou du carburant. Désactivé tant que le pilote ne l'a pas accepté (l'ingénieur le propose).
    /// </summary>
    public bool LmuPitMenu { get; set; }

    /// <summary>Accusés de réception radio en anglais (« Copy », « Understood »), comme une vraie radio d'équipe.</summary>
    public bool EnglishCalls { get; set; } = true;

    /// <summary>
    /// Spécialistes : un modèle par domaine (« pneus », « stratégie », « voiture », « course »), vide = modèle « réflexion ».
    /// Chaque spécialiste ne reçoit que les faits et les connaissances de son domaine : réponses plus rapides et plus justes.
    /// </summary>
    public Dictionary<string, string> Specialists { get; set; } = new();

    /// <summary>Temps (s) au-delà duquel l'ingénieur répond « Copy, je regarde et je reviens vers vous » et laisse le pilote poser une autre question.</summary>
    public double AckAfterSeconds { get; set; } = 2.5;

    /// <summary>
    /// Modèle pour les questions difficiles (analyse, « pourquoi », plusieurs sujets, longues), par exemple un plus gros modèle
    /// sur un serveur de calcul (« nom@machine ») ; vide = le modèle habituel. Les questions simples restent sur le modèle habituel.
    /// </summary>
    public string HardModel { get; set; } = "";

    /// <summary>Lexique de piste : corrections ajoutées par le pilote pour la dictée (« ce qu'elle entend » → « ce que j'ai dit »).</summary>
    public List<SecondMonitor.Hub.Core.Ai.LexiconEntry> Lexicon { get; set; } = new();

    /// <summary>
    /// Durée maximale (s) d'un morceau d'audio envoyé au modèle « écoute ». Au-delà, l'enregistrement est découpé sur un silence
    /// et chaque morceau est transcrit à part (certains modèles refusent plus de 30 ou 60 secondes).
    /// </summary>
    public int ListenChunkSeconds { get; set; } = 28;
}

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.Never };
    private readonly object _lock = new();
    private readonly string _path;

    public SettingsStore(string dataDirectory)
    {
        DataDirectory = dataDirectory;
        _path = Path.Combine(dataDirectory, "settings.json");
        Current = Load();
    }

    public string DataDirectory { get; }

    public HubSettings Current { get; private set; }

    public event Action<HubSettings>? Changed;

    public static string DefaultDataDirectory()
    {
        string root = OperatingSystem.IsMacOS()
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support")
            : Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(root, "SecondMonitorHub");
    }

    public void Update(Action<HubSettings> change)
    {
        HubSettings copy;
        lock (_lock)
        {
            change(Current);
            Directory.CreateDirectory(DataDirectory);
            File.WriteAllText(_path, JsonSerializer.Serialize(Current, Options));
            copy = Current;
        }

        Changed?.Invoke(copy);
    }

    private HubSettings Load()
    {
        try
        {
            if (File.Exists(_path))
            {
                return JsonSerializer.Deserialize<HubSettings>(File.ReadAllText(_path), Options) ?? new HubSettings();
            }
        }
        catch (Exception)
        {
            // Fichier corrompu : on repart des valeurs par défaut.
        }

        return new HubSettings();
    }
}
