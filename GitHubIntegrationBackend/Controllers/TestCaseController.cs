using Microsoft.AspNetCore.Mvc;
using GitHubIntegrationBackend.Services;

namespace GitHubIntegrationBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly GeminiService _gemini;

        public TestController(GeminiService gemini)
        {
            _gemini = gemini;
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateTests([FromBody] TestRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Code))
                return BadRequest(new { error = "Missing code." });

            var prompt = $@"
You are a senior test automation engineer.
Generate 5 JUnit 5 test cases for the following code.
Include positive, negative, and boundary cases.

Code:
{req.Code}
";
            var result = await _gemini.GenerateAsync(prompt);
            return Ok(new { result });
        }

        [HttpPost("validate")]
        public async Task<IActionResult> ValidateTests([FromBody] ValidationRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Tests))
                return BadRequest(new { error = "Missing tests." });

            var prompt = $@"
You are a QA validator.and Do not consider diff came from test folder which is in any project folder .
Check if these tests follow good practices and respond '✅ Follows' or '❌ Does Not Follow' with one line reason.

Tests:
{req.Tests}
";

            var result = await _gemini.GenerateAsync(prompt);
            return Ok(new { result });
        }
    }

    public class TestRequest { public string Code { get; set; } = string.Empty; }
    public class ValidationRequest { public string Tests { get; set; } = string.Empty; }
}
