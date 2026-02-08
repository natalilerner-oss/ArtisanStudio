import React, { useEffect, useRef, useState, useCallback } from 'react';
import {
  BarChart, Bar, LineChart, Line, PieChart, Pie, Cell, AreaChart, Area,
  XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Legend
} from 'recharts';
import {
  ChevronLeft, ChevronRight, Download, Copy, FileText, Edit3,
  Maximize2, Minimize2, MessageSquare
} from 'lucide-react';

const CHART_COLORS = ['#8B5CF6', '#06B6D4', '#10B981', '#F59E0B', '#EF4444', '#EC4899', '#6366F1'];

const STYLE_THEMES = {
  corporate: { bg: '#1A1A2E', accent: '#8B5CF6', text: '#FFFFFF', secondary: '#A1A1AA', titleBg: 'linear-gradient(135deg, #1A1A2E 0%, #2D1B69 100%)' },
  modern: { bg: '#0F172A', accent: '#06B6D4', text: '#FFFFFF', secondary: '#94A3B8', titleBg: 'linear-gradient(135deg, #0F172A 0%, #164E63 100%)' },
  minimal: { bg: '#1E1E2E', accent: '#A78BFA', text: '#FFFFFF', secondary: '#9CA3AF', titleBg: 'linear-gradient(135deg, #1E1E2E 0%, #312E81 100%)' },
  creative: { bg: '#1A1625', accent: '#F59E0B', text: '#FFFFFF', secondary: '#B8B0C8', titleBg: 'linear-gradient(135deg, #1A1625 0%, #4A1942 100%)' },
  dark: { bg: '#0A0A0F', accent: '#8B5CF6', text: '#E5E5E5', secondary: '#71717A', titleBg: 'linear-gradient(135deg, #0A0A0F 0%, #1A1A2E 100%)' }
};

function SlideChart({ chart, theme }) {
  if (!chart?.data?.labels?.length) return null;

  const data = chart.data.labels.map((label, i) => ({
    name: label,
    value: chart.data.values[i] || 0
  }));

  const chartType = chart.type?.toLowerCase() || 'bar';

  return (
    <div className="slide-chart">
      <ResponsiveContainer width="100%" height={200}>
        {chartType === 'pie' ? (
          <PieChart>
            <Pie data={data} dataKey="value" nameKey="name" cx="50%" cy="50%" outerRadius={80} label={({ name, percent }) => `${name} ${(percent * 100).toFixed(0)}%`}>
              {data.map((_, i) => (<Cell key={i} fill={CHART_COLORS[i % CHART_COLORS.length]} />))}
            </Pie>
            <Tooltip contentStyle={{ background: '#1A1A2E', border: '1px solid #2D2D44', color: '#fff' }} />
          </PieChart>
        ) : chartType === 'line' ? (
          <LineChart data={data}>
            <CartesianGrid strokeDasharray="3 3" stroke="#2D2D44" />
            <XAxis dataKey="name" stroke="#A1A1AA" fontSize={12} />
            <YAxis stroke="#A1A1AA" fontSize={12} />
            <Tooltip contentStyle={{ background: '#1A1A2E', border: '1px solid #2D2D44', color: '#fff' }} />
            <Line type="monotone" dataKey="value" stroke={theme.accent} strokeWidth={2} dot={{ fill: theme.accent }} />
          </LineChart>
        ) : chartType === 'area' ? (
          <AreaChart data={data}>
            <CartesianGrid strokeDasharray="3 3" stroke="#2D2D44" />
            <XAxis dataKey="name" stroke="#A1A1AA" fontSize={12} />
            <YAxis stroke="#A1A1AA" fontSize={12} />
            <Tooltip contentStyle={{ background: '#1A1A2E', border: '1px solid #2D2D44', color: '#fff' }} />
            <Area type="monotone" dataKey="value" stroke={theme.accent} fill={theme.accent + '40'} />
          </AreaChart>
        ) : (
          <BarChart data={data}>
            <CartesianGrid strokeDasharray="3 3" stroke="#2D2D44" />
            <XAxis dataKey="name" stroke="#A1A1AA" fontSize={12} />
            <YAxis stroke="#A1A1AA" fontSize={12} />
            <Tooltip contentStyle={{ background: '#1A1A2E', border: '1px solid #2D2D44', color: '#fff' }} />
            <Bar dataKey="value" radius={[4, 4, 0, 0]}>
              {data.map((_, i) => (<Cell key={i} fill={CHART_COLORS[i % CHART_COLORS.length]} />))}
            </Bar>
          </BarChart>
        )}
      </ResponsiveContainer>
    </div>
  );
}

