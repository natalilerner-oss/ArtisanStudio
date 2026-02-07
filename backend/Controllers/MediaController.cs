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
    private readonly ILogger<MediaController> _logger;

    public MediaController(
        IImageGenerationService imageService,
        IVideoGenerationService videoService,
        IPromptEnhancementService promptService,
        ILogger<MediaController> logger)
    {
        _imageService = imageService;
        _videoService = videoService;
        _promptService = promptService;
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
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    public ActionResult Health()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            version = "2.0.0"
        });
    }
}
