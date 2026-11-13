namespace SecondMonitor.Hub.Tests;

using System.Net.Http;
using NUnit.Framework;
using SecondMonitor.Hub.Core.Music;

/// <summary>Musique Deezer : commandes vocales, liens, recherche, réglage par session, priorité à la radio.</summary>
[TestFixture]
public class MusicTests
{
    [TestCase("Mets de la musique", MusicAction.Play)]
    [TestCase("mets de la musique s'il te plaît", MusicAction.Play)]
    [TestCase("Lance la musique", MusicAction.Play)]
    [TestCase("Remets la musique", MusicAction.Play)]
    [TestCase("reprends la musique", MusicAction.Play)]
    [TestCase("Musique !", MusicAction.Play)]
    [TestCase("Pause musique", MusicAction.Pause)]
    [TestCase("Coupe la musique", MusicAction.Pause)]
    [TestCase("arrête la musique", MusicAction.Pause)]
    [TestCase("mets la musique en pause", MusicAction.Pause)]
    [TestCase("Musique en pause", MusicAction.Pause)]
    [TestCase("Chanson suivante", MusicAction.Next)]
    [TestCase("morceau suivant stp", MusicAction.Next)]
    [TestCase("Passe la chanson", MusicAction.Next)]
    [TestCase("change de musique", MusicAction.Next)]
    [TestCase("Suivant", MusicAction.Next)]
    [TestCase("Remets la précédente", MusicAction.Previous)]
    [TestCase("chanson précédente", MusicAction.Previous)]
    [TestCase("remets le morceau d'avant", MusicAction.Previous)]
    [TestCase("Monte la musique", MusicAction.VolumeUp)]
    [TestCase("monte un peu le son", MusicAction.VolumeUp)]
    [TestCase("musique plus fort", MusicAction.VolumeUp)]
    [TestCase("Baisse la musique", MusicAction.VolumeDown)]
    [TestCase("baisse un peu le son", MusicAction.VolumeDown)]
    [TestCase("C'est quoi ce morceau ?", MusicAction.NowPlaying)]
    [TestCase("c'est quoi cette chanson", MusicAction.NowPlaying)]
    [TestCase("Qui chante ?", MusicAction.NowPlaying)]
    [TestCase("quel est ce titre", MusicAction.NowPlaying)]
    [TestCase("Mets mon Flow", MusicAction.Flow)]
    [TestCase("lance le flow", MusicAction.Flow)]
    [TestCase("Flow", MusicAction.Flow)]
    public void Reconnait_les_commandes(string phrase, MusicAction expected)
    {
        MusicIntent intent = MusicCommands.Parse(phrase);
        Assert.That(intent, Is.Not.Null, phrase);
        Assert.That(intent.Action, Is.EqualTo(expected), phrase);
    }

    [TestCase("Mets la playlist rock des années 90", MusicKind.Playlist, "rock des annees 90", true)]
    [TestCase("mets l'album Discovery", MusicKind.Album, "discovery", true)]
    [TestCase("Mets l'artiste Daft Punk", MusicKind.Artist, "daft punk", true)]
    [TestCase("Mets la chanson Thunderstruck", MusicKind.Track, "thunderstruck", true)]
    [TestCase("mets de la musique de Queen", MusicKind.Artist, "queen", true)]
    [TestCase("Mets Daft Punk", MusicKind.Any, "daft punk", false)]
    [TestCase("mets du AC/DC", MusicKind.Any, "ac dc", false)]
    [TestCase("Mets Daft Punk sur Deezer", MusicKind.Any, "daft punk", true)]
    [TestCase("mets du rock", MusicKind.Playlist, "rock", true)]
    [TestCase("je veux écouter Queen", MusicKind.Any, "queen", false)]
    public void Reconnait_mets_X(string phrase, MusicKind kind, string query, bool isExplicit)
    {
        MusicIntent intent = MusicCommands.Parse(phrase);
        Assert.That(intent, Is.Not.Null, phrase);
        Assert.That(intent.Action, Is.EqualTo(MusicAction.Open), phrase);
        Assert.That(intent.Kind, Is.EqualTo(kind), phrase);
        Assert.That(intent.Query, Is.EqualTo(query), phrase);
        Assert.That(intent.Explicit, Is.EqualTo(isExplicit), phrase);
    }

