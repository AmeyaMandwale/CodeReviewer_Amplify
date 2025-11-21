// using Microsoft.AspNetCore.Mvc;
// using System.Text.Json;
// using GitHubIntegrationBackend.Data;
// using GitHubIntegrationBackend.Models;
// using GitHubIntegrationBackend.Services;
// using Microsoft.EntityFrameworkCore;


// namespace GitHubIntegrationBackend.Controllers
// {
//     [Route("api/gitlab")]
//     public class GitLabController : ControllerBase
//     {
//         private readonly IConfiguration _config;
//         private readonly HttpClient _http;

//         public GitLabController(IConfiguration config, IHttpClientFactory factory)
//         {
//             _config = config;
//             _http = factory.CreateClient();
//         }

//         [HttpGet("login")]
//         public IActionResult Login()
//         {
//             var clientId = _config["GITLAB_CLIENT_ID"];
//             var redirectUri = _config["GITLAB_REDIRECT_URI"];
//             var scope = "api read_user read_repository";

//             var authUrl = $"https://gitlab.com/oauth/authorize?client_id={clientId}&redirect_uri={redirectUri}&response_type=code&scope={scope}";
//             return Redirect(authUrl);
//         }

//         [HttpGet("callback")]
//         public async Task<IActionResult> Callback(string code)
//         {
//             var clientId = _config["GITLAB_CLIENT_ID"];
//             var clientSecret = _config["GITLAB_CLIENT_SECRET"];
//             var redirectUri = _config["GITLAB_REDIRECT_URI"];

//             var tokenResponse = await _http.PostAsync("https://gitlab.com/oauth/token", new FormUrlEncodedContent(new Dictionary<string, string>
//         {
//             {"client_id", clientId},
//             {"client_secret", clientSecret},
//             {"code", code},
//             {"grant_type", "authorization_code"},
//             {"redirect_uri", redirectUri}
//         }));

//             var tokenJson = await tokenResponse.Content.ReadAsStringAsync();

// // Parse only access_token (optional but cleaner)
// using var doc = JsonDocument.Parse(tokenJson);
// var accessToken = doc.RootElement.GetProperty("access_token").GetString();

// // Redirect back to your React app with token in query param
// var redirectFrontend = $"http://localhost:5173/?gitlab_token={accessToken}";
// return Redirect(redirectFrontend);

//         }
//     }
// }
