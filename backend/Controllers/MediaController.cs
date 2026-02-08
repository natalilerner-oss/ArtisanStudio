using Microsoft.AspNetCore.Mvc;
using ArtisanStudio.Models;
using ArtisanStudio.Services;

namespace ArtisanStudio.Controllers;

[ApiController]
[Route("api")]
public class MediaController : ControllerBase
{
    private readonly IImageGenerationService _imageService;
    private readonly IVideoGenerationService _videoService;
    private readonly IPromptEnhancementService _promptService;
    private readonly IPresentationService _presentationService;
    private readonly ILogger<MediaController> _logger;

    public MediaController(
        IImageGenerationService imageService,
        IVideoGenerationService videoService,
        IPromptEnhancementService promptService,
        IPresentationService presentationService,
        ILogger<MediaController> logger)
    {
        _imageService = imageService;
        _videoService = videoService;
        _promptService = promptService;
        _presentationService = presentationService;
        _logger = logger;
    }

    /// <summary>
    /// Enhance a prompt with AI-powered suggestions
    /// </summary>
    [HttpPost("prompts/enhance")]
    public async Task<ActionResult<PromptEnhancementResult>> EnhancePrompt([FromBody] PromptEnhanceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
            return BadRequest(new { error = "Prompt is required" });

        var result = await _promptService.EnhancePromptAsync(request.Prompt, request.MediaType ?? "image");
        return Ok(result);
    }

    /// <summary>
    /// Generate an AI image from a text prompt
    /// </summary>
    [HttpPost("images/generate")]
    public async Task<ActionResult<ImageGenerationResponse>> GenerateImage([FromBody] ImageGenerationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
            return BadRequest(new { error = "Prompt is required" });

        _logger.LogInformation("Image generation request: {Prompt}", request.Prompt);
        
        var result = await _imageService.GenerateImageAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Generate an AI video from a text prompt
    /// </summary>
    [HttpPost("videos/generate")]
    public async Task<ActionResult<VideoGenerationResponse>> GenerateVideo([FromBody] VideoGenerationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
            return BadRequest(new { error = "Prompt is required" });

        _logger.LogInformation("Video generation request: {Prompt}", request.Prompt);
        
        var result = await _videoService.GenerateVideoAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Check the status of a video generation job
    /// </summary>
    [HttpGet("videos/status/{jobId}")]
    public async Task<ActionResult<VideoGenerationResponse>> GetVideoStatus(string jobId)
    {
        _logger.LogInformation("Video status check: {JobId}", jobId);
        
        var result = await _videoService.GetVideoStatusAsync(jobId);
        return Ok(result);
    }

    /// <summary>
    /// Generate an AI presentation from a text prompt
    /// </summary>
    [HttpPost("presentations/generate")]
    public async Task<ActionResult<PresentationResponse>> GeneratePresentation([FromBody] PresentationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
            return BadRequest(new { error = "Prompt is required" });

        _logger.LogInformation("Presentation generation request: {Prompt}", request.Prompt);

        var result = await _presentationService.GeneratePresentationAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Check the status of a presentation generation job
    /// </summary>
    [HttpGet("presentations/{id}/status")]
    public async Task<ActionResult<PresentationResponse>> GetPresentationStatus(string id)
    {
        _logger.LogInformation("Presentation status check: {Id}", id);

        var result = await _presentationService.GetPresentationStatusAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Get the full presentation data
    /// </summary>
    [HttpGet("presentations/{id}")]
    public async Task<ActionResult<Presentation>> GetPresentation(string id)
    {
        var result = await _presentationService.GetPresentationAsync(id);
        if (result == null)
            return NotFound(new { error = "Presentation not found" });
        return Ok(result);
    }

    /// <summary>
    /// Download presentation in PPTX or PDF format
    /// </summary>
    [HttpGet("presentations/{id}/download")]
    public async Task<IActionResult> DownloadPresentation(string id, [FromQuery] string format = "pptx")
    {
        _logger.LogInformation("Presentation download: {Id} format={Format}", id, format);

        var data = await _presentationService.ExportPresentationAsync(id, format);
        if (data == null)
            return NotFound(new { error = "Presentation not found or not ready" });

        var contentType = format == "pptx"
            ? "application/vnd.openxmlformats-officedocument.presentationml.presentation"
            : "application/pdf";
        var fileName = $"presentation.{format}";

        return File(data, contentType, fileName);
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    public ActionResult Health()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            version = "3.0.0"
        });
    }
}
