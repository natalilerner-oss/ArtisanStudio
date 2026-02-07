using System.Net.Http.Json;
using System.Text.Json;
using ArtisanStudio.Models;

namespace ArtisanStudio.Services;

public class ImageGenerationService : IImageGenerationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IStorageService _storageService;
    private readonly ILogger<ImageGenerationService> _logger;
    private readonly string _apiKey;
    private readonly string _endpoint;

    public ImageGenerationService(
        IHttpClientFactory httpClientFactory,
        IStorageService storageService,
        ILogger<ImageGenerationService> logger,
        IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _storageService = storageService;
        _logger = logger;
        _apiKey = config["AZURE_DALLE_API_KEY"]
            ?? Environment.GetEnvironmentVariable("AZURE_DALLE_API_KEY") ?? "";
        _endpoint = config["AZURE_DALLE_ENDPOINT"]
            ?? Environment.GetEnvironmentVariable("AZURE_DALLE_ENDPOINT")
            ?? "https://mycompanyaimodel.openai.azure.com";
    }

    public async Task<ImageGenerationResponse> GenerateImageAsync(ImageGenerationRequest request)
    {
        try
        {
            _logger.LogInformation("Generating image with DALL-E 3: {Prompt}", request.Prompt);

            if (string.IsNullOrEmpty(_apiKey))
            {
                // Demo mode - return placeholder
                return new ImageGenerationResponse
                {
                    Success = true,
                    Message = "Demo mode - configure AZURE_DALLE_API_KEY for real generation",
                    Images = new List<GeneratedImage>
                    {
                        new()
                        {
                            Id = Guid.NewGuid().ToString(),
                            Url = $"https://picsum.photos/seed/{Guid.NewGuid()}/1024/1024",
                            Prompt = request.Prompt,
                            Model = "demo"
                        }
                    }
                };
            }

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("api-key", _apiKey);

            // Azure OpenAI DALL-E 3 API
            var url = $"{_endpoint.TrimEnd('/')}/openai/deployments/dall-e-3/images/generations?api-version=2024-02-01";

            var payload = new
            {
                prompt = request.Prompt,
                n = 1,
                size = request.Size ?? "1024x1024",
                quality = request.Quality ?? "standard",
                style = request.Style ?? "vivid"
            };

            var response = await client.PostAsJsonAsync(url, payload);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Azure DALL-E 3 API error: {StatusCode} {Error}", response.StatusCode, error);
                return new ImageGenerationResponse
                {
                    Success = false,
                    Message = $"API error: {response.StatusCode}"
                };
            }

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();

            if (result.TryGetProperty("data", out var dataArray) && dataArray.GetArrayLength() > 0)
            {
                var firstImage = dataArray[0];
                var imageUrl = firstImage.GetProperty("url").GetString()!;
                var revisedPrompt = firstImage.TryGetProperty("revised_prompt", out var rp)
                    ? rp.GetString() ?? "" : "";

                // Download and save locally
                using var downloadClient = new HttpClient();
                var imageBytes = await downloadClient.GetByteArrayAsync(imageUrl);
                var savedUrl = await _storageService.SaveImageAsync(imageBytes, $"dalle3_{Guid.NewGuid()}.png");

                return new ImageGenerationResponse
                {
                    Success = true,
                    Message = "Image generated successfully with DALL-E 3",
                    Images = new List<GeneratedImage>
                    {
                        new()
                        {
                            Id = Guid.NewGuid().ToString(),
                            Url = savedUrl,
                            Prompt = request.Prompt,
                            RevisedPrompt = revisedPrompt,
                            Model = "dall-e-3"
                        }
                    }
                };
            }

            return new ImageGenerationResponse
            {
                Success = false,
                Message = "No image returned from API"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating image");
            return new ImageGenerationResponse
            {
                Success = false,
                Message = ex.Message
            };
        }
    }
}