function SlideDiagram({ diagram }) {
  const containerRef = useRef(null);
  const [rendered, setRendered] = useState(false);

  useEffect(() => {
    if (!diagram?.mermaidCode || !containerRef.current) return;

    let cancelled = false;

    const renderDiagram = async () => {
      try {
        const mermaid = (await import('mermaid')).default;
        mermaid.initialize({
          startOnLoad: false,
          theme: 'dark',
          themeVariables: {
            primaryColor: '#8B5CF6',
            primaryTextColor: '#FFFFFF',
            primaryBorderColor: '#6D28D9',
            lineColor: '#A78BFA',
            secondaryColor: '#1A1A2E',
            tertiaryColor: '#252540',
            background: '#1A1A2E',
            mainBkg: '#252540',
            nodeBorder: '#6D28D9',
            clusterBkg: '#1A1A2E',
            titleColor: '#FFFFFF',
            edgeLabelBackground: '#1A1A2E'
          },
          flowchart: { htmlLabels: true, curve: 'basis' },
          securityLevel: 'loose'
        });

        const id = `mermaid-${Math.random().toString(36).substr(2, 9)}`;
        const { svg } = await mermaid.render(id, diagram.mermaidCode);

        if (!cancelled && containerRef.current) {
          containerRef.current.innerHTML = svg;
          setRendered(true);
        }
      } catch (err) {
        console.warn('Mermaid render failed:', err);
        if (!cancelled && containerRef.current) {
          containerRef.current.innerHTML = `<pre style="color: #A1A1AA; font-size: 12px; white-space: pre-wrap;">${diagram.mermaidCode}</pre>`;
        }
      }
    };

    renderDiagram();
    return () => { cancelled = true; };
  }, [diagram?.mermaidCode]);

  return <div className="slide-diagram" ref={containerRef} />;
}

function SlideRenderer({ slide, theme, aspectRatio }) {
  const isWide = aspectRatio === '16:9';
  const t = theme || STYLE_THEMES.corporate;

  const slideStyle = {
    background: slide.type === 'title' || slide.type === 'closing' ? t.titleBg : t.bg,
    aspectRatio: isWide ? '16/9' : '4/3',
    color: t.text,
    position: 'relative',
    overflow: 'hidden',
    borderRadius: '0.75rem',
    padding: '2rem 2.5rem',
    display: 'flex',
    flexDirection: 'column'
  };

  if (slide.type === 'title' || slide.type === 'closing') {
    return (
      <div className="slide-render" style={slideStyle}>
        <div className="slide-center-content">
          <h1 className="slide-title-main" style={{ color: t.accent }}>{slide.title}</h1>
          {slide.subtitle && <p className="slide-subtitle-main">{slide.subtitle}</p>}
          {slide.bullets?.length > 0 && (
            <div className="slide-contact-info">
              {slide.bullets.map((b, i) => <span key={i}>{b}</span>)}
            </div>
          )}
        </div>
        <div className="slide-number" style={{ color: t.secondary }}>{slide.slideNumber}</div>
      </div>
    );
  }

  if (slide.type === 'stats') {
    return (
      <div className="slide-render" style={slideStyle}>
        <h2 className="slide-title" style={{ color: t.accent }}>{slide.title}</h2>
        <div className="slide-stats-grid">
          {slide.bullets?.map((stat, i) => {
            const parts = stat.split(/(?<=\d)\s+|:\s*/);
            return (
              <div key={i} className="stat-card" style={{ borderColor: CHART_COLORS[i % CHART_COLORS.length] }}>
                <span className="stat-value" style={{ color: CHART_COLORS[i % CHART_COLORS.length] }}>{parts[0]}</span>
                {parts[1] && <span className="stat-label" style={{ color: t.secondary }}>{parts[1]}</span>}
              </div>
            );
          })}
        </div>
        <div className="slide-number" style={{ color: t.secondary }}>{slide.slideNumber}</div>
      </div>
    );
  }

  if (slide.type === 'comparison') {
    const half = Math.ceil((slide.bullets?.length || 0) / 2);
    const leftItems = slide.bullets?.slice(0, half) || [];
    const rightItems = slide.bullets?.slice(half) || [];

    return (
      <div className="slide-render" style={slideStyle}>
        <h2 className="slide-title" style={{ color: t.accent }}>{slide.title}</h2>
        <div className="slide-comparison">
          <div className="comparison-col">
            {leftItems.map((item, i) => (
              <div key={i} className="comparison-item">{item}</div>
            ))}
          </div>
          <div className="comparison-divider" style={{ background: t.accent }} />
          <div className="comparison-col">
            {rightItems.map((item, i) => (
              <div key={i} className="comparison-item">{item}</div>
            ))}
          </div>
        </div>
        <div className="slide-number" style={{ color: t.secondary }}>{slide.slideNumber}</div>
      </div>
    );
  }

  const hasChart = slide.chart && slide.type === 'content_with_chart';
  const hasDiagram = slide.diagram && (slide.type === 'diagram' || slide.type === 'timeline');
  const isSplit = slide.layout === 'split_left' || slide.layout === 'split_right';

  return (
    <div className="slide-render" style={slideStyle}>
      <h2 className="slide-title" style={{ color: t.accent }}>{slide.title}</h2>
      <div className={`slide-body ${isSplit && (hasChart || hasDiagram) ? 'slide-split' : ''}`}>
        <div className="slide-text-content">
          {slide.bodyText && <p className="slide-body-text" style={{ color: t.secondary }}>{slide.bodyText}</p>}
          {slide.bullets?.length > 0 && (
            <ul className="slide-bullets">
              {slide.bullets.map((b, i) => (
                <li key={i} style={{ color: t.text }}>
                  <span className="bullet-dot" style={{ background: t.accent }} />
                  {b}
                </li>
              ))}
            </ul>
          )}
        </div>
        {hasChart && <SlideChart chart={slide.chart} theme={t} />}
        {hasDiagram && <SlideDiagram diagram={slide.diagram} />}
      </div>
      <div className="slide-number" style={{ color: t.secondary }}>{slide.slideNumber}</div>
    </div>
  );
}

