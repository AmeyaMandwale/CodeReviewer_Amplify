// using System.Net.Http.Headers;
// using System.Text;
// using System.Text.Json;
// using GitHubIntegrationBackend.Models;
// using Microsoft.Extensions.Configuration;

// public class GitHubService
// {
//     private readonly HttpClient _httpClient;
//     private readonly IConfiguration _config;

//     public GitHubService(HttpClient httpClient, IConfiguration config)
//     {
//         _httpClient = httpClient;
//         _config = config;
//     }

//     public async Task<string> ExchangeCodeForTokenAsync(string code)
// {
//     var requestData = new
//     {
//         client_id = _config["GitHub:ClientId"],
//         client_secret = _config["GitHub:ClientSecret"],
//         code,
//         redirect_uri = _config["GitHub:RedirectUri"]
//     };

//     var request = new HttpRequestMessage(
//         HttpMethod.Post,
//         "https://github.com/login/oauth/access_token")
//     {
//         Content = new StringContent(
//             JsonSerializer.Serialize(requestData),
//             Encoding.UTF8,
//             "application/json")
//     };

//     request.Headers.Accept.Add(
//         new MediaTypeWithQualityHeaderValue("application/json"));

//     var response = await _httpClient.SendAsync(request);
//     var json = await response.Content.ReadAsStringAsync();
//     Console.WriteLine("🔁 GitHub raw response: " + json);

//     if (!response.IsSuccessStatusCode)
//         throw new Exception($"GitHub OAuth failed: {json}");

//     using var doc = JsonDocument.Parse(json);
//     if (!doc.RootElement.TryGetProperty("access_token", out var token))
//         throw new Exception("Access token not found in response.");

//     return token.GetString();
// }


//     public async Task<string> PostReviewCommentAsync(
//         string repoOwner, string repoName, int prNumber,
//         GeminiResponse geminiResponse, string accessToken)
//     {
//         var commentBody = $@"
// ###  Gemini AI Code Review
// **Summary:** {geminiResponse.Summary}

// **Key Changes:**  
// {geminiResponse.KeyChanges}

// **Suggestions:**  
// {geminiResponse.Suggestions}
// ";

//         var request = new HttpRequestMessage(
//             HttpMethod.Post,
//             $"https://api.github.com/repos/{repoOwner}/{repoName}/issues/{prNumber}/comments");

//         request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
//         request.Headers.UserAgent.ParseAdd("GitHubIntegrationApp");

//         request.Content = new StringContent(
//             JsonSerializer.Serialize(new { body = commentBody }),
//             Encoding.UTF8, "application/json");

//         var response = await _httpClient.SendAsync(request);
//         response.EnsureSuccessStatusCode();

//         var result = await response.Content.ReadAsStringAsync();
//         using var doc = JsonDocument.Parse(result);
//         return doc.RootElement.GetProperty("html_url").GetString();
//     }

//     public async Task<string> GetPullRequestsAsync(string repoOwner, string repoName, string accessToken)
//     {
//         var request = new HttpRequestMessage(
//             HttpMethod.Get,
//             $"https://api.github.com/repos/{repoOwner}/{repoName}/pulls");

//         request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
//         request.Headers.UserAgent.ParseAdd("GitHubIntegrationApp");

//         var response = await _httpClient.SendAsync(request);
//         return await response.Content.ReadAsStringAsync();
//     }

//     public async Task<string> GetUserActivityAsync(string username, string accessToken)
//     {
//         var request = new HttpRequestMessage(
//             HttpMethod.Get,
//             $"https://api.github.com/users/{username}/events");

//         request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
//         request.Headers.UserAgent.ParseAdd("GitHubIntegrationApp");

//         var response = await _httpClient.SendAsync(request);
//         return await response.Content.ReadAsStringAsync();
//     }
// }

using System.Net.Http.Headers;
using System.Text.Json;

namespace GitHubIntegrationBackend.Services
{
    public class GitHubService
    {
        private readonly HttpClient _client;
        private readonly IConfiguration _config;

        public GitHubService(HttpClient client, IConfiguration config)
        {
            _client = client;
            _config = config;
            _client.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("MyApp", "1.0")
            );
        }

        public async Task<string?> ExchangeCodeForTokenAsync(string code)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://github.com/login/oauth/access_token");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "client_id", _config["GITHUB_CLIENT_ID"] },
                { "client_secret", _config["GITHUB_CLIENT_SECRET"] },
                { "code", code }
            });

            var resp = await _client.SendAsync(request);
            resp.EnsureSuccessStatusCode();

            var json = await resp.Content.ReadAsStringAsync();
            var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

            return obj?["access_token"]?.ToString();
        }

        public async Task<(bool ok, string body, int status)> GetUserReposAsync(string token)
        {
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            _client.DefaultRequestHeaders.UserAgent.Clear();
            _client.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("MyApp", "1.0")
            );

            var resp = await _client.GetAsync("https://api.github.com/user/repos");
            var body = await resp.Content.ReadAsStringAsync();
            return (resp.IsSuccessStatusCode, body, (int)resp.StatusCode);
        }

        public async Task<(bool ok, string body, int status)> CreateWebhookAsync(
            string token,
            string owner,
            string repo,
            string webhookUrl,
            string secret)
        {
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            _client.DefaultRequestHeaders.UserAgent.Clear();
            _client.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("MyApp", "1.0")
            );

            var bodyData = new
            {
                name = "web",
                active = true,
                events = new[] { "push", "pull_request" },
                config = new
                {
                    url = webhookUrl,
                    content_type = "json",
                    secret
                }
            };

            var req = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://api.github.com/repos/{owner}/{repo}/hooks"
            );

            req.Content = new StringContent(JsonSerializer.Serialize(bodyData),
                System.Text.Encoding.UTF8,
                "application/json");

            var resp = await _client.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();

            return (resp.IsSuccessStatusCode, body, (int)resp.StatusCode);
        }
    }
}
