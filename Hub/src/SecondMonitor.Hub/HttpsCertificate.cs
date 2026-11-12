namespace SecondMonitor.Hub;

using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

/// <summary>
/// Certificat auto-signé pour servir l'interface en HTTPS sur le réseau local.
/// Les navigateurs n'autorisent le micro (appuyer-pour-parler) et les manettes/volants (Gamepad API) que sur
/// http://localhost ou en HTTPS : sans lui, le PC de jeu, un iPad ou un téléphone ne pourraient pas parler à l'IA.
/// Le navigateur affiche un avertissement la première fois (certificat non reconnu) : il suffit de l'accepter.
/// </summary>
public static class HttpsCertificate
{
    private const string Password = "secondmonitor-hub";

    public static X509Certificate2? LoadOrCreate(string dataDir, ILogger? logger = null)
    {
        try
        {
            string path = Path.Combine(dataDir, "https-certificate.pfx");
            var wanted = LocalNames();
            if (File.Exists(path))
            {
                X509Certificate2 existing = X509CertificateLoader.LoadPkcs12FromFile(path, Password, X509KeyStorageFlags.Exportable);
                string sans = string.Join(",", existing.Extensions.OfType<X509SubjectAlternativeNameExtension>().SelectMany(e => e.EnumerateIPAddresses().Select(a => a.ToString()).Concat(e.EnumerateDnsNames())));
                bool covers = wanted.Ips.All(ip => sans.Contains(ip.ToString(), StringComparison.Ordinal));
                if (covers && existing.NotAfter > DateTime.Now.AddDays(30))
                {
                    return existing;
                }
            }

            using RSA rsa = RSA.Create(2048);
            var request = new CertificateRequest("CN=SecondMonitor Hub", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var san = new SubjectAlternativeNameBuilder();
            foreach (string dns in wanted.Dns)
            {
                san.AddDnsName(dns);
            }

            foreach (IPAddress ip in wanted.Ips)
            {
                san.AddIpAddress(ip);
            }

            request.CertificateExtensions.Add(san.Build());
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, false));
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false));
            using X509Certificate2 created = request.CreateSelfSigned(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddYears(2));
            byte[] pfx = created.Export(X509ContentType.Pfx, Password);
            Directory.CreateDirectory(dataDir);
            File.WriteAllBytes(path, pfx);
            return X509CertificateLoader.LoadPkcs12(pfx, Password, X509KeyStorageFlags.Exportable);
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "HTTPS indisponible (certificat)");
            return null;
        }
    }

    private static (List<string> Dns, List<IPAddress> Ips) LocalNames()
    {
        var dns = new List<string> { "localhost" };
        string host = Dns.GetHostName();
        if (!string.IsNullOrWhiteSpace(host))
        {
            dns.Add(host);
            if (!host.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
            {
                dns.Add(host + ".local");
            }
        }

        var ips = new List<IPAddress> { IPAddress.Loopback };
        foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces().Where(n => n.OperationalStatus == OperationalStatus.Up))
        {
            ips.AddRange(ni.GetIPProperties().UnicastAddresses.Select(a => a.Address).Where(a => a.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a)));
        }

        return (dns.Distinct().ToList(), ips.Distinct().ToList());
    }
}
