using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ArtisanStudio.Controllers;
using ArtisanStudio.Services;
using ArtisanStudio.Models;

namespace ArtisanStudio.Tests.Controllers;

/// <summary>
/// Unit tests for MediaController
/// Demonstrates: Controller testing, Action result validation
/// </summary>
public class MediaControllerTests
{
    private readonly Mock<IImageGenerationService> _imageServiceMock;
    private readonly Mock<IVideoGenerationService> _videoServiceMock;
    private readonly Mock<ILogger<MediaController>> _loggerMock;
    private readonly MediaController _controller;

    public MediaControllerTests()
    {
        _imageServiceMock = new Mock<IImageGenerationService>();
        _videoServiceMock = new Mock<IVideoGenerationService>();
        _loggerMock = new Mock<ILogger<MediaController>>();
        
        _controller = new MediaController(
            _imageServiceMock.Object,
            _videoServiceMock.Object,
            _loggerMock.Object
        );
    }

    #region Image Generation Tests

    [Fact]
    public async Task GenerateImage_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new ImageGenerationRequest
        {
            Prompt = "A beautiful sunset over mountains",
            Style = "vivid",
            Size = "1024x1024",
            Quality = "standard"
        };

        var expectedResponse = new ImageGenerationResponse
        {
            Success = true,
            Message = "Image generated successfully",
            Images = new List<GeneratedImage>
            {
                new() { Id = "123", Url = "http://example.com/image.png", Prompt = request.Prompt }
            }
        };

        _imageServiceMock
            .Setup(s => s.GenerateImageAsync(It.IsAny<ImageGenerationRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GenerateImage(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ImageGenerationResponse>().Subject;
        response.Success.Should().BeTrue();
        response.Images.Should().HaveCount(1);
    }

    [Fact]
    public async Task GenerateImage_WithEmptyPrompt_ReturnsBadRequest()
    {
        // Arrange
        var request = new ImageGenerationRequest
        {
            Prompt = "", // Empty prompt
            Style = "vivid",
            Size = "1024x1024"
        };

        // Act
        var result = await _controller.GenerateImage(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GenerateImage_WithWhitespacePrompt_ReturnsBadRequest()
    {
        // Arrange
        var request = new ImageGenerationRequest
        {
            Prompt = "   ", // Whitespace only
            Style = "vivid",
            Size = "1024x1024"
        };

        // Act
        var result = await _controller.GenerateImage(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GenerateImage_ServiceCalled_WithCorrectParameters()
    {
        // Arrange
        var request = new ImageGenerationRequest
        {
            Prompt = "Test prompt",
            Style = "natural",
            Size = "1792x1024",
            Quality = "hd"
        };

        _imageServiceMock
            .Setup(s => s.GenerateImageAsync(It.IsAny<ImageGenerationRequest>()))
            .ReturnsAsync(new ImageGenerationResponse { Success = true });

        // Act
        await _controller.GenerateImage(request);

        // Assert
        _imageServiceMock.Verify(
            s => s.GenerateImageAsync(It.Is<ImageGenerationRequest>(r =>
                r.Prompt == "Test prompt" &&
                r.Style == "natural" &&
                r.Size == "1792x1024" &&
                r.Quality == "hd"
            )),
            Times.Once
        );
    }

    #endregion

    #region Video Generation Tests

    [Fact]
    public async Task GenerateVideo_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new VideoGenerationRequest
        {
            Prompt = "Ocean waves crashing on rocks",
            DurationSeconds = 5,
            AspectRatio = "16:9"
        };

        var expectedResponse = new VideoGenerationResponse
        {
            Success = true,
            JobId = "job-123",
            Status = "processing",
            Message = "Video generation started"
        };

        _videoServiceMock
            .Setup(s => s.GenerateVideoAsync(It.IsAny<VideoGenerationRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GenerateVideo(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<VideoGenerationResponse>().Subject;
        response.Success.Should().BeTrue();
        response.JobId.Should().Be("job-123");
        response.Status.Should().Be("processing");
    }

    [Fact]
    public async Task GenerateVideo_WithEmptyPrompt_ReturnsBadRequest()
    {
        // Arrange
        var request = new VideoGenerationRequest
        {
            Prompt = "",
            DurationSeconds = 5,
            AspectRatio = "16:9"
        };

        // Act
        var result = await _controller.GenerateVideo(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetVideoStatus_WithValidJobId_ReturnsOk()
    {
        // Arrange
        var jobId = "job-123";
        var expectedResponse = new VideoGenerationResponse
        {
            Success = true,
            JobId = jobId,
            Status = "completed",
            Video = new GeneratedVideo
            {
                Id = jobId,
                Url = "http://example.com/video.mp4"
            }
        };

        _videoServiceMock
            .Setup(s => s.GetVideoStatusAsync(jobId))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetVideoStatus(jobId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<VideoGenerationResponse>().Subject;
        response.Status.Should().Be("completed");
        response.Video.Should().NotBeNull();
    }

    [Fact]
    public async Task GetVideoStatus_WithInvalidJobId_ReturnsNotFound()
    {
        // Arrange
        var jobId = "invalid-job";
        var expectedResponse = new VideoGenerationResponse
        {
            Success = false,
            Status = "not_found",
            Message = "Job not found"
        };

        _videoServiceMock
            .Setup(s => s.GetVideoStatusAsync(jobId))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetVideoStatus(jobId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<VideoGenerationResponse>().Subject;
        response.Success.Should().BeFalse();
        response.Status.Should().Be("not_found");
    }

    #endregion

    #region Health Check Tests

    [Fact]
    public void Health_ReturnsOkWithStatus()
    {
        // Act
        var result = _controller.Health();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    #endregion
}
