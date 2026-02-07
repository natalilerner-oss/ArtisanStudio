using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ArtisanStudio.Controllers;
using ArtisanStudio.Services;
using ArtisanStudio.Models;

namespace ArtisanStudio.Tests.Controllers;

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

    #region GenerateImage Tests

    [Fact]
    public async Task GenerateImage_WithValidRequest_ReturnsOkResult()
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
                new GeneratedImage
                {
                    Id = "test-id",
                    Url = "/media/images/test.png",
                    Prompt = request.Prompt,
                    Model = "flux-1.1-pro"
                }
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
        response.Images[0].Url.Should().Be("/media/images/test.png");
    }

    [Fact]
    public async Task GenerateImage_WithEmptyPrompt_ReturnsBadRequest()
    {
        // Arrange
        var request = new ImageGenerationRequest
        {
            Prompt = "",
            Style = "vivid"
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
            Prompt = "   ",
            Style = "vivid"
        };

        // Act
        var result = await _controller.GenerateImage(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GenerateImage_WhenServiceFails_ReturnsOkWithFailureStatus()
    {
        // Arrange
        var request = new ImageGenerationRequest
        {
            Prompt = "Test prompt"
        };

        var failedResponse = new ImageGenerationResponse
        {
            Success = false,
            Message = "API error occurred"
        };

        _imageServiceMock
            .Setup(s => s.GenerateImageAsync(It.IsAny<ImageGenerationRequest>()))
            .ReturnsAsync(failedResponse);

        // Act
        var result = await _controller.GenerateImage(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ImageGenerationResponse>().Subject;
        
        response.Success.Should().BeFalse();
        response.Message.Should().Be("API error occurred");
    }

    #endregion

    #region GenerateVideo Tests

    [Fact]
    public async Task GenerateVideo_WithValidRequest_ReturnsOkWithJobId()
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
            Prompt = ""
        };

        // Act
        var result = await _controller.GenerateVideo(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region GetVideoStatus Tests

    [Fact]
    public async Task GetVideoStatus_WithValidJobId_ReturnsStatus()
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
                Url = "/media/videos/test.mp4",
                Prompt = "Test video"
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
        response.Video!.Url.Should().Be("/media/videos/test.mp4");
    }

    [Fact]
    public async Task GetVideoStatus_WithNonExistentJobId_ReturnsNotFound()
    {
        // Arrange
        var jobId = "non-existent-job";
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
    public void Health_ReturnsHealthyStatus()
    {
        // Act
        var result = _controller.Health();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    #endregion
}
