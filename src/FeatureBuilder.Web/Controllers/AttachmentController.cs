using FeatureBuilder.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FeatureBuilder.Web.Controllers;

[ApiController]
[Route("api/attachment")]
[Authorize]
public class AttachmentController : ControllerBase
{
    private readonly IFileParsingService _fileParsingService;
    private readonly ILogger<AttachmentController> _logger;
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB

    public AttachmentController(IFileParsingService fileParsingService, ILogger<AttachmentController> logger)
    {
        _fileParsingService = fileParsingService;
        _logger = logger;
    }

    [HttpPost("extract")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExtractText(IFormFile? file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { success = false, error = "No file provided." });

        if (file.Length > MaxFileSizeBytes)
            return BadRequest(new { success = false, error = $"File exceeds the maximum size of 10MB." });

        if (!_fileParsingService.IsSupportedFile(file))
            return BadRequest(new { success = false, error = $"Unsupported file type: {Path.GetExtension(file.FileName)}" });

        try
        {
            var text = await _fileParsingService.ExtractTextFromFileAsync(file);
            return Ok(new { success = true, text, fileName = file.FileName });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from {FileName}", file.FileName);
            return Ok(new { success = false, error = ex.Message });
        }
    }
}
