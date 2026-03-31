using FeatureBuilder.Web.Models;
using FeatureBuilder.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FeatureBuilder.Web.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly IOpenAIService _openAIService;
    private readonly ILogger<AiController> _logger;

    public AiController(IOpenAIService openAIService, ILogger<AiController> logger)
    {
        _openAIService = openAIService;
        _logger = logger;
    }

    [HttpPost("improve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImproveDescription([FromBody] AiImproveRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new AiResponse { Success = false, Error = "Title is required." });

        var result = await _openAIService.ImproveIssueDescriptionAsync(request);
        return Ok(result);
    }

    [HttpPost("question")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AskQuestion([FromBody] AiQuestionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest(new AiResponse { Success = false, Error = "Question is required." });

        var result = await _openAIService.AnswerQuestionAsync(request);
        return Ok(result);
    }

    [HttpPost("analyze-image")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AnalyzeImage([FromBody] AiImageAnalysisRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ImageBase64))
            return BadRequest(new AiResponse { Success = false, Error = "Image data is required." });

        var result = await _openAIService.AnalyzeImageAsync(request);
        return Ok(result);
    }
}
