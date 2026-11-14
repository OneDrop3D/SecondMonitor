namespace SecondMonitor.Hub.Services;

using SecondMonitor.Hub.Core.Ai;
using SecondMonitor.Hub.Music;

/// <summary>
/// Audio reçu dans un autre format que WAV (webm, ogg, m4a, mp3… selon l'appareil) : converti en WAV 16 kHz mono avant la
/// transcription, par ffmpeg s'il est installé, sinon par afconvert (fourni avec macOS, sans webm ni ogg). Sans outil, l'audio
/// part tel quel.
/// </summary>
public static class AudioConverter
{
    private static readonly string[] FfmpegPlaces =
    {
        "/opt/homebrew/bin/ffmpeg", "/usr/local/bin/ffmpeg", "/usr/bin/ffmpeg",
        @"C:\ffmpeg\bin\ffmpeg.exe", @"C:\Program Files\ffmpeg\bin\ffmpeg.exe",
    };

    /// <summary>WAV (inchangé s'il l'était déjà), ou null si la conversion est impossible.</summary>
    public static async Task<byte[]?> ToWavAsync(byte[] data, string fileName, CancellationToken ct)
    {
        if (WavAudio.IsWav(data))
        {
            return data;
        }

        string? ffmpeg = FindFfmpeg();
        bool afconvert = OperatingSystem.IsMacOS() && File.Exists("/usr/bin/afconvert");
        if (ffmpeg == null && !afconvert)
        {
            return null;
        }

        string ext = Path.GetExtension(fileName);
        string dir = Path.Combine(Path.GetTempPath(), "secondmonitor-audio");
        Directory.CreateDirectory(dir);
        string input = Path.Combine(dir, Guid.NewGuid().ToString("N") + (string.IsNullOrEmpty(ext) || ext.Length > 6 ? ".bin" : ext));
        string output = Path.ChangeExtension(input, ".wav");
        try
        {
            await File.WriteAllBytesAsync(input, data, ct);
            int code = ffmpeg != null
                ? (await ProcessRunner.RunAsync(ffmpeg, new[] { "-y", "-hide_banner", "-loglevel", "error", "-i", input, "-ac", "1", "-ar", "16000", "-c:a", "pcm_s16le", output }, ct, 20000)).Code
                : (await ProcessRunner.RunAsync("/usr/bin/afconvert", new[] { "-f", "WAVE", "-d", "LEI16@16000", "-c", "1", input, output }, ct, 20000)).Code;
            return code == 0 && File.Exists(output) ? await File.ReadAllBytesAsync(output, ct) : null;
        }
        catch (IOException)
        {
            return null;
        }
        finally
        {
            TryDelete(input);
            TryDelete(output);
        }
    }

    private static string? FindFfmpeg()
    {
        foreach (string p in FfmpegPlaces)
        {
            if (File.Exists(p))
            {
                return p;
            }
        }

        string exe = OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";
        foreach (string dir in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            string candidate = Path.Combine(dir, exe);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception)
        {
            // fichier temporaire : tant pis
        }
    }
}
