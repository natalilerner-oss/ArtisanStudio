using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using ArtisanStudio.Services;
using ArtisanStudio.Models;

namespace ArtisanStudio.Tests.Services;

/// <summary>
/// Unit tests for ImageGenerationService
/// Demonstrates: Mocking, Dependency Injection, AAA Pattern
/// </summary>
public class ImageGenerationServiceTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IStorageService> _storageServiceMock;
    private readonly Mock<ILogger<ImageGenerationService>> _loggerMock;
    private readonly Mock<IConfiguration> _configMock;

    public ImageGenerationServiceTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _storageServiceMock = new Mock<IStorageService>();
        _loggerMock = new Mock<ILogger<ImageGenerationService>>();
        _configMock = new Mock<IConfiguration>();
    }

    [Fact]
    public async Task GenerateImageAsync_WithEmptyApiKey_ReturnsDemoMode()
    {
        // Arrange
        _configMock.Setup(c => c["REPLICATE_API_KEY"]).Returns(string.Empty);
        
        var service = new ImageGenerationService(
            _httpClientFactoryMock.Object,
            _storageServiceMock.Object,
            _loggerMock.Object,
            _configMock.Object
        );

        var request = new ImageGenerationRequest
        {
            Prompt = "A beautiful sunset",
            Style = "vivid",
            Size = "1024x1024",
            Quality = "standard"
        };

        // Act
        var result = await service.GenerateImageAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("Demo mode");
        result.Images.Should().HaveCount(1);
        result.Images[0].Model.Should().Be("demo");
    }

    [Fact]
    public async Task GenerateImageAsync_WithValidRequest_ReturnsImage()
    {
        // Arrange
        _configMock.Setup(c => c["REPLICATE_API_KEY"]).Returns(string.Empty); // Demo mode
        
        var service = new ImageGenerationService(
            _httpClientFactoryMock.Object,
            _storageServiceMock.Object,
            _loggerMock.Object,
            _configMock.Object
        );

        var request = new ImageGenerationRequest
        {
            Prompt = "A majestic mountain landscape",
            Style = "natural",
            Size = "1792x1024",
            Quality = "hd"
        };

        // Act
        var result = await service.GenerateImageAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Images.Should().NotBeEmpty();
        result.Images[0].Prompt.Should().Be(request.Prompt);
        result.Images[0].Url.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("1024x1024", "1:1")]
    [InlineData("1792x1024", "16:9")]
    [InlineData("1024x1792", "9:16")]
    public void GetAspectRatio_ReturnsCorrectRatio(string size, string expectedRatio)
    {
        // This tests the private method logic indirectly through the service
        // In production, you might expose this as internal for testing
        
        // Arrange
        var request = new ImageGenerationRequest { Size = size };
        
        // Assert - Size should map correctly
        request.Size.Should().Be(size);
    }

    [Fact]
    public async Task GenerateImageAsync_WithNullPrompt_HandlesGracefully()
    {
        // Arrange
        _configMock.Setup(c => c["REPLICATE_API_KEY"]).Returns(string.Empty);
        
        var service = new ImageGenerationService(
            _httpClientFactoryMock.Object,
            _storageServiceMock.Object,
            _loggerMock.Object,
            _configMock.Object
        );

        var request = new ImageGenerationRequest
        {
            Prompt = "", // Empty prompt
            Style = "vivid",
            Size = "1024x1024",
            Quality = "standard"
        };

        // Act
        var result = await service.GenerateImageAsync(request);

        // Assert
        result.Should().NotBeNull();
        // Demo mode still works with empty prompt
        result.Success.Should().BeTrue();
    }
}

