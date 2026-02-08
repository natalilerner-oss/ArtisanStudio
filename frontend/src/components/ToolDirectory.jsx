import React, { useState, useMemo } from 'react';
import { Search, ExternalLink, Check, X, Filter, LayoutGrid, Columns } from 'lucide-react';
import tools from '../data/tools.json';

const CATEGORIES = [
  { value: 'all', label: 'All Tools' },
  { value: 'image', label: 'Image' },
  { value: 'video', label: 'Video' },
  { value: 'presentation', label: 'Presentation' }
];

const PRICING = [
  { value: 'all', label: 'All Pricing' },
  { value: 'free', label: 'Free' },
  { value: 'freemium', label: 'Freemium' },
  { value: 'paid', label: 'Paid' }
];

function PricingBadge({ pricing }) {
  const colors = {
    free: { bg: 'rgba(16,185,129,0.15)', color: '#10B981' },
    freemium: { bg: 'rgba(139,92,246,0.15)', color: '#A78BFA' },
    paid: { bg: 'rgba(245,158,11,0.15)', color: '#F59E0B' }
  };
  const c = colors[pricing] || colors.freemium;
  return <span className="pricing-badge" style={{ background: c.bg, color: c.color }}>{pricing}</span>;
}

function CategoryIcon({ category }) {
  const icons = { image: '🎨', video: '🎬', presentation: '📊' };
  return <span className="category-icon">{icons[category] || '🔧'}</span>;
}

export default function ToolDirectory() {
  const [search, setSearch] = useState('');
  const [category, setCategory] = useState('all');
  const [pricing, setPricing] = useState('all');
  const [msOnly, setMsOnly] = useState(false);
  const [hebrewOnly, setHebrewOnly] = useState(false);
  const [compareMode, setCompareMode] = useState(false);
  const [compareList, setCompareList] = useState([]);

  const filtered = useMemo(() => {
    return tools.filter(t => {
      if (category !== 'all' && t.category !== category) return false;
      if (pricing !== 'all' && t.pricing !== pricing) return false;
      if (msOnly && !t.microsoftIntegration) return false;
      if (hebrewOnly && !t.hebrewSupport) return false;
      if (search) {
        const q = search.toLowerCase();
        return t.name.toLowerCase().includes(q)
          || t.description.toLowerCase().includes(q)
          || t.bestFor.some(b => b.toLowerCase().includes(q));
      }
      return true;
    });
  }, [search, category, pricing, msOnly, hebrewOnly]);

  const toggleCompare = (tool) => {
    setCompareList(prev =>
      prev.find(t => t.id === tool.id)
        ? prev.filter(t => t.id !== tool.id)
        : prev.length < 3 ? [...prev, tool] : prev
    );
  };

  return (
    <div className="directory-page">
      <div className="directory-header">
        <h1>AI Tool Directory</h1>
        <p>Browse and compare the best AI tools for media creation</p>
      </div>

      <div className="directory-controls">
        <div className="search-bar">
          <Search size={18} />
          <input
            type="text"
            placeholder="Search tools, features, or use cases..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>

        <div className="filters">
          <select value={category} onChange={(e) => setCategory(e.target.value)}>
            {CATEGORIES.map(c => <option key={c.value} value={c.value}>{c.label}</option>)}
          </select>
          <select value={pricing} onChange={(e) => setPricing(e.target.value)}>
            {PRICING.map(p => <option key={p.value} value={p.value}>{p.label}</option>)}
          </select>
          <button className={`filter-toggle ${msOnly ? 'active' : ''}`} onClick={() => setMsOnly(!msOnly)}>
            Microsoft 365
          </button>
          <button className={`filter-toggle ${hebrewOnly ? 'active' : ''}`} onClick={() => setHebrewOnly(!hebrewOnly)}>
            Hebrew
          </button>
          <button className={`filter-toggle compare-toggle ${compareMode ? 'active' : ''}`} onClick={() => { setCompareMode(!compareMode); setCompareList([]); }}>
            <Columns size={16} /> Compare
          </button>
        </div>
      </div>

      <div className="directory-count">{filtered.length} tool{filtered.length !== 1 ? 's' : ''} found</div>

      <div className="tools-grid">
        {filtered.map(tool => (
          <div key={tool.id} className={`tool-card ${compareList.find(t => t.id === tool.id) ? 'selected' : ''}`}>
            {compareMode && (
              <button className="compare-check" onClick={() => toggleCompare(tool)}>
                {compareList.find(t => t.id === tool.id) ? <Check size={16} /> : <span className="check-empty" />}
              </button>
            )}
            <div className="tool-card-header">
              <CategoryIcon category={tool.category} />
              <div>
                <h3>{tool.name}</h3>
                <PricingBadge pricing={tool.pricing} />
              </div>
            </div>
            <p className="tool-description">{tool.description}</p>
            <div className="tool-tags">
              {tool.bestFor.map((tag, i) => <span key={i} className="tool-tag">{tag}</span>)}
            </div>
            <div className="tool-features">
              <span className={tool.microsoftIntegration ? 'feature-yes' : 'feature-no'}>
                {tool.microsoftIntegration ? <Check size={14} /> : <X size={14} />} Microsoft 365
              </span>
              <span className={tool.hebrewSupport ? 'feature-yes' : 'feature-no'}>
                {tool.hebrewSupport ? <Check size={14} /> : <X size={14} />} Hebrew
              </span>
            </div>
            <a href={tool.url} target="_blank" rel="noopener noreferrer" className="tool-link">
              Visit <ExternalLink size={14} />
            </a>
          </div>
        ))}
      </div>

      {compareMode && compareList.length >= 2 && (
        <div className="compare-panel">
          <div className="compare-header">
            <h2>Compare Tools ({compareList.length})</h2>
            <button className="close-btn" onClick={() => { setCompareMode(false); setCompareList([]); }}><X size={18} /></button>
          </div>
          <div className="compare-table-wrapper">
            <table className="compare-table">
              <thead>
                <tr>
                  <th>Feature</th>
                  {compareList.map(t => <th key={t.id}>{t.name}</th>)}
                </tr>
              </thead>
              <tbody>
                <tr>
                  <td>Category</td>
                  {compareList.map(t => <td key={t.id}><CategoryIcon category={t.category} /> {t.category}</td>)}
                </tr>
                <tr>
                  <td>Pricing</td>
                  {compareList.map(t => <td key={t.id}><PricingBadge pricing={t.pricing} /></td>)}
                </tr>
                <tr>
                  <td>Microsoft 365</td>
                  {compareList.map(t => <td key={t.id} className={t.microsoftIntegration ? 'cell-yes' : 'cell-no'}>{t.microsoftIntegration ? 'Yes' : 'No'}</td>)}
                </tr>
                <tr>
                  <td>Hebrew Support</td>
                  {compareList.map(t => <td key={t.id} className={t.hebrewSupport ? 'cell-yes' : 'cell-no'}>{t.hebrewSupport ? 'Yes' : 'No'}</td>)}
                </tr>
                <tr>
                  <td>Best For</td>
                  {compareList.map(t => <td key={t.id}>{t.bestFor.join(', ')}</td>)}
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}
