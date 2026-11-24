#nullable enable
namespace SecondMonitor.Hub.Tests;

using System.Net;
using System.Text;
using NUnit.Framework;
using SecondMonitor.Hub.Services;

/// <summary>Fournisseurs d'API : import du JSON d'une autre IA, prompt, et bascule d'un fournisseur à l'autre quand le crédit est épuisé.</summary>
[TestFixture]
public class AiRoutingTests
{
    private const string Json = """
        Voici ta config :
        ```json
        {
          "version": 1,
          "providers": [
            { "name": "NVIDIA", "url": "https://integrate.api.nvidia.com/v1/", "limits": "40 req/min", "pauseMinutes": 1440 },
            { "name": "Pirate", "url": "http://evil.example.com" },
            { "name": "Groq", "url": "https://api.groq.com/openai/v1" },
            { "name": "GLM", "url": "https://api.z.ai/api/paas/v4" },
            { "name": "Gemini", "url": "https://generativelanguage.googleapis.com", "apiPath": "/v1beta/openai" },
            { "name": "DeepSeek", "url": "https://api.deepseek.com" },
            { "name": "Sans", "url": "https://sans-chemin.example.com", "apiPath": "" },
            { "name": "Claude", "api": "anthropic", "url": "https://api.anthropic.com/v1" },
            { "name": "Traversal", "url": "https://x.example.com", "apiPath": "/../admin" }
          ],
          "routes": [
            { "name": "rapide", "targets": ["claude-haiku-5-5@Claude", "llama-8b@NVIDIA", "llama-8b-instant@Groq", "x@Inconnu", "sans-fournisseur"] },
            { "name": "vide", "targets": ["x@Inconnu"] }
          ],
          "ignores": [ { "name": "DeepAI", "reason": "API non compatible OpenAI" } ]
        }
        ```
        """;

    private static AiNode Gateway(string name, string url, string token = "") => new() { Name = name, Url = url, Api = "openai", Token = token };

    [Test]
    public void Import_PrendLeBonEtEcarteLeReste()
    {
        var ai = new AiSettings();
        AiRouting.Report report = AiRouting.Import(Json, ai);

        Assert.That(ai.Nodes.Select(n => n.Name), Is.EqualTo(new[] { "NVIDIA", "Groq", "GLM", "Gemini", "DeepSeek", "Sans", "Claude" }), "http et chemin piégé écartés");
        AiNode nvidia = ai.Nodes[0];
        Assert.That(nvidia.Url, Is.EqualTo("https://integrate.api.nvidia.com"));
        Assert.That(nvidia.ApiPath, Is.EqualTo("/v1"), "la version vient de l'adresse");
        Assert.That(nvidia.PauseMinutes, Is.EqualTo(1440));
        Assert.That(nvidia.Token, Is.Empty, "aucune clé ne vient du JSON");
        string[] paths = ai.Nodes.Select(n => n.ApiPath).ToArray();
        Assert.That(paths, Is.EqualTo(new[] { "/v1", "/openai/v1", "/api/paas/v4", "/v1beta/openai", "/v1", "", "/v1" }));
        Assert.That(ai.Nodes.Single(n => n.Name == "Claude").Api, Is.EqualTo("anthropic"));
        Assert.That(ai.Nodes.Single(n => n.Name == "GLM").Api, Is.EqualTo("openai"));
        Assert.That(ai.Routes.Select(r => r.Name), Is.EqualTo(new[] { "rapide" }), "une route sans cible utilisable est écartée");
        Assert.That(ai.Routes[0].Targets, Is.EqualTo(new[] { "claude-haiku-5-5@Claude", "llama-8b@NVIDIA", "llama-8b-instant@Groq" }));
        Assert.That(report.Warnings.Count, Is.EqualTo(7), string.Join(" | ", report.Warnings)); // Pirate, Traversal, 3 cibles, route vide, DeepAI
    }

