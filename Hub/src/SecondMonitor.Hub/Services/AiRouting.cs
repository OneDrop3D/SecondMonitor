namespace SecondMonitor.Hub.Services;

using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

/// <summary>
/// Configuration des fournisseurs d'API et des modèles à bascule (<see cref="AiRoute"/>) par une autre IA :
/// <see cref="BuildPrompt"/> écrit le prompt à coller dans Gemini, ChatGPT ou Mistral ; <see cref="Import"/> lit le JSON qu'elles renvoient.
/// Le JSON ne porte jamais de clé d'API : elles se saisissent dans l'application.
/// </summary>
public static class AiRouting
{
    public const int MaxBytes = 256 * 1024;

    private const int MaxProviders = 20;
    private const int MaxRoutes = 20;
    private const int MaxTargets = 12;

    private static readonly Regex NameRule = new(@"^[A-Za-z0-9][A-Za-z0-9 _.\-]{0,39}$", RegexOptions.Compiled);
    private static readonly Regex ModelRule = new(@"^[^\s@]{1,200}$", RegexOptions.Compiled);
    private static readonly Regex PathRule = new(@"^(/[A-Za-z0-9._\-]{1,40}){0,5}$", RegexOptions.Compiled);

    /// <summary>Fournisseurs proposés quand aucun n'est encore relié ; l'autre IA vérifie leurs adresses et leurs limites.</summary>
    private static readonly string[] Catalogue =
    {
        "NVIDIA (build.nvidia.com : Nemotron, Llama, Kimi, GLM…)", "Anthropic (Claude)", "DeepSeek", "Kimi (Moonshot AI)", "GLM (Z.ai / Zhipu)",
        "Google Gemini", "OpenRouter", "Groq", "Mistral (La Plateforme)", "Cerebras", "Together AI", "Experiential Labs", "DeepAI",
    };

    /// <summary>Résultat d'un import : ce qui a été pris, et pourquoi le reste a été écarté.</summary>
    public sealed record Report(int Providers, int Routes, List<string> Warnings, List<string> Assigned);

    /// <summary>Rôles que le JSON peut régler : la réflexion (réponses radio), les questions difficiles, les quatre spécialistes.</summary>
    private static readonly string[] SpecialistKeys = { Core.Ai.Specialists.Tyres, Core.Ai.Specialists.Strategy, Core.Ai.Specialists.Car, Core.Ai.Specialists.Race };

    /// <summary>Le prompt : ce que l'application sait déjà (fournisseurs, modèles réellement listés, routes) + le format du JSON attendu.</summary>
    public static string BuildPrompt(AiSettings ai, IReadOnlyDictionary<string, List<string>> live, string? wish)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Tu configures le routeur d'IA de l'application SecondMonitor (un ingénieur de course IA qui répond à la radio du pilote : réponses courtes et rapides, en français).");
        sb.AppendLine("Date du jour : " + DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ". Cherche sur le web les informations actuelles si tu le peux ; sinon dis-le dans « ignores ».");
        sb.AppendLine();
        sb.AppendLine("## Comment l'application fonctionne");
        sb.AppendLine("- Elle sait parler à deux sortes de fournisseurs : « openai » (API compatible OpenAI : NVIDIA, DeepSeek, Kimi, GLM, Groq, OpenRouter, Mistral, Gemini…) et « anthropic » (Claude, par son API native).");
        sb.AppendLine("- « url » est l'adresse de l'API jusqu'AVANT « /models » et « /chat/completions », version comprise (exemples : https://integrate.api.nvidia.com/v1, https://api.z.ai/api/paas/v4, https://generativelanguage.googleapis.com/v1beta/openai, https://api.anthropic.com/v1).");
        sb.AppendLine("- Un « modèle à bascule » (route) est une liste ordonnée de « identifiant-du-modèle@fournisseur ». L'application essaie le premier ; si son fournisseur n'a plus de crédit (ou refuse la clé), elle passe au suivant, et ainsi de suite jusqu'à la fin de la liste.");
        sb.AppendLine("- Place d'abord le MÊME modèle chez tous les fournisseurs qui le proposent, puis seulement ensuite un autre modèle moins bon (secours).");
        sb.AppendLine();
        sb.AppendLine("## Fournisseurs déjà dans l'application et modèles qu'elle y voit réellement");
        List<AiNode> gateways = ai.Nodes.Where(n => n.IsGateway).ToList();
        if (gateways.Count == 0)
        {
            sb.AppendLine("(aucun)");
        }

