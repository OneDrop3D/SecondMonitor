namespace SecondMonitor.Collector
{
    using System;
    using System.Net.Http;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Bouton radio sur le PC de jeu : une touche du clavier ou un bouton du volant, lu même quand le jeu est au premier plan.
    /// Un appui est transmis à SecondMonitor (POST /api/radio/press) : le poste radio démarre ou envoie la dictée,
    /// ou coupe l'ingénieur s'il parle. Avec « Parler avec le micro de ce PC », c'est la fenêtre du collecteur
    /// qui enregistre (voir EngineerLink). Windows uniquement (lecture du clavier et des manettes par l'API système,
    /// 50 fois par seconde, seulement quand un bouton est choisi).
    /// </summary>
    internal sealed class RadioButton : IDisposable
    {
        private const int PollMs = 20;
        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(2) };

        private readonly CollectorRuntime _runtime;
        private readonly Thread _thread;
        private volatile bool _running = true;
        private bool _wasPressed;
        private uint[] _learnBaseline;

        public RadioButton(CollectorRuntime runtime)
        {
            _runtime = runtime;
            _thread = new Thread(Loop) { IsBackground = true, Name = "Bouton radio", Priority = ThreadPriority.BelowNormal };
            if (OperatingSystem.IsWindows())
            {
                _thread.Start();
            }
        }

        public static bool Supported => OperatingSystem.IsWindows();

        /// <summary>En attente de l'appui à associer.</summary>
        public bool Learning { get; private set; }

        public string Description => Describe(_runtime.Settings.BoutonRadio);

        public void Learn()
        {
            _learnBaseline = null;
            Learning = Supported;
        }

        public void Clear()
        {
            Learning = false;
            _runtime.Settings.BoutonRadio = null;
            _runtime.Settings.Save();
            _runtime.Log("Bouton radio retiré.");
        }

        public void Dispose()
        {
            _running = false;
        }

        public static string Describe(BoutonRadio b) => b == null
            ? "aucun"
            : b.Type == "manette" ? $"bouton {b.Bouton + 1} du volant / de la manette {b.Manette + 1}" : $"touche {KeyName(b.Code)}";

        private void Loop()
        {
            while (_running)
            {
                try
                {
                    if (Learning)
                    {
                        LearnTick();
                    }
                    else if (_runtime.Settings.BoutonRadio is BoutonRadio b)
                    {
                        bool pressed = IsPressed(b);
                        if (pressed && !_wasPressed)
                        {
                            _ = SendPressAsync();
                        }

                        _wasPressed = pressed;
                    }
                }
                catch (Exception)
                {
                    // Manette débranchée, etc. : on continue.
                }

                Thread.Sleep(Learning || _runtime.Settings.BoutonRadio != null ? PollMs : 500);
            }
        }

        private void LearnTick()
        {
            // Clavier : première touche enfoncée (hors souris).
            for (int vk = 8; vk < 255; vk++)
            {
                if ((GetAsyncKeyState(vk) & 0x8000) != 0)
                {
                    Bind(new BoutonRadio { Type = "clavier", Code = vk });
                    return;
                }
            }

            // Manettes et volants : un bouton qui n'était pas enfoncé au début de l'apprentissage.
            _learnBaseline ??= new uint[16];
            for (uint id = 0; id < 16; id++)
            {
                uint buttons = Buttons(id);
                if (buttons == uint.MaxValue)
                {
                    continue;
                }

                uint fresh = buttons & ~_learnBaseline[id];
                _learnBaseline[id] |= buttons;
                if (fresh != 0)
                {
                    int bit = 0;
                    while ((fresh & (1u << bit)) == 0)
                    {
                        bit++;
                    }

                    Bind(new BoutonRadio { Type = "manette", Manette = (int)id, Bouton = bit });
                    return;
                }
            }
        }

        private void Bind(BoutonRadio b)
        {
            Learning = false;
            _wasPressed = true; // le bouton encore enfoncé ne doit pas déclencher la radio
            _runtime.Settings.BoutonRadio = b;
            _runtime.Settings.Save();
            _runtime.Log($"Bouton radio : {Describe(b)}.");
        }

        private static bool IsPressed(BoutonRadio b)
        {
            if (b.Type == "manette")
            {
                uint buttons = Buttons((uint)b.Manette);
                return buttons != uint.MaxValue && (buttons & (1u << b.Bouton)) != 0;
            }

            return (GetAsyncKeyState(b.Code) & 0x8000) != 0;
        }

        /// <summary>Boutons enfoncés d'une manette (32 au plus), ou uint.MaxValue si elle n'existe pas.</summary>
        private static uint Buttons(uint id)
        {
            var info = new JoyInfoEx { Size = (uint)Marshal.SizeOf<JoyInfoEx>(), Flags = 0x80 }; // JOY_RETURNBUTTONS
            return joyGetPosEx(id, ref info) == 0 ? info.Buttons : uint.MaxValue;
        }

        private async Task SendPressAsync()
        {
            // « Parler avec le micro de ce PC » : la fenêtre du collecteur enregistre elle-même (ou coupe l'ingénieur).
            if (_runtime.Engineer?.TryHandlePress() == true)
            {
                return;
            }

            string[] hubs = _runtime.Hubs;
            if (hubs.Length == 0)
            {
                _runtime.Log("Bouton radio : aucun SecondMonitor connecté.");
                return;
            }

            foreach (string hub in hubs)
            {
                string host = hub.Contains(':') && hub.Split(':').Length == 2 ? hub.Split(':')[0] : hub;
                try
                {
                    using var r = await Http.PostAsync($"http://{host}:{_runtime.Settings.PortSecondMonitor}/api/radio/press?de=collecteur", null).ConfigureAwait(false);
                    if (!r.IsSuccessStatusCode)
                    {
                        _runtime.Log($"Bouton radio : SecondMonitor ({host}) a répondu {(int)r.StatusCode}.");
                    }
                }
                catch (Exception ex)
                {
                    _runtime.Log($"Bouton radio : SecondMonitor ({host}:{_runtime.Settings.PortSecondMonitor}) injoignable ({ex.Message}).");
                }
            }
        }

        private static string KeyName(int vk) => vk switch
        {
            >= 0x30 and <= 0x39 or >= 0x41 and <= 0x5A => ((char)vk).ToString(),
            >= 0x70 and <= 0x87 => $"F{vk - 0x6F}",
            >= 0x60 and <= 0x69 => $"pavé {vk - 0x60}",
            0x20 => "Espace",
            0x0D => "Entrée",
            0x09 => "Tab",
            0x10 or 0xA0 or 0xA1 => "Maj",
            0x11 or 0xA2 or 0xA3 => "Ctrl",
            0x12 or 0xA4 or 0xA5 => "Alt",
            0x14 => "Verr. Maj",
            0x2D => "Inser",
            0x2E => "Suppr",
            0x24 => "Origine",
            0x23 => "Fin",
            0x21 => "Page préc.",
            0x22 => "Page suiv.",
            0x25 => "←",
            0x26 => "↑",
            0x27 => "→",
            0x28 => "↓",
            _ => $"n° {vk}",
        };

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("winmm.dll")]
        private static extern uint joyGetPosEx(uint uJoyID, ref JoyInfoEx pji);

        [StructLayout(LayoutKind.Sequential)]
        private struct JoyInfoEx
        {
            public uint Size;
            public uint Flags;
            public uint X;
            public uint Y;
            public uint Z;
            public uint R;
            public uint U;
            public uint V;
            public uint Buttons;
            public uint ButtonNumber;
            public uint Pov;
            public uint Reserved1;
            public uint Reserved2;
        }
    }

    /// <summary>Touche ou bouton choisi pour la radio (collecteur.json).</summary>
    public sealed class BoutonRadio
    {
        /// <summary>« clavier » ou « manette ».</summary>
        public string Type { get; set; }

        /// <summary>Code de touche Windows (clavier).</summary>
        public int Code { get; set; }

        /// <summary>Numéro de la manette ou du volant (0 à 15).</summary>
        public int Manette { get; set; }

        /// <summary>Numéro du bouton (0 à 31).</summary>
        public int Bouton { get; set; }
    }
}