    [Test]
    public void Import_UneCleNeSuitPasUneAdresseRecueD_UneIA()
    {
        var ai = new AiSettings();
        ai.Nodes.Add(Gateway("NVIDIA", "https://integrate.api.nvidia.com", "nvapi-secret"));
        ai.Nodes.Add(Gateway("Groq", "https://autre.exemple.com", "gsk-secret"));
        ai.Nodes.Add(Gateway("Claude", "https://api.anthropic.com", "sk-ant-secret"));
        ai.Nodes.Add(new AiNode { Name = "raspberry", Url = "pi.local" });

        AiRouting.Import(Json.Replace("\"name\": \"Groq\"", "\"name\": \"Groq\"").Replace("\"Pirate\"", "\"raspberry\"").Replace("http://evil.example.com", "https://evil.example.com"), ai);

        Assert.That(ai.Nodes.Single(n => n.Name == "NVIDIA").Token, Is.EqualTo("nvapi-secret"), "même adresse : clé gardée");
        Assert.That(ai.Nodes.Single(n => n.Name == "Groq").Token, Is.Empty, "adresse changée : clé retirée");
        Assert.That(ai.Nodes.Single(n => n.Name == "Claude").Token, Is.EqualTo("sk-ant-secret"), "même adresse : le format passe en anthropic, la clé reste");
        Assert.That(ai.Nodes.Single(n => n.Name == "Claude").Api, Is.EqualTo("anthropic"));
        Assert.That(ai.Nodes.Single(n => n.Name == "raspberry").Api, Is.Empty, "une machine Ollama n'est jamais réécrite");
    }

    [Test]
    public void Import_JsonInutilisable_NeToucheARien()
    {
        var ai = new AiSettings();
        ai.Nodes.Add(Gateway("NVIDIA", "https://integrate.api.nvidia.com", "k"));
        Assert.Throws<FormatException>(() => AiRouting.Import("pas de json ici", ai));
        Assert.Throws<FormatException>(() => AiRouting.Import("{ \"truc\": 1 }", ai));
        Assert.Throws<FormatException>(() => AiRouting.Import("{ \"providers\": [ }", ai));
        Assert.That(ai.Nodes.Count, Is.EqualTo(1));
    }

    [Test]
    public void Prompt_ListeLesFournisseursEtLeursModeles_SansLesCles()
    {
        var ai = new AiSettings();
        ai.Nodes.Add(Gateway("NVIDIA", "https://integrate.api.nvidia.com", "nvapi-secret"));
        ai.Routes.Add(new AiRoute { Name = "rapide", Targets = { "llama-8b@NVIDIA" } });
        string prompt = AiRouting.BuildPrompt(ai, new Dictionary<string, List<string>> { ["NVIDIA"] = new() { "meta/llama-3.1-8b-instruct", "mistralai/mixtral-8x22b" } }, "un seul modèle nommé haiku");

        Assert.That(prompt, Does.Contain("meta/llama-3.1-8b-instruct").And.Contain("https://integrate.api.nvidia.com").And.Contain("un seul modèle nommé haiku").And.Contain("rapide : llama-8b@NVIDIA"));
        Assert.That(prompt, Does.Not.Contain("nvapi-secret"));
        Assert.That(AiRouting.ModelKey("google/gemma-4-31B-it:free"), Is.EqualTo("gemma-4-31b-it"));
        Assert.That(AiRouting.ModelKey("models/gemma-4-31b-it"), Is.EqualTo("gemma-4-31b-it"));
        Assert.That(prompt, Does.Contain("OpenRouter").And.Contain("Kimi").And.Contain("DeepSeek").And.Contain("GLM").And.Contain("Anthropic (Claude)"), "les fournisseurs pas encore reliés sont proposés");
        Assert.That(prompt, Does.Contain("https://integrate.api.nvidia.com/v1"));
    }

    // ───── Bascule ─────

    private sealed class FakeApi : HttpMessageHandler
    {
        public readonly List<string> Hosts = new();
        public readonly List<(string Host, string Path, string? ApiKey, string Body)> Requests = new();
        public readonly Dictionary<string, HttpStatusCode> Status = new();
        public readonly Dictionary<string, string> Reply = new(); // corps d'une réponse 200, par hôte (sinon format OpenAI)

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string host = request.RequestUri!.Host;
            Hosts.Add(host);
            string sent = request.Content == null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add((host, request.RequestUri.AbsolutePath, request.Headers.TryGetValues("x-api-key", out var k) ? k.First() : null, sent));
            HttpStatusCode code = Status.GetValueOrDefault(host, HttpStatusCode.OK);
            string body = code == HttpStatusCode.OK ? Reply.GetValueOrDefault(host, $"{{\"choices\":[{{\"message\":{{\"content\":\"réponse de {host}\"}}}}]}}") : code == HttpStatusCode.PaymentRequired ? "{\"error\":\"Insufficient credits\"}" : "{\"error\":\"model not found\"}";
            return new HttpResponseMessage(code) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }

    private static (OllamaClient Client, FakeApi Api, string Route, SettingsStore Store) Setup(string id, params string[] nodes)
    {
        string dir = Path.Combine(Path.GetTempPath(), "sm-ai-" + Guid.NewGuid().ToString("N"));
        var store = new SettingsStore(dir);
        store.Update(s =>
        {
            foreach (string n in nodes)
            {
                s.Ai.Nodes.Add(Gateway(n + id, $"https://{n.ToLowerInvariant()}{id}.test", "k"));
            }

            s.Ai.Routes.Add(new AiRoute { Name = "rapide" + id, Targets = nodes.Select(n => "m@" + n + id).ToList() });
        });
        var api = new FakeApi();
        return (new OllamaClient(new HttpClient(api), store), api, "rapide" + id, store);
    }

    private static Task<string> Ask(OllamaClient c, string route) => c.ChatAsync(route, new object[] { new { role = "user", content = "salut" } }, CancellationToken.None);

    [Test]
    public async Task Bascule_CreditEpuise_PasseAuSuivantEtNeRetapePasLePremier()
    {
        var (client, api, route, _) = Setup("1", "Alpha", "Beta");
        api.Status["alpha1.test"] = HttpStatusCode.PaymentRequired;

        Assert.That(await Ask(client, route), Is.EqualTo("réponse de beta1.test"));
        Assert.That(OllamaClient.PausedMinutes("Alpha1"), Is.GreaterThan(0), "crédit épuisé : fournisseur en pause");
        Assert.That(OllamaClient.AnsweredBy(route), Is.EqualTo("m@Beta1"));

        api.Hosts.Clear();
        Assert.That(await Ask(client, route), Is.EqualTo("réponse de beta1.test"));
        Assert.That(api.Hosts, Is.EqualTo(new[] { "beta1.test" }), "le fournisseur en pause n'est plus interrogé");

        OllamaClient.Resume("Alpha1"); // nouvelle clé
        Assert.That(OllamaClient.PausedMinutes("Alpha1"), Is.EqualTo(0));
    }

    [Test]
    public async Task Bascule_ToutEstEpuise_Leve_UneErreur()
    {
        var (client, api, route, _) = Setup("2", "Alpha", "Beta");
        api.Status["alpha2.test"] = HttpStatusCode.PaymentRequired;
        api.Status["beta2.test"] = HttpStatusCode.PaymentRequired;

        Assert.ThrowsAsync<AiHttpException>(() => Ask(client, route));
        InvalidOperationException? again = Assert.ThrowsAsync<InvalidOperationException>(() => Ask(client, route));
        Assert.That(again!.Message, Does.Contain("en pause"));
    }

    [Test]
    public async Task Bascule_ErreurSansRapportAvecLeCredit_NeMetPasEnPause()
    {
        var (client, api, route, _) = Setup("3", "Alpha", "Beta");
        api.Status["alpha3.test"] = HttpStatusCode.NotFound; // modèle inconnu chez ce fournisseur

        Assert.That(await Ask(client, route), Is.EqualTo("réponse de beta3.test"));
        Assert.That(OllamaClient.PausedMinutes("Alpha3"), Is.EqualTo(0));
    }

    // ───── Formats des fournisseurs ─────

    [Test]
    public async Task Claude_APINative_SystemAEtLaCleDansXApiKey()
    {
        var (client, api, route, store) = Setup("4", "Claude", "Beta");
        store.Update(s => s.Ai.Nodes[0].Api = "anthropic");
        api.Reply["claude4.test"] = """{"content":[{"type":"thinking","thinking":""},{"type":"text","text":"Copy, box dans trois tours."}],"stop_reason":"end_turn"}""";
        string spec = "claude-haiku-5-5@Claude4";
        store.Update(s => s.Ai.Routes[0].Targets[0] = spec);

        string answer = await client.ChatAsync(route, new object[] { new { role = "system", content = "Tu es un ingénieur." }, new { role = "user", content = "Salut ?" } }, CancellationToken.None);

        Assert.That(answer, Is.EqualTo("Copy, box dans trois tours."));
        var req = api.Requests.Single();
        Assert.That(req.Path, Is.EqualTo("/v1/messages"));
        Assert.That(req.ApiKey, Is.EqualTo("k"));
        var body = System.Text.Json.Nodes.JsonNode.Parse(req.Body)!;
        Assert.That(body["system"]!.GetValue<string>(), Is.EqualTo("Tu es un ingénieur."), "le system n'est pas un message");
        Assert.That(body["messages"]!.AsArray().Count, Is.EqualTo(1));
        Assert.That(body["messages"]![0]!["role"]!.GetValue<string>(), Is.EqualTo("user"));
        Assert.That(body["output_config"]!["effort"]!.GetValue<string>(), Is.EqualTo("low"));
        Assert.That(body["max_tokens"]!.GetValue<int>(), Is.GreaterThanOrEqualTo(2048), "la réflexion ne doit pas manger la réponse");
    }