    // Phrases de course qui ne doivent JAMAIS lancer de musique.
    [TestCase("Mets les pneus pluie")]
    [TestCase("mets la carto 3")]
    [TestCase("Je passe au stand")]
    [TestCase("passe sur mistral pour la voix")]
    [TestCase("On met les slicks au prochain arrêt ?")]
    [TestCase("Quel est son écart ?")]
    [TestCase("Quelle est la piste ?")]
    [TestCase("C'est qui devant ?")]
    [TestCase("Combien de tours d'essence ?")]
    [TestCase("Les pneus glissent")]
    [TestCase("Lance le chrono")]
    [TestCase("mets le paquet")]
    [TestCase("mets en pause l'analyse")]
    [TestCase("baisse la pression avant")]
    [TestCase("monte l'aileron")]
    [TestCase("Suivant au classement ?")]
    [TestCase("c'est quoi ce bruit dans le moteur")]
    [TestCase("merci")]
    [TestCase("")]
    public void Ignore_les_phrases_de_course(string phrase)
    {
        Assert.That(MusicCommands.Parse(phrase), Is.Null, phrase);
    }

    [Test]
    public void Liens_Deezer()
    {
        Assert.That(DeezerCatalog.AppLink(MusicKind.Playlist, 1306931615), Is.EqualTo("deezer://www.deezer.com/playlist/1306931615?autoplay=true"));
        Assert.That(DeezerCatalog.WebLink(MusicKind.Artist, 27), Is.EqualTo("https://www.deezer.com/artist/27?autoplay=true"));
        Assert.That(DeezerCatalog.AppLink(MusicKind.Track, 3135556), Does.StartWith("deezer://www.deezer.com/track/3135556"));
        Assert.That(DeezerCatalog.SearchUrl(MusicKind.Album, "AC/DC back in black", 5), Is.EqualTo("https://api.deezer.com/search/album?q=AC%2FDC%20back%20in%20black&limit=5"));
        Assert.That(DeezerCatalog.FlowAppLink, Does.StartWith("deezer://"));
    }

    [Test]
    public void Lit_les_resultats_et_choisit_le_bon()
    {
        const string json = """
            {"data":[
              {"id":1,"name":"Daft Punk Tribute","nb_fan":12,"picture_medium":"x"},
              {"id":27,"name":"Daft Punk","nb_fan":5000000,"picture_medium":"y"},
              {"name":"sans id"}
            ],"total":3}
            """;
        List<DeezerItem> items = DeezerCatalog.ParseSearch(MusicKind.Artist, json);
        Assert.That(items, Has.Count.EqualTo(2));
        Assert.That(DeezerCatalog.PickBest("daft punk", items, exactOnly: true).Id, Is.EqualTo(27));
        Assert.That(DeezerCatalog.PickBest("prudence", items, exactOnly: true), Is.Null, "pas de nom identique : rien (c'était peut-être une consigne)");

        const string albums = """{"data":[{"id":302127,"title":"Discovery","artist":{"name":"Daft Punk"},"cover_medium":"c","nb_tracks":14}]}""";
        DeezerItem album = DeezerCatalog.ParseSearch(MusicKind.Album, albums).Single();
        Assert.That(album.Subtitle, Is.EqualTo("Daft Punk"));
        Assert.That(MusicCommands.Describe(album), Is.EqualTo("l'album Discovery, de Daft Punk"));
        Assert.That(DeezerCatalog.ParseSearch(MusicKind.Playlist, """{"error":{"type":"Exception"}}"""), Is.Empty);
    }

