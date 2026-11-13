namespace SecondMonitor.Hub.Music;

using System.Runtime.InteropServices;
using System.Runtime.Versioning;

/// <summary>
/// Appels directs aux API publiques de macOS (sans autorisation) :
/// - CoreAudio (macOS 14.2+) : quelles applications envoient du son en ce moment (Deezer joue-t-il ?) ;
/// - Accessibilité : l'application a-t-elle le droit d'envoyer des touches multimédia ?
/// </summary>
[SupportedOSPlatform("macos")]
internal static class MacNative
{
    private const string CoreAudio = "/System/Library/Frameworks/CoreAudio.framework/CoreAudio";
    private const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    private const string AppServices = "/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices";

    private const uint SystemObject = 1;

    [StructLayout(LayoutKind.Sequential)]
    private struct PropertyAddress
    {
        public uint Selector;
        public uint Scope;
        public uint Element;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CFRange
    {
        public nint Location;
        public nint Length;
    }

    [DllImport(CoreAudio)]
    private static extern int AudioObjectGetPropertyDataSize(uint objectId, ref PropertyAddress address, uint qualifierSize, IntPtr qualifier, out uint size);

    [DllImport(CoreAudio)]
    private static extern int AudioObjectGetPropertyData(uint objectId, ref PropertyAddress address, uint qualifierSize, IntPtr qualifier, ref uint size, IntPtr data);

    [DllImport(CoreFoundation)]
    private static extern nint CFStringGetLength(IntPtr str);

    [DllImport(CoreFoundation)]
    private static extern void CFStringGetCharacters(IntPtr str, CFRange range, IntPtr buffer); // UTF-16

    [DllImport(CoreFoundation)]
    private static extern void CFRelease(IntPtr obj);

    [DllImport(CoreFoundation)]
    private static extern IntPtr CFDictionaryCreate(IntPtr allocator, IntPtr[] keys, IntPtr[] values, nint count, IntPtr keyCallBacks, IntPtr valueCallBacks);

    [DllImport(AppServices)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool AXIsProcessTrusted();

    [DllImport(AppServices)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool AXIsProcessTrustedWithOptions(IntPtr options);

    private static uint Code(string s) => (uint)((s[0] << 24) | (s[1] << 16) | (s[2] << 8) | s[3]);

    private static PropertyAddress Address(string selector) => new() { Selector = Code(selector), Scope = Code("glob"), Element = 0 };

    /// <summary>
    /// Vrai si un processus dont l'identifiant contient <paramref name="bundleFragment"/> envoie du son en ce moment,
    /// faux s'il existe mais ne joue pas, null si on ne peut pas savoir (macOS trop ancien, application absente).
    /// </summary>
    public static bool? IsAppPlayingAudio(string bundleFragment)
    {
        try
        {
            PropertyAddress list = Address("prs#"); // kAudioHardwarePropertyProcessObjectList
            if (AudioObjectGetPropertyDataSize(SystemObject, ref list, 0, IntPtr.Zero, out uint size) != 0 || size == 0)
            {
                return null;
            }

            IntPtr buffer = Marshal.AllocHGlobal((int)size);
            try
            {
                if (AudioObjectGetPropertyData(SystemObject, ref list, 0, IntPtr.Zero, ref size, buffer) != 0)
                {
                    return null;
                }

                int count = (int)size / sizeof(uint);
                bool found = false;
                for (int i = 0; i < count; i++)
                {
                    uint process = (uint)Marshal.ReadInt32(buffer, i * sizeof(uint));
                    string? bundle = ReadString(process, "pbid"); // kAudioProcessPropertyBundleID
                    if (bundle == null || !bundle.Contains(bundleFragment, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    found = true;
                    if (ReadUInt(process, "piro") == 1) // kAudioProcessPropertyIsRunningOutput
                    {
                        return true;
                    }
                }

                return found ? false : null;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
        catch (Exception)
        {
            return null; // CoreAudio sans liste de processus (macOS < 14.2)
        }
    }

    private static uint? ReadUInt(uint objectId, string selector)
    {
        PropertyAddress a = Address(selector);
        uint size = sizeof(uint);
        IntPtr p = Marshal.AllocHGlobal(sizeof(uint));
        try
        {
            return AudioObjectGetPropertyData(objectId, ref a, 0, IntPtr.Zero, ref size, p) == 0 ? (uint)Marshal.ReadInt32(p) : null;
        }
        finally
        {
            Marshal.FreeHGlobal(p);
        }
    }

    private static string? ReadString(uint objectId, string selector)
    {
        PropertyAddress a = Address(selector);
        uint size = (uint)IntPtr.Size;
        IntPtr p = Marshal.AllocHGlobal(IntPtr.Size);
        try
        {
            if (AudioObjectGetPropertyData(objectId, ref a, 0, IntPtr.Zero, ref size, p) != 0)
            {
                return null;
            }

            IntPtr cf = Marshal.ReadIntPtr(p);
            if (cf == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                nint length = CFStringGetLength(cf);
                IntPtr chars = Marshal.AllocHGlobal((int)Math.Max(1, length * 2));
                try
                {
                    CFStringGetCharacters(cf, new CFRange { Location = 0, Length = length }, chars);
                    return Marshal.PtrToStringUni(chars, (int)length);
                }
                finally
                {
                    Marshal.FreeHGlobal(chars);
                }
            }
            finally
            {
                CFRelease(cf);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(p);
        }
    }

    /// <summary>L'application (SecondMonitor) a l'autorisation Accessibilité.</summary>
    public static bool IsTrusted()
    {
        try
        {
            return AXIsProcessTrusted();
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Demande l'autorisation Accessibilité : macOS affiche sa fenêtre et ajoute SecondMonitor à la liste des
    /// réglages (case à cocher). Retourne vrai si elle est déjà accordée.
    /// </summary>
    public static bool RequestTrust()
    {
        try
        {
            IntPtr cf = NativeLibrary.Load(CoreFoundation);
            IntPtr ax = NativeLibrary.Load(AppServices);
            IntPtr key = Marshal.ReadIntPtr(NativeLibrary.GetExport(ax, "kAXTrustedCheckOptionPrompt"));
            IntPtr yes = Marshal.ReadIntPtr(NativeLibrary.GetExport(cf, "kCFBooleanTrue"));
            IntPtr dict = CFDictionaryCreate(IntPtr.Zero, new[] { key }, new[] { yes }, 1, NativeLibrary.GetExport(cf, "kCFTypeDictionaryKeyCallBacks"), NativeLibrary.GetExport(cf, "kCFTypeDictionaryValueCallBacks"));
            try
            {
                return AXIsProcessTrustedWithOptions(dict);
            }
            finally
            {
                CFRelease(dict);
            }
        }
        catch (Exception)
        {
            return IsTrusted();
        }
    }
}
