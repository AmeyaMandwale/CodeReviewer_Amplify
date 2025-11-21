using GitHubIntegrationBackend.Data;
using GitHubIntegrationBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace GitHubIntegrationBackend.Services
{
    public class PRReviewService
    {
        private readonly AppDbContext _ctx;
        private readonly PRFileSyncService _sync;
        private readonly GeminiService _gemini;
        private readonly GitHubPRCommentService _comment;

        public PRReviewService(
            AppDbContext ctx,
            PRFileSyncService sync,
            GeminiService gemini,
            GitHubPRCommentService comment)
        {
            _ctx = ctx;
            _sync = sync;
            _gemini = gemini;
            _comment = comment;
        }

        public async Task ReviewPR(int prId)
        {
            // ✅ Ensure DB PR exists
            var pr = await _ctx.PullRequests
                .Include(p => p.Repository)
                .FirstOrDefaultAsync(p => p.Id == prId);

            if (pr == null)
                throw new Exception("❌ PR not found");

            if (string.IsNullOrWhiteSpace(pr.ExternalId))
                throw new Exception("❌ External PR number missing");

            // Split full repo name "owner/repo"
            var parts = pr.Repository.Name.Split('/');
            if (parts.Length != 2)
                throw new Exception($"❌ Invalid repo name: {pr.Repository.Name}");

            var owner = parts[0];
            var repo = parts[1];
            var prNumber = int.Parse(pr.ExternalId);

            // ✅ step 1 — sync files (optional)
            await _sync.SyncPRFiles(prId);

            // ✅ step 2 — get files from DB
            var files = await _ctx.PRFiles
                .Where(f => f.PrId == prId)
                .ToListAsync();

            if (!files.Any())
                throw new Exception("❌ No PR files found to review");

            // Combine all file patches in one prompt
            var allDiff = string.Join("\n---\n", files.Select(f =>
                $"FILE: {f.Path}\n{f.Diff}"
            ));

            var prompt =
                "You are a senior code reviewer. Analyze the PR diff and provide summary of issues and improvements:\n\n"
                + allDiff;

            // ✅ step 3 — send to Gemini
            var aiResponse = await _gemini.GenerateAsync(prompt);

            // ✅ step 4 — post as GitHub comment
            await _comment.PostCommentAsync(owner, repo, prNumber, aiResponse);
        }
    }
}
