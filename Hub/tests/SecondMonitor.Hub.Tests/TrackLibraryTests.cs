namespace SecondMonitor.Hub.Tests;

using System;
using System.Linq;
using NUnit.Framework;
using SecondMonitor.Hub.Core.Track;

[TestFixture]
public class TrackLibraryTests
{
    // Noms tels que les jeux les envoient (nom du circuit, configuration, longueur du tour).
    [TestCase("Silverstone", "", 5891, "gb-1948")]
    [TestCase("Circuit de Spa-Francorchamps", "", 7004, "be-1925")]
    [TestCase("Spa-Francorchamps", "", 7000, "be-1925")]
    [TestCase("Autodromo Nazionale Monza", "", 5793, "it-1922")]
    [TestCase("Melbourne Grand Prix Circuit", "", 5278, "au-1953")]
    [TestCase("Algarve International Circuit", "", 4653, "pt-2008")]
    [TestCase("Autódromo José Carlos Pace", "", 4309, "br-1940")]
    [TestCase("Lusail International Circuit", "", 5419, "qa-2004")]
    // F1 24 : la variante courte de Bahreïn est le circuit extérieur.
    [TestCase("Bahrain International Circuit - Short", "", 3543, "bh-outer")]
    // ACC (noms internes, « _ » = espace)
    [TestCase("brands_hatch", "", 3908, "gb-brands-hatch-gp")]
    [TestCase("nurburgring_24h", "", 25378, "de-nurburgring-24h")]
    [TestCase("nurburgring", "", 5137, "de-1927")]
    [TestCase("valencia", "", 4005, "es-valencia")]
    [TestCase("oulton_park", "", 4307, "gb-oulton-international")]
    [TestCase("snetterton", "", 4779, "gb-snetterton-300")]
    [TestCase("laguna_seca", "", 3602, "us-laguna-seca")]
    [TestCase("indianapolis", "", 3925, "us-indianapolis-road")]
    [TestCase("zolder", "", 4011, "be-zolder")]
    [TestCase("misano", "", 4226, "it-misano")]
    // Assetto Corsa (« ks_ » retiré par le connecteur) et configurations
    [TestCase("brands hatch", "indy", 1929, "gb-brands-hatch-indy")]
    [TestCase("nordschleife", "", 20832, "de-nordschleife")]
    [TestCase("nordschleife", "endurance", 25378, "de-nurburgring-24h")]
    [TestCase("nurburgring", "sprint", 3629, "de-nurburgring-sprint")]
    [TestCase("red bull ring", "national", 2336, "at-red-bull-ring-national")]
    [TestCase("vallelunga", "", 4085, "it-vallelunga")]
    [TestCase("magione", "", 2507, "it-magione")]
    // Le Mans Ultimate / rFactor 2
    [TestCase("Circuit de la Sarthe", "", 13626, "fr-le-mans")]
    [TestCase("Sebring International Raceway", "", 6019, "us-sebring")]
    [TestCase("Fuji Speedway", "", 4563, "jp-fuji")]
    [TestCase("Bahrain Endurance Circuit", "", 6299, "bh-endurance")]
    [TestCase("Bahrain Outer Circuit", "", 3543, "bh-outer")]
    // Automobilista 2 / Project CARS 2 : nom + variante
    [TestCase("Interlagos", "GP", 4309, "br-1940")]
    [TestCase("Curitiba", "", 3695, "br-curitiba")]
    [TestCase("Goiania", "", 3835, "br-goiania")]
    [TestCase("Donington Park", "National", 3149, "gb-donington-national")]
    [TestCase("Donington Park", "Grand Prix", 4020, "gb-donington-gp")]
    [TestCase("Oulton Park", "Island", 3621, "gb-oulton-island")]
    [TestCase("Oulton Park", "Fosters", 2690, "gb-oulton-fosters")]
    [TestCase("Snetterton", "200 Circuit", 3213, "gb-snetterton-200")]
    [TestCase("Silverstone", "International", 2979, "gb-silverstone-international")]
    [TestCase("Mazda Raceway", "Laguna Seca", 3602, "us-laguna-seca")]
    [TestCase("Watkins Glen International", "Grand Prix", 5430, "us-1956")]
    [TestCase("Watkins Glen International", "Short Circuit", 3943, "us-watkins-glen-short")]
    [TestCase("Daytona International Speedway", "Road Course", 5729, "us-daytona-road")]
    [TestCase("Daytona International Speedway", "Tri-Oval", 4023, "us-daytona-oval")]
    [TestCase("Le Mans", "Le Circuit Bugatti", 4185, "fr-le-mans-bugatti")]
    [TestCase("Le Mans", "Circuit des 24 Heures du Mans", 13626, "fr-le-mans")]
    // RaceRoom
    [TestCase("Hockenheimring", "National", 3692, "de-hockenheim-national")]
    [TestCase("Hockenheimring", "Grand Prix", 4574, "de-1932")]
    [TestCase("Motorsport Arena Oschersleben", "Grand Prix", 3696, "de-oschersleben")]
    [TestCase("Anderstorp Raceway", "Grand Prix", 4025, "se-anderstorp")]
    [TestCase("RedBull Ring Spielberg", "Grand Prix Circuit", 4318, "at-1969")]
    [TestCase("Slovakia Ring", "Grand Prix", 5922, "sk-slovakia-ring")]
    public void FindsTheRealCircuit(string game, string layout, double length, string id)
    {
        Assert.That(TrackLibrary.Find(game, layout, length)?.Id, Is.EqualTo(id));
    }

