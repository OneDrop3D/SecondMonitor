namespace SecondMonitor.Hub.Tests;

using NUnit.Framework;
using SecondMonitor.Hub.Core.Race;
using SecondMonitor.Hub.Core.Track;

[TestFixture]
public class TrackPlacesTests
{
    private static TrackSegment Corner(double apex) => new() { Kind = "corner", Start = apex - 60, End = apex + 60, Apex = apex };

    private static TrackSegment Straight(double start, double end) => new() { Kind = "straight", Start = start, End = end, Apex = end - 50 };

    // Distances du Mans dans Le Mans Ultimate (corde des virages).
    [TestCase(560, "Chicane Dunlop")]
    [TestCase(1600, "Tertre Rouge")]
    [TestCase(3900, "1re chicane")]
    [TestCase(5900, "2e chicane")]
    [TestCase(7520, "Mulsanne")]
    [TestCase(9650, "Indianapolis")]
    [TestCase(10050, "Arnage")]
    [TestCase(11800, "Virages Porsche")]
    [TestCase(13450, "Chicane Ford")]
    public void Virages_du_Mans_nommes(double apex, string place)
    {
        var s = Corner(apex);
        TrackPlaces.Apply(new[] { s }, "Circuit de la Sarthe", 13624);
        Assert.That(s.Place, Is.EqualTo(place));
    }

    [Test]
    public void Lignes_droites_nommees_par_leur_milieu_meme_a_cheval_sur_la_ligne()
    {
        var stands = Straight(13560, 380);
        var hunaudieres = Straight(2500, 3750);
        TrackPlaces.Apply(new[] { stands, hunaudieres }, "Circuit de la Sarthe", 13624);
        Assert.That(stands.Place, Is.EqualTo("Ligne droite des stands"));
        Assert.That(hunaudieres.Place, Is.EqualTo("Hunaudières"));
    }

    [Test]
    public void Autre_circuit_ou_autre_trace_sans_nom()
    {
        var other = Corner(7520);
        var bugatti = Corner(1600);
        var outside = Corner(2900);
        TrackPlaces.Apply(new[] { other }, "Circuit de démonstration", 13624);
        TrackPlaces.Apply(new[] { bugatti }, "Circuit de la Sarthe", 4185);
        TrackPlaces.Apply(new[] { outside }, "Circuit de la Sarthe", 13624);
        Assert.That(other.Place, Is.Null);
        Assert.That(bugatti.Place, Is.Null);
        Assert.That(outside.Place, Is.Null, "pas de virage au milieu des Hunaudières : la zone garde son numéro");
    }
}