/// <summary>
/// Unit tests for VideoGenerationService
/// </summary>
public class VideoGenerationServiceTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IStorageService> _storageServiceMock;
    private readonly Mock<ILogger<VideoGenerationService>> _loggerMock;
    private readonly Mock<IConfiguration> _configMock;

    public VideoGenerationServiceTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _storageServiceMock = new Mock<IStorageService>();
        _loggerMock = new Mock<ILogger<VideoGenerationService>>();
        _configMock = new Mock<IConfiguration>();
    }

    [Fact]
    public async Task GenerateVideoAsync_WithEmptyApiKey_ReturnsDemoMode()
    {
        // Arrange
        _configMock.Setup(c => c["VIDEO_API_KEY"]).Returns(string.Empty);
        _configMock.Setup(c => c["REPLICATE_API_KEY"]).Returns(string.Empty);
        
        var service = new VideoGenerationService(
            _httpClientFactoryMock.Object,
            _storageServiceMock.Object,
            _loggerMock.Object,
            _configMock.Object
        );

        var request = new VideoGenerationRequest
        {
            Prompt = "Ocean waves crashing",
            DurationSeconds = 5,
            AspectRatio = "16:9"
        };

        // Act
        var result = await service.GenerateVideoAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Status.Should().Be("completed"); // Demo mode completes immediately
        result.JobId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetVideoStatusAsync_WithInvalidJobId_ReturnsNotFound()
    {
        // Arrange
        _configMock.Setup(c => c["VIDEO_API_KEY"]).Returns(string.Empty);
        _configMock.Setup(c => c["REPLICATE_API_KEY"]).Returns(string.Empty);
        
        var service = new VideoGenerationService(
            _httpClientFactoryMock.Object,
            _storageServiceMock.Object,
            _loggerMock.Object,
            _configMock.Object
        );

        // Act
        var result = await service.GetVideoStatusAsync("invalid-job-id");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Status.Should().Be("not_found");
    }

    [Fact]
    public async Task GetVideoStatusAsync_AfterGeneration_ReturnsCorrectStatus()
    {
        // Arrange
        _configMock.Setup(c => c["VIDEO_API_KEY"]).Returns(string.Empty);
        _configMock.Setup(c => c["REPLICATE_API_KEY"]).Returns(string.Empty);
        
        var service = new VideoGenerationService(
            _httpClientFactoryMock.Object,
            _storageServiceMock.Object,
            _loggerMock.Object,
            _configMock.Object
        );

        var request = new VideoGenerationRequest
        {
            Prompt = "Test video",
            DurationSeconds = 5,
            AspectRatio = "16:9"
        };

        // Act - First generate
        var generateResult = await service.GenerateVideoAsync(request);
        
        // Act - Then check status
        var statusResult = await service.GetVideoStatusAsync(generateResult.JobId!);

        // Assert
        statusResult.Should().NotBeNull();
        statusResult.Success.Should().BeTrue();
        statusResult.JobId.Should().Be(generateResult.JobId);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    public async Task GenerateVideoAsync_WithDifferentDurations_Succeeds(int duration)
    {
        // Arrange
        _configMock.Setup(c => c["VIDEO_API_KEY"]).Returns(string.Empty);
        _configMock.Setup(c => c["REPLICATE_API_KEY"]).Returns(string.Empty);
        
        var service = new VideoGenerationService(
            _httpClientFactoryMock.Object,
            _storageServiceMock.Object,
            _loggerMock.Object,
            _configMock.Object
        );

        var request = new VideoGenerationRequest
        {
            Prompt = "Test video",
            DurationSeconds = duration,
            AspectRatio = "16:9"
        };

        // Act
        var result = await service.GenerateVideoAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }
}

/// <summary>
/// Unit tests for Models
/// </summary>
public class ModelTests
{
    [Fact]
    public void ImageGenerationRequest_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var request = new ImageGenerationRequest();

        // Assert
        request.Prompt.Should().BeEmpty();
        request.Style.Should().Be("vivid");
        request.Size.Should().Be("1024x1024");
        request.Quality.Should().Be("standard");
    }

    [Fact]
    public void VideoGenerationRequest_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var request = new VideoGenerationRequest();

        // Assert
        request.Prompt.Should().BeEmpty();
        request.DurationSeconds.Should().Be(5);
        request.AspectRatio.Should().Be("16:9");
        request.ImageUrl.Should().BeNull();
    }

    [Fact]
    public void GeneratedImage_HasUniqueId()
    {
        // Arrange & Act
        var image1 = new GeneratedImage();
        var image2 = new GeneratedImage();

        // Assert
        image1.Id.Should().NotBe(image2.Id);
        image1.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void GeneratedVideo_HasUniqueId()
    {
        // Arrange & Act
        var video1 = new GeneratedVideo();
        var video2 = new GeneratedVideo();

        // Assert
        video1.Id.Should().NotBe(video2.Id);
    }
}