    [TestCase("Silverstone Short", "", 1800)]
    [TestCase("Circuit de la Sarthe", "", 4185)]
    [TestCase("Démonstration", "", 5200)]
    [TestCase("Brands Hatch", "Indy", 3908)] // jamais le tracé GP pour une variante annoncée
    [TestCase("Oulton Park", "Island", 4307)]
    [TestCase("Bahrain International Circuit", "Paddock", 3705)]
    [TestCase("Sebring International Raceway", "School", 3200)]
    [TestCase("Hockenheimring", "Short", 4574)]
    [TestCase("Interlagos", "Historic", 7960)]
    [TestCase("nurburgring", "sprint", 5148)]
    public void NeverGuessesAWrongLayout(string game, string layout, double length)
    {
        Assert.That(TrackLibrary.Find(game, layout, length), Is.Null);
    }

    [Test]
    public void ShortLayoutNeverGetsTheLongOne_AndViceVersa()
    {
        Assert.That(TrackLibrary.Find("Silverstone", "National", 2638)?.Id, Is.Not.EqualTo("gb-1948"));
        Assert.That(TrackLibrary.Find("Silverstone", "", 5891)?.Id, Is.EqualTo("gb-1948"));
        Assert.That(TrackLibrary.Find("nurburgring", "", 5148)?.Id, Is.EqualTo("de-1927"));
    }

    [Test]
    public void Library_Covers120CircuitsAndVariants()
    {
        Assert.That(TrackLibrary.Count, Is.GreaterThanOrEqualTo(120));
    }

    [Test]
    public void Outlines_Have400Points_StartingOnTheLine()
    {
        LibraryTrack t = TrackLibrary.Find("Silverstone", "", 5891);
        Assert.That(t.Points, Has.Count.EqualTo(400));
        Assert.That(t.Confidence, Is.GreaterThanOrEqualTo(LibraryTrack.DisplayConfidence));
    }

    [Test]
    public void EveryOutline_IsClosedAndMatchesItsOfficialLength()
    {
        foreach (string id in new[] { "brands_hatch", "Circuit de la Sarthe", "nordschleife", "Fuji Speedway", "Curitiba" })
        {
            LibraryTrack t = TrackLibrary.Find(id, string.Empty, 0);
            Assert.That(t, Is.Not.Null, id);
            double perimeter = t.Points.Select((p, i) => Distance(p, t.Points[(i + 1) % t.Points.Count])).Sum();
            Assert.That(Math.Abs(perimeter - t.Length) / t.Length, Is.LessThan(0.06), $"{t.Id} : {perimeter:0} m pour {t.Length} m");
            Assert.That(Distance(t.Points[0], t.Points[^1]), Is.LessThan(t.Length / 100), t.Id);
        }
    }

    private static double Distance(double[] a, double[] b) => Math.Sqrt(Math.Pow(a[0] - b[0], 2) + Math.Pow(a[1] - b[1], 2));
}
