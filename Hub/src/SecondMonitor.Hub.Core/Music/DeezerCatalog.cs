namespace SecondMonitor.Hub.Core.Music
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using SecondMonitor.Hub.Core.Ai;

    /// <summary>Résultat de recherche Deezer (artiste, playlist, album ou titre).</summary>
    public sealed class DeezerItem
    {
        public MusicKind Kind { get; set; }

        public long Id { get; set; }

        public string Title { get; set; }

        /// <summary>Artiste (album, titre) ou créateur (playlist).</summary>
        public string Subtitle { get; set; }

        public string Picture { get; set; }

        /// <summary>Fans (artiste) ou nombre de titres (playlist, album).</summary>
        public long Count { get; set; }
    }

    /// <summary>
    /// Catalogue Deezer : recherche par l'API publique (api.deezer.com, sans clé ni compte) et liens d'ouverture
    /// dans l'application Deezer de l'ordinateur (deezer://…) ou dans le lecteur web (https://www.deezer.com/…).
    /// La lecture elle-même se fait dans Deezer, avec le compte et la qualité de l'utilisateur.
    /// Aucune information personnelle n'est envoyée : seulement les mots recherchés.
    /// </summary>
    public static class DeezerCatalog
    {
        public const string ApiBase = "https://api.deezer.com";

        public static string KindPath(MusicKind kind) => kind switch
        {
            MusicKind.Artist => "artist",
            MusicKind.Playlist => "playlist",
            MusicKind.Album => "album",
            MusicKind.Track => "track",
            _ => "artist",
        };

        public static bool TryParseKind(string text, out MusicKind kind)
        {
            switch ((text ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "artist": case "artiste": kind = MusicKind.Artist; return true;
                case "playlist": kind = MusicKind.Playlist; return true;
                case "album": kind = MusicKind.Album; return true;
                case "track": case "titre": case "chanson": kind = MusicKind.Track; return true;
                case "": case "any": case "tout": kind = MusicKind.Any; return true;
                default: kind = MusicKind.Any; return false;
            }
        }

        public static string SearchUrl(MusicKind kind, string query, int limit = 8) =>
            $"{ApiBase}/search/{KindPath(kind)}?q={Uri.EscapeDataString((query ?? string.Empty).Trim())}&limit={Math.Clamp(limit, 1, 25)}";

        /// <summary>Lien qui ouvre l'application Deezer de l'ordinateur sur l'élément (lecture automatique demandée).</summary>
        public static string AppLink(MusicKind kind, long id) => $"deezer://www.deezer.com/{KindPath(kind)}/{id}?autoplay=true";

        /// <summary>Même élément dans le lecteur web (navigateur connecté au compte Deezer).</summary>
        public static string WebLink(MusicKind kind, long id) => $"https://www.deezer.com/{KindPath(kind)}/{id}?autoplay=true";

        /// <summary>Le Flow (mix personnel infini) : réservé au compte connecté, donc ouvert dans Deezer.</summary>
        public const string FlowAppLink = "deezer://www.deezer.com/flow?autoplay=true";

        public const string FlowWebLink = "https://www.deezer.com/flow?autoplay=true";

        public static List<DeezerItem> ParseSearch(MusicKind kind, string json)
        {
            var list = new List<DeezerItem>();
            if (string.IsNullOrWhiteSpace(json))
            {
                return list;
            }

            using JsonDocument doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("data", out JsonElement data) || data.ValueKind != JsonValueKind.Array)
            {
                return list;
            }

            foreach (JsonElement e in data.EnumerateArray())
            {
                if (!e.TryGetProperty("id", out JsonElement idEl) || !idEl.TryGetInt64(out long id))
                {
                    continue;
                }

                var item = new DeezerItem { Kind = kind, Id = id };
                switch (kind)
                {
                    case MusicKind.Artist:
                        item.Title = Str(e, "name");
                        item.Picture = Str(e, "picture_medium");
                        item.Count = Num(e, "nb_fan");
                        break;
                    case MusicKind.Playlist:
                        item.Title = Str(e, "title");
                        item.Subtitle = e.TryGetProperty("user", out JsonElement u) ? Str(u, "name") : null;
                        item.Picture = Str(e, "picture_medium");
                        item.Count = Num(e, "nb_tracks");
                        break;
                    case MusicKind.Album:
                        item.Title = Str(e, "title");
                        item.Subtitle = e.TryGetProperty("artist", out JsonElement a) ? Str(a, "name") : null;
                        item.Picture = Str(e, "cover_medium");
                        item.Count = Num(e, "nb_tracks");
                        break;
                    default:
                        item.Title = Str(e, "title");
                        item.Subtitle = e.TryGetProperty("artist", out JsonElement ta) ? Str(ta, "name") : null;
                        item.Picture = e.TryGetProperty("album", out JsonElement al) ? Str(al, "cover_medium") : null;
                        break;
                }

                if (!string.IsNullOrWhiteSpace(item.Title))
                {
                    list.Add(item);
                }
            }

            return list;
        }

        /// <summary>
        /// Choisit le meilleur résultat. Si <paramref name="exactOnly"/>, seul un nom identique à la demande est accepté
        /// (« mets Daft Punk » sans autre précision : il ne faut pas lancer de musique sur une consigne de course mal comprise).
        /// </summary>
        public static DeezerItem PickBest(string query, IReadOnlyList<DeezerItem> items, bool exactOnly = false)
        {
            if (items == null || items.Count == 0)
            {
                return null;
            }

            string q = Compact(query);
            DeezerItem exact = items.FirstOrDefault(i => Compact(i.Title) == q)
                ?? items.FirstOrDefault(i => Compact(StripThe(i.Title)) == Compact(StripThe(query)));
            if (exact != null || exactOnly)
            {
                return exact;
            }

            // L'API classe déjà par pertinence ; on préfère un résultat qui contient vraiment les mots demandés.
            return items.FirstOrDefault(i => Compact(i.Title).Contains(q, StringComparison.Ordinal) || q.Contains(Compact(i.Title), StringComparison.Ordinal))
                ?? items[0];
        }

        /// <summary>Recherche sur l'API publique de Deezer (sans clé). Lève une exception si Deezer est injoignable.</summary>
        public static async Task<List<DeezerItem>> SearchAsync(HttpClient http, MusicKind kind, string query, CancellationToken ct, int limit = 8)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new List<DeezerItem>();
            }

            if (kind == MusicKind.Any)
            {
                kind = MusicKind.Artist;
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(6));
            string json = await http.GetStringAsync(SearchUrl(kind, query, limit), cts.Token).ConfigureAwait(false);
            return ParseSearch(kind, json);
        }

        /// <summary>
        /// « Mets X » sans précision : artiste d'abord (nom identique), puis playlist, album, titre.
        /// Si <paramref name="explicitRequest"/> est faux, seul un artiste au nom identique est accepté.
        /// </summary>
        public static async Task<DeezerItem> FindAsync(HttpClient http, MusicKind kind, string query, bool explicitRequest, CancellationToken ct)
        {
            if (kind != MusicKind.Any)
            {
                return PickBest(query, await SearchAsync(http, kind, query, ct).ConfigureAwait(false));
            }

            List<DeezerItem> artists = await SearchAsync(http, MusicKind.Artist, query, ct, 5).ConfigureAwait(false);
            DeezerItem artist = PickBest(query, artists, exactOnly: true);
            if (artist != null || !explicitRequest)
            {
                return artist;
            }

            foreach (MusicKind k in new[] { MusicKind.Playlist, MusicKind.Album, MusicKind.Track })
            {
                DeezerItem found = PickBest(query, await SearchAsync(http, k, query, ct, 5).ConfigureAwait(false));
                if (found != null)
                {
                    return found;
                }
            }

            return PickBest(query, artists);
        }

        private static string Str(JsonElement e, string name) => e.TryGetProperty(name, out JsonElement v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

        private static long Num(JsonElement e, string name) => e.TryGetProperty(name, out JsonElement v) && v.TryGetInt64(out long n) ? n : 0;

        private static string Compact(string text) => RadioCommands.Normalize(text).Replace(" ", string.Empty, StringComparison.Ordinal);

        private static string StripThe(string text) => System.Text.RegularExpressions.Regex.Replace(RadioCommands.Normalize(text), @"^(the|les|le|la) ", string.Empty);
    }
}
