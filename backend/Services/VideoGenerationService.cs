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
    private readonly string _endpoint;

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
        _apiKey = config["AZURE_SORA_API_KEY"]
            ?? Environment.GetEnvironmentVariable("AZURE_SORA_API_KEY") ?? "";
        _endpoint = config["AZURE_SORA_ENDPOINT"]
            ?? Environment.GetEnvironmentVariable("AZURE_SORA_ENDPOINT")
            ?? "https://natal-me0fuhjl-eastus2.openai.azure.com";
    }

    public async Task<VideoGenerationResponse> GenerateVideoAsync(VideoGenerationRequest request)
    {
        try
        {
            _logger.LogInformation("Generating video with Azure Sora: {Prompt}", request.Prompt);

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
                    Message = "Demo mode - configure AZURE_SORA_API_KEY for real generation",
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
            client.DefaultRequestHeaders.Add("api-key", _apiKey);

            // Azure OpenAI Sora video generation API
            var url = $"{_endpoint.TrimEnd('/')}/openai/deployments/sora/videos/generations?api-version=2025-03-01-preview";

            var size = request.AspectRatio switch
            {
                "9:16" => "1080x1920",
                "1:1" => "1080x1080",
                _ => "1920x1080"  // 16:9 default
            };

            var payload = new
            {
                prompt = request.Prompt,
                n = 1,
                size,
                duration_seconds = request.DurationSeconds
            };

            var response = await client.PostAsJsonAsync(url, payload);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Azure Sora API error: {StatusCode} {Error}", response.StatusCode, error);
                return new VideoGenerationResponse
                {
                    Success = false,
                    Message = $"API error: {response.StatusCode}"
                };
            }

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();

            // Check if the response includes a job/operation ID for async polling
            if (result.TryGetProperty("id", out var idProp))
            {
                var operationId = idProp.GetString()!;

                _jobs[jobId] = new VideoJob
                {
                    JobId = jobId,
                    ProviderJobId = operationId,
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

            // Synchronous response — video returned directly
            if (result.TryGetProperty("data", out var dataArray) && dataArray.GetArrayLength() > 0)
            {
                var videoData = dataArray[0];
                var videoUrl = videoData.GetProperty("url").GetString()!;

                using var downloadClient = new HttpClient();
                var videoBytes = await downloadClient.GetByteArrayAsync(videoUrl);
                var savedUrl = await _storageService.SaveVideoAsync(videoBytes, $"sora_{jobId}.mp4");

                _jobs[jobId] = new VideoJob
                {
                    JobId = jobId,
                    Status = "completed",
                    Prompt = request.Prompt,
                    VideoUrl = savedUrl,
                    CreatedAt = DateTime.UtcNow
                };

                return new VideoGenerationResponse
                {
                    Success = true,
                    JobId = jobId,
                    Status = "completed",
                    Video = new GeneratedVideo
                    {
                        Id = jobId,
                        Url = savedUrl,
                        Prompt = request.Prompt,
                        Model = "sora",
                        DurationSeconds = request.DurationSeconds,
                        CreatedAt = DateTime.UtcNow
                    }
                };
            }

            return new VideoGenerationResponse
            {
                Success = false,
                Message = "Unexpected response from Azure Sora API"
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
                    Model = "sora",
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
        client.DefaultRequestHeaders.Add("api-key", _apiKey);

        var pollUrl = $"{_endpoint.TrimEnd('/')}/openai/deployments/sora/videos/generations/{job.ProviderJobId}?api-version=2025-03-01-preview";

        for (int i = 0; i < 120; i++) // Max ~6 minutes
        {
            await Task.Delay(3000);

            try
            {
                var response = await client.GetAsync(pollUrl);
                var result = await response.Content.ReadFromJsonAsync<JsonElement>();

                var status = result.TryGetProperty("status", out var statusProp)
                    ? statusProp.GetString() : null;

                if (status == "succeeded" || status == "completed")
                {
                    string? videoUrl = null;
                    if (result.TryGetProperty("data", out var dataArray) && dataArray.GetArrayLength() > 0)
                    {
                        videoUrl = dataArray[0].GetProperty("url").GetString();
                    }
                    else if (result.TryGetProperty("result", out var resultProp)
                             && resultProp.TryGetProperty("url", out var urlProp))
                    {
                        videoUrl = urlProp.GetString();
                    }

                    if (!string.IsNullOrEmpty(videoUrl))
                    {
                        using var downloadClient = new HttpClient();
                        var videoBytes = await downloadClient.GetByteArrayAsync(videoUrl);
                        var savedUrl = await _storageService.SaveVideoAsync(videoBytes, $"sora_{jobId}.mp4");

                        job.Status = "completed";
                        job.VideoUrl = savedUrl;
                    }
                    else
                    {
                        job.Status = "failed";
                        job.Error = "Video completed but no URL returned";
                    }
                    return;
                }
                else if (status == "failed" || status == "cancelled")
                {
                    job.Status = "failed";
                    job.Error = result.TryGetProperty("error", out var err) ? err.GetString() : "Video generation failed";
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
