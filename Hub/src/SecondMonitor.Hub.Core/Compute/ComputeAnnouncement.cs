namespace SecondMonitor.Hub.Core.Compute
{
    using System;
    using System.Linq;
    using System.Text;
    using System.Text.Json;

    /// <summary>
    /// Annonce d'un serveur de calcul SecondMonitor sur le réseau local : un datagramme UDP (JSON UTF-8, quelques dizaines d'octets)
    /// envoyé toutes les 2 secondes en diffusion sur le port <see cref="DefaultPort"/> :
    /// <code>{"sm":"secondmonitor-calcul","v":1,"id":"5f0c…","nom":"PC-RTX","port":8686,"modeles":4,"ollama":true}</code>
    /// L'adresse du serveur est celle d'où vient le datagramme (elle n'est pas dans l'annonce). Rien de secret n'y figure :
    /// pour se servir du serveur, il faut le code d'appairage affiché sur son écran.
    /// </summary>
    public sealed class ComputeAnnouncement
    {
        /// <summary>Marque des annonces de SecondMonitor (tout autre datagramme reçu sur le port est ignoré).</summary>
        public const string Kind = "secondmonitor-calcul";

        public const int ProtocolVersion = 1;

        /// <summary>Port UDP des annonces (réglable : « calcul.discoveryPort » dans settings.json).</summary>
        public const int DefaultPort = 8689;

        /// <summary>Au-delà, le datagramme n'est pas une annonce.</summary>
        public const int MaxBytes = 1024;

        /// <summary>Identifiant stable du serveur (suit le serveur quand son adresse change).</summary>
        public string Id { get; set; }

        /// <summary>Nom affiché (nom de l'ordinateur ou nom choisi sur le serveur).</summary>
        public string Name { get; set; }

        /// <summary>Port HTTP de SecondMonitor sur le serveur (le relais vers Ollama est sous « /calcul »).</summary>
        public int Port { get; set; }

        /// <summary>Nombre de modèles installés dans l'Ollama du serveur.</summary>
        public int Models { get; set; }

        /// <summary>L'Ollama du serveur répond.</summary>
        public bool OllamaOk { get; set; }

        public byte[] Encode()
        {
            string json = JsonSerializer.Serialize(new
            {
                sm = Kind,
                v = ProtocolVersion,
                id = Id,
                nom = CleanName(Name),
                port = Port,
                modeles = Math.Max(0, Models),
                ollama = OllamaOk,
            });
            return Encoding.UTF8.GetBytes(json);
        }

        /// <summary>Lit une annonce ; faux pour tout ce qui n'en est pas une (autre programme, datagramme abîmé, valeurs hors limites).</summary>
        public static bool TryParse(byte[] data, int length, out ComputeAnnouncement announcement)
        {
            announcement = null;
            if (data == null || length <= 0 || length > MaxBytes || length > data.Length)
            {
                return false;
            }

            try
            {
                using JsonDocument doc = JsonDocument.Parse(new ReadOnlyMemory<byte>(data, 0, length));
                JsonElement root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object
                    || !root.TryGetProperty("sm", out JsonElement sm) || sm.ValueKind != JsonValueKind.String || sm.GetString() != Kind
                    || !root.TryGetProperty("v", out JsonElement v) || v.ValueKind != JsonValueKind.Number || !v.TryGetInt32(out int version) || version < 1
                    || !root.TryGetProperty("id", out JsonElement id) || id.ValueKind != JsonValueKind.String || !IsValidId(id.GetString())
                    || !root.TryGetProperty("nom", out JsonElement nom) || nom.ValueKind != JsonValueKind.String
                    || !root.TryGetProperty("port", out JsonElement port) || port.ValueKind != JsonValueKind.Number || !port.TryGetInt32(out int portNumber)
                    || portNumber < 1 || portNumber > 65535)
                {
                    return false;
                }

                string name = CleanName(nom.GetString());
                if (name.Length == 0)
                {
                    return false;
                }

                int models = root.TryGetProperty("modeles", out JsonElement m) && m.ValueKind == JsonValueKind.Number && m.TryGetInt32(out int count) ? Math.Clamp(count, 0, 10000) : 0;
                bool ollama = root.TryGetProperty("ollama", out JsonElement o) && o.ValueKind == JsonValueKind.True;
                announcement = new ComputeAnnouncement { Id = id.GetString(), Name = name, Port = portNumber, Models = models, OllamaOk = ollama };
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        /// <summary>Identifiant : 1 à 64 lettres, chiffres ou tirets.</summary>
        public static bool IsValidId(string id) =>
            !string.IsNullOrEmpty(id) && id.Length <= 64 && id.All(c => c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '-');

        /// <summary>
        /// Nom présentable et utilisable comme nom de machine dans « modèle@machine » : sans « @ », sans caractères de contrôle,
        /// espaces resserrés, 40 caractères au plus.
        /// </summary>
        public static string CleanName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            var sb = new StringBuilder(name.Length);
            bool space = false;
            foreach (char c in name.Trim())
            {
                if (c == '@' || char.IsControl(c))
                {
                    continue;
                }

                if (char.IsWhiteSpace(c))
                {
                    space = sb.Length > 0;
                    continue;
                }

                if (space)
                {
                    sb.Append(' ');
                    space = false;
                }

                sb.Append(c);
                if (sb.Length >= 40)
                {
                    break;
                }
            }

            return sb.ToString();
        }
    }
}
