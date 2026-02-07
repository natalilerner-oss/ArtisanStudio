namespace ArtisanStudio.Services;

public class LocalStorageService : IStorageService
{
    private readonly string _mediaPath;
    private readonly ILogger<LocalStorageService> _logger;

    public LocalStorageService(IWebHostEnvironment env, ILogger<LocalStorageService> logger)
    {
        _logger = logger;
        _mediaPath = Path.Combine(env.WebRootPath ?? "wwwroot", "media");
        
        // Ensure directories exist
        Directory.CreateDirectory(Path.Combine(_mediaPath, "images"));
        Directory.CreateDirectory(Path.Combine(_mediaPath, "videos"));
    }

    public async Task<string> SaveImageAsync(byte[] data, string filename)
    {
        var path = Path.Combine(_mediaPath, "images", filename);
        await File.WriteAllBytesAsync(path, data);
        _logger.LogInformation("Saved image: {Path}", path);
        return $"/media/images/{filename}";
    }

    public async Task<string> SaveVideoAsync(byte[] data, string filename)
    {
        var path = Path.Combine(_mediaPath, "videos", filename);
        await File.WriteAllBytesAsync(path, data);
        _logger.LogInformation("Saved video: {Path}", path);
        return $"/media/videos/{filename}";
    }

    public string GetMediaUrl(string filename)
    {
        return $"/media/{filename}";
    }
}
