namespace SecondMonitor.Hub.Music;

using System.Text.Json;
using SecondMonitor.Hub.Core.Music;

/// <summary>Réglages de la musique (fichier à part : music.json dans le dossier de données).</summary>
public sealed class MusicSettings
{
    /// <summary>Deezer activé (désactivé par défaut : rien ne se passe tant que l'utilisateur ne l'a pas choisi).</summary>
    public bool Enabled { get; set; }

    /// <summary>« app » = application Deezer de l'ordinateur (meilleure qualité), « web » = lecteur web dans le navigateur.</summary>
    public string Player { get; set; } = "app";

    /// <summary><see cref="MusicPolicy"/> : « toujours », « pasEnCourse », « essaisSeulement ».</summary>
    public string SessionPolicy { get; set; } = MusicPolicy.Always;

    /// <summary>Quand la radio parle : « pause » (partout), « volume » (Windows, Linux : baisse Deezer seul), « aucun ».</summary>
    public string DuckMode { get; set; } = "pause";

    /// <summary>Volume de Deezer pendant la radio en mode « volume » (% du volume d'avant).</summary>
    public int DuckVolumePct { get; set; } = 25;

    /// <summary>Pas du volume pour « monte / baisse la musique » (points de %).</summary>
    public int VolumeStepPct { get; set; } = 15;

    /// <summary>Mac : utiliser les touches multimédia (lecture, suivant, précédent) — demande l'autorisation Accessibilité.</summary>
    public bool MediaKeys { get; set; }

    /// <summary>Mac : l'autorisation Accessibilité a déjà été demandée une fois (on ne la redemande pas tout seul).</summary>
    public bool AccessibilityAsked { get; set; }
}

public sealed class MusicSettingsStore
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly object _lock = new();
    private readonly string _path;

    public MusicSettingsStore(string dataDirectory)
    {
        _path = Path.Combine(dataDirectory, "music.json");
        Current = Load();
    }

    public MusicSettings Current { get; private set; }

    public void Update(Action<MusicSettings> change)
    {
        lock (_lock)
        {
            change(Current);
            Normalize(Current);
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, JsonSerializer.Serialize(Current, Options));
        }
    }

    private MusicSettings Load()
    {
        try
        {
            if (File.Exists(_path))
            {
                MusicSettings s = JsonSerializer.Deserialize<MusicSettings>(File.ReadAllText(_path), Options) ?? new MusicSettings();
                Normalize(s);
                return s;
            }
        }
        catch (Exception)
        {
            // Fichier abîmé : réglages par défaut.
        }

        return new MusicSettings();
    }

    private static void Normalize(MusicSettings s)
    {
        s.Player = s.Player == "web" ? "web" : "app";
        s.SessionPolicy = s.SessionPolicy is MusicPolicy.NotInRace or MusicPolicy.PracticeOnly ? s.SessionPolicy : MusicPolicy.Always;
        s.DuckMode = s.DuckMode is "volume" or "aucun" ? s.DuckMode : "pause";
        s.DuckVolumePct = Math.Clamp(s.DuckVolumePct, 0, 90);
        s.VolumeStepPct = Math.Clamp(s.VolumeStepPct, 5, 50);
    }
}
