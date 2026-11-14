#nullable enable
namespace SecondMonitor.Hub.Tests;

using System.Net;
using System.Net.Sockets;
using System.Text;
using NUnit.Framework;
using SecondMonitor.Hub.Core.Compute;

/// <summary>
/// Serveur de calcul (« ordinateur 3 ») : lecture des annonces sur le réseau, découverte UDP, code d'appairage et jetons.
/// Le relais et l'essai à deux SecondMonitor sont dans <see cref="ServeurCalculBoutEnBoutTests"/>.
/// </summary>
[TestFixture]
public class ServeurCalculTests
{
    private static byte[] Bytes(string s) => Encoding.UTF8.GetBytes(s);

    // ───── Annonces ─────

    [Test]
    public void Annonce_EncodeeEtRelue()
    {
        var a = new ComputeAnnouncement { Id = "5f0c2a", Name = "PC RTX", Port = 8686, Models = 4, OllamaOk = true };
        byte[] data = a.Encode();
        Assert.That(data.Length, Is.LessThan(ComputeAnnouncement.MaxBytes));
        Assert.That(ComputeAnnouncement.TryParse(data, data.Length, out ComputeAnnouncement? read), Is.True);
        Assert.That(read!.Id, Is.EqualTo("5f0c2a"));
        Assert.That(read.Name, Is.EqualTo("PC RTX"));
        Assert.That(read.Port, Is.EqualTo(8686));
        Assert.That(read.Models, Is.EqualTo(4));
        Assert.That(read.OllamaOk, Is.True);
    }

    [Test]
    public void Annonce_FormatDuProtocole()
    {
        // Le format est le contrat entre deux versions de SecondMonitor : on le lit tel qu'écrit dans la documentation.
        byte[] data = Bytes("""{"sm":"secondmonitor-calcul","v":1,"id":"abc-123","nom":"Gros PC","port":9000,"modeles":2,"ollama":false}""");
        Assert.That(ComputeAnnouncement.TryParse(data, data.Length, out ComputeAnnouncement? a), Is.True);
        Assert.That((a!.Id, a.Name, a.Port, a.Models, a.OllamaOk), Is.EqualTo(("abc-123", "Gros PC", 9000, 2, false)));

        // Une version suivante peut ajouter des champs : on lit ceux qu'on connaît.
        data = Bytes("""{"sm":"secondmonitor-calcul","v":2,"id":"abc","nom":"X","port":8686,"gpu":"RTX 4090"}""");
        Assert.That(ComputeAnnouncement.TryParse(data, data.Length, out a), Is.True);
        Assert.That(a!.Models, Is.EqualTo(0));
    }

    [TestCase("")]
    [TestCase("bonjour")]
    [TestCase("[1,2,3]")]
    [TestCase("""{"sm":"autre-programme","v":1,"id":"a","nom":"X","port":8686}""")]
    [TestCase("""{"sm":"secondmonitor-calcul","v":0,"id":"a","nom":"X","port":8686}""")]
    [TestCase("""{"sm":"secondmonitor-calcul","v":1,"nom":"X","port":8686}""")]
    [TestCase("""{"sm":"secondmonitor-calcul","v":1,"id":"a b","nom":"X","port":8686}""")]
    [TestCase("""{"sm":"secondmonitor-calcul","v":1,"id":"a/../b","nom":"X","port":8686}""")]
    [TestCase("""{"sm":"secondmonitor-calcul","v":1,"id":"a","nom":"   ","port":8686}""")]
    [TestCase("""{"sm":"secondmonitor-calcul","v":1,"id":"a","nom":"X","port":0}""")]
    [TestCase("""{"sm":"secondmonitor-calcul","v":1,"id":"a","nom":"X","port":70000}""")]
    [TestCase("""{"sm":"secondmonitor-calcul","v":1,"id":"a","nom":"X","port":"8686"}""")]
    [TestCase("""{"sm":"secondmonitor-calcul","v":1,"id":"a","nom":"X","port":8686""")]
    public void Annonce_RefuseCeQuiNEnEstPasUne(string text)
    {
        byte[] data = Bytes(text);
        Assert.That(ComputeAnnouncement.TryParse(data, data.Length, out _), Is.False);
    }

