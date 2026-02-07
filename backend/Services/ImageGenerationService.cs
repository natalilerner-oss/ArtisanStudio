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
    private readonly string _provider;

    public ImageGenerationService(
        IHttpClientFactory httpClientFactory,
        IStorageService storageService,
        ILogger<ImageGenerationService> logger,
        IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _storageService = storageService;
        _logger = logger;
        _apiKey = config["REPLICATE_API_KEY"] ?? Environment.GetEnvironmentVariable("REPLICATE_API_KEY") ?? "";
        _provider = config["IMAGE_PROVIDER"] ?? "flux";
    }

    public async Task<ImageGenerationResponse> GenerateImageAsync(ImageGenerationRequest request)
    {
        try
        {
            _logger.LogInformation("Generating image: {Prompt}", request.Prompt);

            if (string.IsNullOrEmpty(_apiKey))
            {
                // Demo mode - return placeholder
                return new ImageGenerationResponse
                {
                    Success = true,
                    Message = "Demo mode - configure REPLICATE_API_KEY for real generation",
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
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            // Use Flux 1.1 Pro via Replicate
            var payload = new
            {
                version = "2c8e954decbf70b7607a4414e5785ef9e4de79f5074fe989a0965d3e1c5327e8",
                input = new
                {
                    prompt = request.Prompt,
                    aspect_ratio = GetAspectRatio(request.Size),
                    output_format = "png",
                    output_quality = request.Quality == "hd" ? 100 : 80,
                    safety_tolerance = 2,
                    prompt_upsampling = true
                }
            };

            var response = await client.PostAsJsonAsync("https://api.replicate.com/v1/predictions", payload);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Replicate API error: {Error}", error);
                return new ImageGenerationResponse
                {
                    Success = false,
                    Message = $"API error: {response.StatusCode}"
                };
            }

            var prediction = await response.Content.ReadFromJsonAsync<JsonElement>();
            var predictionId = prediction.GetProperty("id").GetString()!;

            // Poll for completion
            var result = await PollForCompletionAsync(client, predictionId);

            if (result.TryGetProperty("output", out var output))
            {
                var imageUrl = output.ValueKind == JsonValueKind.String 
                    ? output.GetString() 
                    : output.ToString();

                if (!string.IsNullOrEmpty(imageUrl))
                {
                    // Download and save locally
                    using var downloadClient = new HttpClient();
                    var imageBytes = await downloadClient.GetByteArrayAsync(imageUrl);
                    var savedUrl = await _storageService.SaveImageAsync(imageBytes, $"flux_{Guid.NewGuid()}.png");

                    return new ImageGenerationResponse
                    {
                        Success = true,
                        Message = "Image generated successfully with Flux 1.1 Pro",
                        Images = new List<GeneratedImage>
                        {
                            new()
                            {
                                Id = Guid.NewGuid().ToString(),
                                Url = savedUrl,
                                Prompt = request.Prompt,
                                Model = "flux-1.1-pro"
                            }
                        }
                    };
                }
            }

            var errorMsg = result.TryGetProperty("error", out var err) ? err.GetString() : "Unknown error";
            return new ImageGenerationResponse
            {
                Success = false,
                Message = errorMsg
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

    private async Task<JsonElement> PollForCompletionAsync(HttpClient client, string predictionId)
    {
        for (int i = 0; i < 120; i++)
        {
            await Task.Delay(1000);
            
            var response = await client.GetAsync($"https://api.replicate.com/v1/predictions/{predictionId}");
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            
            var status = result.GetProperty("status").GetString();
            if (status == "succeeded" || status == "failed")
                return result;
        }
        
        throw new TimeoutException("Image generation timed out");
    }

    private static string GetAspectRatio(string size) => size switch
    {
        "1792x1024" => "16:9",
        "1024x1792" => "9:16",
        _ => "1:1"
    };
}
