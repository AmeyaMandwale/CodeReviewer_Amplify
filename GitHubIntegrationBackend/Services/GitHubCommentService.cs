using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using GitHubIntegrationBackend.Data;
using Microsoft.EntityFrameworkCore;

namespace GitHubIntegrationBackend.Services
{
    public class GitHubCommentService
    {
        private readonly AppDbContext _ctx;
        private readonly IHttpClientFactory _factory;
        private readonly GitHubPRFileService _prFileService;
        public GitHubCommentService(AppDbContext ctx, IHttpClientFactory factory, GitHubPRFileService prFileService)
        {
            _ctx = ctx;
            _factory = factory;
            _prFileService = prFileService;
        }

        // ---------------- EXISTING METHOD (UNCHANGED) ----------------
        public async Task<bool> AddCommentAsync(string owner, string repo, string externalId, string comment)
        { 
            var integration = await _ctx.Integrations
                .FirstOrDefaultAsync(i => i.Provider == "github");

            if (integration == null)
                throw new Exception("❌ GitHub Integration not found");

            var token = ExtractToken(integration.Config);

            var http = _factory.CreateClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("App");
            http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // Convert GitHub internal ID → PR number
            var prNumber = await _prFileService.GetPRNumberFromExternalId(http, owner, repo, externalId);
            if (prNumber == null)
                throw new Exception("❌ Could not resolve PR number for comment.");

            string url = $"https://api.github.com/repos/{owner}/{repo}/issues/{prNumber}/comments";

            var payload = new { body = comment };
            var res = await http.PostAsJsonAsync(url, payload);

            return res.IsSuccessStatusCode;
        }

        // ---------------- NEW METHOD TO APPEND REPORT URL ----------------
        public async Task<bool> AddCommentWithReportAsync(
            string owner,
            string repo,
            string externalId,
            string baseComment,
            string reportUrl)
        {

            // Prepare the final combined comment
            string finalComment =
                $"{baseComment}\n\n" +
                $"According to the current PR, the raised issues or modifications can be reviewed here:\n\n" +
                $"➡️ {reportUrl}";

            // Reuse existing method (NO breaking change)
            return await AddCommentAsync(owner, repo, externalId, finalComment);
        }


        // ---------------- EXISTING AI COMMENT LOGIC (UNCHANGED) ----------------
        public async Task<DateTime?> GetLastAICommentTimeAsync(HttpClient http, string owner, string repo, int prNumber)
        {
            var url = $"https://api.github.com/repos/{owner}/{repo}/issues/{prNumber}/comments";
            var res = await http.GetStringAsync(url);

            var comments = JsonSerializer.Deserialize<List<GitHubCommentDto>>(res);

            var aiComment = comments?
                .Where(c => c.body != null && c.body.Contains("[AI-REVIEW]"))
                .OrderByDescending(c => c.created_at)
                .FirstOrDefault();

            return aiComment?.created_at;
        }

        public class GitHubCommentDto
        {
            public string body { get; set; }
            public DateTime created_at { get; set; }
        }

        private string ExtractToken(string config)
        {
            try
            {
                var root = JsonDocument.Parse(config);
                if (root.RootElement.TryGetProperty("access_token", out var t))
                    return t.GetString()!;
            }
            catch { }

            return config;
        }
    }
}
