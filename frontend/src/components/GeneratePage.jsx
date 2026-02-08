import React, { useState } from 'react';
import { Video, Image, Loader2, Download, RefreshCw, Wand2, Film, Zap, Info, X } from 'lucide-react';

const API_URL = import.meta.env.VITE_API_URL || '/api';

export default function GeneratePage({ gallery, setGallery }) {
  const [activeTab, setActiveTab] = useState('image');
  const [prompt, setPrompt] = useState('');
  const [imageSettings, setImageSettings] = useState({
    style: 'vivid',
    size: '1024x1024',
    quality: 'standard'
  });
  const [videoSettings, setVideoSettings] = useState({
    duration: 5,
    aspectRatio: '16:9'
  });
  const [loading, setLoading] = useState(false);
  const [enhancing, setEnhancing] = useState(false);
  const [result, setResult] = useState(null);
  const [error, setError] = useState(null);
  const [videoStatus, setVideoStatus] = useState(null);
  const [enhancement, setEnhancement] = useState(null);
  const [showEnhancement, setShowEnhancement] = useState(false);

  const enhancePrompt = async () => {
    if (!prompt.trim()) return;
    setEnhancing(true);
    setError(null);
    try {
      const response = await fetch(`${API_URL}/prompts/enhance`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ prompt, mediaType: activeTab })
      });
      const data = await response.json();
      if (data.enhancedPrompt) {
        setEnhancement(data);
        setShowEnhancement(true);
      }
    } catch (err) {
      console.error('Enhancement error:', err);
    } finally {
      setEnhancing(false);
    }
  };

  const applyEnhancement = () => {
    if (enhancement?.enhancedPrompt) {
      setPrompt(enhancement.enhancedPrompt);
      setShowEnhancement(false);
      setEnhancement(null);
    }
  };

  const generateImage = async () => {
    if (!prompt.trim()) return;
    setLoading(true);
    setError(null);
    setResult(null);
    try {
      const response = await fetch(`${API_URL}/images/generate`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ prompt, ...imageSettings })
      });
      const data = await response.json();
      if (data.success && data.images?.length > 0) {
        setResult({ type: 'image', data: data.images[0] });
        setGallery(prev => [{ type: 'image', ...data.images[0] }, ...prev]);
      } else {
        setError(data.message || 'Failed to generate image');
      }
    } catch (err) {
      setError('Connection error. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const generateVideo = async () => {
    if (!prompt.trim()) return;
    setLoading(true);
    setError(null);
    setResult(null);
    setVideoStatus('starting');
    try {
      const response = await fetch(`${API_URL}/videos/generate`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ prompt, durationSeconds: videoSettings.duration, aspectRatio: videoSettings.aspectRatio })
      });
      const data = await response.json();
      if (data.success && data.jobId) {
        setVideoStatus('processing');
        pollVideoStatus(data.jobId);
      } else if (data.success && data.video) {
        setResult({ type: 'video', data: data.video });
        setGallery(prev => [{ type: 'video', ...data.video }, ...prev]);
        setVideoStatus(null);
        setLoading(false);
      } else {
        setError(data.message || 'Failed to start video generation');
        setVideoStatus(null);
        setLoading(false);
      }
    } catch (err) {
      setError('Connection error. Please try again.');
      setVideoStatus(null);
      setLoading(false);
    }
  };

  const pollVideoStatus = async (jobId) => {
    const poll = async () => {
      try {
        const response = await fetch(`${API_URL}/videos/status/${jobId}`);
        const data = await response.json();
        if (data.status === 'completed' && data.video) {
          setResult({ type: 'video', data: data.video });
          setGallery(prev => [{ type: 'video', ...data.video }, ...prev]);
          setVideoStatus(null);
          setLoading(false);
        } else if (data.status === 'failed') {
          setError(data.message || 'Video generation failed');
          setVideoStatus(null);
          setLoading(false);
        } else {
          setTimeout(poll, 3000);
        }
      } catch (err) {
        setError('Error checking video status');
        setVideoStatus(null);
        setLoading(false);
      }
    };
    poll();
  };

  const handleGenerate = () => {
    if (activeTab === 'image') generateImage();
    else generateVideo();
  };

  return (
    <div className="generate-page">
      <div className="main">
        <section className="generator-panel">
          <div className="tabs">
            <button className={`tab ${activeTab === 'image' ? 'active' : ''}`} onClick={() => setActiveTab('image')}>
              <Image size={20} /><span>Image</span>
            </button>
            <button className={`tab ${activeTab === 'video' ? 'active' : ''}`} onClick={() => setActiveTab('video')}>
              <Film size={20} /><span>Video</span>
            </button>
          </div>

          <div className="prompt-section">
            <div className="prompt-header">
              <label>Describe what you want to create</label>
              <button className="enhance-btn" onClick={enhancePrompt} disabled={!prompt.trim() || enhancing} title="Enhance prompt with AI suggestions">
                {enhancing ? <Loader2 className="spin" size={16} /> : <Zap size={16} />}
                <span>Enhance</span>
              </button>
            </div>
            <textarea
              value={prompt}
              onChange={(e) => setPrompt(e.target.value)}
              placeholder={activeTab === 'image'
                ? "A majestic mountain landscape at golden hour, with snow-capped peaks reflecting in a crystal clear lake..."
                : "A butterfly emerging from a cocoon, wings slowly unfolding in warm sunlight..."}
              rows={4}
            />
          </div>

          {showEnhancement && enhancement && (
            <div className="enhancement-panel">
              <div className="enhancement-header">
                <Zap size={18} /><span>AI Enhancement Suggestions</span>
                <button className="close-btn" onClick={() => setShowEnhancement(false)}><X size={16} /></button>
              </div>
              <div className="enhancement-content">
                <div className="enhanced-prompt">
                  <label>Enhanced Prompt:</label>
                  <p>{enhancement.enhancedPrompt}</p>
                </div>
                {enhancement.suggestions?.length > 0 && (
                  <div className="suggestions">
                    <label>Suggestions:</label>
                    <ul>{enhancement.suggestions.map((s, i) => (<li key={i}><Info size={14} /> {s}</li>))}</ul>
                  </div>
                )}
                <button className="apply-btn" onClick={applyEnhancement}>Apply Enhanced Prompt</button>
              </div>
            </div>
          )}

          <div className="settings">
            {activeTab === 'image' ? (
              <>
                <div className="setting-group">
                  <label>Style</label>
                  <select value={imageSettings.style} onChange={(e) => setImageSettings({...imageSettings, style: e.target.value})}>
                    <option value="vivid">Vivid (Dramatic)</option>
                    <option value="natural">Natural (Realistic)</option>
                  </select>
                </div>
                <div className="setting-group">
                  <label>Size</label>
                  <select value={imageSettings.size} onChange={(e) => setImageSettings({...imageSettings, size: e.target.value})}>
                    <option value="1024x1024">Square (1024x1024)</option>
                    <option value="1792x1024">Landscape (1792x1024)</option>
                    <option value="1024x1792">Portrait (1024x1792)</option>
                  </select>
                </div>
                <div className="setting-group">
                  <label>Quality</label>
                  <select value={imageSettings.quality} onChange={(e) => setImageSettings({...imageSettings, quality: e.target.value})}>
                    <option value="standard">Standard</option>
                    <option value="hd">HD (More detail)</option>
                  </select>
                </div>
              </>
            ) : (
              <>
                <div className="setting-group">
                  <label>Duration</label>
                  <select value={videoSettings.duration} onChange={(e) => setVideoSettings({...videoSettings, duration: parseInt(e.target.value)})}>
                    <option value={5}>5 seconds</option>
                    <option value={10}>10 seconds</option>
                  </select>
                </div>
                <div className="setting-group">
                  <label>Aspect Ratio</label>
                  <select value={videoSettings.aspectRatio} onChange={(e) => setVideoSettings({...videoSettings, aspectRatio: e.target.value})}>
                    <option value="16:9">Landscape (16:9)</option>
                    <option value="9:16">Portrait (9:16)</option>
                    <option value="1:1">Square (1:1)</option>
                  </select>
                </div>
              </>
            )}
          </div>

          <button className="generate-btn" onClick={handleGenerate} disabled={loading || !prompt.trim() || videoStatus === 'processing'}>
            {loading ? (<><Loader2 className="spin" size={20} /><span>Generating...</span></>)
              : videoStatus === 'processing' ? (<><Loader2 className="spin" size={20} /><span>Processing video...</span></>)
              : (<><Wand2 size={20} /><span>Generate {activeTab === 'image' ? 'Image' : 'Video'}</span></>)}
          </button>

          {error && <div className="error-message">{error}</div>}

          <div className="model-info">
            {activeTab === 'image' ? (
              <p>Powered by <strong>DALL-E 3</strong> - State of the art image generation</p>
            ) : (
              <p>Powered by <strong>Sora</strong> - Advanced AI video creation</p>
            )}
          </div>
        </section>

        <section className="result-panel">
          {result ? (
            <div className="result-content">
              <h3>{result.type === 'image' ? 'Generated Image' : 'Generated Video'}</h3>
              {result.type === 'image' ? (
                <div className="result-image-container"><img src={result.data.url} alt={result.data.prompt} /></div>
              ) : (
                <div className="result-video-container"><video controls autoPlay loop><source src={result.data.url} type="video/mp4" /></video></div>
              )}
              <div className="result-actions">
                <a href={result.data.url} download className="action-btn"><Download size={18} /><span>Download</span></a>
                <button className="action-btn secondary" onClick={() => setResult(null)}><RefreshCw size={18} /><span>Create New</span></button>
              </div>
              {result.data.model && <p className="result-model">Created with {result.data.model}</p>}
            </div>
          ) : videoStatus === 'processing' ? (
            <div className="processing-state">
              <Loader2 className="spin large" size={48} />
              <h3>Creating your video...</h3>
              <p>This usually takes 1-3 minutes. Please wait.</p>
              <div className="progress-bar"><div className="progress-fill"></div></div>
            </div>
          ) : (
            <div className="empty-state">
              <div className="empty-icon">{activeTab === 'image' ? <Image size={64} /> : <Video size={64} />}</div>
              <h3>Your creation will appear here</h3>
              <p>Enter a prompt and click Generate to create {activeTab === 'image' ? 'an image' : 'a video'}</p>
              <p className="tip">Tip: Click "Enhance" to improve your prompt with AI suggestions</p>
            </div>
          )}
        </section>
      </div>

      {gallery.length > 0 && (
        <section className="gallery">
          <h2>Recent Creations</h2>
          <div className="gallery-grid">
            {gallery.slice(0, 8).map((item, index) => (
              <div key={index} className="gallery-item" onClick={() => setResult({ type: item.type, data: item })}>
                {item.type === 'image' ? <img src={item.url} alt={item.prompt} /> : <video src={item.url} muted />}
                <div className="gallery-overlay"><span>{item.type === 'image' ? '🎨' : '🎬'}</span></div>
              </div>
            ))}
          </div>
        </section>
      )}
    </div>
  );
}
