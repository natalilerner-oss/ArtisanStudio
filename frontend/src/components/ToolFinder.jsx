import React, { useState, useMemo } from 'react';
import { Compass, ArrowRight, RotateCcw, ExternalLink, Check, X, Star } from 'lucide-react';
import tools from '../data/tools.json';

const USE_CASES = [
  { value: 'presentation', label: 'Presentation Visuals', icon: '📊' },
  { value: 'pitch', label: 'Pitch Decks', icon: '🚀' },
  { value: 'explainer', label: 'Video Explainers', icon: '🎬' },
  { value: 'linkedin', label: 'LinkedIn Content', icon: '💼' },
  { value: 'marketing', label: 'Marketing Materials', icon: '📢' },
  { value: 'training', label: 'Training Videos', icon: '📚' }
];

const LANGUAGES = [
  { value: 'english', label: 'English Only', icon: '🇬🇧' },
  { value: 'hebrew', label: 'Hebrew Required', icon: '🇮🇱' },
  { value: 'multilingual', label: 'Multilingual', icon: '🌍' }
];

const BUDGETS = [
  { value: 'free', label: 'Free Only', icon: '🆓' },
  { value: 'freemium', label: 'Free + Paid OK', icon: '💰' },
  { value: 'any', label: 'Any Budget', icon: '💎' }
];

function scoreTools(useCase, language, budget) {
  return tools.map(tool => {
    let score = 0;

    // Use case matching
    const useCaseMap = {
      presentation: ['presentations', 'presentation visuals', 'pitch decks', 'quick designs'],
      pitch: ['pitch decks', 'business presentations', 'presentations', 'marketing'],
      explainer: ['explainer videos', 'training videos', 'video editing', 'corporate communications'],
      linkedin: ['LinkedIn content', 'social media', 'social media clips', 'content repurposing', 'social media graphics'],
      marketing: ['marketing materials', 'marketing', 'marketing videos', 'brand imagery', 'creative campaigns'],
      training: ['training videos', 'e-learning', 'corporate communications', 'multilingual content']
    };

    const relevantTags = useCaseMap[useCase] || [];
    const matchCount = tool.bestFor.filter(b =>
      relevantTags.some(r => b.toLowerCase().includes(r.toLowerCase()))
    ).length;
    score += matchCount * 3;

    // Language matching
    if (language === 'hebrew' && tool.hebrewSupport) score += 5;
    if (language === 'hebrew' && !tool.hebrewSupport) score -= 10;
    if (language === 'multilingual' && tool.hebrewSupport) score += 3;

    // Budget matching
    if (budget === 'free') {
      if (tool.pricing === 'free') score += 5;
      else if (tool.pricing === 'freemium') score += 2;
      else score -= 5;
    } else if (budget === 'freemium') {
      if (tool.pricing === 'free' || tool.pricing === 'freemium') score += 3;
    }

    // Bonus for Microsoft integration
    score += tool.microsoftIntegration ? 1 : 0;

    return { ...tool, score };
  })
  .filter(t => {
    if (language === 'hebrew' && !t.hebrewSupport) return false;
    if (budget === 'free' && t.pricing === 'paid') return false;
    return true;
  })
  .sort((a, b) => b.score - a.score);
}