export default function SlidePreview({ presentation, onDownload }) {
  const [currentSlide, setCurrentSlide] = useState(0);
  const [showNotes, setShowNotes] = useState(false);

  const slides = presentation?.slides || [];
  const theme = STYLE_THEMES[presentation?.metadata?.style] || STYLE_THEMES.corporate;
  const aspectRatio = presentation?.metadata?.aspectRatio || '16:9';

  const goNext = useCallback(() => {
    if (currentSlide < slides.length - 1) setCurrentSlide(c => c + 1);
  }, [currentSlide, slides.length]);

  const goPrev = useCallback(() => {
    if (currentSlide > 0) setCurrentSlide(c => c - 1);
  }, [currentSlide]);

  useEffect(() => {
    const handler = (e) => {
      if (e.key === 'ArrowRight' || e.key === 'ArrowDown') { e.preventDefault(); goNext(); }
      if (e.key === 'ArrowLeft' || e.key === 'ArrowUp') { e.preventDefault(); goPrev(); }
    };
    window.addEventListener('keydown', handler);
    return () => window.removeEventListener('keydown', handler);
  }, [goNext, goPrev]);

  if (!slides.length) return null;

  const slide = slides[currentSlide];

  const handleCopyAll = () => {
    const text = slides.map(s =>
      `--- Slide ${s.slideNumber}: ${s.title} ---\n` +
      (s.subtitle ? `${s.subtitle}\n` : '') +
      (s.bullets?.length ? s.bullets.map(b => `  - ${b}`).join('\n') + '\n' : '') +
      (s.bodyText ? `${s.bodyText}\n` : '') +
      (s.speakerNotes ? `[Notes: ${s.speakerNotes}]\n` : '')
    ).join('\n');
    navigator.clipboard.writeText(text);
  };

  return (
    <div className="slide-preview-container">
      <div className="slide-preview-header">
        <h3>{presentation.title}</h3>
        <span className="slide-counter">{currentSlide + 1} / {slides.length}</span>
      </div>

      <div className="slide-preview-main">
        <button className="slide-nav-btn prev" onClick={goPrev} disabled={currentSlide === 0}>
          <ChevronLeft size={24} />
        </button>

        <div className="slide-viewport">
          <SlideRenderer slide={slide} theme={theme} aspectRatio={aspectRatio} />
        </div>

        <button className="slide-nav-btn next" onClick={goNext} disabled={currentSlide === slides.length - 1}>
          <ChevronRight size={24} />
        </button>
      </div>

      {showNotes && slide.speakerNotes && (
        <div className="speaker-notes-panel">
          <div className="notes-header">
            <MessageSquare size={16} />
            <span>Speaker Notes</span>
          </div>
          <p>{slide.speakerNotes}</p>
        </div>
      )}

      <div className="slide-thumbnails">
        {slides.map((s, i) => (
          <button
            key={i}
            className={`slide-thumb ${i === currentSlide ? 'active' : ''}`}
            onClick={() => setCurrentSlide(i)}
            title={`Slide ${s.slideNumber}: ${s.title}`}
          >
            <span className="thumb-number">{s.slideNumber}</span>
            <span className="thumb-title">{s.title}</span>
          </button>
        ))}
      </div>

      <div className="slide-actions">
        {onDownload && (
          <button className="action-btn" onClick={() => onDownload('pptx')}>
            <Download size={16} /><span>Download PPTX</span>
          </button>
        )}
        <button className="action-btn secondary" onClick={handleCopyAll}>
          <Copy size={16} /><span>Copy All</span>
        </button>
        <button
          className={`action-btn secondary ${showNotes ? 'active-toggle' : ''}`}
          onClick={() => setShowNotes(!showNotes)}
        >
          <MessageSquare size={16} /><span>Notes</span>
        </button>
      </div>
    </div>
  );
}
