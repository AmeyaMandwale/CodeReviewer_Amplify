// using GitHubIntegrationBackend.Models;
// using Microsoft.AspNetCore.Mvc;

// [ApiController]
// [Route("github")]
// public class GitHubController : ControllerBase
// {
//     private readonly GitHubService _gitHubService;

//     public GitHubController(GitHubService gitHubService)
//     {
//         _gitHubService = gitHubService;
//     }

//     [HttpPost("postReview")]
// public async Task<IActionResult> PostReview([FromBody] PostReviewRequest request)
// {
//     var commentUrl = await _gitHubService.PostReviewCommentAsync(
//         request.RepoOwner,
//         request.RepoName,
//         request.PrNumber,
//         request.GeminiResponse,
//         request.AccessToken
//     );

//     return Ok(new { success = true, commentUrl });
// }


//     [HttpGet("prs")]
//     public async Task<IActionResult> GetPRs([FromQuery] string repoOwner, [FromQuery] string repoName, [FromQuery] string accessToken)
//     {
//         var data = await _gitHubService.GetPullRequestsAsync(repoOwner, repoName, accessToken);
//         return Content(data, "application/json");
//     }

//     [HttpGet("activity")]
//     public async Task<IActionResult> GetActivity([FromQuery] string username, [FromQuery] string accessToken)
//     {
//         var data = await _gitHubService.GetUserActivityAsync(username, accessToken);
//         return Content(data, "application/json");
//     }
// }

// using Microsoft.AspNetCore.Mvc;
// using System.Net.Http.Headers;
// using System.Text.Json;
// using System.Text;
// using GitHubIntegrationBackend.Data;

// namespace GitHubIntegrationBackend.Controllers
// {
//     [Route("api/[controller]")]
//     [ApiController]
//     public class GitHubController : ControllerBase
//     {
//         private readonly IHttpClientFactory _httpClientFactory;
//         private readonly AppDbContext _context;
//         private readonly IConfiguration _config;

//         public GitHubController(IHttpClientFactory httpClientFactory, AppDbContext context, IConfiguration config)
//         {
//             _httpClientFactory = httpClientFactory;
//             _context = context;
//             _config = config;
//         }

//         // STEP 1: Redirect user to GitHub OAuth
//         [HttpGet("login")]
//         public IActionResult Login()
//         {
//             var clientId = _config["GITHUB_CLIENT_ID"];
//             var redirectUri = _config["GITHUB_REDIRECT_URI"];
//             var githubUrl = $"https://github.com/login/oauth/authorize?client_id={clientId}&redirect_uri={redirectUri}&scope=repo,admin:repo_hook";
//             return Redirect(githubUrl);
//         }

//         // STEP 2: GitHub redirects here with ?code=...
//    [HttpGet("callback")]
// public async Task<IActionResult> Callback(string code)
// {
//     var client = _httpClientFactory.CreateClient();
//     client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
//     client.DefaultRequestHeaders.UserAgent.ParseAdd("CtplCodeReviewer/1.0");

//     var data = new Dictionary<string, string>
//     {
//         ["client_id"] = _config["GITHUB_CLIENT_ID"]!,
//         ["client_secret"] = _config["GITHUB_CLIENT_SECRET"]!,
//         ["code"] = code,
//         ["redirect_uri"] = _config["GITHUB_REDIRECT_URI"]!
//     };

//     var response = await client.PostAsync(
//         "https://github.com/login/oauth/access_token",
//         new FormUrlEncodedContent(data));

//     var content = await response.Content.ReadAsStringAsync();
//     Console.WriteLine("TOKEN RESPONSE: " + content);

//     var json = JsonSerializer.Deserialize<Dictionary<string, object>>(content);

//     if (json == null)
//         return BadRequest("Invalid GitHub response");

//     json.TryGetValue("access_token", out var tokenObj);
//     var accessToken = tokenObj?.ToString();

