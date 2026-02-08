import React, { useState } from 'react';
import { Sparkles, Wand2, BookOpen, Compass } from 'lucide-react';
import GeneratePage from './components/GeneratePage';
import ToolDirectory from './components/ToolDirectory';
import ToolFinder from './components/ToolFinder';
import './App.css';

const NAV_ITEMS = [
  { id: 'home', label: 'Home', icon: Sparkles },
  { id: 'generate', label: 'Generate', icon: Wand2 },
  { id: 'finder', label: 'Tool Finder', icon: Compass },
  { id: 'directory', label: 'Directory', icon: BookOpen },
];

function HomePage({ onNavigate }) {
  return (
    <div className="home-page">
      <div className="hero">
        <h1>AI Media Hub</h1>
        <p className="hero-sub">Your complete toolkit for AI-powered media creation</p>
        <div className="hero-cards">
          <div className="hero-card" onClick={() => onNavigate('generate')}>
            <Wand2 size={32} />
            <h3>Generate</h3>
            <p>Create images with DALL-E 3 and videos with Sora</p>
          </div>
          <div className="hero-card" onClick={() => onNavigate('finder')}>
            <Compass size={32} />
            <h3>Tool Finder</h3>
            <p>Get personalized AI tool recommendations</p>
          </div>
          <div className="hero-card" onClick={() => onNavigate('directory')}>
            <BookOpen size={32} />
            <h3>Directory</h3>
            <p>Browse and compare 11+ AI media tools</p>
          </div>
        </div>
      </div>
    </div>
  );
}

function App() {
  const [page, setPage] = useState('home');
  const [gallery, setGallery] = useState([]);

  return (
    <div className="app">
      <header className="header">
        <div className="header-content">
          <div className="header-top">
            <div className="logo" onClick={() => setPage('home')} style={{ cursor: 'pointer' }}>
              <Sparkles className="logo-icon" />
              <span>Artisan Studio</span>
            </div>
            <nav className="nav">
              {NAV_ITEMS.map(item => (
                <button
                  key={item.id}
                  className={`nav-btn ${page === item.id ? 'active' : ''}`}
                  onClick={() => setPage(item.id)}
                >
                  <item.icon size={16} />
                  <span>{item.label}</span>
                </button>
              ))}
            </nav>
          </div>
        </div>
      </header>

      <div className="page-content">
        {page === 'home' && <HomePage onNavigate={setPage} />}
        {page === 'generate' && <GeneratePage gallery={gallery} setGallery={setGallery} />}
        {page === 'finder' && <ToolFinder />}
        {page === 'directory' && <ToolDirectory />}
      </div>

      <footer className="footer">
        <p>Artisan Studio • AI-Powered Creative Tools • Powered by <strong>DocumentInsight.ai</strong> • Created by Natali Koifman</p>
      </footer>
    </div>
  );
}

export default App;
