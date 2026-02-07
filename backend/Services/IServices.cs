using ArtisanStudio.Models;

namespace ArtisanStudio.Services;

public interface IImageGenerationService
{
    Task<ImageGenerationResponse> GenerateImageAsync(ImageGenerationRequest request);
}

public interface IVideoGenerationService
{
    Task<VideoGenerationResponse> GenerateVideoAsync(VideoGenerationRequest request);
    Task<VideoGenerationResponse> GetVideoStatusAsync(string jobId);
}

public interface IStorageService
{
    Task<string> SaveImageAsync(byte[] data, string filename);
    Task<string> SaveVideoAsync(byte[] data, string filename);
    string GetMediaUrl(string filename);
}
