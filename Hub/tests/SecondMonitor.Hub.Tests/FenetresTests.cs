namespace SecondMonitor.Hub.Tests;

using System.IO;
using NUnit.Framework;
using SecondMonitor.Hub.Core.Display;

/// <summary>Plusieurs fenêtres (une par écran) : placement sur l'autre écran, place retrouvée au lancement, fenetres.json.</summary>
[TestFixture]
public class FenetresTests
{
    // MacBook (écran principal, 1728 × 1117, barre des menus 37 px) et écran externe 2560 × 1440 à sa droite, haut aligné.
    private static readonly ScreenArea MacBook = new(new ScreenRect(0, 0, 1728, 1117), new ScreenRect(0, 37, 1728, 1010));
    private static readonly ScreenArea Externe = new(new ScreenRect(1728, 0, 2560, 1440), new ScreenRect(1728, 25, 2560, 1415));
    private static readonly ScreenRect FenetrePrincipale = new(114, 88, 1500, 940);

    [Test]
    public void Mac_les_ecrans_Cocoa_sont_convertis_depuis_le_haut_de_l_ecran_principal()
    {
        // Cocoa : origine en bas à gauche de l'écran principal. Écran externe de 1440 px, haut aligné sur celui du MacBook.
        Assert.That(WindowPlacement.FromCocoa(new ScreenRect(1728, -323, 2560, 1440), 1117), Is.EqualTo(new ScreenRect(1728, 0, 2560, 1440)));
        // Surface utile du MacBook (Dock de 70 px en bas, barre des menus de 37 px en haut).
        Assert.That(WindowPlacement.FromCocoa(new ScreenRect(0, 70, 1728, 1010), 1117), Is.EqualTo(new ScreenRect(0, 37, 1728, 1010)));
        // Écran externe au-dessus du MacBook : y négatif.
        Assert.That(WindowPlacement.FromCocoa(new ScreenRect(0, 1117, 1920, 1080), 1117), Is.EqualTo(new ScreenRect(0, -1080, 1920, 1080)));
    }

    [Test]
    public void Nouvelle_fenetre_sur_l_autre_ecran_qu_elle_remplit()
    {
        ScreenRect place = WindowPlacement.PlaceNew(FenetrePrincipale, new ScreenRect[0], new[] { MacBook, Externe }, out bool autre);
        Assert.That(autre, Is.True);
        Assert.That(place, Is.EqualTo(Externe.Work));
    }

    [Test]
    public void Fenetre_principale_sur_l_ecran_externe_la_nouvelle_va_sur_le_MacBook()
    {
        var principale = new ScreenRect(2000, 100, 1500, 940);
        ScreenRect place = WindowPlacement.PlaceNew(principale, new ScreenRect[0], new[] { MacBook, Externe }, out bool autre);
        Assert.That(autre, Is.True);
        Assert.That(place, Is.EqualTo(MacBook.Work));
    }

    [Test]
    public void Trois_ecrans_la_troisieme_fenetre_va_sur_l_ecran_encore_libre()
    {
        var troisieme = new ScreenArea(new ScreenRect(-1920, 0, 1920, 1080), new ScreenRect(-1920, 0, 1920, 1040));
        ScreenRect deuxieme = Externe.Work;
        ScreenRect place = WindowPlacement.PlaceNew(FenetrePrincipale, new[] { deuxieme }, new[] { MacBook, Externe, troisieme }, out bool autre);
        Assert.That(autre, Is.True);
        Assert.That(place, Is.EqualTo(troisieme.Work));
    }

    [Test]
    public void Un_seul_ecran_la_fenetre_s_ouvre_decalee_dans_l_ecran()
    {
        ScreenRect place = WindowPlacement.PlaceNew(FenetrePrincipale, new ScreenRect[0], new[] { MacBook }, out bool autre);
        Assert.That(autre, Is.False);
        Assert.That(place, Is.Not.EqualTo(FenetrePrincipale), "pas exactement par-dessus la principale");
        AssertInside(place, MacBook.Work);
        Assert.That(place.Width, Is.GreaterThanOrEqualTo(WindowPlacement.MinWidth));
        Assert.That(place.Height, Is.GreaterThanOrEqualTo(WindowPlacement.MinHeight));
    }

