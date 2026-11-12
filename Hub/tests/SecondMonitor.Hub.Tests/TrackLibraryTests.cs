namespace SecondMonitor.Hub.Tests;

using NUnit.Framework;
using SecondMonitor.Hub.Core.Track;

[TestFixture]
public class TrackLibraryTests
{
    [TestCase("Silverstone", "", 5891, "gb-1948")]
    [TestCase("Circuit de Spa-Francorchamps", "", 7004, "be-1925")]
    [TestCase("Spa-Francorchamps", "", 7000, "be-1925")]
    [TestCase("Autodromo Nazionale Monza", "", 5793, "it-1922")]
    [TestCase("Melbourne Grand Prix Circuit", "", 5278, "au-1953")]
    [TestCase("Algarve International Circuit", "", 4653, "pt-2008")]
    [TestCase("Autódromo José Carlos Pace", "", 4309, "br-1940")]
    [TestCase("Lusail International Circuit", "", 5419, "qa-2004")]
    public void FindsTheRealCircuit(string game, string layout, double length, string id)
    {
        Assert.That(TrackLibrary.Find(game, layout, length)?.Id, Is.EqualTo(id));
    }

    [TestCase("Silverstone Short", "", 1800)]
    [TestCase("Bahrain International Circuit - Short", "", 3543)]
    [TestCase("Circuit de la Sarthe", "", 13626)]
    [TestCase("Démonstration", "", 5200)]
    public void NeverGuessesAWrongLayout(string game, string layout, double length)
    {
        Assert.That(TrackLibrary.Find(game, layout, length), Is.Null);
    }

    [Test]
    public void Outlines_Have400Points_StartingOnTheLine()
    {
        LibraryTrack t = TrackLibrary.Find("Silverstone", "", 5891);
        Assert.That(t.Points, Has.Count.EqualTo(400));
        Assert.That(TrackLibrary.Count, Is.GreaterThanOrEqualTo(40));
    }
}