    [Test]
    public void Annonce_TropLongueOuTronquee()
    {
        string longName = new('x', 2000);
        byte[] data = Bytes($$"""{"sm":"secondmonitor-calcul","v":1,"id":"a","nom":"{{longName}}","port":8686}""");
        Assert.That(ComputeAnnouncement.TryParse(data, data.Length, out _), Is.False, "au-delà de 1 Ko, ce n'est pas une annonce");

        byte[] ok = new ComputeAnnouncement { Id = "a", Name = "X", Port = 8686 }.Encode();
        Assert.That(ComputeAnnouncement.TryParse(ok, ok.Length - 5, out _), Is.False, "datagramme tronqué");
        Assert.That(ComputeAnnouncement.TryParse(ok, ok.Length + 5, out _), Is.False, "longueur incohérente");
    }

    [Test]
    public void Annonce_NomNettoye()
    {
        // Le nom devient un nom de machine dans « modèle@machine » : pas de « @ », pas de caractères de contrôle.
        byte[] data = Bytes("{\"sm\":\"secondmonitor-calcul\",\"v\":1,\"id\":\"a\",\"nom\":\"  PC\\u0007 de   jeu@salon \",\"port\":8686}");
        Assert.That(ComputeAnnouncement.TryParse(data, data.Length, out ComputeAnnouncement? a), Is.True);
        Assert.That(a!.Name, Is.EqualTo("PC de jeusalon"));
        Assert.That(ComputeAnnouncement.CleanName(new string('y', 100)).Length, Is.EqualTo(40));
    }

    // ───── Découverte ─────

    [Test]
    public void Decouverte_GardeLAdresseDuReseauEtOublieLesServeursMuets()
    {
        using var discovery = new ComputeDiscovery(0);
        byte[] data = new ComputeAnnouncement { Id = "srv1", Name = "Calcul", Port = 8686, Models = 3, OllamaOk = true }.Encode();
        DateTime t = new(2026, 11, 14, 20, 0, 0, DateTimeKind.Utc);

        Assert.That(discovery.Accept(data, data.Length, new IPEndPoint(IPAddress.Parse("192.168.1.40"), 50000), t), Is.True);
        // La même annonce arrive aussi par 127.0.0.1 (serveur sur le même ordinateur) : l'adresse du réseau reste.
        Assert.That(discovery.Accept(data, data.Length, new IPEndPoint(IPAddress.Loopback, 50000), t.AddSeconds(1)), Is.True);
        DiscoveredServer s = discovery.Servers(t.AddSeconds(2)).Single();
        Assert.That(s.Address, Is.EqualTo("192.168.1.40"));
        Assert.That(s.BaseUrl, Is.EqualTo("http://192.168.1.40:8686/calcul"));
        Assert.That(s.RecentAddresses(t.AddSeconds(2), TimeSpan.FromSeconds(30)), Is.EquivalentTo(new[] { "192.168.1.40", "127.0.0.1" }));

        // Plus rien du réseau depuis longtemps, seulement 127.0.0.1 : on prend 127.0.0.1.
        Assert.That(discovery.Accept(data, data.Length, new IPEndPoint(IPAddress.Loopback, 50000), t.AddSeconds(30)), Is.True);
        Assert.That(discovery.Servers(t.AddSeconds(30)).Single().Address, Is.EqualTo("127.0.0.1"));

        // Nouvelle adresse sur le réseau (box qui redistribue les adresses) : on la suit.
        discovery.Accept(data, data.Length, new IPEndPoint(IPAddress.Parse("192.168.1.77"), 50000), t.AddSeconds(31));
        Assert.That(discovery.Servers(t.AddSeconds(31)).Single().Address, Is.EqualTo("192.168.1.77"));

        // Un serveur qui ne s'annonce plus disparaît de la liste.
        Assert.That(discovery.Servers(t.AddSeconds(31) + ComputeDiscovery.Expiry + TimeSpan.FromSeconds(1)), Is.Empty);

        // Un datagramme quelconque ne change rien.
        byte[] junk = Bytes("M-SEARCH * HTTP/1.1");
        Assert.That(discovery.Accept(junk, junk.Length, new IPEndPoint(IPAddress.Parse("192.168.1.50"), 1900), t), Is.False);
    }

