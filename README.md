# 🎨 Artisan Studio

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/React-18.2-61DAFB?style=flat-square&logo=react)](https://reactjs.org/)
[![Flux](https://img.shields.io/badge/AI-Flux%201.1%20Pro-8B5CF6?style=flat-square)](https://replicate.com/)
[![License](https://img.shields.io/badge/License-MIT-green?style=flat-square)](LICENSE)

> **AI-Powered Image & Video Generation Platform** — A full-stack web application demonstrating modern AI integration, clean architecture, and production-ready development practices.

![Artisan Studio Demo](docs/demo-screenshot.png)

## 🎯 Project Overview

Artisan Studio is a production-ready web application that enables users to generate AI images and videos using state-of-the-art models. This project demonstrates:

- **AI/ML Integration** — Working with cutting-edge generative AI APIs
- **Full-Stack Development** — React frontend + ASP.NET Core backend
- **Clean Architecture** — SOLID principles, dependency injection, service patterns
- **Async Processing** — Background job handling for long-running video generation
- **Modern UI/UX** — Responsive design with real-time status updates

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              ARTISAN STUDIO                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────────────────┐         ┌─────────────────────────────────────┐   │
│  │                     │         │         ASP.NET Core API            │   │
│  │   React Frontend    │  HTTP   │  ┌─────────────────────────────┐   │   │
│  │                     │ ──────► │  │     MediaController         │   │   │
│  │  • Image Generator  │         │  │  • POST /api/images/generate│   │   │
│  │  • Video Generator  │ ◄────── │  │  • POST /api/videos/generate│   │   │
│  │  • Gallery View     │  JSON   │  │  • GET  /api/videos/status  │   │   │
│  │  • Real-time Status │         │  └──────────────┬──────────────┘   │   │
│  │                     │         │                 │                   │   │
│  └─────────────────────┘         │  ┌──────────────▼──────────────┐   │   │
│                                  │  │      Service Layer          │   │   │
│                                  │  │  • IImageGenerationService  │   │   │
│                                  │  │  • IVideoGenerationService  │   │   │
│                                  │  │  • IStorageService          │   │   │
│                                  │  └──────────────┬──────────────┘   │   │
│                                  └─────────────────┼─────────────────┘   │
│                                                    │                      │
│  ┌─────────────────────────────────────────────────┼────────────────────┐ │
│  │                    External AI Services         │                    │ │
│  │  ┌─────────────┐  ┌─────────────┐  ┌──────────▼─────────┐          │ │
│  │  │   Flux 1.1  │  │  CogVideoX  │  │   Local Storage    │          │ │
│  │  │   Pro       │  │  / Runway   │  │   (Images/Videos)  │          │ │
│  │  │  (Images)   │  │  (Videos)   │  │                    │          │ │
│  │  └─────────────┘  └─────────────┘  └────────────────────┘          │ │
│  └──────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────────┘
```

## ✨ Key Features

| Feature | Description | Technology |
|---------|-------------|------------|
| 🖼️ **Image Generation** | Text-to-image with multiple styles & sizes | Flux 1.1 Pro via Replicate |
| 🎬 **Video Generation** | Text-to-video with async processing | CogVideoX / Runway Gen-3 |
| 📊 **Real-time Status** | Polling-based status updates for videos | React hooks + REST API |
| 🎨 **Modern UI** | Dark theme, responsive, accessible | React + CSS Variables |
| 💾 **Media Storage** | Persistent storage for generated content | Local filesystem / Azure Blob |
| 🔄 **Demo Mode** | Works without API keys for testing | Placeholder images |

## 🛠️ Tech Stack

### Frontend
- **React 18** — Modern hooks-based components
- **Vite** — Fast build tool and dev server
- **Lucide React** — Beautiful icon library
- **CSS Variables** — Theming and dark mode

### Backend
- **ASP.NET Core 8** — High-performance web API
- **Dependency Injection** — Built-in IoC container
- **HttpClientFactory** — Managed HTTP connections
- **Swagger/OpenAPI** — API documentation

### AI Services
- **Replicate API** — Access to Flux, CogVideoX, and more
- **Runway ML** (optional) — Gen-3 Alpha video generation
- **Luma AI** (optional) — Dream Machine integration

## 🚀 Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/)
- [Replicate API Key](https://replicate.com/account/api-tokens) (free tier available)

### Quick Start

```bash
# Clone the repository
git clone https://github.com/natalilerner-oss/ArtisanStudio.git
cd ArtisanStudio

# Start the application
./start.sh        # macOS/Linux
start.bat         # Windows
```

### Manual Setup

```bash
# Backend
cd backend
dotnet restore
dotnet run --urls "http://localhost:5000"

# Frontend (new terminal)
cd frontend
npm install
npm run dev
```

### Configuration

Set your API key in `backend/appsettings.json` or as environment variable:

```bash
export REPLICATE_API_KEY=r8_your_api_key_here
```

## 📁 Project Structure

```
ArtisanStudio/
├── 📂 frontend/                    # React application
│   ├── 📂 src/
│   │   ├── App.jsx                # Main component with state management
│   │   ├── App.css                # Styled with CSS variables
│   │   └── main.jsx               # Application entry point
│   ├── index.html
│   ├── package.json
│   └── vite.config.js
│
├── 📂 backend/                     # ASP.NET Core API
│   ├── 📂 Controllers/
│   │   └── MediaController.cs     # REST API endpoints
│   ├── 📂 Services/
│   │   ├── IServices.cs           # Service interfaces (DI)
│   │   ├── ImageGenerationService.cs
│   │   ├── VideoGenerationService.cs
│   │   └── LocalStorageService.cs
│   ├── 📂 Models/
│   │   └── MediaModels.cs         # Request/Response DTOs
│   ├── Program.cs                 # App configuration & DI setup
│   └── appsettings.json
│
├── 📂 docs/                        # Documentation
│   ├── architecture.md
│   ├── api-reference.md
│   └── interview-prep.md
│
├── 📂 tests/                       # Unit & integration tests
│   └── ArtisanStudio.Tests/
│
├── docker-compose.yml              # Container orchestration
├── Dockerfile                      # Multi-stage build
└── README.md
```

## 🔌 API Reference

### Generate Image
```http
POST /api/images/generate
Content-Type: application/json

{
  "prompt": "A majestic mountain at golden hour",
  "style": "vivid",
  "size": "1024x1024",
  "quality": "hd"
}
```

### Generate Video
```http
POST /api/videos/generate
Content-Type: application/json

{
  "prompt": "Ocean waves crashing on rocks",
  "durationSeconds": 5,
  "aspectRatio": "16:9"
}
```

### Check Video Status
```http
GET /api/videos/status/{jobId}
```

## 🐳 Docker Deployment

```bash
# Build and run with Docker Compose
docker-compose up --build

# Or build manually
docker build -t artisan-studio .
docker run -p 5000:5000 -e REPLICATE_API_KEY=your_key artisan-studio
```

## 🧪 Testing

```bash
cd tests/ArtisanStudio.Tests
dotnet test --verbosity normal
```

## 🎯 Design Decisions

### Why Flux 1.1 Pro?
- Currently one of the highest-quality image models available
- Excellent prompt adherence and photorealism
- Fast inference times (~10 seconds)

### Why Async Video Generation?
- Video generation takes 1-3 minutes
- Polling pattern prevents HTTP timeouts
- Better UX with real-time status updates

### Why ASP.NET Core?
- High performance and low latency
- Excellent dependency injection support
- Strong typing with C# catches errors early
- Easy Azure deployment

## 📈 Future Enhancements

- [ ] User authentication with OAuth
- [ ] Image-to-video generation
- [ ] Prompt history and favorites
- [ ] Batch generation
- [ ] Style presets and templates
- [ ] WebSocket for real-time updates

## 📄 License

MIT License — see [LICENSE](LICENSE) for details.

## 👤 Author

**Natali Lerner**
- GitHub: [@natalilerner-oss](https://github.com/natalilerner-oss)
- Email: natali.koifman@lernersoft.onmicrosoft.com

---

<p align="center">
  Built with ❤️ for the AI generation revolution
</p>
