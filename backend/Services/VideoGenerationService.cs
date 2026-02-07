using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using ArtisanStudio.Models;

namespace ArtisanStudio.Services;

public class VideoGenerationService : IVideoGenerationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IStorageService _storageService;
    private readonly ILogger<VideoGenerationService> _logger;
    private readonly string _apiKey;
    private readonly string _provider;

    private static readonly ConcurrentDictionary<string, VideoJob> _jobs = new();

    public VideoGenerationService(
        IHttpClientFactory httpClientFactory,
        IStorageService storageService,
        ILogger<VideoGenerationService> logger,
        IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _storageService = storageService;
        _logger = logger;
        _apiKey = config["VIDEO_API_KEY"] ?? config["REPLICATE_API_KEY"] 
            ?? Environment.GetEnvironmentVariable("VIDEO_API_KEY") 
            ?? Environment.GetEnvironmentVariable("REPLICATE_API_KEY") ?? "";
        _provider = config["VIDEO_PROVIDER"] ?? "replicate";
    }

    public async Task<VideoGenerationResponse> GenerateVideoAsync(VideoGenerationRequest request)
    {
        try
        {
            _logger.LogInformation("Generating video: {Prompt}", request.Prompt);

            var jobId = Guid.NewGuid().ToString();

            if (string.IsNullOrEmpty(_apiKey))
            {
                // Demo mode
                _jobs[jobId] = new VideoJob
                {
                    JobId = jobId,
                    Status = "completed",
                    Prompt = request.Prompt,
                    VideoUrl = "https://sample-videos.com/video321/mp4/720/big_buck_bunny_720p_1mb.mp4",
                    CreatedAt = DateTime.UtcNow
                };

                return new VideoGenerationResponse
                {
                    Success = true,
                    JobId = jobId,
                    Status = "completed",
                    Message = "Demo mode - configure API keys for real generation",
                    Video = new GeneratedVideo
                    {
                        Id = jobId,
                        Url = _jobs[jobId].VideoUrl!,
                        Prompt = request.Prompt,
                        Model = "demo"
                    }
                };
            }

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            // Use Runway-compatible model via Replicate (CogVideoX or similar)
            var payload = new
            {
                version = "2dce02f1b271dbe8836e9c1f9f99e7d5bb879f622a041559d2f4c52c9f0f81c5", // cogvideox-5b
                input = new
                {
                    prompt = request.Prompt,
                    num_frames = request.DurationSeconds * 8,
                    guidance_scale = 6,
                    num_inference_steps = 50
                }
            };

            var response = await client.PostAsJsonAsync("https://api.replicate.com/v1/predictions", payload);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Video API error: {Error}", error);
                return new VideoGenerationResponse
                {
                    Success = false,
                    Message = $"API error: {response.StatusCode}"
                };
            }

            var prediction = await response.Content.ReadFromJsonAsync<JsonElement>();
            var predictionId = prediction.GetProperty("id").GetString()!;

            _jobs[jobId] = new VideoJob
            {
                JobId = jobId,
                ProviderJobId = predictionId,
                Status = "processing",
                Prompt = request.Prompt,
                CreatedAt = DateTime.UtcNow
            };

            // Start background polling
            _ = PollVideoStatusAsync(jobId);

            return new VideoGenerationResponse
            {
                Success = true,
                JobId = jobId,
                Status = "processing",
                Message = "Video generation started. Check status for completion."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating video");
            return new VideoGenerationResponse
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public async Task<VideoGenerationResponse> GetVideoStatusAsync(string jobId)
    {
        if (!_jobs.TryGetValue(jobId, out var job))
        {
            return new VideoGenerationResponse
            {
                Success = false,
                Status = "not_found",
                Message = "Job not found"
            };
        }

        if (job.Status == "completed" && !string.IsNullOrEmpty(job.VideoUrl))
        {
            return new VideoGenerationResponse
            {
                Success = true,
                JobId = jobId,
                Status = "completed",
                Video = new GeneratedVideo
                {
                    Id = jobId,
                    Url = job.VideoUrl,
                    Prompt = job.Prompt,
                    Model = "cogvideox",
                    DurationSeconds = 5,
                    CreatedAt = job.CreatedAt
                }
            };
        }

        if (job.Status == "failed")
        {
            return new VideoGenerationResponse
            {
                Success = false,
                JobId = jobId,
                Status = "failed",
                Message = job.Error ?? "Video generation failed"
            };
        }

        return new VideoGenerationResponse
        {
            Success = true,
            JobId = jobId,
            Status = "processing",
            Message = "Video is still being generated..."
        };
    }

    private async Task PollVideoStatusAsync(string jobId)
    {
        if (!_jobs.TryGetValue(jobId, out var job) || string.IsNullOrEmpty(job.ProviderJobId))
            return;

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

        for (int i = 0; i < 300; i++) // Max 5 minutes
        {
            await Task.Delay(3000);

            try
            {
                var response = await client.GetAsync($"https://api.replicate.com/v1/predictions/{job.ProviderJobId}");
                var result = await response.Content.ReadFromJsonAsync<JsonElement>();
                
                var status = result.GetProperty("status").GetString();

                if (status == "succeeded")
                {
                    string? videoUrl = null;
                    if (result.TryGetProperty("output", out var output))
                    {
                        videoUrl = output.ValueKind == JsonValueKind.String
                            ? output.GetString()
                            : output.ValueKind == JsonValueKind.Array && output.GetArrayLength() > 0
                                ? output[0].GetString()
                                : null;
                    }

                    if (!string.IsNullOrEmpty(videoUrl))
                    {
                        // Download and save locally
                        using var downloadClient = new HttpClient();
                        var videoBytes = await downloadClient.GetByteArrayAsync(videoUrl);
                        var savedUrl = await _storageService.SaveVideoAsync(videoBytes, $"video_{jobId}.mp4");

                        job.Status = "completed";
                        job.VideoUrl = savedUrl;
                    }
                    return;
                }
                else if (status == "failed")
                {
                    job.Status = "failed";
                    job.Error = result.TryGetProperty("error", out var err) ? err.GetString() : "Unknown error";
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error polling video status");
            }
        }

        job.Status = "failed";
        job.Error = "Timeout waiting for video generation";
    }

    private class VideoJob
    {
        public string JobId { get; set; } = "";
        public string? ProviderJobId { get; set; }
        public string Status { get; set; } = "pending";
        public string Prompt { get; set; } = "";
        public string? VideoUrl { get; set; }
        public string? Error { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
