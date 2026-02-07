# Architecture Documentation

## Overview

Artisan Studio follows a **Clean Architecture** pattern with clear separation of concerns between the presentation layer (React), application layer (Controllers), and infrastructure layer (Services).

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                        PRESENTATION LAYER                           │
│  ┌───────────────────────────────────────────────────────────────┐ │
│  │                     React Frontend                             │ │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐   │ │
│  │  │  App.jsx    │  │  App.css    │  │  React Hooks        │   │ │
│  │  │  (State)    │  │  (Styling)  │  │  (useState, etc.)   │   │ │
│  │  └──────┬──────┘  └─────────────┘  └─────────────────────┘   │ │
│  └─────────┼─────────────────────────────────────────────────────┘ │
│            │ HTTP/JSON                                              │
│            ▼                                                        │
│  ┌───────────────────────────────────────────────────────────────┐ │
│  │                    ASP.NET Core API                            │ │
│  │  ┌─────────────────────────────────────────────────────────┐  │ │
│  │  │                  MediaController                         │  │ │
│  │  │  • POST /api/images/generate                            │  │ │
│  │  │  • POST /api/videos/generate                            │  │ │
│  │  │  • GET  /api/videos/status/{jobId}                      │  │ │
│  │  │  • GET  /api/health                                     │  │ │
│  │  └──────────────────────┬──────────────────────────────────┘  │ │
│  └─────────────────────────┼─────────────────────────────────────┘ │
└────────────────────────────┼────────────────────────────────────────┘
                             │
┌────────────────────────────┼────────────────────────────────────────┐
│                            ▼                                        │
│                    APPLICATION LAYER                                │
│  ┌───────────────────────────────────────────────────────────────┐ │
│  │                   Service Interfaces                           │ │
│  │  ┌──────────────────────┐  ┌───────────────────────────────┐  │ │
│  │  │ IImageGenerationSvc  │  │ IVideoGenerationService       │  │ │
│  │  │ + GenerateImageAsync │  │ + GenerateVideoAsync          │  │ │
│  │  └──────────┬───────────┘  │ + GetVideoStatusAsync         │  │ │
│  │             │              └────────────────┬──────────────┘  │ │
│  └─────────────┼───────────────────────────────┼─────────────────┘ │
│                │                               │                    │
│                ▼                               ▼                    │
│  ┌─────────────────────────┐   ┌─────────────────────────────────┐│
│  │ ImageGenerationService  │   │ VideoGenerationService          ││
│  │ • Flux 1.1 Pro          │   │ • CogVideoX                     ││
│  │ • DALL-E 3              │   │ • Runway Gen-3                  ││
│  │ • SDXL                  │   │ • Job tracking                  ││
│  └───────────┬─────────────┘   └─────────────────┬───────────────┘│
└──────────────┼───────────────────────────────────┼─────────────────┘
               │                                   │
┌──────────────┼───────────────────────────────────┼─────────────────┐
│              ▼                                   ▼                  │
│                      INFRASTRUCTURE LAYER                           │
│  ┌─────────────────────────────────────────────────────────────────┐
│  │                    External Services                            │
│  │  ┌─────────────┐  ┌─────────────┐  ┌──────────────────┐       │
│  │  │  Replicate  │  │   Runway    │  │  Local Storage   │       │
│  │  │  API        │  │   API       │  │  (IStorageService)│      │
│  │  └─────────────┘  └─────────────┘  └──────────────────┘       │
│  └─────────────────────────────────────────────────────────────────┘
└─────────────────────────────────────────────────────────────────────┘
```

## Design Patterns Used

### 1. Dependency Injection (DI)

All services are registered in `Program.cs` and injected via constructors:

```csharp
// Program.cs
builder.Services.AddSingleton<IImageGenerationService, ImageGenerationService>();
builder.Services.AddSingleton<IVideoGenerationService, VideoGenerationService>();
builder.Services.AddSingleton<IStorageService, LocalStorageService>();
```

**Benefits:**
- Loose coupling between components
- Easy to mock for unit testing
- Swap implementations without code changes

### 2. Repository Pattern (Storage)

`IStorageService` abstracts file storage operations:

```csharp
public interface IStorageService
{
    Task<string> SaveImageAsync(byte[] data, string filename);
    Task<string> SaveVideoAsync(byte[] data, string filename);
}
```

**Benefits:**
- Can switch from local storage to Azure Blob without changing services
- Testable with mock storage

### 3. Strategy Pattern (AI Providers)

Different AI providers are selected based on configuration:

```csharp
// In ImageGenerationService
return _provider.ToLower() switch
{
    "flux" => await GenerateWithFluxAsync(request),
    "dalle" => await GenerateWithDalleAsync(request),
    "sdxl" => await GenerateWithSDXLAsync(request),
    _ => await GenerateWithFluxAsync(request)
};
```

**Benefits:**
- Add new providers without modifying existing code
- Runtime provider selection via configuration

### 4. Job Queue Pattern (Video Generation)

Video generation uses async job tracking:

```
1. Client sends request → Returns JobId immediately
2. Backend starts async generation
3. Client polls /videos/status/{jobId}
4. Backend returns status (processing/completed/failed)
```

```csharp
private static readonly ConcurrentDictionary<string, VideoJob> _jobs = new();
```

**Benefits:**
- Handles long-running operations (1-3 minutes)
- Non-blocking HTTP requests
- Thread-safe job tracking

## Data Flow

### Image Generation Flow

```
User Input
    │
    ▼
