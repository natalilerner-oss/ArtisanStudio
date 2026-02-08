import React, { useState } from 'react';
import { Video, Image, Loader2, Download, RefreshCw, Wand2, Film, Zap, Info, X, LayoutDashboard } from 'lucide-react';
import SlidePreview from './SlidePreview';

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
  const [presSettings, setPresSettings] = useState({
    template: 'business_report',
    slideCount: 10,
    style: 'corporate',
    diagramType: 'auto',
    chartStyle: 'bar',
    aspectRatio: '16:9',
    language: 'en',
    includeDiagrams: true,
    includeCharts: true,
    includeSpeakerNotes: true,
    includeAnimations: true
  });
  const [loading, setLoading] = useState(false);
  const [enhancing, setEnhancing] = useState(false);
  const [result, setResult] = useState(null);
  const [error, setError] = useState(null);
  const [videoStatus, setVideoStatus] = useState(null);
  const [enhancement, setEnhancement] = useState(null);
  const [showEnhancement, setShowEnhancement] = useState(false);
  const [presStatus, setPresStatus] = useState(null);
  const [presProgress, setPresProgress] = useState({ completed: 0, total: 0 });

  const enhancePrompt = async () => {
    if (!prompt.trim()) return;
    setEnhancing(true);
    setError(null);
    try {
      const response = await fetch(`${API_URL}/prompts/enhance`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ prompt, mediaType: activeTab === 'presentation' ? 'presentation' : activeTab })
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

  const generatePresentation = async () => {
    if (!prompt.trim()) return;
    setLoading(true);
    setError(null);
    setResult(null);
    setPresStatus('starting');
    setPresProgress({ completed: 0, total: presSettings.slideCount });

    try {
      const response = await fetch(`${API_URL}/presentations/generate`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ prompt, ...presSettings })
      });
      const data = await response.json();

      if (data.success && data.id) {
        setPresStatus('generating');
        pollPresentationStatus(data.id);
      } else if (data.success && data.presentation) {
        setResult({ type: 'presentation', data: data.presentation });
        setGallery(prev => [{ type: 'presentation', title: data.presentation.title, slideCount: data.presentation.slides?.length }, ...prev]);
        setPresStatus(null);
        setLoading(false);
      } else {
        setError(data.message || 'Failed to start presentation generation');
        setPresStatus(null);
        setLoading(false);
      }
    } catch (err) {
      setError('Connection error. Please try again.');
      setPresStatus(null);
      setLoading(false);
    }
  };

  const pollPresentationStatus = async (id) => {
    const poll = async () => {
      try {
        const response = await fetch(`${API_URL}/presentations/${id}/status`);
        const data = await response.json();

        setPresProgress({ completed: data.completedSlides || 0, total: data.totalSlides || presSettings.slideCount });

        if (data.status === 'completed' && data.presentation) {
          setResult({ type: 'presentation', data: data.presentation, id });
          setGallery(prev => [{ type: 'presentation', title: data.presentation.title, slideCount: data.presentation.slides?.length }, ...prev]);
          setPresStatus(null);
          setLoading(false);
        } else if (data.status === 'failed') {
          setError(data.message || 'Presentation generation failed');
          setPresStatus(null);
          setLoading(false);
        } else {
          setTimeout(poll, 2000);
        }
      } catch (err) {
        setError('Error checking presentation status');
        setPresStatus(null);
        setLoading(false);
      }
    };
    poll();
  };

  const handleDownloadPres = async (format) => {
    if (!result?.id) return;
    try {
      const response = await fetch(`${API_URL}/presentations/${result.id}/download?format=${format}`);
      if (response.ok) {
        const blob = await response.blob();
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `presentation.${format}`;
        a.click();
        URL.revokeObjectURL(url);
      }
    } catch (err) {
      console.error('Download error:', err);
    }
  };

  const handleGenerate = () => {
    if (activeTab === 'image') generateImage();
    else if (activeTab === 'video') generateVideo();
    else generatePresentation();
  };

  const getPlaceholder = () => {
    if (activeTab === 'image') return "A majestic mountain landscape at golden hour, with snow-capped peaks reflecting in a crystal clear lake...";
    if (activeTab === 'video') return "A butterfly emerging from a cocoon, wings slowly unfolding in warm sunlight...";
    return "Q3 financial results for board meeting with revenue charts, market analysis, and growth projections...";
  };

  const getButtonLabel = () => {
    if (loading) return 'Generating...';
    if (videoStatus === 'processing') return 'Processing video...';
    if (presStatus === 'generating') return `Generating slides (${presProgress.completed}/${presProgress.total})...`;
    if (activeTab === 'image') return 'Generate Image';
    if (activeTab === 'video') return 'Generate Video';
    return 'Generate Presentation';
  };

  const isProcessing = loading || videoStatus === 'processing' || presStatus === 'generating';

  return (
    <div className="generate-page">
      <div className={`main ${activeTab === 'presentation' && result?.type === 'presentation' ? 'main-wide' : ''}`}>
        <section className="generator-panel">
          <div className="tabs">
            <button className={`tab ${activeTab === 'image' ? 'active' : ''}`} onClick={() => setActiveTab('image')}>
              <Image size={20} /><span>Image</span>
            </button>
            <button className={`tab ${activeTab === 'video' ? 'active' : ''}`} onClick={() => setActiveTab('video')}>
              <Film size={20} /><span>Video</span>
            </button>
            <button className={`tab ${activeTab === 'presentation' ? 'active' : ''}`} onClick={() => setActiveTab('presentation')}>
              <LayoutDashboard size={20} /><span>Presentation</span>
            </button>
          </div>

          <div className="prompt-section">
            <div className="prompt-header">
              <label>{activeTab === 'presentation' ? 'Describe your presentation' : 'Describe what you want to create'}</label>
              <button className="enhance-btn" onClick={enhancePrompt} disabled={!prompt.trim() || enhancing} title="Enhance prompt with AI suggestions">
                {enhancing ? <Loader2 className="spin" size={16} /> : <Zap size={16} />}
                <span>Enhance</span>
              </button>
            </div>
            <textarea
              value={prompt}
              onChange={(e) => setPrompt(e.target.value)}
              placeholder={getPlaceholder()}
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
            ) : activeTab === 'video' ? (
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
            ) : (
              <>
                <div className="setting-group">
                  <label>Template</label>
                  <select value={presSettings.template} onChange={(e) => setPresSettings({...presSettings, template: e.target.value})}>
                    <option value="business_report">Business Report</option>
                    <option value="pitch_deck">Pitch Deck</option>
                    <option value="financial_review">Financial Review</option>
                    <option value="project_status">Project Status</option>
                    <option value="sales_proposal">Sales Proposal</option>
                    <option value="custom">Custom</option>
                  </select>
                </div>
                <div className="setting-group">
                  <label>Slides</label>
                  <select value={presSettings.slideCount} onChange={(e) => setPresSettings({...presSettings, slideCount: parseInt(e.target.value)})}>
                    <option value={5}>5 slides</option>
                    <option value={8}>8 slides</option>
                    <option value={10}>10 slides</option>
                    <option value={15}>15 slides</option>
                    <option value={20}>20 slides</option>
                  </select>
                </div>
                <div className="setting-group">
                  <label>Style</label>
                  <select value={presSettings.style} onChange={(e) => setPresSettings({...presSettings, style: e.target.value})}>
                    <option value="corporate">Corporate (Clean)</option>
                    <option value="modern">Modern (Bold)</option>
                    <option value="minimal">Minimal (Elegant)</option>
                    <option value="creative">Creative (Colorful)</option>
                    <option value="dark">Dark (Professional)</option>
                  </select>
                </div>
                <div className="setting-group">
                  <label>Diagram Type</label>
                  <select value={presSettings.diagramType} onChange={(e) => setPresSettings({...presSettings, diagramType: e.target.value})}>
                    <option value="auto">Auto-detect</option>
                    <option value="flowchart">Flowchart</option>
                    <option value="orgchart">Org Chart</option>
                    <option value="timeline">Timeline</option>
                    <option value="mindmap">Mind Map</option>
                    <option value="process">Process Flow</option>
                    <option value="swot">SWOT</option>
                  </select>
                </div>
                <div className="setting-group">
                  <label>Chart Style</label>
                  <select value={presSettings.chartStyle} onChange={(e) => setPresSettings({...presSettings, chartStyle: e.target.value})}>
                    <option value="bar">Bar</option>
                    <option value="line">Line</option>
                    <option value="pie">Pie</option>
                    <option value="area">Area</option>
                    <option value="combo">Combo</option>
                    <option value="None">None</option>
                  </select>
                </div>
                <div className="setting-group">
                  <label>Aspect Ratio</label>
                  <select value={presSettings.aspectRatio} onChange={(e) => setPresSettings({...presSettings, aspectRatio: e.target.value})}>
                    <option value="16:9">16:9 (Widescreen)</option>
                    <option value="4:3">4:3 (Standard)</option>
                  </select>
                </div>
                <div className="setting-group">
                  <label>Language</label>
                  <select value={presSettings.language} onChange={(e) => setPresSettings({...presSettings, language: e.target.value})}>
                    <option value="en">English</option>
                    <option value="he">Hebrew</option>
                    <option value="auto">Auto-detect</option>
                  </select>
                </div>
              </>
            )}
          </div>

          {activeTab === 'presentation' && (
            <div className="pres-toggles">
              <label className="toggle-switch">
                <input type="checkbox" checked={presSettings.includeDiagrams} onChange={(e) => setPresSettings({...presSettings, includeDiagrams: e.target.checked})} />
                <span className="toggle-slider" />
                <span className="toggle-label">Include diagrams</span>
              </label>
              <label className="toggle-switch">
                <input type="checkbox" checked={presSettings.includeCharts} onChange={(e) => setPresSettings({...presSettings, includeCharts: e.target.checked})} />
                <span className="toggle-slider" />
                <span className="toggle-label">Include charts with sample data</span>
              </label>
              <label className="toggle-switch">
                <input type="checkbox" checked={presSettings.includeSpeakerNotes} onChange={(e) => setPresSettings({...presSettings, includeSpeakerNotes: e.target.checked})} />
                <span className="toggle-slider" />
                <span className="toggle-label">Include speaker notes</span>
              </label>
              <label className="toggle-switch">
                <input type="checkbox" checked={presSettings.includeAnimations} onChange={(e) => setPresSettings({...presSettings, includeAnimations: e.target.checked})} />
                <span className="toggle-slider" />
                <span className="toggle-label">Add animations/transitions</span>
              </label>
            </div>
          )}

          <button className="generate-btn" onClick={handleGenerate} disabled={isProcessing || !prompt.trim()}>
            {isProcessing ? (
              <><Loader2 className="spin" size={20} /><span>{getButtonLabel()}</span></>
            ) : (
              <><Wand2 size={20} /><span>{getButtonLabel()}</span></>
            )}
          </button>

          {error && <div className="error-message">{error}</div>}

          <div className="model-info">
            {activeTab === 'image' ? (
              <p>Powered by <strong>DALL-E 3</strong> - State of the art image generation</p>
            ) : activeTab === 'video' ? (
              <p>Powered by <strong>Sora</strong> - Advanced AI video creation</p>
            ) : (
              <p>Powered by <strong>AI</strong> - Interactive Presentation Engine</p>
            )}
          </div>
        </section>

        <section className="result-panel">
          {result?.type === 'presentation' && result.data ? (
            <div className="result-content presentation-result">
              <SlidePreview
                presentation={result.data}
                onDownload={result.id ? handleDownloadPres : null}
              />
            </div>
          ) : result ? (
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
          ) : presStatus === 'generating' ? (
            <div className="processing-state">
              <Loader2 className="spin large" size={48} />
              <h3>Creating your presentation...</h3>
              <p>Generating slide {presProgress.completed} of {presProgress.total}...</p>
              <div className="progress-bar">
                <div className="progress-fill" style={{ width: presProgress.total > 0 ? `${(presProgress.completed / presProgress.total) * 100}%` : '30%', animation: presProgress.total > 0 ? 'none' : undefined }} />
              </div>
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
              <div className="empty-icon">
                {activeTab === 'image' ? <Image size={64} /> : activeTab === 'video' ? <Video size={64} /> : <LayoutDashboard size={64} />}
              </div>
              <h3>Your creation will appear here</h3>
              <p>Enter a prompt and click Generate to create {activeTab === 'image' ? 'an image' : activeTab === 'video' ? 'a video' : 'a presentation'}</p>
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
              <div key={index} className="gallery-item" onClick={() => {
                if (item.type === 'presentation') return;
                setResult({ type: item.type, data: item });
              }}>
                {item.type === 'image' ? (
                  <img src={item.url} alt={item.prompt} />
                ) : item.type === 'video' ? (
                  <video src={item.url} muted />
                ) : (
                  <div className="gallery-pres-thumb">
                    <LayoutDashboard size={32} />
                    <span>{item.title || 'Presentation'}</span>
                    <small>{item.slideCount} slides</small>
                  </div>
                )}
                <div className="gallery-overlay">
                  <span>{item.type === 'image' ? '🎨' : item.type === 'video' ? '🎬' : '📊'}</span>
                </div>
              </div>
            ))}
          </div>
        </section>
      )}
    </div>
  );
}