export default function ToolFinder() {
  const [step, setStep] = useState(0);
  const [useCase, setUseCase] = useState('');
  const [language, setLanguage] = useState('');
  const [budget, setBudget] = useState('');

  const results = useMemo(() => {
    if (step < 3) return [];
    return scoreTools(useCase, language, budget);
  }, [step, useCase, language, budget]);

  const reset = () => {
    setStep(0);
    setUseCase('');
    setLanguage('');
    setBudget('');
  };

  const selectUseCase = (val) => { setUseCase(val); setStep(1); };
  const selectLanguage = (val) => { setLanguage(val); setStep(2); };
  const selectBudget = (val) => { setBudget(val); setStep(3); };

  return (
    <div className="finder-page">
      <div className="finder-header">
        <Compass size={32} className="finder-icon" />
        <h1>AI Tool Finder</h1>
        <p>Answer 3 questions and we'll recommend the best AI tools for your needs</p>
      </div>

      <div className="finder-progress">
        {[0, 1, 2].map(i => (
          <div key={i} className={`progress-step ${step > i ? 'done' : step === i ? 'current' : ''}`}>
            <div className="step-dot">{step > i ? <Check size={14} /> : i + 1}</div>
            <span>{['Use Case', 'Language', 'Budget'][i]}</span>
          </div>
        ))}
      </div>

      {step === 0 && (
        <div className="finder-step">
          <h2>What do you need to create?</h2>
          <div className="option-grid">
            {USE_CASES.map(uc => (
              <button key={uc.value} className={`option-card ${useCase === uc.value ? 'selected' : ''}`} onClick={() => selectUseCase(uc.value)}>
                <span className="option-icon">{uc.icon}</span>
                <span>{uc.label}</span>
              </button>
            ))}
          </div>
        </div>
      )}

      {step === 1 && (
        <div className="finder-step">
          <h2>What language support do you need?</h2>
          <div className="option-grid small">
            {LANGUAGES.map(l => (
              <button key={l.value} className={`option-card ${language === l.value ? 'selected' : ''}`} onClick={() => selectLanguage(l.value)}>
                <span className="option-icon">{l.icon}</span>
                <span>{l.label}</span>
              </button>
            ))}
          </div>
        </div>
      )}

      {step === 2 && (
        <div className="finder-step">
          <h2>What's your budget?</h2>
          <div className="option-grid small">
            {BUDGETS.map(b => (
              <button key={b.value} className={`option-card ${budget === b.value ? 'selected' : ''}`} onClick={() => selectBudget(b.value)}>
                <span className="option-icon">{b.icon}</span>
                <span>{b.label}</span>
              </button>
            ))}
          </div>
        </div>
      )}

      {step === 3 && (
        <div className="finder-results">
          <div className="results-header">
            <h2>Recommended Tools ({results.length})</h2>
            <button className="reset-btn" onClick={reset}><RotateCcw size={16} /> Start Over</button>
          </div>
          <div className="results-summary">
            <span>{USE_CASES.find(u => u.value === useCase)?.icon} {USE_CASES.find(u => u.value === useCase)?.label}</span>
            <span>{LANGUAGES.find(l => l.value === language)?.icon} {LANGUAGES.find(l => l.value === language)?.label}</span>
            <span>{BUDGETS.find(b => b.value === budget)?.icon} {BUDGETS.find(b => b.value === budget)?.label}</span>
          </div>
          <div className="results-list">
            {results.map((tool, i) => (
              <div key={tool.id} className={`result-card ${i === 0 ? 'top-pick' : ''}`}>
                {i === 0 && <div className="top-pick-badge"><Star size={14} /> Top Pick</div>}
                <div className="result-card-header">
                  <h3>{tool.name}</h3>
                  <span className={`pricing-badge pricing-${tool.pricing}`}>{tool.pricing}</span>
                </div>
                <p>{tool.description}</p>
                <div className="result-features">
                  <span className={tool.microsoftIntegration ? 'feature-yes' : 'feature-no'}>
                    {tool.microsoftIntegration ? <Check size={14} /> : <X size={14} />} Microsoft 365
                  </span>
                  <span className={tool.hebrewSupport ? 'feature-yes' : 'feature-no'}>
                    {tool.hebrewSupport ? <Check size={14} /> : <X size={14} />} Hebrew
                  </span>
                </div>
                <div className="result-tags">
                  {tool.bestFor.map((tag, j) => <span key={j} className="tool-tag">{tag}</span>)}
                </div>
                <a href={tool.url} target="_blank" rel="noopener noreferrer" className="tool-link">
                  Try {tool.name} <ExternalLink size={14} />
                </a>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
