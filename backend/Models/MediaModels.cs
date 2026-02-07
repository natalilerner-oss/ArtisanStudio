namespace ArtisanStudio.Models;

public record PromptEnhanceRequest
{
    public string Prompt { get; init; } = string.Empty;
    public string? MediaType { get; init; } = "image";
}

public record ImageGenerationRequest
{
    public string Prompt { get; init; } = string.Empty;
    public string Style { get; init; } = "vivid";
    public string Size { get; init; } = "1024x1024";
    public string Quality { get; init; } = "standard";
}

public record ImageGenerationResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public List<GeneratedImage> Images { get; init; } = new();
}

public record GeneratedImage
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Url { get; init; } = string.Empty;
    public string Prompt { get; init; } = string.Empty;
    public string RevisedPrompt { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

public record VideoGenerationRequest
{
    public string Prompt { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public int DurationSeconds { get; init; } = 5;
    public string AspectRatio { get; init; } = "16:9";
}

public record VideoGenerationResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public GeneratedVideo? Video { get; init; }
    public string? JobId { get; init; }
    public string Status { get; init; } = "pending";
}

public record GeneratedVideo
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Url { get; init; } = string.Empty;
    public string Prompt { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public int DurationSeconds { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
