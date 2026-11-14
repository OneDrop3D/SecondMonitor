// Partagé avec le collecteur du PC de jeu (sans usings implicites ni annotations par défaut).
#nullable enable
namespace SecondMonitor.Hub.Music;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.Json;

/// <summary>
/// Deezer sur Windows.
/// - Commandes et titre en cours : contrôles multimédia du système (SMTC, la petite fenêtre « média » de Windows),
///   en visant la session de l'application Deezer ; à défaut, touches multimédia (aucune autorisation nécessaire).
/// - Volume de Deezer seul : mélangeur de volume de Windows (session audio de Deezer.exe), sans toucher à la voix.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsMusicPlayer : IMusicPlayer
{
    private const string SmtcScript = """
        param([string]$Action = 'etat', [string]$Web = '0')
        [Console]::OutputEncoding = [System.Text.Encoding]::UTF8
        try {
          Add-Type -AssemblyName System.Runtime.WindowsRuntime
          $asTask = ([System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object { $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1' })[0]
          function Await($op, [Type]$type) { $t = $asTask.MakeGenericMethod($type).Invoke($null, @($op)); $t.Wait(-1) | Out-Null; $t.Result }
          [void][Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager, Windows.Media.Control, ContentType = WindowsRuntime]
          $mgr = Await ([Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager]::RequestAsync()) ([Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager])
          $s = $mgr.GetSessions() | Where-Object { $_.SourceAppUserModelId -match '(?i)deezer' } | Select-Object -First 1
          if (-not $s -and $Web -eq '1') { $s = $mgr.GetCurrentSession() }
          if (-not $s) { '{"found":false}'; exit 0 }
          switch ($Action) {
            'play' { [void](Await ($s.TryPlayAsync()) ([bool])) }
            'pause' { [void](Await ($s.TryPauseAsync()) ([bool])) }
            'next' { [void](Await ($s.TrySkipNextAsync()) ([bool])) }
            'previous' { [void](Await ($s.TrySkipPreviousAsync()) ([bool])) }
          }
          $p = Await ($s.TryGetMediaPropertiesAsync()) ([Windows.Media.Control.GlobalSystemMediaTransportControlsSessionMediaProperties])
          [pscustomobject]@{ found = $true; app = $s.SourceAppUserModelId; status = $s.GetPlaybackInfo().PlaybackStatus.ToString(); title = $p.Title; artist = $p.Artist; album = $p.AlbumTitle } | ConvertTo-Json -Compress
        } catch { [pscustomobject]@{ found = $false; error = $_.Exception.Message } | ConvertTo-Json -Compress }
        """;

    private readonly string _script;
    private readonly Func<bool> _webMode;

    /// <param name="webMode">Vrai : lecteur web Deezer dans le navigateur (n'importe quelle session multimédia), sinon l'application Deezer.</param>
    public WindowsMusicPlayer(string dataDir, Func<bool> webMode)
    {
        _webMode = webMode;
        string dir = Path.Combine(dataDir, "musique");
        Directory.CreateDirectory(dir);
        _script = Path.Combine(dir, "smtc.ps1");
        File.WriteAllText(_script, SmtcScript);
    }

    public string Platform => "windows";

    public bool CanSetVolume => true;

    public bool AppInstalled()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey("deezer");
            return key != null;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public bool AppRunning()
    {
        Process[] list = Process.GetProcessesByName("Deezer");
        foreach (Process p in list)
        {
            p.Dispose();
        }

        return list.Length > 0;
    }

    private async Task<PlayerState?> SmtcAsync(string action, CancellationToken ct)
    {
        var (code, output) = await ProcessRunner.RunAsync(
            "powershell.exe",
            new[] { "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File", _script, "-Action", action, "-Web", _webMode() ? "1" : "0" },
            ct,
            6000);
        if (code != 0 || !output.StartsWith('{'))
        {
            return null;
        }

        try
        {
            using JsonDocument doc = JsonDocument.Parse(output);
            JsonElement r = doc.RootElement;
            if (!r.TryGetProperty("found", out JsonElement f) || f.ValueKind != JsonValueKind.True)
            {
                return null;
            }

            string? status = Str(r, "status");
            return new PlayerState
            {
                Playing = status == null ? null : status == "Playing",
                Title = Str(r, "title"),
                Artist = Str(r, "artist"),
                Album = Str(r, "album"),
                Source = Str(r, "app"),
                Via = "SMTC",
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public async Task<PlayerState> GetStateAsync(CancellationToken ct)
    {
        PlayerState state = await SmtcAsync("etat", ct) ?? new PlayerState();
        WindowsAudioSessions.DeezerInfo? audio = WindowsAudioSessions.Find();
        if (audio != null)
        {
            state.Volume = (int)Math.Round(audio.Value.Volume * 100);
            if (state.Playing == null)
            {
                state.Playing = audio.Value.Active;
                state.Via ??= "mélangeur";
            }
        }

        return state;
    }

    public async Task<bool> SendAsync(TransportCommand command, PlayerState? known, CancellationToken ct)
    {
        string action = command switch
        {
            TransportCommand.Play => "play",
            TransportCommand.Pause => "pause",
            TransportCommand.Next => "next",
            _ => "previous",
        };
        if (await SmtcAsync(action, ct) != null)
        {
            return true;
        }

        // Pas de session Deezer dans les contrôles multimédia : touche multimédia (lecture/pause est une bascule).
        if ((command == TransportCommand.Play && known?.Playing == true) || (command == TransportCommand.Pause && known?.Playing == false))
        {
            return true;
        }

        byte vk = command switch
        {
            TransportCommand.Next => 0xB0,
            TransportCommand.Previous => 0xB1,
            _ => 0xB3,
        };
        keybd_event(vk, 0, 1, UIntPtr.Zero);
        keybd_event(vk, 0, 1 | 2, UIntPtr.Zero);
        return true;
    }

    public Task<int?> SetVolumeAsync(int pct, CancellationToken ct) =>
        Task.FromResult(WindowsAudioSessions.SetVolume(Math.Clamp(pct, 0, 100) / 100f) ? (int?)Math.Clamp(pct, 0, 100) : null);

    public Task OpenAsync(string link, CancellationToken ct)
    {
        ProcessRunner.OpenWithSystem(link);
        return Task.CompletedTask;
    }

    public PermissionInfo Permission(bool mediaKeys) => new(false, true, null, null);

    public bool RequestPermission() => true;

    public void OpenPermissionSettings()
    {
    }

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extraInfo);

    private static string? Str(JsonElement e, string name) => e.TryGetProperty(name, out JsonElement v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
}

/// <summary>Mélangeur de volume de Windows (Core Audio) : volume de l'application Deezer seule.</summary>
[SupportedOSPlatform("windows")]
internal static class WindowsAudioSessions
{
    public readonly record struct DeezerInfo(float Volume, bool Active);

    public static DeezerInfo? Find()
    {
        DeezerInfo? found = null;
        ForEachDeezerSession((control, volume) =>
        {
            volume.GetMasterVolume(out float level);
            control.GetState(out int state);
            bool active = state == 1;
            if (found == null || active)
            {
                found = new DeezerInfo(level, active || found?.Active == true);
            }
        });
        return found;
    }

    public static bool SetVolume(float level)
    {
        bool any = false;
        Guid context = Guid.Empty;
        ForEachDeezerSession((_, volume) =>
        {
            volume.SetMasterVolume(level, ref context);
            any = true;
        });
        return any;
    }

    private static void ForEachDeezerSession(Action<IAudioSessionControl2, ISimpleAudioVolume> action)
    {
        try
        {
            var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorCom();
            enumerator.GetDefaultAudioEndpoint(0 /* eRender */, 1 /* eMultimedia */, out IMMDevice device);
            Guid iid = typeof(IAudioSessionManager2).GUID;
            device.Activate(ref iid, 23 /* CLSCTX_ALL */, IntPtr.Zero, out object managerObj);
            var manager = (IAudioSessionManager2)managerObj;
            manager.GetSessionEnumerator(out IAudioSessionEnumerator sessions);
            sessions.GetCount(out int count);
            for (int i = 0; i < count; i++)
            {
                sessions.GetSession(i, out IAudioSessionControl2 control);
                control.GetProcessId(out uint pid);
                if (pid == 0 || !IsDeezer(pid))
                {
                    continue;
                }

                action(control, (ISimpleAudioVolume)control);
            }
        }
        catch (Exception)
        {
            // pas de périphérique de sortie, ou COM indisponible
        }
    }

    private static bool IsDeezer(uint pid)
    {
        try
        {
            using Process p = Process.GetProcessById((int)pid);
            return p.ProcessName.Contains("deezer", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception)
        {
            return false;
        }
    }

    [ComImport]
    [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    private class MMDeviceEnumeratorCom
    {
    }

    [ComImport]
    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        [PreserveSig] int EnumAudioEndpoints(int dataFlow, int stateMask, out IntPtr devices);

        [PreserveSig] int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice endpoint);
    }

    [ComImport]
    [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        [PreserveSig] int Activate(ref Guid iid, int clsCtx, IntPtr activationParams, [MarshalAs(UnmanagedType.IUnknown)] out object instance);
    }

    [ComImport]
    [Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioSessionManager2
    {
        [PreserveSig] int GetAudioSessionControl(IntPtr groupingParam, int flags, out IntPtr sessionControl);

        [PreserveSig] int GetSimpleAudioVolume(IntPtr groupingParam, int flags, out IntPtr audioVolume);

        [PreserveSig] int GetSessionEnumerator(out IAudioSessionEnumerator sessionEnum);
    }

    [ComImport]
    [Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioSessionEnumerator
    {
        [PreserveSig] int GetCount(out int sessionCount);

        [PreserveSig] int GetSession(int sessionIndex, out IAudioSessionControl2 session);
    }

    [ComImport]
    [Guid("BFB7FF88-7239-4FC9-8FA2-07C950BE9C6D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioSessionControl2
    {
        // IAudioSessionControl
        [PreserveSig] int GetState(out int state);

        [PreserveSig] int GetDisplayName(out IntPtr name);

        [PreserveSig] int SetDisplayName(IntPtr name, ref Guid eventContext);

        [PreserveSig] int GetIconPath(out IntPtr path);

        [PreserveSig] int SetIconPath(IntPtr path, ref Guid eventContext);

        [PreserveSig] int GetGroupingParam(out Guid groupingParam);

        [PreserveSig] int SetGroupingParam(ref Guid groupingParam, ref Guid eventContext);

        [PreserveSig] int RegisterAudioSessionNotification(IntPtr client);

        [PreserveSig] int UnregisterAudioSessionNotification(IntPtr client);

        // IAudioSessionControl2
        [PreserveSig] int GetSessionIdentifier(out IntPtr id);

        [PreserveSig] int GetSessionInstanceIdentifier(out IntPtr id);

        [PreserveSig] int GetProcessId(out uint processId);
    }

    [ComImport]
    [Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ISimpleAudioVolume
    {
        [PreserveSig] int SetMasterVolume(float level, ref Guid eventContext);

        [PreserveSig] int GetMasterVolume(out float level);
    }
}