//     if (string.IsNullOrEmpty(accessToken))
//     {
//         return BadRequest(new
//         {
//             error = "No access token found",
//             github_response = json
//         });
//     }

//     return Redirect($"http://localhost:5173/code-overview/repositories?token={accessToken}");
// }



//         // STEP 3: List user repositories
//         [HttpGet("repos")]
// public async Task<IActionResult> GetRepos([FromQuery] string token)
// {
//     try
//     {
//         if (string.IsNullOrEmpty(token))
//             return BadRequest("Missing token");

//         var client = new HttpClient();
//         client.DefaultRequestHeaders.UserAgent.ParseAdd("request");
//         client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
//         client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

//         var response = await client.GetAsync("https://api.github.com/user/repos");

//         var body = await response.Content.ReadAsStringAsync();
//         Console.WriteLine("🔹 GitHub Response Status: " + response.StatusCode);
//         Console.WriteLine("🔹 GitHub Body Length: " + body.Length);
//         Console.WriteLine("🔹 GitHub Body: " + body);

//         if (!response.IsSuccessStatusCode)
//         {
//             return StatusCode((int)response.StatusCode, body);
//         }

//         var repos = JsonSerializer.Deserialize<object>(body);

//         return Ok(repos);
//     }
//     catch (Exception ex)
//     {
//         return StatusCode(500, ex.Message);
//     }
// }

//         // STEP 4: Connect a specific repository (creates webhook)
//         [HttpPost("connect-repo")]
//         public async Task<IActionResult> ConnectRepo([FromQuery] string token, [FromBody] RepoConnectRequest req)
//         {
//             var client = _httpClientFactory.CreateClient();
//             client.DefaultRequestHeaders.Authorization =
//                 new AuthenticationHeaderValue("Bearer", token);
//             client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("App", "1.0"));

//             var webhookPayload = new
//             {
//                 name = "web",
//                 active = true,
//                 events = new[] { "pull_request" },
//                 config = new
//                 {
//                     url = "http://localhost:5142/api/github/webhook",
//                     content_type = "json",
//                     secret = "mywebhooksecret"
//                 }
//             };

//             var response = await client.PostAsync(
//                 $"https://api.github.com/repos/{req.Owner}/{req.Repo}/hooks",
//                 new StringContent(JsonSerializer.Serialize(webhookPayload), Encoding.UTF8, "application/json"));

//             var content = await response.Content.ReadAsStringAsync();
//             return Ok(new { message = "Repo connected successfully", details = content });
//         }

//         // STEP 5: Receive PR webhook
//         [HttpPost("webhook")]
//         public async Task<IActionResult> ReceiveWebhook([FromBody] JsonElement payload)
//         {
//             var action = payload.GetProperty("action").GetString();
//             var pr = payload.GetProperty("pull_request").GetProperty("html_url").GetString();

//             Console.WriteLine($"🔔 PR Event: {action} - {pr}");
//             return Ok();
//         }
//     }

//     public class RepoConnectRequest
//     {
//         public string Owner { get; set; } = string.Empty;
//         public string Repo { get; set; } = string.Empty;
//     }
// }



// using Microsoft.AspNetCore.Mvc;
// using System.Text.Json;
// using GitHubIntegrationBackend.Data;
// using GitHubIntegrationBackend.Models;
// using GitHubIntegrationBackend.Services;
// using Microsoft.EntityFrameworkCore;


// namespace GitHubIntegrationBackend.Controllers
// {
//     [ApiController]
//     [Route("api/[controller]")]
//     public class GitHubController : ControllerBase
//     {
//         private readonly IConfiguration _config;
//         private readonly GitHubService _gitHubService;
//         private readonly AppDbContext _db;
//         private readonly ILogger<GitHubController> _logger;

//         public GitHubController(IConfiguration config, GitHubService gitHubService, AppDbContext db, ILogger<GitHubController> logger)
//         {
//             _config = config;
//             _gitHubService = gitHubService;
//             _db = db;
//             _logger = logger;
//         }

