namespace SecondMonitor.Hub.Core.Music
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;
    using SecondMonitor.Hub.Core.Ai;

    /// <summary>Ce que le pilote demande à la musique.</summary>
    public enum MusicAction
    {
        Play,
        Pause,
        Next,
        Previous,
        VolumeUp,
        VolumeDown,
        NowPlaying,
        Flow,
        Open,
    }

    /// <summary>Ce qu'il faut chercher sur Deezer pour « mets … ».</summary>
    public enum MusicKind
    {
        Any,
        Artist,
        Playlist,
        Album,
        Track,
    }

    /// <summary>Commande musicale reconnue dans une phrase radio.</summary>
    public sealed class MusicIntent
    {
        public MusicAction Action { get; set; }

        public MusicKind Kind { get; set; }

        /// <summary>Ce qu'il faut chercher (« daft punk », « rock »), pour <see cref="MusicAction.Open"/>.</summary>
        public string Query { get; set; }

        /// <summary>
        /// Vrai si la phrase parle clairement de musique (« la playlist … », « … sur Deezer »). Faux pour « mets X » tout court :
        /// on ne lance alors que si Deezer connaît un artiste de ce nom exact (sinon c'est peut-être une consigne de course).
        /// </summary>
        public bool Explicit { get; set; }

        public override string ToString() => $"{Action} {Kind} « {Query} »{(Explicit ? string.Empty : " (à confirmer)")}";
    }

    /// <summary>
    /// Commandes vocales de la musique, reconnues sans passer par le modèle IA (réponse immédiate) :
    /// « mets de la musique », « pause musique », « chanson suivante », « remets la précédente », « monte la musique »,
    /// « mets la playlist rock », « mets Daft Punk », « mets mon Flow », « c'est quoi ce morceau ? ».
    /// </summary>
    public static class MusicCommands
    {
        private const string Noun = @"(musique|musiques|zik|zic|son|chanson|chansons|morceau|morceaux|titre|deezer)";

        /// <summary>Pour « c'est quoi ce morceau » : ni « son » (« quel est son écart ») ni « piste ».</summary>
        private const string NowNoun = @"(musique|zik|chanson|morceau|titre)";
        private const string Polite = @"( (s il te plait|s il vous plait|stp|svp|merci|maintenant|please|un peu|vite))*$";

        /// <summary>Mots de course : « mets les pneus pluie », « mets la carto 3 » ne sont pas de la musique.</summary>
        private static readonly Regex RacingWords = new(
            @"\b(pneu|pneus|gomme|gommes|carburant|essence|fuel|stand|stands|box|carto|cartographie|map|mapping|frein|freins|freinage|repartition|mode|aileron|pression|pressions|pluie|pluies|slick|slicks|inter|intermediaire|intermediaires|tour|tours|ecart|ecarts|chrono|chronometre|temps|moteur|course|depart|session|qualif|qualification|essais|simulation|replay|pit|pitstop|safety|vsc|drapeau|demo|test|enregistrement|video|camera|analyse|debrief|briefing|resume|bilan|rapport|point|statistiques|stats|abs|tc|antipatinage|setup|reglage|reglages|strategie|rythme|gaz|attaque|push|defense|drs|ers|batterie|energie|voiture|modele|ia|ecoute|reflexion|voix|radio|note|memoire|alerte|alertes|info|infos|position|classement|delta|secteur|secteurs|meteo|piste|degat|degats|reparation|reparations|paquet|undercut|overcut|relais|limiteur|phares|essuie)\b",
            RegexOptions.Compiled);

        /// <summary>Genres et ambiances : « mets du rock » cherche une playlist.</summary>
        private static readonly Regex Genres = new(
            @"^(rock|pop|rap|hip hop|jazz|blues|soul|funk|disco|electro|electronique|techno|house|trance|drum and bass|dubstep|metal|hard rock|punk|reggae|classique|lofi|lo fi|chill|ambient|variete|variete francaise|chanson francaise|annees 80|annees 90|annees 2000|80s|90s|country|latino|reggaeton|kpop|k pop|r b|rnb|motivation|sport|workout|focus|synthwave|phonk|epic|bandes originales|musique de film)$",
            RegexOptions.Compiled);

        public static MusicIntent Parse(string text)
        {
            string t = RadioCommands.Normalize(text);
            if (t.Length == 0 || t.Split(' ').Length > 16)
            {
                return null;
            }

            // Politesse en tête (« s'il te plaît mets… », « ok mets… ») retirée.
            t = Regex.Replace(t, @"^((ok|bon|alors|euh|allez|dis|dis moi|hey|stp|svp|s il te plait|s il vous plait|tu peux|tu pourrais|vous pouvez|est ce que tu peux|peux tu|pouvez vous) )+", string.Empty);

            // ───── Quel morceau ? ─────
            if (Regex.IsMatch(t, @"\b(qui chante|c est qui qui chante|c est qui le chanteur|c est quel artiste|c est quel groupe)\b")
                || Regex.IsMatch(t, $@"\b(c est quoi|quel est|quelle est|qu est ce que c est|on ecoute quoi|c est quel|quel|quelle) (ce|cette|le|la|comme)? ?{NowNoun}( (en cours|qui passe|qu on ecoute|la|actuel|actuelle|de deezer))?$")
                || Regex.IsMatch(t, $@"^{NowNoun} (en cours|actuel|actuelle)$")
                || Regex.IsMatch(t, @"\bc est quoi (ce|cette) son$"))
            {
                return new MusicIntent { Action = MusicAction.NowPlaying, Explicit = true };
            }

            // ───── Pause ─────
            if (Regex.IsMatch(t, $@"\b(coupe|coupes|couper|arrete|arretes|arreter|stoppe|stop|stopper|eteins|eteindre|pause|mets en pause|met en pause|silence)( moi)? (la |le |les |cette |ce )?{Noun}\b")
                || Regex.IsMatch(t, $@"\b{Noun} (en )?(pause|stop|off)\b")
                || Regex.IsMatch(t, $@"\bmets? (la |le )?{Noun} en pause\b")
                || Regex.IsMatch(t, @"^(pause|stop) (la )?(musique|deezer)$"))
            {
                return new MusicIntent { Action = MusicAction.Pause, Explicit = true };
            }

            // ───── Précédent (avant « suivant » : « remets la précédente ») ─────
            if (Regex.IsMatch(t, @"\b(chanson|morceau|titre|piste|musique|son) (precedente?|d avant|d avant s il te plait)\b")
                || Regex.IsMatch(t, @"\b(remets?|remettre|reviens|revenir|retourne|retour) (a |au |sur )?(la |le |l )?(precedente?|chanson precedente|morceau precedent|chanson d avant|morceau d avant|titre d avant|titre precedent)\b")
                || Regex.IsMatch(t, @"^(precedent|precedente|la precedente|le precedent)" + Polite))
            {
                return new MusicIntent { Action = MusicAction.Previous, Explicit = true };
            }

            // ───── Suivant ─────
            if (Regex.IsMatch(t, @"\b(chanson|morceau|titre|piste|musique|son) (suivante?|d apres)\b")
                || Regex.IsMatch(t, @"\b(passe|passes|zappe|zapper|saute|sauter|change|changer|skip) (de |la |le |cette |ce |a la |au )?(chanson|morceau|titre|piste|musique|son)\b")
                || Regex.IsMatch(t, @"^(suivant|suivante|au suivant|la suivante|le suivant|next|skip)" + Polite))
            {
                return new MusicIntent { Action = MusicAction.Next, Explicit = true };
            }

            // ───── Volume ─────
            if (Regex.IsMatch(t, @"\b(monte|montes|monter|augmente|augmenter|remonte|pousse|plus fort)( un peu| encore)? (la |le )?(musique|son de la musique|volume de la musique|zik|deezer|son)\b")
                || Regex.IsMatch(t, @"\b(musique|zik|deezer) plus forte?\b")
                || Regex.IsMatch(t, @"^plus fort (la )?musique"))
            {
                return new MusicIntent { Action = MusicAction.VolumeUp, Explicit = true };
            }

            if (Regex.IsMatch(t, @"\b(baisse|baisses|baisser|diminue|diminuer|reduis|reduire|descends|moins fort)( un peu| encore)? (la |le )?(musique|son de la musique|volume de la musique|zik|deezer|son)\b")
                || Regex.IsMatch(t, @"\b(musique|zik|deezer) moins forte?\b")
                || Regex.IsMatch(t, @"^moins fort (la )?musique"))
            {
                return new MusicIntent { Action = MusicAction.VolumeDown, Explicit = true };
            }

            // ───── Flow ─────
            if (Regex.IsMatch(t, @"\b(mets|met|mettre|lance|lancer|joue|jouer|demarre|envoie|remets|relance)( moi| nous)? (mon|le|ma|notre) flow\b")
                || Regex.IsMatch(t, @"^(mon |le )?flow( deezer)?" + Polite))
            {
                return new MusicIntent { Action = MusicAction.Flow, Explicit = true };
            }

            // ───── Lecture ─────
            if (Regex.IsMatch(t, @"^(mets|met|mettre|lance|lancer|remets|relance|joue|demarre|envoie|reprends|reprend|reprendre|rallume|allume|on met|on remet|on lance)( moi| nous)? (de la |la |un peu de |une |des |du |le |l )?(musique|zik|zic|son|chanson|chansons|deezer)" + Polite)
                || Regex.IsMatch(t, @"^(musique|deezer|play|lecture)" + Polite)
                || Regex.IsMatch(t, @"^(reprends|reprend|reprendre|relance|remets) (la )?(musique|lecture|deezer)\b"))
            {
                return new MusicIntent { Action = MusicAction.Play, Explicit = true };
            }

            // ───── « Mets … » : playlist, album, artiste, chanson ─────
            Match open = Regex.Match(t, @"^(je veux ecouter|on ecoute|on met|on lance|ecoute|ecouter|mets|met|mettre|lance|lancer|joue|jouer|envoie|demarre|passe)( moi| nous)? (.+)$");
            if (!open.Success)
            {
                return null;
            }

            string verb = open.Groups[1].Value;
            string rest = Regex.Replace(open.Groups[3].Value, Polite, string.Empty).Trim();
            if (rest.Length == 0 || rest.StartsWith("en ", StringComparison.Ordinal) || rest.StartsWith("toi ", StringComparison.Ordinal))
            {
                return null;
            }

            bool onDeezer = false;
            Match deezer = Regex.Match(rest, @"^(.+?) (sur|dans|avec) deezer$");
            if (deezer.Success)
            {
                rest = deezer.Groups[1].Value;
                onDeezer = true;
            }

            (string pattern, MusicKind kind)[] kinds =
            {
                (@"^(la |une |ma )?(playlist|play list|liste de lecture|compil|compilation) (de |du |des )?(.+)$", MusicKind.Playlist),
                (@"^(l |un |le dernier |le nouvel )?album (de |du |des )?(.+)$", MusicKind.Album),
                (@"^(l |un )?(artiste|groupe|chanteur|chanteuse) (.+)$", MusicKind.Artist),
                (@"^(la |une |le |un )?(chanson|morceau|titre|single|son) (.+)$", MusicKind.Track),
                (@"^(de la musique|des chansons|du son|un peu|quelque chose|un morceau|un titre|une chanson|un son|des morceaux|des titres) (de |d |du |des )(.+)$", MusicKind.Artist),
            };
            foreach (var (pattern, kind) in kinds)
            {
                Match m = Regex.Match(rest, pattern);
                if (m.Success)
                {
                    string q = Clean(m.Groups[m.Groups.Count - 1].Value);
                    return q.Length < 2 ? null : new MusicIntent { Action = MusicAction.Open, Kind = kind, Query = q, Explicit = true };
                }
            }

            // « passe … » (« passe au stand ») : seulement avec « sur Deezer ».
            if (verb == "passe" && !onDeezer)
            {
                return null;
            }

            string query = Clean(Regex.Replace(rest, @"^(du |de la |de l |des |le |la |les |l |un peu de |un peu d |de |d )", string.Empty));
            if (query.Length < 2 || (!onDeezer && (RacingWords.IsMatch(query) || Regex.IsMatch(query, @"^\d+$") || query.Split(' ').Length > 6)))
            {
                return null;
            }

            if (!onDeezer && Genres.IsMatch(query))
            {
                return new MusicIntent { Action = MusicAction.Open, Kind = MusicKind.Playlist, Query = query, Explicit = true };
            }

            return new MusicIntent { Action = MusicAction.Open, Kind = MusicKind.Any, Query = query, Explicit = onDeezer };
        }

        /// <summary>Phrase de confirmation dite à la radio (courte : la musique reprend juste après).</summary>
        public static string Describe(DeezerItem item) => item?.Kind switch
        {
            MusicKind.Playlist => $"la playlist {item.Title}",
            MusicKind.Album => string.IsNullOrEmpty(item.Subtitle) ? $"l'album {item.Title}" : $"l'album {item.Title}, de {item.Subtitle}",
            MusicKind.Track => string.IsNullOrEmpty(item.Subtitle) ? item.Title : $"{item.Title}, de {item.Subtitle}",
            MusicKind.Artist => item.Title,
            _ => item?.Title ?? string.Empty,
        };

        private static string Clean(string q)
        {
            q = Regex.Replace(q ?? string.Empty, @"\b(s il te plait|s il vous plait|stp|svp|please|merci)\b", string.Empty);
            return string.Join(' ', q.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(w => w.Length > 0)).Trim();
        }
    }
}
