namespace SecondMonitor.Hub.Core.Compute
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.Json;

    /// <summary>
    /// Appairage d'un serveur de calcul (« ordinateur 3 ») avec les SecondMonitor qui s'en servent (« ordinateur 2 ») :
    ///  - le serveur affiche un code à 6 chiffres (sur son écran seulement) ;
    ///  - l'ordinateur 2 l'envoie avec son nom ; si le code est bon, il reçoit un jeton (32 octets aléatoires) qu'il joint
    ///    ensuite à chaque requête (« Authorization: Bearer … ») ; le code change aussitôt ;
    ///  - le serveur ne garde que l'empreinte SHA-256 de chaque jeton (fichier « serveur-calcul.json »).
    /// Contre les essais au hasard : nouveau code après <see cref="MaxFailuresPerCode"/> erreurs, et plus aucun essai accepté
    /// après <see cref="MaxFailuresPerWindow"/> erreurs en 10 minutes.
    /// </summary>
    public sealed class ComputePairing
    {
        public const int MaxFailuresPerCode = 5;

        public const int MaxFailuresPerWindow = 10;

        public static readonly TimeSpan FailureWindow = TimeSpan.FromMinutes(10);

        /// <summary>Un SecondMonitor qui a fait signe depuis moins longtemps est « en ligne ».</summary>
        public static readonly TimeSpan OnlineDelay = TimeSpan.FromSeconds(50);

        private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        private readonly object _lock = new();
        private readonly string _path;
        private readonly Func<DateTime> _clock;
        private readonly List<DateTime> _failures = new();
        private readonly Dictionary<string, Activity> _activity = new(StringComparer.Ordinal);
        private readonly PairingFile _file;
        private int _failuresOnCode;

        public ComputePairing(string path, Func<DateTime> clock = null)
        {
            _path = path;
            _clock = clock ?? (() => DateTime.UtcNow);
            _file = Load(path);
            if (!IsValidCode(_file.Code))
            {
                _file.Code = NewCodeValue();
                Save();
            }
        }

        /// <summary>Le code a changé (nouvel appairage, trop d'erreurs, bouton « Nouveau code »).</summary>
        public event Action<string> CodeChanged;

        /// <summary>Code à 6 chiffres à saisir sur l'ordinateur 2.</summary>
        public string Code
        {
            get { lock (_lock) { return _file.Code; } }
        }

        /// <summary>Remplace le code (l'ancien ne marche plus).</summary>
        public string RenewCode()
        {
            string code;
            lock (_lock)
            {
                _file.Code = NewCodeValue();
                _failuresOnCode = 0;
                Save();
                code = _file.Code;
            }

            CodeChanged?.Invoke(code);
            return code;
        }

        /// <summary>
        /// Essai d'appairage. <paramref name="clientId"/> : identifiant stable du SecondMonitor qui demande (un nouvel appairage
        /// du même SecondMonitor remplace l'ancien jeton) ; <paramref name="clientName"/> : son nom, affiché sur le serveur.
        /// </summary>
        public PairingResult TryPair(string code, string clientId, string clientName, string address)
        {
            string renewed = null;
            PairingResult result;
            lock (_lock)
            {
                DateTime now = _clock();
                _failures.RemoveAll(f => now - f > FailureWindow);
                if (_failures.Count >= MaxFailuresPerWindow)
                {
                    TimeSpan wait = _failures.Min() + FailureWindow - now;
                    return new PairingResult { Status = PairingStatus.Locked, RetryAfter = wait < TimeSpan.Zero ? TimeSpan.Zero : wait };
                }

                if (!CodesEqual(NormalizeCode(code), _file.Code))
                {
                    _failures.Add(now);
                    if (++_failuresOnCode >= MaxFailuresPerCode)
                    {
                        _file.Code = NewCodeValue();
                        _failuresOnCode = 0;
                        Save();
                        renewed = _file.Code;
                    }

                    result = new PairingResult { Status = PairingStatus.WrongCode };
                }
                else
                {
                    string id = ComputeAnnouncement.IsValidId(clientId) ? clientId : Guid.NewGuid().ToString("N");
                    string name = ComputeAnnouncement.CleanName(clientName);
                    byte[] secret = RandomNumberGenerator.GetBytes(32);
                    string token = Base64Url(secret);
                    _file.Clients.RemoveAll(c => c.Id == id);
                    var client = new PairedClient { Id = id, Name = name.Length > 0 ? name : "SecondMonitor", Hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token))), PairedUtc = now };
                    _file.Clients.Add(client);
                    _activity[id] = new Activity { Address = address, LastSeenUtc = now };
                    // Le code a servi : il ne doit plus pouvoir relier un autre appareil.
                    _file.Code = NewCodeValue();
                    _failuresOnCode = 0;
                    Save();
                    renewed = _file.Code;
                    result = new PairingResult { Status = PairingStatus.Paired, Token = token, ClientId = id, ClientName = client.Name };
                }
            }

            if (renewed != null)
            {
                CodeChanged?.Invoke(renewed);
            }

            return result;
        }

        /// <summary>SecondMonitor relié qui présente ce jeton (en-tête « Authorization: Bearer … »), sinon null.</summary>
        public PairedClient Authenticate(string authorization)
        {
            const string prefix = "Bearer ";
            if (string.IsNullOrEmpty(authorization) || !authorization.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string token = authorization.Substring(prefix.Length).Trim();
            if (token.Length == 0 || token.Length > 200)
            {
                return null;
            }

            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            lock (_lock)
            {
                PairedClient found = null;
                foreach (PairedClient c in _file.Clients)
                {
                    byte[] known;
                    try
                    {
                        known = Convert.FromBase64String(c.Hash ?? string.Empty);
                    }
                    catch (FormatException)
                    {
                        continue;
                    }

                    // Comparaison à temps constant ; on parcourt tout, sans s'arrêter au premier trouvé.
                    if (CryptographicOperations.FixedTimeEquals(known, hash))
                    {
                        found = c;
                    }
                }

                return found;
            }
        }

        /// <summary>Note une requête d'un SecondMonitor relié (écran d'état du serveur).</summary>
        public void Touch(PairedClient client, string address, string what, bool countsAsRequest)
        {
            if (client == null)
            {
                return;
            }

            lock (_lock)
            {
                if (!_activity.TryGetValue(client.Id, out Activity a))
                {
                    a = new Activity();
                    _activity[client.Id] = a;
                }

                a.LastSeenUtc = _clock();
                a.Address = address ?? a.Address;
                if (countsAsRequest)
                {
                    a.Requests++;
                    a.LastRequest = what;
                    a.LastRequestUtc = a.LastSeenUtc;
                }
            }
        }

        /// <summary>Retire un SecondMonitor relié : son jeton est refusé dès maintenant.</summary>
        public bool Revoke(string clientId)
        {
            lock (_lock)
            {
                int removed = _file.Clients.RemoveAll(c => c.Id == clientId);
                _activity.Remove(clientId ?? string.Empty);
                if (removed > 0)
                {
                    Save();
                }

                return removed > 0;
            }
        }

        /// <summary>SecondMonitor reliés, pour l'écran d'état.</summary>
        public IReadOnlyList<PairedClientView> Clients()
        {
            lock (_lock)
            {
                DateTime now = _clock();
                return _file.Clients.Select(c =>
                {
                    _activity.TryGetValue(c.Id, out Activity a);
                    return new PairedClientView
                    {
                        Id = c.Id,
                        Name = c.Name,
                        PairedUtc = c.PairedUtc,
                        Address = a?.Address,
                        LastSeenUtc = a?.LastSeenUtc,
                        Online = a != null && now - a.LastSeenUtc <= OnlineDelay,
                        Requests = a?.Requests ?? 0,
                        LastRequest = a?.LastRequest,
                        LastRequestUtc = a?.LastRequestUtc,
                    };
                }).OrderByDescending(c => c.LastSeenUtc ?? c.PairedUtc).ToList();
            }
        }

        /// <summary>« 482913 », « 482 913 », « 482-913 » : seuls les chiffres comptent.</summary>
        public static string NormalizeCode(string code) => new((code ?? string.Empty).Where(char.IsAsciiDigit).Take(12).ToArray());

        /// <summary>Code présenté par groupes de trois chiffres (« 482 913 »).</summary>
        public static string FormatCode(string code) => code != null && code.Length == 6 ? $"{code[..3]} {code[3..]}" : code;

        private static bool IsValidCode(string code) => code != null && code.Length == 6 && code.All(char.IsAsciiDigit);

        private static string NewCodeValue() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

        private static bool CodesEqual(string a, string b) =>
            a.Length == b.Length && CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(a), Encoding.ASCII.GetBytes(b));

        private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        private static PairingFile Load(string path)
        {
            try
            {
                if (path != null && File.Exists(path))
                {
                    PairingFile file = JsonSerializer.Deserialize<PairingFile>(File.ReadAllText(path), Json);
                    if (file != null)
                    {
                        file.Clients ??= new List<PairedClient>();
                        file.Clients.RemoveAll(c => c == null || !ComputeAnnouncement.IsValidId(c.Id) || string.IsNullOrEmpty(c.Hash));
                        return file;
                    }
                }
            }
            catch (Exception)
            {
                // Fichier abîmé : on repart sans SecondMonitor relié (il suffira de les relier de nouveau).
            }

            return new PairingFile();
        }

        private void Save()
        {
            if (_path == null)
            {
                return;
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path) ?? ".");
                string temp = _path + ".tmp";
                File.WriteAllText(temp, JsonSerializer.Serialize(_file, Json));
                File.Move(temp, _path, overwrite: true);
            }
            catch (Exception)
            {
                // Dossier en lecture seule : l'appairage tient jusqu'au prochain démarrage.
            }
        }

        private sealed class PairingFile
        {
            public string Code { get; set; }

            public List<PairedClient> Clients { get; set; } = new();
        }

        private sealed class Activity
        {
            public string Address { get; set; }

            public DateTime LastSeenUtc { get; set; }

            public int Requests { get; set; }

            public string LastRequest { get; set; }

            public DateTime? LastRequestUtc { get; set; }
        }
    }

    public enum PairingStatus
    {
        Paired,
        WrongCode,
        Locked,
    }

    public sealed class PairingResult
    {
        public PairingStatus Status { get; set; }

        /// <summary>Jeton remis au SecondMonitor relié (seulement si <see cref="Status"/> vaut <see cref="PairingStatus.Paired"/>).</summary>
        public string Token { get; set; }

        public string ClientId { get; set; }

        public string ClientName { get; set; }

        /// <summary>Attente avant un nouvel essai (trop d'erreurs).</summary>
        public TimeSpan RetryAfter { get; set; }
    }

    /// <summary>SecondMonitor relié, tel qu'enregistré sur le serveur (empreinte du jeton, jamais le jeton).</summary>
    public sealed class PairedClient
    {
        public string Id { get; set; }

        public string Name { get; set; }

        /// <summary>Empreinte SHA-256 du jeton (base 64).</summary>
        public string Hash { get; set; }

        public DateTime PairedUtc { get; set; }
    }

    public sealed class PairedClientView
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public DateTime PairedUtc { get; set; }

        public string Address { get; set; }

        public DateTime? LastSeenUtc { get; set; }

        public bool Online { get; set; }

        public int Requests { get; set; }

        public string LastRequest { get; set; }

        public DateTime? LastRequestUtc { get; set; }
    }
}
