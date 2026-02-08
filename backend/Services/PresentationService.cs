using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ArtisanStudio.Models;

namespace ArtisanStudio.Services;

public class PresentationService : IPresentationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PresentationService> _logger;
    private readonly string _openAiEndpoint;
    private readonly string _openAiKey;
    private readonly string _deploymentName;

    private static readonly ConcurrentDictionary<string, PresentationJob> _jobs = new();

    public PresentationService(
        IHttpClientFactory httpClientFactory,
        ILogger<PresentationService> logger,
        IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _openAiEndpoint = config["AZURE_OPENAI_ENDPOINT"]
            ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
            ?? config["AZURE_DALLE_ENDPOINT"]
            ?? Environment.GetEnvironmentVariable("AZURE_DALLE_ENDPOINT")
            ?? "";
        _openAiKey = config["AZURE_OPENAI_API_KEY"]
            ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")
            ?? config["AZURE_DALLE_API_KEY"]
            ?? Environment.GetEnvironmentVariable("AZURE_DALLE_API_KEY")
            ?? "";
        _deploymentName = config["AZURE_OPENAI_DEPLOYMENT"]
            ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT")
            ?? "gpt-4o";
    }

    public async Task<PresentationResponse> GeneratePresentationAsync(PresentationRequest request)
    {
        var jobId = $"pres_{Guid.NewGuid():N}";

        _logger.LogInformation("Starting presentation generation: {JobId} - {Prompt}", jobId, request.Prompt);

        var job = new PresentationJob
        {
            Id = jobId,
            Request = request,
            Status = "generating",
            TotalSlides = request.SlideCount,
            CreatedAt = DateTime.UtcNow
        };
        _jobs[jobId] = job;

        // Start generation in background
        _ = GenerateInBackgroundAsync(job);

        return new PresentationResponse
        {
            Success = true,
            Id = jobId,
            Status = "generating",
            TotalSlides = request.SlideCount,
            CompletedSlides = 0,
            Message = "Presentation generation started"
        };
    }

    public Task<PresentationResponse> GetPresentationStatusAsync(string id)
    {
        if (!_jobs.TryGetValue(id, out var job))
        {
            return Task.FromResult(new PresentationResponse
            {
                Success = false,
                Status = "not_found",
                Message = "Presentation not found"
            });
        }

        return Task.FromResult(new PresentationResponse
        {
            Success = job.Status != "failed",
            Id = id,
            Status = job.Status,
            TotalSlides = job.TotalSlides,
            CompletedSlides = job.CompletedSlides,
            Message = job.Error,
            Presentation = job.Status == "completed" ? job.Result : null
        });
    }

    public Task<Presentation?> GetPresentationAsync(string id)
    {
        if (_jobs.TryGetValue(id, out var job) && job.Result != null)
            return Task.FromResult<Presentation?>(job.Result);
        return Task.FromResult<Presentation?>(null);
    }

    public async Task<byte[]?> ExportPresentationAsync(string id, string format)
    {
        if (!_jobs.TryGetValue(id, out var job) || job.Result == null)
            return null;

        if (format == "pptx")
        {
            return PptxExportService.GeneratePptx(job.Result);
        }

        return null;
    }

    private async Task GenerateInBackgroundAsync(PresentationJob job)
    {
        try
        {
            if (string.IsNullOrEmpty(_openAiKey))
            {
                // Demo mode - generate sample presentation
                _logger.LogInformation("Demo mode: generating sample presentation for {Id}", job.Id);
                await Task.Delay(2000);
                job.Result = GenerateDemoPresentation(job.Request);
                job.CompletedSlides = job.TotalSlides;
                job.Status = "completed";
                return;
            }

            // Use Azure OpenAI to generate presentation
            var presentation = await GenerateWithAIAsync(job);
            if (presentation != null)
            {
                job.Result = presentation;
                job.CompletedSlides = presentation.Slides.Count;
                job.Status = "completed";
            }
            else
            {
                job.Status = "failed";
                job.Error = "Failed to generate presentation content";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating presentation {Id}", job.Id);
            job.Status = "failed";
            job.Error = ex.Message;
        }
    }

    private async Task<Presentation?> GenerateWithAIAsync(PresentationJob job)
    {
        var req = job.Request;
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("api-key", _openAiKey);
        client.Timeout = TimeSpan.FromMinutes(3);

        var endpoint = _openAiEndpoint.TrimEnd('/');
        var url = $"{endpoint}/openai/deployments/{_deploymentName}/chat/completions?api-version=2024-02-01";

        var systemPrompt = BuildSystemPrompt(req);
        var userPrompt = BuildUserPrompt(req);

        var payload = new
        {
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = 0.7,
            max_tokens = 4000,
            response_format = new { type = "json_object" }
        };

        _logger.LogInformation("Calling Azure OpenAI for presentation: {Url}", url);

        var response = await client.PostAsJsonAsync(url, payload);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("Azure OpenAI error: {Status} {Error}", response.StatusCode, error);

            // Fallback to demo mode on API error
            _logger.LogInformation("Falling back to demo generation for {Id}", job.Id);
            return GenerateDemoPresentation(req);
        }

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();

        var content = result.GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrEmpty(content))
        {
            return GenerateDemoPresentation(req);
        }

        _logger.LogInformation("AI response received, parsing...");

        try
        {
            var aiPresentation = JsonSerializer.Deserialize<JsonElement>(content);
            return ParseAIPresentation(aiPresentation, req, job.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing AI response, falling back to demo");
            return GenerateDemoPresentation(req);
        }
    }

    private string BuildSystemPrompt(PresentationRequest req)
    {
        return @"You are a professional presentation designer. Generate a structured business presentation as JSON.
The JSON must have this exact structure:
{
  ""title"": ""Presentation Title"",
  ""slides"": [
    {
      ""slideNumber"": 1,
      ""type"": ""title|content|content_with_chart|diagram|comparison|timeline|stats|closing"",
      ""title"": ""Slide Title"",
      ""subtitle"": ""Optional subtitle"",
      ""bullets"": [""Point 1"", ""Point 2""],
      ""bodyText"": ""Optional body text"",
      ""chart"": { ""type"": ""bar|line|pie|area"", ""data"": { ""labels"": [""A"",""B""], ""values"": [10,20] } },
      ""diagram"": { ""type"": ""flowchart|orgchart|timeline|mindmap"", ""mermaidCode"": ""graph TD; A-->B"" },
      ""speakerNotes"": ""Notes for the presenter"",
      ""layout"": ""full_width|split_left|split_right|centered""
    }
  ]
}

Rules:
- First slide must be type ""title"" with the presentation title and subtitle
- Last slide must be type ""closing"" with thank you / Q&A
- Include a mix of slide types for visual variety
- Charts must have realistic sample data with at least 3-5 data points
- Mermaid diagram code must be valid Mermaid syntax
- " + (req.IncludeSpeakerNotes ? "Include speaker notes for each slide" : "Omit speakerNotes") + @"
- " + (req.IncludeCharts ? "Include at least 2 slides with charts" : "Do not include charts") + @"
- " + (req.IncludeDiagrams ? "Include at least 1 slide with a diagram" : "Do not include diagrams") + @"
- Language: " + (req.Language == "he" ? "Hebrew" : "English") + @"
- Template style: " + req.Template + @"
- Visual style: " + req.Style;
    }

    private string BuildUserPrompt(PresentationRequest req)
    {
        return $"Create a {req.SlideCount}-slide {req.Template.Replace("_", " ")} presentation about: {req.Prompt}\n\n" +
               $"Chart style preference: {req.ChartStyle}\n" +
               $"Diagram type preference: {req.DiagramType}\n" +
               "Return ONLY valid JSON.";
    }

    private Presentation ParseAIPresentation(JsonElement ai, PresentationRequest req, string id)
    {
        var presentation = new Presentation
        {
            Id = id,
            Title = ai.TryGetProperty("title", out var t) ? t.GetString() ?? "Presentation" : "Presentation",
            Status = "completed",
            Metadata = new PresentationMetadata
            {
                Template = req.Template,
                Style = req.Style,
                AspectRatio = req.AspectRatio,
                Language = req.Language,
                GeneratedAt = DateTime.UtcNow
            }
        };

        if (ai.TryGetProperty("slides", out var slides))
        {
            foreach (var s in slides.EnumerateArray())
            {
                var slide = new Slide
                {
                    SlideNumber = s.TryGetProperty("slideNumber", out var sn) ? sn.GetInt32() : 0,
                    Type = s.TryGetProperty("type", out var tp) ? tp.GetString() ?? "content" : "content",
                    Title = s.TryGetProperty("title", out var ti) ? ti.GetString() ?? "" : "",
                    Subtitle = s.TryGetProperty("subtitle", out var st) ? st.GetString() : null,
                    BodyText = s.TryGetProperty("bodyText", out var bt) ? bt.GetString() : null,
                    SpeakerNotes = s.TryGetProperty("speakerNotes", out var sno) ? sno.GetString() : null,
                    Layout = s.TryGetProperty("layout", out var lay) ? lay.GetString() ?? "full_width" : "full_width"
                };

                if (s.TryGetProperty("bullets", out var bullets) && bullets.ValueKind == JsonValueKind.Array)
                {
                    slide.Bullets = bullets.EnumerateArray()
                        .Select(b => b.GetString() ?? "")
                        .Where(b => !string.IsNullOrEmpty(b))
                        .ToList();
                }

                if (s.TryGetProperty("chart", out var chart) && chart.ValueKind == JsonValueKind.Object)
                {
                    slide.Chart = new ChartData
                    {
                        Type = chart.TryGetProperty("type", out var ct) ? ct.GetString() ?? "bar" : "bar"
                    };

                    if (chart.TryGetProperty("data", out var cd))
                    {
                        slide.Chart.Data = new ChartDataset();

                        if (cd.TryGetProperty("labels", out var labels) && labels.ValueKind == JsonValueKind.Array)
                        {
                            slide.Chart.Data.Labels = labels.EnumerateArray()
                                .Select(l => l.GetString() ?? "")
                                .ToList();
                        }

                        if (cd.TryGetProperty("values", out var values) && values.ValueKind == JsonValueKind.Array)
                        {
                            slide.Chart.Data.Values = values.EnumerateArray()
                                .Select(v =>
                                {
                                    if (v.ValueKind == JsonValueKind.Number) return v.GetDouble();
                                    if (double.TryParse(v.GetString(), out var d)) return d;
                                    return 0.0;
                                })
                                .ToList();
                        }
                    }
                }

                if (s.TryGetProperty("diagram", out var diagram) && diagram.ValueKind == JsonValueKind.Object)
                {
                    slide.Diagram = new DiagramData
                    {
                        Type = diagram.TryGetProperty("type", out var dt) ? dt.GetString() ?? "flowchart" : "flowchart",
                        MermaidCode = diagram.TryGetProperty("mermaidCode", out var mc) ? mc.GetString() ?? "" : ""
                    };
                }

                presentation.Slides.Add(slide);
            }
        }

        return presentation;
    }

    private Presentation GenerateDemoPresentation(PresentationRequest req)
    {
        var id = $"pres_{Guid.NewGuid():N}";
        var isHebrew = req.Language == "he";

        var title = ExtractTitle(req.Prompt, req.Template);
        var slideCount = req.SlideCount;

        var slides = new List<Slide>();
        int num = 1;

        // Slide 1: Title
        slides.Add(new Slide
        {
            SlideNumber = num++,
            Type = "title",
            Title = title,
            Subtitle = isHebrew ? "מצגת עסקית" : "Business Presentation",
            BackgroundStyle = "gradient_dark",
            SpeakerNotes = req.IncludeSpeakerNotes
                ? (isHebrew ? "ברוכים הבאים למצגת" : "Welcome to the presentation. Today we'll cover key topics.")
                : null,
            Layout = "centered"
        });

        // Slide 2: Agenda / Overview
        slides.Add(new Slide
        {
            SlideNumber = num++,
            Type = "content",
            Title = isHebrew ? "סדר יום" : "Agenda",
            Bullets = isHebrew
                ? new List<string> { "סקירה כללית", "תוצאות עיקריות", "ניתוח שוק", "תוכנית קדימה", "שאלות ותשובות" }
                : new List<string> { "Overview & Context", "Key Results", "Market Analysis", "Growth Strategy", "Q&A" },
            SpeakerNotes = req.IncludeSpeakerNotes ? "Let's start with our agenda for today." : null,
            Layout = "full_width"
        });

        // Slide 3: Stats / KPIs
        slides.Add(new Slide
        {
            SlideNumber = num++,
            Type = "stats",
            Title = isHebrew ? "מדדים מרכזיים" : "Key Metrics",
            Bullets = new List<string> { "$12.4M Revenue", "15% YoY Growth", "2,500+ Customers", "98% Satisfaction" },
            SpeakerNotes = req.IncludeSpeakerNotes ? "These are our key performance indicators." : null,
            Layout = "full_width"
        });

        // Slide 4: Chart
        if (req.IncludeCharts && num <= slideCount)
        {
            slides.Add(new Slide
            {
                SlideNumber = num++,
                Type = "content_with_chart",
                Title = isHebrew ? "סקירת הכנסות" : "Revenue Overview",
                Bullets = new List<string>
                {
                    isHebrew ? "צמיחה עקבית רבעונית" : "Consistent quarterly growth",
                    isHebrew ? "חציון שני חזק" : "Strong H2 performance"
                },
                Chart = new ChartData
                {
                    Type = req.ChartStyle == "None" ? "bar" : req.ChartStyle.ToLower(),
                    Data = new ChartDataset
                    {
                        Labels = new List<string> { "Q1", "Q2", "Q3", "Q4" },
                        Values = new List<double> { 8.2, 10.1, 12.4, 14.8 }
                    }
                },
                SpeakerNotes = req.IncludeSpeakerNotes ? "Revenue has grown steadily across all quarters." : null,
                Layout = "split_left"
            });
        }

        // Slide 5: Diagram
        if (req.IncludeDiagrams && num <= slideCount)
        {
            var (diagramType, mermaidCode) = GetDiagramForType(req.DiagramType, isHebrew);
            slides.Add(new Slide
            {
                SlideNumber = num++,
                Type = "diagram",
                Title = isHebrew ? "מבנה ארגוני" : "Organization Structure",
                Diagram = new DiagramData
                {
                    Type = diagramType,
                    MermaidCode = mermaidCode
                },
                SpeakerNotes = req.IncludeSpeakerNotes ? "This shows our current organizational structure." : null,
                Layout = "full_width"
            });
        }

        // Slide 6: Second chart
        if (req.IncludeCharts && num <= slideCount)
        {
            slides.Add(new Slide
            {
                SlideNumber = num++,
                Type = "content_with_chart",
                Title = isHebrew ? "ניתוח שוק" : "Market Analysis",
                Bullets = new List<string>
                {
                    isHebrew ? "נתח שוק של 35%" : "35% market share",
                    isHebrew ? "שוק צומח של $2.1B" : "$2.1B total addressable market"
                },
                Chart = new ChartData
                {
                    Type = "pie",
                    Data = new ChartDataset
                    {
                        Labels = new List<string> { "Our Company", "Competitor A", "Competitor B", "Others" },
                        Values = new List<double> { 35, 28, 22, 15 }
                    }
                },
                SpeakerNotes = req.IncludeSpeakerNotes ? "We hold the largest market share in our segment." : null,
                Layout = "split_right"
            });
        }

        // Slide 7: Timeline
        if (num <= slideCount)
        {
            slides.Add(new Slide
            {
                SlideNumber = num++,
                Type = "timeline",
                Title = isHebrew ? "תוכנית פעולה" : "Roadmap",
                Bullets = new List<string>
                {
                    "Q1 2025: Platform Launch",
                    "Q2 2025: Market Expansion",
                    "Q3 2025: Enterprise Features",
                    "Q4 2025: International Growth"
                },
                Diagram = new DiagramData
                {
                    Type = "timeline",
                    MermaidCode = "timeline\n    title Product Roadmap\n    Q1 2025 : Platform Launch\n    Q2 2025 : Market Expansion\n    Q3 2025 : Enterprise Features\n    Q4 2025 : International Growth"
                },
                SpeakerNotes = req.IncludeSpeakerNotes ? "Our roadmap for the next year." : null,
                Layout = "full_width"
            });
        }

        // Slide 8: Comparison
        if (num <= slideCount)
        {
            slides.Add(new Slide
            {
                SlideNumber = num++,
                Type = "comparison",
                Title = isHebrew ? "השוואה" : "Before vs After",
                Bullets = new List<string>
                {
                    "Before: Manual processes, 2-day turnaround",
                    "After: Automated workflow, real-time results",
                    "Before: Limited data insights",
                    "After: AI-powered analytics dashboard"
                },
                SpeakerNotes = req.IncludeSpeakerNotes ? "The transformation we've achieved." : null,
                Layout = "split_left"
            });
        }

        // Fill remaining slides with content
        while (num <= slideCount - 1)
        {
            slides.Add(new Slide
            {
                SlideNumber = num,
                Type = "content",
                Title = isHebrew ? $"נושא {num}" : $"Key Topic {num}",
                Bullets = new List<string>
                {
                    isHebrew ? "נקודה מרכזית ראשונה" : "First key point for this section",
                    isHebrew ? "נתונים תומכים" : "Supporting data and evidence",
                    isHebrew ? "סיכום וצעדים הבאים" : "Summary and next steps"
                },
                SpeakerNotes = req.IncludeSpeakerNotes ? $"Additional details for topic {num}." : null,
                Layout = num % 2 == 0 ? "split_left" : "full_width"
            });
            num++;
        }

        // Closing slide
        slides.Add(new Slide
        {
            SlideNumber = num,
            Type = "closing",
            Title = isHebrew ? "תודה רבה" : "Thank You",
            Subtitle = isHebrew ? "שאלות?" : "Questions & Discussion",
            Bullets = new List<string>
            {
                isHebrew ? "צרו קשר: info@company.com" : "Contact: info@company.com",
                isHebrew ? "אתר: www.company.com" : "Website: www.company.com"
            },
            SpeakerNotes = req.IncludeSpeakerNotes ? "Thank you for your time. I'm happy to take questions." : null,
            Layout = "centered"
        });

        return new Presentation
        {
            Id = id,
            Title = title,
            Status = "completed",
            Slides = slides,
            Metadata = new PresentationMetadata
            {
                Template = req.Template,
                Style = req.Style,
                AspectRatio = req.AspectRatio,
                Language = req.Language,
                GeneratedAt = DateTime.UtcNow
            }
        };
    }

    private string ExtractTitle(string prompt, string template)
    {
        // Extract a reasonable title from the prompt
        if (prompt.Length <= 60) return prompt;

        var sentence = prompt.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (sentence != null && sentence.Length <= 80) return sentence.Trim();

        return prompt.Substring(0, 57) + "...";
    }

    private (string type, string code) GetDiagramForType(string diagramType, bool isHebrew)
    {
        return diagramType.ToLower() switch
        {
            "flowchart" => ("flowchart", "graph TD\n    A[Start] --> B{Decision}\n    B -->|Yes| C[Process A]\n    B -->|No| D[Process B]\n    C --> E[Result]\n    D --> E"),
            "orgchart" or "org chart" => ("orgchart", "graph TD\n    CEO[CEO] --> CTO[CTO]\n    CEO --> CFO[CFO]\n    CEO --> COO[COO]\n    CTO --> Dev[Dev Team]\n    CTO --> QA[QA Team]\n    CFO --> Finance[Finance]\n    COO --> Ops[Operations]"),
            "mindmap" or "mind map" => ("mindmap", "mindmap\n  root((Strategy))\n    Growth\n      New Markets\n      Product Expansion\n    Technology\n      AI Integration\n      Cloud Migration\n    Team\n      Hiring\n      Training"),
            "timeline" => ("timeline", "timeline\n    title Project Timeline\n    Phase 1 : Research\n    Phase 2 : Development\n    Phase 3 : Testing\n    Phase 4 : Launch"),
            "process" or "process flow" => ("flowchart", "graph LR\n    A[Input] --> B[Process 1]\n    B --> C[Process 2]\n    C --> D[Review]\n    D --> E[Output]"),
            _ => ("orgchart", "graph TD\n    CEO[CEO] --> CTO[CTO]\n    CEO --> CFO[CFO]\n    CEO --> COO[COO]\n    CTO --> Dev[Dev Team]\n    CTO --> QA[QA Team]\n    CFO --> Finance[Finance]\n    COO --> Ops[Operations]")
        };
    }

    private class PresentationJob
    {
        public string Id { get; set; } = "";
        public PresentationRequest Request { get; set; } = new();
        public string Status { get; set; } = "pending";
        public int TotalSlides { get; set; }
        public int CompletedSlides { get; set; }
        public string? Error { get; set; }
        public Presentation? Result { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
