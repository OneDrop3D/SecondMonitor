namespace SecondMonitor.Hub.Tests;

using NUnit.Framework;
using SecondMonitor.Hub.Core.Ai;

/// <summary>Texte envoyé à la synthèse vocale : tous les chiffres et symboles en toutes lettres.</summary>
[TestFixture]
public class SpeechTests
{
    [TestCase(0, "zéro")]
    [TestCase(1, "un")]
    [TestCase(17, "dix-sept")]
    [TestCase(21, "vingt et un")]
    [TestCase(71, "soixante et onze")]
    [TestCase(77, "soixante-dix-sept")]
    [TestCase(80, "quatre-vingts")]
    [TestCase(81, "quatre-vingt-un")]
    [TestCase(91, "quatre-vingt-onze")]
    [TestCase(100, "cent")]
    [TestCase(200, "deux cents")]
    [TestCase(205, "deux cent cinq")]
    [TestCase(1000, "mille")]
    [TestCase(3600, "trois mille six cents")]
    public void Numbers(long n, string expected)
    {
        Assert.That(FrenchSpeech.Number(n), Is.EqualTo(expected));
    }

    [TestCase("Rentrez au stand au tour 24, dans 6 tours, soit environ 11 minutes.", "Rentrez au stand au tour vingt-quatre. dans six tours, soit environ onze minutes.")]
    [TestCase("Votre meilleur tour : 2:01.803.", "Votre meilleur tour. deux minutes, une seconde huit.")]
    [TestCase("La voiture derrière vous, numéro 52, est 2,3 secondes derrière.", "La voiture derrière vous, numéro cinquante-deux. est deux virgule trois secondes derrière.")]
    [TestCase("Delta +0,57 s", "Delta plus zéro virgule cinquante-sept secondes")]
    [TestCase("Avant gauche 80 °C, avant droit 90, arrière gauche 50, arrière droit 55.", "Avant gauche quatre-vingts degrés. avant droit quatre-vingt-dix. arrière gauche cinquante. arrière droit cinquante-cinq.")]
    [TestCase("Box dans 2 tours (T11), #52 à 1,05 s.", "Box dans deux tours (tour onze), numéro cinquante-deux à un virgule zéro cinq secondes.")]
    [TestCase("Pluie à 18 %, piste 34 °C, 205 km/h.", "Pluie à dix-huit pour cent. piste trente-quatre degrés. deux cent cinq kilomètres heure.")]
    [TestCase("Il vous reste 9,9 tours d'essence : 31,7 litres.", "Il vous reste neuf virgule neuf tours d'essence. trente et un virgule sept litres.")]
    [TestCase("Vous êtes P1 dans votre catégorie.", "Vous êtes P un dans votre catégorie.")]
    [TestCase("Il revient de 1 seconde par tour.", "Il revient de une seconde par tour.")]
    [TestCase("Votre rythme baisse de −0,02 seconde par tour.", "Votre rythme baisse de moins zéro virgule zéro deux seconde par tour.")]
    public void Sentences(string text, string expected)
    {
        Assert.That(FrenchSpeech.ToSpeech(text), Is.EqualTo(expected));
    }

    [Test]
    public void NoDigitsLeft()
    {
        string s = FrenchSpeech.ToSpeech("Vmax par ligne droite (vous / meilleur de la catégorie) : Ligne droite 1 199 / 201, Ligne droite 4 205 / 207. Usure 3,67 %/tour, 1,69 bar.");
        Assert.That(s, Does.Not.Match(@"\d"));
    }
}
