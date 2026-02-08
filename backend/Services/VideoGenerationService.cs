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
            ?? "https://natal-me0fuhjl-eastus2.cognitiveservices.azure.com";
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
            client.DefaultRequestHeaders.Add("Api-key", _apiKey);

            // Azure Cognitive Services Sora video generation API
            var url = $"{_endpoint.TrimEnd('/')}/openai/v1/video/generations/jobs?api-version=preview";

            // Map aspect ratio to width/height
            var (width, height) = request.AspectRatio switch
            {
                "9:16" => ("1080", "1920"),
                "1:1" => ("1080", "1080"),
                _ => ("1920", "1080")  // 16:9 default
            };

            var payload = new
            {
                model = "sora",
                prompt = request.Prompt,
                height,
                width,
                n_seconds = request.DurationSeconds.ToString(),
                n_variants = "1"
            };

            _logger.LogInformation("Calling Sora API: {Url}", url);
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
            _logger.LogInformation("Sora API response: {Response}", result.ToString());

            // The jobs API returns an ID for polling
            if (result.TryGetProperty("id", out var idProp))
            {
                var providerJobId = idProp.GetString()!;

                _jobs[jobId] = new VideoJob
                {
                    JobId = jobId,
                    ProviderJobId = providerJobId,
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
        client.DefaultRequestHeaders.Add("Api-key", _apiKey);

        var baseJobUrl = $"{_endpoint.TrimEnd('/')}/openai/v1/video/generations/jobs/{job.ProviderJobId}";
        var pollUrl = $"{baseJobUrl}?api-version=preview";

        for (int i = 0; i < 120; i++) // Max ~10 minutes
        {
            await Task.Delay(5000);

            try
            {
                var response = await client.GetAsync(pollUrl);
                var result = await response.Content.ReadFromJsonAsync<JsonElement>();

                _logger.LogInformation("Sora poll response: {Response}", result.ToString());

                var status = result.TryGetProperty("status", out var statusProp)
                    ? statusProp.GetString() : null;

                if (status == "succeeded" || status == "completed")
                {
                    // First try to extract URL from the status response itself
                    string? videoUrl = ExtractVideoUrl(result);

                    // If no URL in status response, fetch from the /content endpoint
                    if (string.IsNullOrEmpty(videoUrl))
                    {
                        var contentUrl = $"{baseJobUrl}/content/video?api-version=preview";
                        _logger.LogInformation("Fetching video content from: {Url}", contentUrl);

                        var contentResponse = await client.GetAsync(contentUrl);
                        if (contentResponse.IsSuccessStatusCode)
                        {
                            var videoBytes = await contentResponse.Content.ReadAsByteArrayAsync();
                            if (videoBytes.Length > 0)
                            {
                                var savedUrl = await _storageService.SaveVideoAsync(videoBytes, $"sora_{jobId}.mp4");
                                job.Status = "completed";
                                job.VideoUrl = savedUrl;
                                return;
                            }
                        }
                        else
                        {
                            // Try alternate content paths
                            var altContentUrl = $"{baseJobUrl}/content?api-version=preview";
                            _logger.LogInformation("Trying alternate content URL: {Url}", altContentUrl);

                            var altResponse = await client.GetAsync(altContentUrl);
                            if (altResponse.IsSuccessStatusCode)
                            {
                                var contentType = altResponse.Content.Headers.ContentType?.MediaType ?? "";
                                if (contentType.StartsWith("video/"))
                                {
                                    var videoBytes = await altResponse.Content.ReadAsByteArrayAsync();
                                    var savedUrl = await _storageService.SaveVideoAsync(videoBytes, $"sora_{jobId}.mp4");
                                    job.Status = "completed";
                                    job.VideoUrl = savedUrl;
                                    return;
                                }
                                else
                                {
                                    // JSON response with URL
                                    var contentResult = await altResponse.Content.ReadFromJsonAsync<JsonElement>();
                                    _logger.LogInformation("Content endpoint response: {Response}", contentResult.ToString());
                                    videoUrl = ExtractVideoUrl(contentResult);
                                }
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(videoUrl))
                    {
                        using var downloadClient = new HttpClient();
                        var dlBytes = await downloadClient.GetByteArrayAsync(videoUrl);
                        var savedPath = await _storageService.SaveVideoAsync(dlBytes, $"sora_{jobId}.mp4");

                        job.Status = "completed";
                        job.VideoUrl = savedPath;
                    }
                    else
                    {
                        job.Status = "failed";
                        job.Error = "Video completed but could not retrieve video content";
                    }
                    return;
                }
                else if (status == "failed" || status == "cancelled")
                {
                    job.Status = "failed";
                    job.Error = result.TryGetProperty("error", out var err)
                        ? err.ToString()
                        : "Video generation failed";
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

    private static string? ExtractVideoUrl(JsonElement result)
    {
        // Try "generations[0].url"
        if (result.TryGetProperty("generations", out var generations)
            && generations.GetArrayLength() > 0)
        {
            var first = generations[0];
            if (first.TryGetProperty("url", out var urlProp))
                return urlProp.GetString();
            if (first.TryGetProperty("video", out var video)
                && video.TryGetProperty("url", out var vidUrl))
                return vidUrl.GetString();
        }

        // Try "data[0].url"
        if (result.TryGetProperty("data", out var data)
            && data.GetArrayLength() > 0)
        {
            if (data[0].TryGetProperty("url", out var urlProp))
                return urlProp.GetString();
        }

        // Try "result.url"
        if (result.TryGetProperty("result", out var res)
            && res.TryGetProperty("url", out var resUrl))
            return resUrl.GetString();

        // Try top-level "url"
        if (result.TryGetProperty("url", out var topUrl))
            return topUrl.GetString();

        return null;
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
