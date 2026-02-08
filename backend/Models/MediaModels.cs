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

// ===== PRESENTATION MODELS =====

public record PresentationRequest
{
    public string Prompt { get; init; } = string.Empty;
    public string Template { get; init; } = "business_report";
    public int SlideCount { get; init; } = 10;
    public string Style { get; init; } = "corporate";
    public string DiagramType { get; init; } = "auto";
    public string ChartStyle { get; init; } = "bar";
    public string AspectRatio { get; init; } = "16:9";
    public string Language { get; init; } = "en";
    public bool IncludeDiagrams { get; init; } = true;
    public bool IncludeCharts { get; init; } = true;
    public bool IncludeSpeakerNotes { get; init; } = true;
    public bool IncludeAnimations { get; init; } = true;
}

public record PresentationResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public string? Id { get; init; }
    public string Status { get; init; } = "pending";
    public int TotalSlides { get; init; }
    public int CompletedSlides { get; init; }
    public Presentation? Presentation { get; init; }
}

public class Presentation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = "generating";
    public List<Slide> Slides { get; set; } = new();
    public PresentationMetadata Metadata { get; set; } = new();
}

public class Slide
{
    public int SlideNumber { get; set; }
    public string Type { get; set; } = "content";
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public List<string> Bullets { get; set; } = new();
    public string? BodyText { get; set; }
    public ChartData? Chart { get; set; }
    public DiagramData? Diagram { get; set; }
    public string? ImageUrl { get; set; }
    public string BackgroundStyle { get; set; } = "default";
    public string? SpeakerNotes { get; set; }
    public string Layout { get; set; } = "full_width";
}

public class ChartData
{
    public string Type { get; set; } = "bar";
    public ChartDataset Data { get; set; } = new();
}

public class ChartDataset
{
    public List<string> Labels { get; set; } = new();
    public List<double> Values { get; set; } = new();
    public List<List<double>>? MultiValues { get; set; }
    public List<string>? SeriesNames { get; set; }
}

public class DiagramData
{
    public string Type { get; set; } = "flowchart";
    public string MermaidCode { get; set; } = string.Empty;
}

public class PresentationMetadata
{
    public string Template { get; set; } = "business_report";
    public string Style { get; set; } = "corporate";
    public string AspectRatio { get; set; } = "16:9";
    public string Language { get; set; } = "en";
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
