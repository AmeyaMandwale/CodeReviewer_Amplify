using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GitHubIntegrationBackend.Data;
using GitHubIntegrationBackend.Models;
using System.Text.Json;

namespace GitHubIntegrationBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RulePackController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RulePackController(AppDbContext context)
        {
            _context = context;
        }

        // ============================
        // 🔹 GET ALL Rule Packs
        // ============================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var packs = await _context.RulePacks.ToListAsync();
            return Ok(packs);
        }

        // ============================
        // 🔹 GET Rule Packs by Org
        // ============================
        [HttpGet("org/{orgId}")]
        public async Task<IActionResult> GetByOrg(int orgId)
        {
            var packs = await _context.RulePacks
                .Where(r => r.OrgId == orgId)
                .ToListAsync();

            return Ok(packs);
        }

        // ============================
        // 🔹 GET Single Rule Pack
        // ============================
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var pack = await _context.RulePacks.FindAsync(id);
            if (pack == null)
                return NotFound(new { message = "Rule Pack not found" });

            return Ok(pack);
        }

        // ============================
        // 🔹 CREATE Rule Pack
        // ============================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RulePack pack)
        {
            if (!_context.Organizations.Any(o => o.Id == pack.OrgId))
                return BadRequest("Invalid OrgId");

            _context.RulePacks.Add(pack);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = pack.Id }, pack);
        }

        // ============================
        // 🔹 UPDATE Rule Pack
        // ============================
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] RulePack updated)
        {
            if (id != updated.Id)
                return BadRequest("ID mismatch");

            var existing = await _context.RulePacks.FindAsync(id);
            if (existing == null)
                return NotFound("Rule Pack not found");

            existing.Name = updated.Name;
            existing.Type = updated.Type;
            existing.Enabled = updated.Enabled;
            existing.Rules = updated.Rules;

            await _context.SaveChangesAsync();

            return Ok(existing);
        }

        // ============================
        // 🔹 DELETE Rule Pack
        // ============================
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var rp = await _context.RulePacks.FindAsync(id);
            if (rp == null)
                return NotFound(new { message = "Rule Pack not found" });

            _context.RulePacks.Remove(rp);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Rule Pack deleted successfully" });
        }

        // ============================
        // 🔹 ENABLE / DISABLE Rule Pack
        // ============================
        [HttpPut("{id}/toggle")]
        public async Task<IActionResult> ToggleRulePack(int id)
        {
            var pack = await _context.RulePacks.FindAsync(id);
            if (pack == null)
                return NotFound("Rule Pack not found");

            pack.Enabled = !pack.Enabled;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Rule pack updated",
                enabled = pack.Enabled
            });
        }

        // ============================
        // 🔹 UPDATE ONLY RULES JSON
        // ============================
        [HttpPut("{id}/rules")]
        public async Task<IActionResult> UpdateRules(int id, [FromBody] JsonElement rulesJson)
        {
            var pack = await _context.RulePacks.FindAsync(id);
            if (pack == null)
                return NotFound("Rule Pack not found");

            pack.Rules = JsonSerializer.Serialize(rulesJson);

            await _context.SaveChangesAsync();

            return Ok(pack);
        }
    }
}