//         // Frontend calls /api/github/login?orgId=1
//         [HttpGet("login")]
//         public IActionResult Login([FromQuery] int orgId)
//         {
//             if (orgId <= 0)
//                 return BadRequest("orgId required");

//             var clientId = _config["GITHUB_CLIENT_ID"];
//             var redirectUri = _config["GITHUB_REDIRECT_URI"];

//             // random nonce
//             var nonce = Guid.NewGuid().ToString("N");
//             var state = $"org:{orgId}:{nonce}";

//             var url =
//                 $"https://github.com/login/oauth/authorize"
//                 + $"?client_id={clientId}"
//                 + $"&redirect_uri={redirectUri}"
//                 + $"&scope=repo,admin:repo_hook"
//                 + $"&state={state}";

//             return Redirect(url);
//         }



//         // callback: GitHub -> /api/github/callback?code=...&state=org:1:nonce
//         [HttpGet("callback")]
//         public async Task<IActionResult> Callback([FromQuery] string? code, [FromQuery] string? state)
//         {
//             if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
//                 return BadRequest("Missing code or state");

//             // Parse state → org:orgId:nonce
//             int orgId = 0;
//             try
//             {
//                 var parts = state.Split(':');
//                 if (parts.Length >= 2 && parts[0] == "org")
//                     orgId = int.Parse(parts[1]);
//             }
//             catch
//             {
//                 return BadRequest("Invalid state");
//             }

//             if (orgId <= 0)
//                 return BadRequest("Invalid org");

//             // TOKEN
//             var token = await _gitHubService.ExchangeCodeForTokenAsync(code);
//             if (string.IsNullOrEmpty(token))
//                 return BadRequest("GitHub token exchange failed");

//             // Save integration
//             var cfg = JsonSerializer.Serialize(new
//             {
//                 access_token = token,
//                 installed_at = DateTime.UtcNow
//             });

//             var integration =
//                 await _db.Integrations
//                     .FirstOrDefaultAsync(i =>
//                         i.OrgId == orgId &&
//                         i.Provider.ToLower() == "github");

//             if (integration == null)
//             {
//                 integration = new Integration
//                 {
//                     OrgId = orgId,
//                     Type = "scm",
//                     Provider = "github",
//                     Config = cfg
//                 };
//                 _db.Integrations.Add(integration);
//             }
//             else
//             {
//                 integration.Config = cfg;
//                 _db.Integrations.Update(integration);
//             }

//             await _db.SaveChangesAsync();

//             // ✅ Redirect to FE
//             var redirectFrontend =
//                 _config["FRONTEND_REPOSITORIES_URL"]
//                 ?? "http://localhost:5173/code-overview/repositories";

//             return Redirect($"{redirectFrontend}?orgId={orgId}");
//         }

//        [HttpGet("repos")]
// public async Task<IActionResult> GetRepos([FromQuery] int orgId)
// {
//     if (orgId <= 0)
//         return BadRequest("orgId required");

//     var integration = await _db.Integrations
//         .FirstOrDefaultAsync(i => i.OrgId == orgId && i.Provider == "github");

//     if (integration == null)
//         return NotFound("GitHub integration not found");

//     var cfg = JsonSerializer.Deserialize<Dictionary<string, object>>(integration.Config);
//     var token = cfg["access_token"]?.ToString();

//     if (string.IsNullOrEmpty(token))
//         return BadRequest("No GitHub token stored");

//     var (ok, body, status) = await _gitHubService.GetUserReposAsync(token);

//     if (!ok)
//         return StatusCode(status, body);

//     try
//     {
//         var repos = JsonSerializer.Deserialize<List<object>>(body)
//                    ?? new List<object>();

//         return Ok(repos);   // ✅ FE gets proper array
//     }
//     catch
//     {
//         return Ok(new List<object>());
//     }
// }


           
//     }
// }
