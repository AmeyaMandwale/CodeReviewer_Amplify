using GitHubIntegrationBackend.Data;
using Microsoft.EntityFrameworkCore;

public class RulePackService
{
    private readonly AppDbContext _ctx;

    public RulePackService(AppDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<string> GetEnabledRulesForOrg(int orgId)
    {
        var packs = await _ctx.RulePacks
            .Where(r => r.OrgId == orgId && r.Enabled == true)
            .ToListAsync();

        if (!packs.Any()) return "No active rules found.";

        // Combine all enabled rules
        var merged = string.Join("\n\n", packs
            .Where(p => !string.IsNullOrWhiteSpace(p.Rules))
            .Select(p => p.Rules));

        return merged;
    }
}
