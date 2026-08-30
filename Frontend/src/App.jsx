import { useState } from 'react';
import { AIProviderProvider, useAIProvider } from './AIProviderContext';
import KnowledgeBase from './KnowledgeBase';
import OfficeStoryView from './OfficeStoryView';
import { QuotaProvider } from './QuotaContext';
import Stories from './Stories';

const NAV_ITEMS = [
  { key: 'stories', label: 'Stories' },
  { key: 'office-view', label: 'Office View' },
  { key: 'knowledge-base', label: 'Knowledge Base' },
];

const styles = {
  app: { minHeight: '100vh', backgroundColor: '#ffffff' },
  header: {
    backgroundColor: '#FF5200',
    color: '#ffffff',
    padding: '16px 20px',
    boxShadow: '0 2px 4px rgba(0, 0, 0, 0.1)',
    marginBottom: '20px',
  },
  headerInner: {
    maxWidth: '1200px',
    margin: '0 auto',
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  title: { margin: '0', fontSize: '28px', fontWeight: '700', color: '#ffffff' },
  nav: { display: 'flex', gap: 8 },
  main: { padding: '20px', maxWidth: '1200px', margin: '0 auto', fontFamily: 'sans-serif' },
};

function App() {
  const [page, setPage] = useState('stories');

  return (
    <AIProviderProvider>
      <div style={styles.app}>
      <header style={styles.header}>
        <div style={styles.headerInner}>
          <h1 style={styles.title}>StoryGen</h1>
          <nav aria-label="Main navigation" style={styles.nav}>
            {NAV_ITEMS.map((item) => {
              const isActive = page === item.key;

              return (
                <button
                  key={item.key}
                  type="button"
                  onClick={() => setPage(item.key)}
                  style={{
                    backgroundColor: isActive ? '#ffffff' : 'rgba(255, 255, 255, 0.2)',
                    color: isActive ? '#FF5200' : '#ffffff',
                  }}
                >
                  {item.label}
                </button>
              );
            })}
          </nav>
          <ProviderSelector />
        </div>
      </header>

      <QuotaProvider>
        <main style={styles.main}>
          {page === 'stories' && <Stories />}
          {page === 'office-view' && <OfficeStoryView />}
          {page === 'knowledge-base' && <KnowledgeBase />}
        </main>
      </QuotaProvider>
      </div>
    </AIProviderProvider>
  );
}

function ProviderSelector() {
  const { provider, setProvider } = useAIProvider();

  return (
    <label style={{ display: 'flex', alignItems: 'center', gap: 8, color: '#ffffff', fontWeight: 600 }}>
      AI provider
      <select
        aria-label="AI provider"
        value={provider}
        onChange={(event) => setProvider(event.target.value)}
        style={{ padding: '6px 8px', borderRadius: 4, border: 'none', color: '#111827' }}
      >
        <option value="gemini">Gemini</option>
        <option value="cohere">Cohere</option>
        <option value="claude">Claude</option>
      </select>
    </label>
  );
}

export default App;
