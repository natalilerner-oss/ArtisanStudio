# 🎬 Quick Demo Script

## Before the Interview
```bash
cd ArtisanStudioWeb
./start.sh   # or start.bat on Windows
```

Verify: http://localhost:3000 is working

---

## Demo Flow (5 min)

### 1️⃣ Show the UI (30 sec)
> "This is Artisan Studio, an AI media generation platform I built."

- Point to the tabs (Image/Video)
- Show the settings options
- Mention the dark theme and responsive design

### 2️⃣ Demo Prompt Enhancement (1 min)
> "Let me show you the prompt enhancement feature."

Type: `a cat`

Click **Enhance** ⚡

> "This shows my understanding of prompt engineering. The system analyzes the prompt and suggests improvements for lighting, style, and composition."

Click **Apply Enhanced Prompt**

### 3️⃣ Generate an Image (1.5 min)
Or type a better prompt:
> "A cozy coffee shop interior with morning sunlight streaming through windows"

Select: **Landscape**, **HD Quality**

Click **Generate**

While waiting (~10 sec):
> "The request goes to my .NET backend, which calls Flux 1.1 Pro - currently one of the best image models available."

Show result, mention quality.

### 4️⃣ Generate a Video (1.5 min)
Switch to **Video** tab

Type:
> "Ocean waves gently lapping on a sandy beach at sunset"

Click **Generate**

> "Video takes longer - about 1-2 minutes. This is why I implemented async processing with job tracking."

Show the processing state.

If demo mode, result is immediate.

### 5️⃣ Show Code (30 sec)
If asked or time permits:
- `backend/Services/ImageGenerationService.cs` - Service pattern
- `backend/Services/PromptEnhancementService.cs` - AI understanding
- `frontend/src/App.jsx` - React state management

---

## Key Phrases to Use

| Topic | What to Say |
|-------|-------------|
| **Architecture** | "Clean separation with dependency injection" |
| **AI Models** | "Flux 1.1 Pro for best-in-class image quality" |
| **Async** | "Job queue pattern for long-running video generation" |
| **UX** | "Real-time status updates, error handling, demo mode" |
| **Testing** | "Unit tests with mocking, demo mode for manual testing" |

---

## If Something Breaks

- **API Error**: "Demo mode is working - in production we'd have retry logic"
- **Slow Response**: "AI inference takes time - that's why async is important"
- **Any Error**: "Let me show you the error handling..." (it's a feature!)

---

## Questions to Expect

1. "How would you scale this?" → Redis, worker services, CDN
2. "Why these models?" → Flux is best quality, CogVideoX is open source
3. "How do you handle failures?" → Retry, timeout, graceful degradation

---

**Remember: You built this. Own it! 💪**
