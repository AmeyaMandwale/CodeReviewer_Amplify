// using System.Net.Http;
// using System.Net.Http.Json;
// using System.Text.Json;

// namespace GitHubIntegrationBackend.Services
// {
//     public class GeminiService
//     {
//         private readonly HttpClient _httpClient;
//         private readonly IConfiguration _config;
//         private readonly string _model = "gemini-2.5-pro";

//         public GeminiService(IHttpClientFactory factory, IConfiguration config)
//         {
//             _httpClient = factory.CreateClient();
//             _config = config;
//         }

//         private string GeminiUrl =>
//             $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_config["Gemini:ApiKey"]}";

//         public async Task<string> GenerateAsync(string prompt, CancellationToken ct = default)
//         {
//             var payload = new
//             {
//                 contents = new[]
//                 {
//                     new { parts = new[] { new { text = prompt } } }
//                 }
//             };

//             var resp = await _httpClient.PostAsJsonAsync(GeminiUrl, payload, ct);
//             resp.EnsureSuccessStatusCode();
//             var json = await resp.Content.ReadAsStringAsync(ct);

//             using var doc = JsonDocument.Parse(json);
//             // Defensive extraction
//             if (doc.RootElement.TryGetProperty("candidates", out var candidates)
//                 && candidates.GetArrayLength() > 0
//                 && candidates[0].TryGetProperty("content", out var content)
//                 && content.TryGetProperty("parts", out var parts)
//                 && parts.GetArrayLength() > 0
//                 && parts[0].TryGetProperty("text", out var textEl))
//             {
//                 return textEl.GetString() ?? string.Empty;
//             }

//             return "⚠️ No meaningful response from Gemini.";
//         }
//     }
// }

using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace GitHubIntegrationBackend.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private const string Model = "gemini-2.5-pro";

        public GeminiService(IHttpClientFactory factory, IConfiguration config)
        {
            _httpClient = factory.CreateClient();
            _config = config;
        }

        private string GeminiUrl =>
            $"https://generativelanguage.googleapis.com/v1beta/models/{Model}:generateContent?key=AIzaSyAOuJY2KOrQO5KwaYyY6jqG0IWWrzsIR2A";

        public async Task<string> GenerateAsync(string prompt, CancellationToken ct = default)
        {
            var payload = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            };

            var response = await _httpClient.PostAsJsonAsync(GeminiUrl, payload, ct);

            if (!response.IsSuccessStatusCode)
                return $"Gemini failed: {response.StatusCode}";

            var json = await response.Content.ReadAsStringAsync(ct);

            try
            {
                using var doc = JsonDocument.Parse(json);

                var text =
                    doc.RootElement.GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();

                return text ?? "";
            }
            catch
            {
                return "⚠️ No meaningful response from Gemini";
            }
        }
    }
}
