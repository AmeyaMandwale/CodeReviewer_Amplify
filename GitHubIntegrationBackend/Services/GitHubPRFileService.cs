// using System.Net.Http.Headers;
// using System.Text.Json;

// public class GitHubPRFilesService
// {
//     public static async Task<List<GitHubFileDto>> GetPRFiles(
//         string owner,
//         string repo,
//         int prNumber,
//         string token)
//     {
//         using var http = new HttpClient();
//         http.DefaultRequestHeaders.Authorization =
//             new AuthenticationHeaderValue("Bearer", token);
//         http.DefaultRequestHeaders.Add("User-Agent", "App");

//         var url = $"https://api.github.com/repos/{owner}/{repo}/pulls/{prNumber}/files";
//         var json = await http.GetStringAsync(url);

//         var files = JsonSerializer.Deserialize<List<GitHubFileDto>>(json);

//         return files ?? new List<GitHubFileDto>();
//     }
// }

// public class GitHubFileDto
// {
//     public string filename { get; set; }
//     public string status { get; set; }
//     public string patch { get; set; }
// }

using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using GitHubIntegrationBackend.Data;

namespace GitHubIntegrationBackend.Services
{
    public class GitHubPRFileService
    {
        private readonly AppDbContext _ctx;
        private readonly IHttpClientFactory _factory;

        public GitHubPRFileService(AppDbContext ctx, IHttpClientFactory factory)
        {
            _ctx = ctx;
            _factory = factory;
        }

        public async Task<List<GitHubFileDto>> GetPRFilesAsync(
            string owner,
            string repo,
            string externalId)
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

            // ✅ convert stored external ID → GitHub PR number
            var prNumber = await GetPRNumberFromExternalId(http, owner, repo, externalId);

            if (prNumber == null)
                throw new Exception($" Could not find PR number for ExternalId={externalId}");


            string url =
                $"https://api.github.com/repos/{owner}/{repo}/pulls/{prNumber}/files";

            Console.WriteLine($"CALL: {url}");

            var res = await http.GetAsync(url);

            res.EnsureSuccessStatusCode();

            var json = await res.Content.ReadAsStringAsync();
            var files = JsonSerializer.Deserialize<List<GitHubFileDto>>(json);

            return files ?? new List<GitHubFileDto>();
        }

        public async Task<int?> GetPRNumberFromExternalId(
            HttpClient http,
            string owner,
            string repo,
            string externalId)
        {
            string listUrl =
                $"https://api.github.com/repos/{owner}/{repo}/pulls?state=all&per_page=200";

            Console.WriteLine($"CALL: {listUrl}");

            var res = await http.GetAsync(listUrl);

            if (!res.IsSuccessStatusCode)
                return null;

            var json = await res.Content.ReadAsStringAsync();

            var prs = JsonSerializer.Deserialize<List<GitHubPRListDto>>(json);

            var match =
                prs?.FirstOrDefault(p => p.id.ToString() == externalId);

            return match?.number;
        }
        
        public async Task<DateTime?> GetLastCommitTimeAsync(HttpClient http, string owner, string repo, int prNumber)
{
    var url = $"https://api.github.com/repos/{owner}/{repo}/pulls/{prNumber}";
    var res = await http.GetStringAsync(url);

    var dto = JsonSerializer.Deserialize<GitHubPRDetailDto>(res);

    return dto?.updated_at;
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
    
    public class GitHubPRDetailDto
{
    public DateTime updated_at { get; set; }
}
    public class GitHubFileDto
    {
        public string filename { get; set; } = "";
        public string status { get; set; } = "";
        public string? patch { get; set; }
    }

    public class GitHubPRListDto
    {
        public long id { get; set; }
        public int number { get; set; }
    }

    public class GitHubCommentDto
{
    public string body { get; set; }
}

}

