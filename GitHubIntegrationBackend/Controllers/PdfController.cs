// using Microsoft.AspNetCore.Mvc;
// using GitHubIntegrationBackend.Services;

// namespace GitHubIntegrationBackend.Controllers
// {
//     [Route("api/pdf")]
//     [ApiController]
//     public class PdfController : ControllerBase
//     {
//         private readonly SonarQubeService _sonar;
//         private readonly PdfService _pdf;
//         private readonly PdfStorageService _pdfStorage;

//         private readonly IWebHostEnvironment _env;

//         public PdfController(SonarQubeService sonar, PdfService pdf, IWebHostEnvironment env)
//         {
//             _sonar = sonar;
//             _pdf = pdf;
//             _env = env;
//         }

//         [HttpGet("sonar-report")]
//         public async Task<IActionResult> GenerateReport(string projectKey)
//         {
//             var metrics = await _sonar.GetProjectMetricsAsync(projectKey);
//             var issues = await _sonar.GetProjectIssuesAsync(projectKey);

//             string templatePath = Path.Combine(_env.ContentRootPath,  "SonarReportTemplate.html");
//             string html = System.IO.File.ReadAllText(templatePath);

//             // Prepare dynamic replacements
//             string metricsRows = "";
//             foreach (dynamic m in metrics)
//             {
//                 metricsRows += $"<tr><td>{m.Metric}</td><td>{m.Value}</td></tr>";
//             }

//             string issuesRows = "";
//             foreach (dynamic i in issues.Take(10))   // recent 10
//             {
//                 issuesRows += $@"
//                     <tr>
//                         <td>{i.Severity}</td>
//                         <td>{i.FilePath}</td>
//                         <td>{i.Message}</td>
//                         <td>{i.Line}</td>
//                         <td>{i.Effort}</td>

//                     </tr>";
//             }

//             // Find specific metrics
//             string critical = metrics.FirstOrDefault(m => ((dynamic)m).Metric == "bugs")?.GetType().GetProperty("Value")?.GetValue(metrics.First())?.ToString() ?? "0";
//             string coverage = metrics.FirstOrDefault(m => ((dynamic)m).Metric == "coverage")?.GetType().GetProperty("Value")?.GetValue(metrics.First())?.ToString() ?? "0";
//             string duplicates = metrics.FirstOrDefault(m => ((dynamic)m).Metric == "duplicated_lines_density")?.GetType().GetProperty("Value")?.GetValue(metrics.First())?.ToString() ?? "0";

//             // Replace placeholders
//             html = html.Replace("{{criticalIssues}}", critical)
//                        .Replace("{{coverage}}", coverage)
//                        .Replace("{{duplication}}", duplicates)
//                        .Replace("{{metricsRows}}", metricsRows)
//                        .Replace("{{issuesRows}}", issuesRows);

//             // Generate PDF
//             byte[] pdfBytes = _pdf.GeneratePdf(html);

//             return File(pdfBytes, "application/pdf", "SonarQube_Report.pdf");
//         }
//}
//}
using Microsoft.AspNetCore.Mvc;
using GitHubIntegrationBackend.Services;
using System.Text;

namespace GitHubIntegrationBackend.Controllers
{
    [Route("api/pdf")]
    [ApiController]
    public class PdfController : ControllerBase
    {
        private readonly SonarQubeService _sonar;
        private readonly PdfService _pdf;
        private readonly PdfStorageService _pdfStorage;
        private readonly IWebHostEnvironment _env;

        public PdfController(
            SonarQubeService sonar,
            PdfService pdf,
            PdfStorageService pdfStorage,
            IWebHostEnvironment env)
        {
            _sonar = sonar;
            _pdf = pdf;
            _pdfStorage = pdfStorage;
            _env = env;
        }

        [HttpGet("sonar-report")]
        public async Task<IActionResult> GenerateReport([FromQuery] string projectKey)
        {
            if (string.IsNullOrWhiteSpace(projectKey))
                return BadRequest(new { success = false, message = "projectKey required" });

            try
            {
                // fetch sonar data (you already have these methods)
                var metrics = await _sonar.GetProjectMetricsAsync(projectKey);
                var issues = await _sonar.GetProjectIssuesAsync(projectKey);

                // load template
                string templatePath = Path.Combine(_env.ContentRootPath, "SonarReportTemplate.html");
                if (!System.IO.File.Exists(templatePath))
                    return StatusCode(500, new { success = false, message = $"Template not found: {templatePath}" });

                string html = System.IO.File.ReadAllText(templatePath);

                // Replace placeholders (your existing replacement logic)
                var metricsRows = new StringBuilder();
                foreach (dynamic m in metrics)
                    metricsRows.Append($"<tr><td>{m.Metric}</td><td>{m.Value}</td></tr>");

                var issuesRows = new StringBuilder();
                foreach (dynamic i in issues.Take(10))
                {
                    issuesRows.Append($@"<tr>
                        <td>{i.Severity}</td>
                        <td>{i.FilePath}</td>
                        <td>{i.Message}</td>
                        <td>{i.Line}</td>
                        <td>{i.Effort}</td>
                    </tr>");
                }

                html = html.Replace("{{metricsRows}}", metricsRows.ToString())
                           .Replace("{{issuesRows}}", issuesRows.ToString());

                // create PDF bytes
                byte[] pdfBytes = _pdf.GeneratePdf(html);

                // Save PDF and persist URL
                string fileUrl = await _pdfStorage.SavePdfAsync(projectKey, pdfBytes);

                // Return JSON with pdfUrl (relative path served by static files)
                return Ok(new { success = true, pdfUrl = fileUrl });
            }
            catch (Exception ex)
            {
                // Log ex (Console for now)
                Console.Error.WriteLine("GenerateReport error: " + ex);

                // Return JSON error (so frontend res.json() doesn't fail)
                return StatusCode(500, new { success = false, message = "Failed to generate report", detail = ex.Message });
            }
        }
    }
}