    [Test]
    public void Un_seul_ecran_occupe_par_deux_fenetres_la_suivante_reste_dans_l_ecran()
    {
        ScreenRect premiere = WindowPlacement.PlaceNew(FenetrePrincipale, new ScreenRect[0], new[] { MacBook }, out _);
        ScreenRect seconde = WindowPlacement.PlaceNew(FenetrePrincipale, new[] { premiere }, new[] { MacBook }, out bool autre);
        Assert.That(autre, Is.False);
        Assert.That(seconde, Is.Not.EqualTo(premiere));
        AssertInside(seconde, MacBook.Work);
    }

    [Test]
    public void Place_retrouvee_si_l_ecran_est_toujours_la()
    {
        var gardee = new ScreenRect(1900, 120, 1600, 1000);
        Assert.That(WindowPlacement.Restore(gardee, Externe.Area, new[] { MacBook, Externe }), Is.EqualTo(gardee));
    }

    [Test]
    public void Place_ramenee_dans_l_ecran_si_elle_depasse()
    {
        var gardee = new ScreenRect(3800, 10, 1000, 2000);
        ScreenRect? place = WindowPlacement.Restore(gardee, Externe.Area, new[] { MacBook, Externe });
        Assert.That(place, Is.Not.Null);
        AssertInside(place.Value, Externe.Work);
    }

    [Test]
    public void Ecran_debranche_pas_de_place_retenue()
    {
        var gardee = new ScreenRect(1900, 120, 1600, 1000);
        Assert.That(WindowPlacement.Restore(gardee, Externe.Area, new[] { MacBook }), Is.Null);
    }

    [Test]
    public void Ecran_deplace_a_gauche_la_fenetre_le_suit()
    {
        // Même écran externe, passé à gauche du MacBook dans les réglages d'affichage.
        var aGauche = new ScreenArea(new ScreenRect(-2560, 0, 2560, 1440), new ScreenRect(-2560, 25, 2560, 1415));
        var gardee = new ScreenRect(1900, 120, 1600, 1000);
        ScreenRect? place = WindowPlacement.Restore(gardee, Externe.Area, new[] { MacBook, aGauche });
        Assert.That(place, Is.EqualTo(new ScreenRect(-2560 + 172, 120, 1600, 1000)));
    }

    [Test]
    public void Barre_de_titre_hors_ecran_la_place_n_est_pas_gardee()
    {
        // Seuls quelques pixels du bas de la fenêtre dépassent sur l'écran : impossible de l'attraper.
        var gardee = new ScreenRect(500, -1200, 1000, 1210);
        Assert.That(WindowPlacement.Restore(gardee, null, new[] { MacBook }), Is.Null);
    }

    [Test]
    public void Numero_de_fenetre_le_plus_petit_libre()
    {
        Assert.That(WindowPlacement.NextNumber(new int[0]), Is.EqualTo(2));
        Assert.That(WindowPlacement.NextNumber(new[] { 2, 3 }), Is.EqualTo(4));
        Assert.That(WindowPlacement.NextNumber(new[] { 3 }), Is.EqualTo(2));
    }

    [TestCase(null, "http://localhost:8686/?fenetre=2")]
    [TestCase("race", "http://localhost:8686/?fenetre=2#race")]
    [TestCase("strategy", "http://localhost:8686/?fenetre=2#strategy")]
    [TestCase("x#y", "http://localhost:8686/?fenetre=2")]
    [TestCase("\"><script>", "http://localhost:8686/?fenetre=2")]
    [TestCase("Race", "http://localhost:8686/?fenetre=2")]
    public void Adresse_de_la_fenetre_avec_sa_page(string onglet, string attendu)
    {
        Assert.That(WindowPlacement.WindowUrl("http://localhost:8686", 2, onglet), Is.EqualTo(attendu));
        Assert.That(WindowPlacement.WindowUrl("http://localhost:8686/", 2, onglet), Is.EqualTo(attendu));
    }

