using System.Net.Http.Json;
using System.Text.Json;

namespace ArtisanStudio.Services;

/// <summary>
/// Service for enhancing user prompts using AI.
/// Demonstrates understanding of prompt engineering principles.
/// </summary>
public interface IPromptEnhancementService
{
    Task<PromptEnhancementResult> EnhancePromptAsync(string originalPrompt, string mediaType);
}

public record PromptEnhancementResult
{
    public string OriginalPrompt { get; init; } = "";
    public string EnhancedPrompt { get; init; } = "";
    public List<string> Suggestions { get; init; } = new();
    public Dictionary<string, string> AddedElements { get; init; } = new();
}

public class PromptEnhancementService : IPromptEnhancementService
{
    private readonly ILogger<PromptEnhancementService> _logger;

    // Prompt engineering knowledge base
    private static readonly Dictionary<string, List<string>> ImageEnhancements = new()
    {
        ["lighting"] = new() { "golden hour lighting", "soft diffused light", "dramatic chiaroscuro", "neon glow", "natural daylight" },
        ["style"] = new() { "photorealistic", "cinematic", "artistic", "hyperrealistic 8K", "oil painting style" },
        ["composition"] = new() { "rule of thirds", "centered composition", "wide angle shot", "close-up detail", "bird's eye view" },
        ["mood"] = new() { "serene and peaceful", "dramatic and intense", "whimsical and playful", "mysterious atmosphere", "warm and inviting" },
        ["quality"] = new() { "highly detailed", "sharp focus", "professional photography", "award-winning", "masterpiece quality" }
    };

    private static readonly Dictionary<string, List<string>> VideoEnhancements = new()
    {
        ["motion"] = new() { "smooth camera movement", "slow motion", "dynamic tracking shot", "steady timelapse", "cinematic dolly" },
        ["style"] = new() { "cinematic look", "documentary style", "dreamlike quality", "high contrast", "film grain aesthetic" },
        ["pacing"] = new() { "gradually revealing", "building intensity", "gentle and flowing", "rhythmic motion", "seamless loop" }
    };

    public PromptEnhancementService(ILogger<PromptEnhancementService> logger)
    {
        _logger = logger;
    }

    public async Task<PromptEnhancementResult> EnhancePromptAsync(string originalPrompt, string mediaType)
    {
        _logger.LogInformation("Enhancing prompt: {Prompt} for {MediaType}", originalPrompt, mediaType);

        var addedElements = new Dictionary<string, string>();
        var suggestions = new List<string>();
        var enhancedPrompt = originalPrompt;

        // Analyze the prompt for missing elements
        var promptLower = originalPrompt.ToLower();
        var enhancements = mediaType == "video" ? VideoEnhancements : ImageEnhancements;

        foreach (var (category, options) in enhancements)
        {
            // Check if category is already addressed in the prompt
            bool hasCategory = category switch
            {
                "lighting" => ContainsAny(promptLower, "light", "sun", "shadow", "glow", "bright", "dark"),
                "style" => ContainsAny(promptLower, "style", "realistic", "artistic", "cinematic", "painting"),
                "composition" => ContainsAny(promptLower, "angle", "shot", "view", "close", "wide", "centered"),
                "mood" => ContainsAny(promptLower, "mood", "atmosphere", "feeling", "serene", "dramatic"),
                "quality" => ContainsAny(promptLower, "detailed", "quality", "hd", "4k", "8k", "sharp"),
                "motion" => ContainsAny(promptLower, "motion", "movement", "moving", "flowing", "tracking"),
                "pacing" => ContainsAny(promptLower, "slow", "fast", "gradual", "smooth", "dynamic"),
                _ => false
            };

            if (!hasCategory)
            {
                // Suggest an enhancement
                var suggestion = options[Random.Shared.Next(options.Count)];
                addedElements[category] = suggestion;
                suggestions.Add($"Consider adding {category}: \"{suggestion}\"");
            }
        }

        // Build enhanced prompt
        if (addedElements.Count > 0)
        {
            var additions = string.Join(", ", addedElements.Values);
            enhancedPrompt = $"{originalPrompt}, {additions}";
        }

        // Add quality suffix for images
        if (mediaType == "image" && !promptLower.Contains("quality") && !promptLower.Contains("detailed"))
        {
            enhancedPrompt += ", highly detailed, professional quality";
        }

        await Task.CompletedTask; // Placeholder for async AI enhancement

        return new PromptEnhancementResult
        {
            OriginalPrompt = originalPrompt,
            EnhancedPrompt = enhancedPrompt,
            Suggestions = suggestions,
            AddedElements = addedElements
        };
    }

    private static bool ContainsAny(string text, params string[] keywords)
    {
        return keywords.Any(k => text.Contains(k));
    }
}

/*
 * INTERVIEW TALKING POINTS for this service:
 * 
 * 1. "This demonstrates my understanding of prompt engineering - 
 *     good prompts need specific elements like lighting, composition, and style."
 * 
 * 2. "I built this as a rule-based system first, but it's designed to be 
 *     swapped with an LLM-based enhancer. The interface stays the same."
 * 
 * 3. "This shows the Strategy pattern - I can switch between rule-based 
 *     and AI-based enhancement without changing the controller."
 * 
 * 4. "The enhancement suggestions help users learn prompt engineering 
 *     while using the app - it's educational, not just functional."
 */
