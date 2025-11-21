using GitHubIntegrationBackend.Data;
using GitHubIntegrationBackend.Services;
using Microsoft.EntityFrameworkCore;
using GitHubIntegrationBackend.Models;

public class PRFileSyncService
{
    private readonly AppDbContext _ctx;
    private readonly GitHubPRFileService _githubPRFileService;
    private readonly GitLabPRFileService _gitlabPRFileService;
    private readonly RulePackService _rules;
    private readonly GeminiService _gemini;
     private readonly GitLabCommentService _gitlabComment;
private readonly GitHubCommentService _githubComment;

private readonly AnalysisResultService _analysisResultService;
    public PRFileSyncService(AppDbContext ctx, GitHubPRFileService githubPRFileService,  GitLabPRFileService gitlabPRFileService, GeminiService gemini,
    GitHubCommentService githubComment, GitLabCommentService gitlabComment,RulePackService rules,AnalysisResultService analysisResultService)
    {
        _ctx = ctx;
        _githubPRFileService = githubPRFileService;
         _gitlabPRFileService = gitlabPRFileService;
         _gemini = gemini;
    _githubComment = githubComment;
     _gitlabComment = gitlabComment;
    _rules = rules;
    _analysisResultService = analysisResultService;
    }



   public async Task SyncPRFiles(int prId)
{
    var pr = await _ctx.PullRequests.Include(p => p.Repository).FirstOrDefaultAsync(p => p.Id == prId);
    if (pr == null)
        throw new Exception("PR not found");

    var provider = pr.Repository.Provider.ToLower();   // github | gitlab
    var files = new List<dynamic>();

    if (provider == "github")
    {
        var parts = pr.Repository.Name.Split('/');
        if (parts.Length != 2)
            throw new Exception("Invalid GitHub repository name format. Expected: owner/repo");

        var owner = parts[0];
        var repo = parts[1];

        var result = await _githubPRFileService.GetPRFilesAsync(owner, repo, pr.ExternalId);
        files.AddRange(result);
    }
    else if (provider == "gitlab")
{
    if (string.IsNullOrWhiteSpace(pr.ExternalId))
        throw new Exception("GitLab ExternalId missing");

    var parts = pr.ExternalId.Split(':');
    if (parts.Length != 2)
        throw new Exception($"Invalid ExternalId format for GitLab PR: {pr.ExternalId}");

    var iid = parts[0];
    var projectId = parts[1];

    var result = await _gitlabPRFileService.GetPRFilesAsync(projectId, iid);
    files.AddRange(result);
}

    else
    {
        throw new Exception($"Unknown provider: {provider}");
    }

    // remove old files
    var old = _ctx.PRFiles.Where(f => f.PrId == prId);
    _ctx.PRFiles.RemoveRange(old);

    // add new files
    foreach (var f in files)
    {
        if (provider == "github")
        {
            var gh = (GitHubFileDto)f;
            _ctx.PRFiles.Add(new PRFile
            {
                PrId = prId,
                Path = gh.filename,
                ChangeType = gh.status,
                Diff = gh.patch
            });
        }
        else if (provider == "gitlab")
        {
            var gl = (GitLabFileDto)f;
            _ctx.PRFiles.Add(new PRFile
            {
                PrId = prId,
                Path = gl.new_path,
                ChangeType = "modified",  // GitLab does not provide exact change type
                Diff = gl.diff
            });
        }
    }

    await _ctx.SaveChangesAsync();

    // -------------------------------------------------
        // 2️⃣ CHECK IF NEW COMMITS EXIST (OPTION B LOGIC)
        // -------------------------------------------------

        var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd("App");

        bool shouldRunGemini = false;

        if (provider == "github")
        {
            var parts = pr.Repository.Name.Split('/');
            var owner = parts[0];
            var repo = parts[1];

            var prNumber = await _githubPRFileService
                .GetPRNumberFromExternalId(http, owner, repo, pr.ExternalId);
             if (prNumber == null)
{
    Console.WriteLine($"⚠ Cannot resolve PR number for PR {pr.Id}. ExternalId={pr.ExternalId}");
    return;
}
            var lastCommit = await _githubPRFileService
                .GetLastCommitTimeAsync(http, owner, repo, prNumber.Value);

            var lastAIComment = await _githubComment
                .GetLastAICommentTimeAsync(http, owner, repo, prNumber.Value);

            shouldRunGemini = lastAIComment == null || lastCommit > lastAIComment;
        }
        else if (provider == "gitlab")
        {
            var parts = pr.ExternalId.Split(':');
            var iid = parts[0];
            var projectId = parts[1];

            var lastCommit = await _gitlabPRFileService
                .GetLastCommitTimeAsync(http, projectId, iid);

            var lastAIComment = await _gitlabComment
                .GetLastAICommentTimeAsync(projectId, iid, http);

            bool isNewMR = lastAIComment == null && files.Count > 0;

shouldRunGemini = isNewMR;
        }

        if (!shouldRunGemini)
        {
            Console.WriteLine($"⏭ Skipping Gemini for PR {pr.Id} — no new commits.");
            return;
        }

     var ruleContent = await _rules.GetEnabledRulesForOrg(pr.Repository.OrgId);

        // --------------------------
        // 🔹 5. Build AI Prompt (350 words)
        // --------------------------
        var fileBlocks = string.Join("\n\n", files.Select(f =>
            provider == "github"
                ? $"FILE: {((GitHubFileDto)f).filename}\nPATCH:\n{((GitHubFileDto)f).patch}"
                : $"FILE: {((GitLabFileDto)f).new_path}\nPATCH:\n{((GitLabFileDto)f).diff}"
        ));

        var fullPrompt = $@"
You are a senior code reviewer AI.  
Follow these RULES strictly:

{ruleContent}

Below is the pull request code diff:

{fileBlocks}

Generate a **350-word structured review** containing:

1. Summary (100–120 words)
2. Major Suggestions (150–180 words)
3. Key Changes (80–100 words)

Be developer-friendly and apply the above rulepacks.
";

        // --------------------------
        // 🔹 6. Generate Gemini Analysis
        // --------------------------
        var analysis = await _gemini.GenerateAsync(fullPrompt);
         var finalComment = "[AI-REVIEW]\n\n" + analysis;
        // --------------------------
        // 🔹 7. Post PR Comment (GitHub only)
        // --------------------------
        if (provider == "github")
        {
            var parts = pr.Repository.Name.Split('/');
            var owner = parts[0];
            var repo = parts[1];

            string? reportUrl= await GetReportUrlByRepoAsync(repo);
           // await _githubComment.AddCommentAsync(owner, repo, pr.ExternalId!, finalComment);
            await _githubComment.AddCommentWithReportAsync(
                owner,
                repo,
                pr.ExternalId,
                finalComment,
                 reportUrl  
            );


        }
         else if (provider == "gitlab")
        {
            await _gitlabComment.AddCommentAsync(pr.ExternalId!, finalComment);
        }

// SAVE ANALYSIS RESULT INTO DB
await _analysisResultService.SaveAnalysisAsync(
    prId,
    finalComment,
    "Gemini",
    "AI-Review"
);

}

// TO FETCH URL OF GENERATED SONARQUBE REPORT
public async Task<string?> GetReportUrlByRepoAsync(string repo)
{
    var report = await _ctx.ReportFiles
        .Where(r => r.ProjectKey == repo)
        .OrderByDescending(r => r.CreatedAt)
        .FirstOrDefaultAsync();

    return report?.FileUrl;
}


}