    [Test]
    public async Task Decouverte_EntendLAnnonceEnvoyeeSurLeReseau()
    {
        int port = FreeUdpPort();
        using var discovery = new ComputeDiscovery(port);
        discovery.Start();
        Assert.That(discovery.Error, Is.Null, discovery.Error);

        using var udp = new UdpClient(AddressFamily.InterNetwork) { EnableBroadcast = true };
        byte[] data = new ComputeAnnouncement { Id = "essai-udp", Name = "Serveur d'essai", Port = 8686, Models = 1, OllamaOk = true }.Encode();
        DiscoveredServer? found = null;
        for (int i = 0; i < 50 && found == null; i++)
        {
            Assert.That(ComputeAnnouncer.Send(udp, data, port), Is.GreaterThan(0));
            await Task.Delay(100);
            found = discovery.Servers().FirstOrDefault(s => s.Announcement.Id == "essai-udp");
        }

        Assert.That(found, Is.Not.Null, "annonce non reçue");
        Assert.That(found!.Announcement.Name, Is.EqualTo("Serveur d'essai"));
        Assert.That(IPAddress.TryParse(found.Address, out _), Is.True);
        Assert.That(ComputeAnnouncer.Targets(), Does.Contain(IPAddress.Broadcast).And.Contain(IPAddress.Loopback));
    }

    // ───── Code d'appairage et jetons ─────