    [Test]
    public async Task Claude_RefusOuReponseVide_PasseAuFournisseurSuivant()
    {
        var (client, api, route, store) = Setup("5", "Claude", "Beta");
        store.Update(s => { s.Ai.Nodes[0].Api = "anthropic"; s.Ai.Routes[0].Targets[0] = "claude-opus-5-5@Claude5"; });
        api.Reply["claude5.test"] = """{"content":[],"stop_reason":"refusal"}""";

        Assert.That(await Ask(client, route), Is.EqualTo("réponse de beta5.test"));
        Assert.That(OllamaClient.PausedMinutes("Claude5"), Is.EqualTo(0), "un refus ne met pas le fournisseur en pause");
    }

    [Test]
    public async Task FournisseurOpenAI_UtiliseSonCheminD_API()
    {
        var (client, api, route, store) = Setup("6", "Glm");
        store.Update(s => s.Ai.Nodes[0].ApiPath = "/api/paas/v4");

        await Ask(client, route);

        Assert.That(api.Requests.Single().Path, Is.EqualTo("/api/paas/v4/chat/completions"));
    }

    // ───── Chaîne de routes et rôles ─────

    [Test]
    public void Import_EnchaineLesRoutesEtRegleLesRoles()
    {
        var ai = new AiSettings();
        ai.Nodes.Add(Gateway("NVIDIA", "https://integrate.api.nvidia.com", "k"));
        ai.ThinkModel = "qwen3.5:9b";
        AiRouting.Report report = AiRouting.Import("""
            { "routes": [
                { "name": "gemma", "targets": ["google/gemma-4-31b-it@NVIDIA"], "then": "nemotron" },
                { "name": "nemotron", "targets": ["nvidia/nemotron-3-ultra-550b-a55b@NVIDIA"], "then": "inconnue" }
              ],
              "assign": { "think": "gemma", "hard": "nemotron", "specialists": { "pneus": "gemma", "strategie": "nemotron", "voiture": "qwen3.5:9b" } } }
            """, ai);

        Assert.That(ai.Routes.Single(r => r.Name == "gemma").Then, Is.EqualTo("nemotron"));
        Assert.That(ai.Routes.Single(r => r.Name == "nemotron").Then, Is.Empty, "route suivante inconnue : la chaîne s'arrête");
        Assert.That(ai.ThinkModel, Is.EqualTo("gemma"));
        Assert.That(ai.HardModel, Is.EqualTo("nemotron"));
        Assert.That(ai.Specialists["pneus"], Is.EqualTo("gemma"));
        Assert.That(ai.Specialists["stratégie"], Is.EqualTo("nemotron"), "« strategie » sans accent accepté");
        Assert.That(ai.Specialists.ContainsKey("voiture"), Is.False, "seulement des routes, pas un modèle quelconque");
        Assert.That(report.Assigned, Does.Contain("Réflexion → gemma"));
        Assert.That(report.Warnings.Count, Is.EqualTo(2), string.Join(" | ", report.Warnings));
    }

    [Test]
    public async Task Chaine_ModeleEpuisePartout_PasseALaRouteSuivante()
    {
        var (client, api, route, store) = Setup("7", "Alpha", "Beta");
        store.Update(s =>
        {
            s.Ai.Routes[0].Targets = new() { "gemma@Alpha7" };
            s.Ai.Routes[0].Then = "suite7";
            s.Ai.Routes.Add(new AiRoute { Name = "suite7", Targets = { "nemotron@Beta7" }, Then = route }); // boucle : ne doit pas tourner sans fin
        });
        api.Status["alpha7.test"] = HttpStatusCode.PaymentRequired;

        Assert.That(await Ask(client, route), Is.EqualTo("réponse de beta7.test"));
        Assert.That(OllamaClient.AnsweredBy(route), Is.EqualTo("nemotron@Beta7"));

        api.Status["beta7.test"] = HttpStatusCode.PaymentRequired;
        Assert.ThrowsAsync<AiHttpException>(() => Ask(client, route), "tout épuisé : erreur, pas de boucle infinie");
    }
}
