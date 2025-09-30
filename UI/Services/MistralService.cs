using Newtonsoft.Json;
using RestSharp;

namespace ReleaseNotesGenerator.Services;

public class MistralService
{
    private readonly RestClient _client;
    private readonly string _apiKey;
    private readonly string _model;

    public MistralService(string apiKey, string model = "mistral-large-latest")
    {
        _client = new RestClient("https://api.mistral.ai");
        _apiKey = apiKey;
        _model = model;
    }

    public async Task<string> ImproveReleaseNotesAsync(string rawMarkdown)
    {
        var request = new RestRequest("/v1/chat/completions", Method.Post);
        request.AddHeader("Authorization", $"Bearer {_apiKey}");
        request.AddHeader("Content-Type", "application/json");

        var prompt = $@"Je veux que tu améliores ces release notes GitLab pour les rendre plus professionnelles et claires.

Objectifs :
1. Réécrire les commits en descriptions claires et compréhensibles
2. Grouper les changements similaires ensemble
3. Garder la structure markdown existante (titres, sections, etc.)
4. Ajouter des emojis appropriés si pertinent
5. Être concis mais informatif
6. Garder ABSOLUMENT toutes les références aux tickets JIRA et MRs (ne pas les supprimer)
7. Si possible, améliorer la formulation pour que ce soit plus professionnel

Voici les release notes brutes :

---

{rawMarkdown}

---

Merci de me retourner uniquement le markdown amélioré, sans commentaires additionnels.";

        var requestBody = new
        {
            model = _model,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = prompt
                }
            },
            temperature = 0.7,
            max_tokens = 4000
        };

        request.AddJsonBody(requestBody);

        try
        {
            var response = await _client.ExecuteAsync(request);

            if (!response.IsSuccessful || string.IsNullOrEmpty(response.Content))
            {
                throw new Exception($"Erreur API Mistral: {response.StatusCode} - {response.ErrorMessage}");
            }

            var jsonResponse = JsonConvert.DeserializeObject<dynamic>(response.Content);

            if (jsonResponse?.choices != null && jsonResponse.choices.Count > 0)
            {
                string? improvedText = jsonResponse.choices[0]?.message?.content?.ToString();
                if (!string.IsNullOrEmpty(improvedText))
                {
                    return improvedText.Trim();
                }
            }

            throw new Exception("Réponse API invalide");
        }
        catch (Exception ex)
        {
            throw new Exception($"Erreur lors de l'amélioration avec Mistral AI: {ex.Message}");
        }
    }
}