    [Test]
    public void Fenetres_retenues_puis_relues()
    {
        string dir = Path.Combine(Path.GetTempPath(), "sm-fenetres-" + Path.GetRandomFileName());
        try
        {
            var store = new WindowLayoutStore(dir);
            var layout = new WindowLayout { Main = new SavedWindow { Number = 1, Rect = FenetrePrincipale, Screen = MacBook.Area } };
            layout.Windows.Add(new SavedWindow { Number = 3, Rect = new ScreenRect(1728, 25, 2560, 1415), Screen = Externe.Area });
            layout.Windows.Add(new SavedWindow { Number = 2, Rect = new ScreenRect(200, 200, 1200, 800) });
            store.Save(layout);

            WindowLayout lu = new WindowLayoutStore(dir).Load();
            Assert.That(lu.Main.Rect, Is.EqualTo(FenetrePrincipale));
            Assert.That(lu.Main.Screen, Is.EqualTo(MacBook.Area));
            Assert.That(lu.Windows.Select(w => w.Number), Is.EqualTo(new[] { 2, 3 }), "rangées par numéro");
            Assert.That(lu.Windows[1].Screen, Is.EqualTo(Externe.Area));
            Assert.That(lu.Windows[0].Screen, Is.Null, "écran inconnu");
            Assert.That(File.ReadAllText(Path.Combine(dir, WindowLayoutStore.FileName)), Does.Contain("\"windows\""));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Test]
    public void Fichier_corrompu_ou_absent_rien_a_rouvrir()
    {
        string dir = Path.Combine(Path.GetTempPath(), "sm-fenetres-" + Path.GetRandomFileName());
        try
        {
            Assert.That(new WindowLayoutStore(dir).Load().Windows, Is.Empty);
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, WindowLayoutStore.FileName), "{ pas du json");
            WindowLayout lu = new WindowLayoutStore(dir).Load();
            Assert.That(lu.Main, Is.Null);
            Assert.That(lu.Windows, Is.Empty);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Test]
    public void Entrees_invalides_ecartees()
    {
        var layout = new WindowLayout { Main = new SavedWindow { Number = 7, Width = 0, Height = 0 } };
        layout.Windows.Add(new SavedWindow { Number = 1, Width = 800, Height = 600 });   // la 1 est la principale
        layout.Windows.Add(new SavedWindow { Number = 2, Width = 800, Height = 600, X = 10 });
        layout.Windows.Add(new SavedWindow { Number = 2, Width = 900, Height = 600, X = 20 }); // doublon
        layout.Windows.Add(new SavedWindow { Number = 4, Width = -5, Height = 600 });
        layout.Windows.Add(null);
        for (int n = 10; n < 30; n++)
        {
            layout.Windows.Add(new SavedWindow { Number = n, Width = 800, Height = 600 });
        }

        WindowLayout propre = WindowLayoutStore.Clean(layout);
        Assert.That(propre.Main, Is.Null);
        Assert.That(propre.Windows[0].Number, Is.EqualTo(2));
        Assert.That(propre.Windows[0].X, Is.EqualTo(10));
        Assert.That(propre.Windows.Select(w => w.Number), Does.Not.Contain(4));
        Assert.That(propre.Windows, Has.Count.EqualTo(WindowLayoutStore.MaxWindows));
    }

    private static void AssertInside(ScreenRect r, ScreenRect work)
    {
        Assert.That(r.X, Is.GreaterThanOrEqualTo(work.X));
        Assert.That(r.Y, Is.GreaterThanOrEqualTo(work.Y));
        Assert.That(r.Right, Is.LessThanOrEqualTo(work.Right));
        Assert.That(r.Bottom, Is.LessThanOrEqualTo(work.Bottom));
    }
}