┌─────────────────┐
│ React Frontend  │
│ POST /images/   │
│ generate        │
└────────┬────────┘
         │ JSON Request
         ▼
┌─────────────────┐
│ MediaController │
│ Validates input │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ ImageGeneration │
│ Service         │
│ • Call AI API   │
│ • Poll result   │
│ • Download image│
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ StorageService  │
│ Save to disk    │
└────────┬────────┘
         │ URL
         ▼
┌─────────────────┐
│ React Frontend  │
│ Display image   │
└─────────────────┘
```

### Video Generation Flow

```
User Input
    │
    ▼
┌─────────────────┐
│ React Frontend  │
│ POST /videos/   │
│ generate        │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ VideoGeneration │
│ Service         │
│ • Start job     │
│ • Return JobId  │
└────────┬────────┘
         │ JobId
         ▼
┌─────────────────┐     ┌──────────────────┐
│ React Frontend  │────►│ Background Task  │
│ Poll status     │     │ • Check AI API   │
│ every 3 seconds │     │ • Update job     │
└────────┬────────┘     └──────────────────┘
         │
         ▼ (when completed)
┌─────────────────┐
│ Display video   │
└─────────────────┘
```

## Security Considerations

1. **API Keys**: Stored in environment variables, never in code
2. **Input Validation**: All endpoints validate input before processing
3. **CORS**: Configured to allow only specific origins
4. **Content Moderation**: AI providers handle content safety
5. **Rate Limiting**: Should be implemented for production

## Scalability Considerations

### Current Limitations
- In-memory job storage (lost on restart)
- Single server deployment
- Local file storage

### Production Improvements
1. **Job Storage**: Use Redis or Azure Table Storage
2. **File Storage**: Use Azure Blob Storage or S3
3. **Horizontal Scaling**: Stateless API behind load balancer
4. **Caching**: Cache frequently used prompts
5. **CDN**: Serve generated media through CDN

## Technology Choices

| Component | Choice | Rationale |
|-----------|--------|-----------|
| Frontend | React 18 | Industry standard, hooks-based |
| Build Tool | Vite | Fast HMR, modern ESM |
| Backend | ASP.NET Core 8 | High performance, strong typing |
| Image AI | Flux 1.1 Pro | Best quality/speed ratio |
| Video AI | CogVideoX | Good open-source option |
| Styling | CSS Variables | Native, no dependencies |

## Testing Strategy

### Unit Tests
- Service layer with mocked dependencies
- Controller layer with mocked services
- Model validation

### Integration Tests (Future)
- End-to-end API tests
- Frontend component tests

### Test Coverage Goals
- Services: 80%+
- Controllers: 90%+
- Models: 100%

## Future Enhancements

1. **Authentication**: OAuth with Azure AD
2. **WebSockets**: Real-time status updates
3. **Prompt Library**: Save and reuse prompts
4. **Batch Processing**: Generate multiple images
5. **Image-to-Video**: Animate generated images
6. **Analytics**: Track usage and popular prompts
