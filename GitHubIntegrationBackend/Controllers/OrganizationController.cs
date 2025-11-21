using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GitHubIntegrationBackend.Data;
using GitHubIntegrationBackend.Models;
using System.Text.Json;
namespace GitHubIntegrationBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrganizationController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OrganizationController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/organization
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Organization>>> GetOrganizations()
        {
            return await _context.Organizations.ToListAsync();
        }

        // GET: api/organization/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Organization>> GetOrganization(int id)
        {
            var org = await _context.Organizations.FindAsync(id);

            if (org == null)
                return NotFound(new { message = "Organization not found" });

            return org;
        }

        // POST: api/organization
        [HttpPost]
        public async Task<ActionResult<Organization>> CreateOrganization(Organization organization)
        {
            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetOrganization), new { id = organization.Id }, organization);
        }

        // PUT: api/organization/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrganization(int id, Organization updatedOrganization)
        {
            if (id != updatedOrganization.Id)
                return BadRequest(new { message = "ID mismatch" });

            _context.Entry(updatedOrganization).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Organizations.Any(e => e.Id == id))
                    return NotFound(new { message = "Organization not found" });
                else
                    throw;
            }

            return NoContent();
        }

        // DELETE: api/organization/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrganization(int id)
        {
            var org = await _context.Organizations.FindAsync(id);
            if (org == null)
                return NotFound(new { message = "Organization not found" });

            _context.Organizations.Remove(org);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Organization deleted successfully" });
        }

//        [HttpPost("{id}/upload-settings")]
// public async Task<IActionResult> UploadOrganizationSettings(
//     int id,
//     IFormFile file,
//     [FromForm] long guidelineId
// )
// {
//     try
//     {
//         var organization = await _context.Organizations.FindAsync(id);
//         if (organization == null)
//             return NotFound(new { message = "Organization not found" });

//         if (file == null || file.Length == 0)
//             return BadRequest(new { message = "No file uploaded" });

//         if (file.Length > 10 * 1024 * 1024)
//             return BadRequest(new { message = "File size exceeds 10MB limit" });

//         var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".txt" };
//         var ext = Path.GetExtension(file.FileName).ToLower();
//         if (!allowedExtensions.Contains(ext))
//             return BadRequest(new { message = "Invalid file type. Allowed: PDF, DOC, DOCX, TXT" });

//         using var memoryStream = new MemoryStream();
//         await file.CopyToAsync(memoryStream);
//         var base64Data = Convert.ToBase64String(memoryStream.ToArray());

//         // Prepare guideline entry
//         var guidelineEntry = new
//         {
//             id = guidelineId,
//             fileName = file.FileName,
//             fileType = ext,
//             fileContent = base64Data,
//             fileSize = file.Length,
//             uploadedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
//         };

//         List<object> guidelineList = new();

//         if (!string.IsNullOrWhiteSpace(organization.Settings))
//         {
//             try
//             {
//                 var settingsJson = JsonSerializer.Deserialize<JsonElement>(organization.Settings);

//                 if (settingsJson.TryGetProperty("guidelines", out var gNode))
//                 {
//                     guidelineList = JsonSerializer.Deserialize<List<object>>(gNode.ToString());
//                 }
//             }
//             catch {}
//         }

//         guidelineList.Add(guidelineEntry);

//         var updatedSettings = new
//         {
//             guidelines = guidelineList,
//             lastUpdated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
//         };

//         organization.Settings = JsonSerializer.Serialize(updatedSettings);
//         await _context.SaveChangesAsync();

//         return Ok(new
//         {
//             message = "File uploaded successfully",
//             guideline = guidelineEntry
//         });
//     }
//     catch (Exception ex)
//     {
//         return StatusCode(500, new { message = ex.Message });
//     }
// }



//         [HttpPut("{id}/settings")]
// public async Task<IActionResult> UpdateOrganizationSettings(int id, [FromBody] JsonElement body)
// {
//     var org = await _context.Organizations.FindAsync(id);
//     if (org == null)
//         return NotFound("Organization not found");

//     List<object> guidelineList = new();

//     if (body.TryGetProperty("guidelines", out JsonElement guidelinesProp))
//     {
//         guidelineList = JsonSerializer.Deserialize<List<object>>(guidelinesProp.ToString());
//     }

//     var updated = new
//     {
//         guidelines = guidelineList,
//         lastUpdated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
//     };

//     org.Settings = JsonSerializer.Serialize(updated);
//     await _context.SaveChangesAsync();

//     return Ok(org);
// }


    
//     [HttpGet("{id}/uploaded-settings")]
// public async Task<IActionResult> GetUploadedSettings(int id)
// {
//     var organization = await _context.Organizations.FindAsync(id);
//     if (organization == null)
//         return NotFound(new { message = "Organization not found" });

//     if (string.IsNullOrWhiteSpace(organization.Settings))
//         return NotFound(new { message = "No uploaded settings found" });

//     try
//     {
//         var settings = JsonSerializer.Deserialize<object>(organization.Settings);

//         return Ok(new
//         {
//             message = "Organization settings fetched successfully",
//             settings
//         });
//     }
//     catch (Exception ex)
//     {
//         return StatusCode(500, new { message = "Error parsing settings", error = ex.Message });
//     }
// }


    }
}