    [Test]
    public void Jeton_AccepteSeulementSiLeCodeEstBon()
    {
        string dir = TempDir();
        try
        {
            var pairing = new ComputePairing(Path.Combine(dir, "serveur-calcul.json"));
            string code = pairing.Code;
            Assert.That(code, Does.Match("^[0-9]{6}$"));

            PairingResult wrong = pairing.TryPair(code == "000000" ? "111111" : "000000", "mac-salon", "Mac du salon", "192.168.1.20");
            Assert.That(wrong.Status, Is.EqualTo(PairingStatus.WrongCode));
            Assert.That(wrong.Token, Is.Null);

            // Le code peut être saisi avec une espace ou un tiret.
            PairingResult ok = pairing.TryPair($"{code[..3]} {code[3..]}", "mac-salon", "Mac du salon", "192.168.1.20");
            Assert.That(ok.Status, Is.EqualTo(PairingStatus.Paired));
            Assert.That(ok.Token, Has.Length.GreaterThanOrEqualTo(40));
            Assert.That(pairing.Code, Is.Not.EqualTo(code), "un code qui a servi ne relie pas un second appareil");
            Assert.That(pairing.TryPair(code, "intrus", "Intrus", "192.168.1.66").Status, Is.EqualTo(PairingStatus.WrongCode));

            Assert.That(pairing.Authenticate($"Bearer {ok.Token}")?.Name, Is.EqualTo("Mac du salon"));
            Assert.That(pairing.Authenticate($"bearer {ok.Token}"), Is.Not.Null);
            Assert.That(pairing.Authenticate(null), Is.Null);
            Assert.That(pairing.Authenticate(""), Is.Null);
            Assert.That(pairing.Authenticate(ok.Token), Is.Null, "sans « Bearer »");
            Assert.That(pairing.Authenticate("Bearer "), Is.Null);
            Assert.That(pairing.Authenticate("Bearer " + ok.Token![..^1] + (ok.Token[^1] == 'A' ? 'B' : 'A')), Is.Null, "un caractère de différence");
            Assert.That(pairing.Authenticate("Basic " + ok.Token), Is.Null);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Test]
    public void Jeton_RetireOuRemplace()
    {
        string dir = TempDir();
        try
        {
            string path = Path.Combine(dir, "serveur-calcul.json");
            var pairing = new ComputePairing(path);
            string first = pairing.TryPair(pairing.Code, "mac-salon", "Mac du salon", "192.168.1.20").Token!;
            string other = pairing.TryPair(pairing.Code, "macbook", "MacBook", "192.168.1.21").Token!;

            // Le même SecondMonitor se relie de nouveau : l'ancien jeton ne marche plus, le nouveau oui ; l'autre n'est pas touché.
            string second = pairing.TryPair(pairing.Code, "mac-salon", "Mac du salon", "192.168.1.20").Token!;
            Assert.That(pairing.Authenticate("Bearer " + first), Is.Null);
            Assert.That(pairing.Authenticate("Bearer " + second), Is.Not.Null);
            Assert.That(pairing.Authenticate("Bearer " + other), Is.Not.Null);
            Assert.That(pairing.Clients().Select(c => c.Name), Is.EquivalentTo(new[] { "Mac du salon", "MacBook" }));

            // Le serveur garde l'appairage après un redémarrage, sans jamais écrire le jeton lui-même.
            var reloaded = new ComputePairing(path);
            Assert.That(reloaded.Authenticate("Bearer " + second)?.Id, Is.EqualTo("mac-salon"));
            Assert.That(reloaded.Code, Is.EqualTo(pairing.Code));
            string file = File.ReadAllText(path);
            Assert.That(file, Does.Not.Contain(second).And.Not.Contain(other));

            Assert.That(reloaded.Revoke("mac-salon"), Is.True);
            Assert.That(reloaded.Authenticate("Bearer " + second), Is.Null);
            Assert.That(reloaded.Authenticate("Bearer " + other), Is.Not.Null);
            Assert.That(new ComputePairing(path).Authenticate("Bearer " + second), Is.Null, "retrait enregistré");
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Test]
    public void Code_ChangeApresCinqErreursEtSeBloqueApresDix()
    {
        string dir = TempDir();
        try
        {
            DateTime now = new(2026, 11, 14, 20, 0, 0, DateTimeKind.Utc);
            var pairing = new ComputePairing(Path.Combine(dir, "serveur-calcul.json"), () => now);
            int renewed = 0;
            pairing.CodeChanged += _ => renewed++;
            string Wrong() => pairing.Code == "000000" ? "999999" : "000000";

            string code = pairing.Code;
            for (int i = 0; i < ComputePairing.MaxFailuresPerCode; i++)
            {
                Assert.That(pairing.TryPair(Wrong(), "x", "X", "192.168.1.66").Status, Is.EqualTo(PairingStatus.WrongCode));
            }

            Assert.That(renewed, Is.EqualTo(1));
            Assert.That(pairing.TryPair(code, "x", "X", "192.168.1.66").Status, Is.EqualTo(PairingStatus.WrongCode), "l'ancien code ne marche plus");

            for (int i = 0; i < 4; i++)
            {
                pairing.TryPair(Wrong(), "x", "X", "192.168.1.66");
            }

            // 10 erreurs en 10 minutes : plus aucun essai, même avec le bon code.
            PairingResult locked = pairing.TryPair(pairing.Code, "x", "X", "192.168.1.66");
            Assert.That(locked.Status, Is.EqualTo(PairingStatus.Locked));
            Assert.That(locked.RetryAfter, Is.GreaterThan(TimeSpan.Zero).And.LessThanOrEqualTo(ComputePairing.FailureWindow));

            now += ComputePairing.FailureWindow + TimeSpan.FromSeconds(1);
            Assert.That(pairing.TryPair(pairing.Code, "mac", "Mac", "192.168.1.20").Status, Is.EqualTo(PairingStatus.Paired));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Test]
    public void Etat_EnLigneSelonLeDernierSigneDeVie()
    {
        DateTime now = new(2026, 11, 14, 20, 0, 0, DateTimeKind.Utc);
        var pairing = new ComputePairing(null!, () => now);
        PairedClient client = pairing.Authenticate("Bearer " + pairing.TryPair(pairing.Code, "mac", "Mac du salon", "192.168.1.20").Token)!;
        pairing.Touch(client, "192.168.1.20", "conversation · qwen3:32b", countsAsRequest: true);
        pairing.Touch(client, "192.168.1.20", "signe de vie", countsAsRequest: false);
        PairedClientView view = pairing.Clients().Single();
        Assert.That(view.Online, Is.True);
        Assert.That(view.Requests, Is.EqualTo(1));
        Assert.That(view.LastRequest, Is.EqualTo("conversation · qwen3:32b"));

        now += ComputePairing.OnlineDelay + TimeSpan.FromSeconds(1);
        Assert.That(pairing.Clients().Single().Online, Is.False);
    }

    internal static string TempDir()
    {
        string dir = Path.Combine(Path.GetTempPath(), "sm-calcul-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    internal static int FreeUdpPort()
    {
        using var udp = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
        return ((IPEndPoint)udp.Client.LocalEndPoint!).Port;
    }
}
