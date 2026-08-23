import { useState } from 'react';
import OfficeStoryView from './OfficeStoryView';
import Stories from './Stories';

function App() {
  const [page, setPage] = useState('stories');

  return (
    <div style={{ minHeight: '100vh', backgroundColor: '#ffffff' }}>
      <header style={{
        backgroundColor: '#FF5200',
        color: '#ffffff',
        padding: '16px 20px',
        boxShadow: '0 2px 4px rgba(0, 0, 0, 0.1)',
        marginBottom: '20px'
      }}>
        <div style={{ maxWidth: '1200px', margin: '0 auto', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <h1 style={{ margin: '0', fontSize: '28px', fontWeight: '700', color: '#ffffff' }}>StoryGen</h1>
          <nav aria-label="Main navigation" style={{ display: 'flex', gap: 8 }}>
            <button
              type="button"
              onClick={() => setPage('stories')}
              style={{
                backgroundColor: page === 'stories' ? '#ffffff' : 'rgba(255, 255, 255, 0.2)',
                color: page === 'stories' ? '#FF5200' : '#ffffff',
              }}
            >
              Stories
            </button>
            <button
              type="button"
              onClick={() => setPage('office-view')}
              style={{
                backgroundColor: page === 'office-view' ? '#ffffff' : 'rgba(255, 255, 255, 0.2)',
                color: page === 'office-view' ? '#FF5200' : '#ffffff',
              }}
            >
              Office View
            </button>
          </nav>
        </div>
      </header>

      <main style={{ padding: '20px', maxWidth: '1200px', margin: '0 auto', fontFamily: 'sans-serif' }}>
        {page === 'stories' ? <Stories /> : <OfficeStoryView />}
      </main>
    </div>
  );
}

export default App;