    [Test]
    [Category("Reseau")]
    public async Task Recherche_reelle_sur_l_API_publique()
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("SecondMonitor-Hub-Tests");
        DeezerItem artist;
        DeezerItem playlist;
        try
        {
            artist = await DeezerCatalog.FindAsync(http, MusicKind.Any, "daft punk", explicitRequest: false, CancellationToken.None);
            playlist = await DeezerCatalog.FindAsync(http, MusicKind.Playlist, "rock", explicitRequest: true, CancellationToken.None);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            Assert.Ignore($"Deezer injoignable depuis cette machine : {ex.Message}");
            return;
        }

        Assert.That(artist, Is.Not.Null);
        Assert.That(artist.Id, Is.EqualTo(27), "Daft Punk = artiste 27 sur Deezer");
        Assert.That(playlist, Is.Not.Null);
        Assert.That(playlist.Kind, Is.EqualTo(MusicKind.Playlist));
    }

    [TestCase(MusicPolicy.Always, "Race", true)]
    [TestCase(MusicPolicy.NotInRace, "Race", false)]
    [TestCase(MusicPolicy.NotInRace, "Qualification", true)]
    [TestCase(MusicPolicy.PracticeOnly, "Practice", true)]
    [TestCase(MusicPolicy.PracticeOnly, "Qualification", false)]
    [TestCase(MusicPolicy.PracticeOnly, "Race", false)]
    [TestCase(MusicPolicy.PracticeOnly, null, true)]
    [TestCase(MusicPolicy.PracticeOnly, "Na", true)]
    public void Musique_selon_la_session(string policy, string session, bool allowed)
    {
        Assert.That(MusicPolicy.Allowed(policy, session, out string reason), Is.EqualTo(allowed));
        Assert.That(reason == null, Is.EqualTo(allowed));
    }

    [Test]
    public void Priorite_radio_baisse_puis_remet()
    {
        var p = new DuckPlanner { ReleaseDelay = TimeSpan.FromSeconds(1), VoiceTimeout = TimeSpan.FromSeconds(30) };
        var t = new DateTime(2026, 1, 1, 12, 0, 0);
        Assert.That(p.Tick(t), Is.EqualTo(DuckStep.None));

        // Le pilote appuie : on baisse une seule fois.
        p.SetRadio(true, t);
        Assert.That(p.Tick(t), Is.EqualTo(DuckStep.Duck));
        Assert.That(p.Tick(t.AddMilliseconds(500)), Is.EqualTo(DuckStep.None));

        // Fin de l'enregistrement, l'ingénieur répond juste après : pas de va-et-vient.
        p.SetRadio(false, t.AddSeconds(3));
        Assert.That(p.Tick(t.AddSeconds(3.5)), Is.EqualTo(DuckStep.None));
        p.SetVoice(true, t.AddSeconds(3.6));
        Assert.That(p.Tick(t.AddSeconds(5)), Is.EqualTo(DuckStep.None));
        p.SetVoice(false, t.AddSeconds(8));
        Assert.That(p.Tick(t.AddSeconds(8.5)), Is.EqualTo(DuckStep.None), "on attend un peu après la fin de la voix");
        Assert.That(p.Tick(t.AddSeconds(9.1)), Is.EqualTo(DuckStep.Restore));
        Assert.That(p.Ducked, Is.False);
    }

    [Test]
    public void Priorite_radio_securite_si_la_fin_de_la_voix_n_arrive_pas()
    {
        var p = new DuckPlanner { ReleaseDelay = TimeSpan.FromSeconds(1), VoiceTimeout = TimeSpan.FromSeconds(30) };
        var t = new DateTime(2026, 1, 1, 12, 0, 0);
        p.SetVoice(true, t);
        Assert.That(p.Tick(t), Is.EqualTo(DuckStep.Duck));
        Assert.That(p.Tick(t.AddSeconds(29)), Is.EqualTo(DuckStep.None));
        Assert.That(p.Tick(t.AddSeconds(30.5)), Is.EqualTo(DuckStep.None));
        Assert.That(p.Tick(t.AddSeconds(31.1)), Is.EqualTo(DuckStep.Restore));
    }
}