        foreach (AiNode n in gateways)
        {
            List<string> ids = live.TryGetValue(n.Name, out List<string>? l) ? l : new();
            sb.AppendLine($"- {n.Name} ({n.Api}) — {n.Url}{n.ApiPath} — " + (ids.Count == 0 ? "modèles non lus (clé absente ou refusée)" : $"{ids.Count} modèles"));
        }

        // Chaque modèle une seule fois, avec toutes ses cibles exactes : c'est la matière des modèles à bascule.
        var byModel = gateways.SelectMany(n => (live.TryGetValue(n.Name, out List<string>? l) ? l : new()).Select(id => (Key: ModelKey(id), Target: $"{id}@{n.Name}")))
            .GroupBy(x => x.Key).OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal).ToList();
        if (byModel.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"## Les {byModel.Count} modèles disponibles, avec leurs cibles exactes (« identifiant@fournisseur », à recopier tels quels)");
            byModel.ForEach(g => sb.AppendLine($"- {g.Key} : {string.Join(", ", g.Select(x => x.Target))}"));
        }

        sb.AppendLine();
        sb.AppendLine("## Fournisseurs que je voudrais ajouter");
        string[] known = gateways.Select(g => g.Name.ToLowerInvariant()).ToArray();
        sb.AppendLine(string.Join(", ", Catalogue.Where(c => !known.Any(k => c.ToLowerInvariant().Contains(k)))) + ".");
        sb.AppendLine("Ajoute aussi tout autre fournisseur que je cite dans « Ce que je veux ». Pour chacun : trouve l'adresse de son API et vérifie qu'elle est compatible OpenAI (« /chat/completions ») ; seul Claude (Anthropic) est géré en plus avec son format natif. Sinon, ne l'ajoute pas : mets-le dans « ignores » avec la raison.");
        sb.AppendLine("Garde les fournisseurs qui offrent un niveau gratuit, des crédits gratuits ou un solde prépayé (Claude, DeepSeek, Kimi : on recharge un solde, et la bascule sert justement quand il est épuisé) ; dis lequel dans « limits ».");
        sb.AppendLine("Nemotron, Kimi et GLM se trouvent aussi chez NVIDIA et OpenRouter : si un modèle est disponible chez plusieurs fournisseurs, mets-le chez tous, c'est ce qui fait la bascule.");
        sb.AppendLine();
        sb.AppendLine("## Modèles à bascule actuels");
        if (ai.Routes == null || ai.Routes.Count == 0)
        {
            sb.AppendLine("(aucun)");
        }
        else
        {
            ai.Routes.ForEach(r => sb.AppendLine($"- {r.Name} : {string.Join(" → ", r.Targets)}{(string.IsNullOrEmpty(r.Then) ? string.Empty : $" ; puis la route « {r.Then} »")}"));
        }

        sb.AppendLine();
        sb.AppendLine("## Ce que je veux");
        string trimmed = wish?.Trim() ?? string.Empty;
        sb.AppendLine(trimmed.Length == 0
            ? "Deux modèles à bascule : « rapide » (réponses de quelques secondes, petit ou moyen modèle) et « puissant » (le meilleur modèle disponible, pour les questions difficiles)."
            : trimmed[..Math.Min(trimmed.Length, 1000)]);
        sb.AppendLine();
        sb.AppendLine("## À faire");
        sb.AppendLine("0. N'utilise que les fournisseurs listés ci-dessus comme « déjà dans l'application » s'ils ont des modèles lus : ce sont ceux dont j'ai la clé. Les autres, seulement s'ils sont dans « Ce que je veux ».");
        sb.AppendLine("1. Pour chaque fournisseur retenu, relève ses limites gratuites actuelles (crédit offert, requêtes par minute, par jour) et écris-les en une phrase dans « limits ».");
        sb.AppendLine("2. Mets dans « pauseMinutes » le temps à attendre avant de réessayer le fournisseur une fois son crédit épuisé : 1440 si la limite se renouvelle chaque jour, 60 si elle est horaire, 43200 si le crédit offert ne se renouvelle pas.");
        sb.AppendLine("3. Construis les modèles à bascule demandés. N'utilise que des identifiants de modèles qui existent vraiment, tels qu'écrits par le fournisseur (recopie-les de la liste « modèles disponibles » ci-dessus, et pour un même modèle mets toutes ses cibles) ; si tu n'es pas sûr d'un identifiant, ne le mets pas.");
        sb.AppendLine("4. Enchaîne les routes : chaque route a « then » = le nom de la route suivante, à utiliser quand tous ses fournisseurs sont épuisés. Fais une seule chaîne du meilleur modèle au moins bon (par exemple gemma → nemotron → glm → kimi → …), la dernière route sans « then ». Ainsi, quand un modèle est épuisé partout, une liste de modèles prend le relais dans l'ordre.");
        sb.AppendLine("5. Mets tout en place dans « assign » : « think » = la route des réponses radio (la plus rapide parmi les bonnes, quelques secondes), « hard » = la route des questions difficiles (la plus puissante), « specialists » = une route par domaine (« pneus », « stratégie », « voiture », « course »), ou rien pour qu'ils suivent « think ». N'y mets que des noms de routes de ta réponse.");
        sb.AppendLine("6. Ne mets AUCUNE clé d'API dans ta réponse.");
        sb.AppendLine();
        sb.AppendLine("## Format de ta réponse");
        sb.AppendLine("Réponds uniquement avec un fichier JSON valide (dans un seul bloc de code, ou en pièce jointe .json), sans autre texte, de cette forme :");
        sb.AppendLine("""
            {
              "version": 1,
              "providers": [
                { "name": "NVIDIA", "api": "openai", "url": "https://integrate.api.nvidia.com/v1", "limits": "crédit gratuit à l'inscription, 40 requêtes par minute", "pauseMinutes": 1440 },
                { "name": "Claude", "api": "anthropic", "url": "https://api.anthropic.com/v1", "limits": "solde prépayé", "pauseMinutes": 60 }
              ],
              "routes": [
                { "name": "rapide", "targets": ["google/gemma-4-31b-it@NVIDIA", "gemma-4-31b-it@Experiential"], "then": "secours" },
                { "name": "secours", "targets": ["moonshotai/kimi-k3.1-pro@NVIDIA", "kimi-k3.1-pro@Kimi"] }
              ],
              "assign": { "think": "rapide", "hard": "secours", "specialists": { "pneus": "rapide", "stratégie": "secours" } },
              "ignores": [
                { "name": "DeepAI", "reason": "API non compatible OpenAI" }
              ]
            }
            """);
        sb.AppendLine("Règles : « name » = lettres, chiffres, espaces, point, tiret (40 caractères au plus) ; « api » = « openai » (par défaut) ou « anthropic » ; « url » commence par https:// ; chaque cible d'une route nomme un fournisseur de « providers » ou déjà relié ci-dessus.");
        return sb.ToString();
    }

    /// <summary>
    /// Lit le JSON d'une autre IA et le fusionne dans <paramref name="ai"/>. Tout est vérifié avant la moindre modification
    /// (<see cref="FormatException"/> si le JSON est inutilisable) ; les éléments invalides sont écartés et listés dans le rapport.
    /// Un fournisseur déjà relié garde sa clé seulement si son adresse ne change pas : une clé ne part jamais vers une adresse reçue d'une IA.
    /// </summary>
    public static Report Import(string text, AiSettings ai)
    {
        int open = text.IndexOf('{'), close = text.LastIndexOf('}');
        JsonNode? root;
        try
        {
            root = open >= 0 && close > open ? JsonNode.Parse(text[open..(close + 1)]) : null; // tolère le texte et les ``` autour du JSON
        }
        catch (System.Text.Json.JsonException ex)
        {
            throw new FormatException("JSON illisible : " + ex.Message);
        }

        if (root is not JsonObject obj || (obj["providers"] is null && obj["routes"] is null))
        {
            throw new FormatException("Ce JSON n'a ni « providers » ni « routes ».");
        }

        var warnings = new List<string>();
        var providers = new List<AiNode>();
        foreach (JsonNode? p in (obj["providers"] as JsonArray ?? new JsonArray()).Take(MaxProviders))
        {
            string name = Str(p, "name"), limits = Str(p, "limits"), api = Str(p, "api").ToLowerInvariant() == "anthropic" ? "anthropic" : "openai";
            if (!NameRule.IsMatch(name) || !Uri.TryCreate(Str(p, "url"), UriKind.Absolute, out Uri? uri) || uri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(uri.UserInfo) || uri.Query.Length > 0)
            {
                warnings.Add($"fournisseur écarté : « {name} » (nom ou adresse https invalide)");
                continue;
            }

            // Chemin de l'API : « apiPath » s'il est donné (même vide), sinon celui de l'adresse (« …/api/paas/v4 »), sinon « /v1 ».
            string urlPath = uri.AbsolutePath.TrimEnd('/');
            string path = p?["apiPath"] is JsonValue ? Str(p, "apiPath").TrimEnd('/') : urlPath.Length > 0 ? urlPath : "/v1";
            if (!PathRule.IsMatch(path) || path.Contains(".."))
            {
                warnings.Add($"fournisseur écarté : « {name} » (chemin d'API invalide)");
                continue;
            }

            int pause = p?["pauseMinutes"] is JsonValue v && v.TryGetValue(out int m) ? Math.Clamp(m, 1, 43200) : 60;
            providers.Add(new AiNode { Name = name, Url = uri.GetLeftPart(UriPartial.Authority), Api = api, ApiPath = path, Limits = limits[..Math.Min(limits.Length, 300)], PauseMinutes = pause });
        }

        // Fournisseurs après fusion : ceux déjà reliés (clé conservée si même adresse) puis les nouveaux.
        var nodes = ai.Nodes.ToList();
        foreach (AiNode incoming in providers)
        {
            AiNode? known = nodes.FirstOrDefault(n => n.Name.Equals(incoming.Name, StringComparison.OrdinalIgnoreCase));
            if (known == null)
            {
                nodes.Add(incoming);
            }
            else if (!known.IsGateway)
            {
                warnings.Add($"« {incoming.Name} » est une machine Ollama déjà configurée : laissée telle quelle");
            }
            else if (string.Equals(known.Url.TrimEnd('/'), incoming.Url, StringComparison.OrdinalIgnoreCase))
            {
                known.Limits = incoming.Limits;
                known.PauseMinutes = incoming.PauseMinutes;
                known.Api = incoming.Api;
                known.ApiPath = incoming.ApiPath;
            }
            else
            {
                warnings.Add($"« {incoming.Name} » : adresse changée, saisissez de nouveau sa clé");
                nodes[nodes.IndexOf(known)] = incoming; // pas de clé : elle ne suit pas une adresse reçue d'une IA
            }
        }

        var routes = new List<AiRoute>();
        foreach (JsonNode? r in (obj["routes"] as JsonArray ?? new JsonArray()).Take(MaxRoutes))
        {
            string name = Str(r, "name");
            var targets = new List<string>();
            foreach (JsonNode? t in (r?["targets"] as JsonArray ?? new JsonArray()).Take(MaxTargets))
            {
                string spec = t is JsonValue tv && tv.TryGetValue(out string? raw) ? raw.Trim() : string.Empty;
                int at = spec.LastIndexOf('@');
                AiNode? node = at > 0 ? nodes.FirstOrDefault(n => n.Name.Equals(spec[(at + 1)..], StringComparison.OrdinalIgnoreCase)) : null;
                if (node == null || !ModelRule.IsMatch(spec[..at]))
                {
                    warnings.Add($"cible écartée dans « {name} » : « {spec} » (fournisseur ou modèle inconnu)");
                    continue;
                }

                targets.Add($"{spec[..at]}@{node.Name}");
            }

            if (!NameRule.IsMatch(name) || targets.Count == 0)
            {
                warnings.Add($"modèle à bascule écarté : « {name} » (nom invalide ou aucune cible utilisable)");
                continue;
            }

            routes.Add(new AiRoute { Name = name, Targets = targets, Then = Str(r, "then") });
        }

        // « then » doit nommer une route connue (importée ou déjà là), sinon la chaîne s'arrête là.
        var allNames = routes.Select(r => r.Name).Concat((ai.Routes ?? new()).Select(r => r.Name)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (AiRoute r in routes.Where(r => r.Then.Length > 0 && (!allNames.Contains(r.Then) || r.Then.Equals(r.Name, StringComparison.OrdinalIgnoreCase))))
        {
            warnings.Add($"« {r.Name} » : route suivante « {r.Then} » inconnue, ignorée");
            r.Then = string.Empty;
        }

        // « assign » : seulement des noms de routes, jamais un modèle quelconque.
        string? Pick(string? value) => value != null && allNames.Contains(value) ? allNames.First(n => n.Equals(value, StringComparison.OrdinalIgnoreCase)) : null;
        JsonNode? assign = obj["assign"];
        string? think = Pick(Str(assign, "think")), hard = Pick(Str(assign, "hard"));
        var specialists = new Dictionary<string, string>();
        foreach (string key in SpecialistKeys)
        {
            string raw = Str(assign?["specialists"], key);
            raw = raw.Length > 0 ? raw : Str(assign?["specialists"], key.Replace("é", "e")); // « strategie » sans accent
            if (Pick(raw) is string route)
            {
                specialists[key] = route;
            }
            else if (raw.Length > 0)
            {
                warnings.Add($"spécialiste « {key} » : « {raw} » n'est pas une route, ignoré");
            }
        }

        foreach (JsonNode? ig in obj["ignores"] as JsonArray ?? new JsonArray())
        {
            warnings.Add($"ignoré par l'IA : {Str(ig, "name")} — {Str(ig, "reason")}");
        }

        // Tout est vérifié : on applique.
        ai.Nodes = nodes;
        ai.Routes = (ai.Routes ?? new()).Where(old => !routes.Any(n => n.Name.Equals(old.Name, StringComparison.OrdinalIgnoreCase))).Concat(routes).ToList();
        var assigned = new List<string>();
        if (think != null)
        {
            ai.ThinkModel = think;
            assigned.Add($"Réflexion → {think}");
        }

        if (hard != null)
        {
            ai.HardModel = hard;
            assigned.Add($"Questions difficiles → {hard}");
        }

        if (assign != null)
        {
            ai.Specialists = specialists; // spécialiste absent = il suit la réflexion
            assigned.AddRange(SpecialistKeys.Select(k => $"Spécialiste {k} → {(specialists.TryGetValue(k, out string? v) ? v : "comme la réflexion")}"));
        }

        return new Report(providers.Count, routes.Count, warnings, assigned);
    }

    /// <summary>Même modèle chez plusieurs fournisseurs : « google/gemma-4-31B-it », « models/gemma-4-31b-it », « …:free » → « gemma-4-31b-it ».</summary>
    internal static string ModelKey(string id)
    {
        string key = id[(id.LastIndexOf('/') + 1)..].ToLowerInvariant();
        return key.EndsWith(":free", StringComparison.Ordinal) ? key[..^5] : key;
    }

    private static string Str(JsonNode? node, string key) =>
        node?[key] is JsonValue v && v.TryGetValue(out string? s) ? s.Trim() : string.Empty;
}
