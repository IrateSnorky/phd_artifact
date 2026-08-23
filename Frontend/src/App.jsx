import { useState, useEffect } from 'react';
import Stories from './Stories';

function App() {
  const [page, setPage] = useState('stories');

  return (
    <div style={{ minHeight: '100vh', backgroundColor: '#ffffff' }}>
      {/* Header */}
      <header style={{
        backgroundColor: '#FF5200',
        color: '#ffffff',
        padding: '16px 20px',
        boxShadow: '0 2px 4px rgba(0, 0, 0, 0.1)',
        marginBottom: '20px'
      }}>
        <div style={{ maxWidth: '1200px', margin: '0 auto', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <h1 style={{ margin: '0', fontSize: '28px', fontWeight: '700', color: '#ffffff' }}>StoryGen</h1>
          <nav style={{ display: 'flex', gap: '12px' }}>
            <button 
              onClick={() => setPage('home')} 
              style={{ 
                backgroundColor: page === 'home' ? '#ffffff' : 'rgba(255, 255, 255, 0.2)',
                color: page === 'home' ? '#FF5200' : '#ffffff',
                border: 'none',
                padding: '8px 16px',
                borderRadius: '4px',
                cursor: 'pointer',
                fontWeight: 600,
                transition: 'all 0.3s'
              }}
            >
              Home
            </button>
            <button 
              onClick={() => setPage('stories')} 
              style={{ 
                backgroundColor: page === 'stories' ? '#ffffff' : 'rgba(255, 255, 255, 0.2)',
                color: page === 'stories' ? '#FF5200' : '#ffffff',
                border: 'none',
                padding: '8px 16px',
                borderRadius: '4px',
                cursor: 'pointer',
                fontWeight: 600,
                transition: 'all 0.3s'
              }}
            >
              Stories
            </button>
          </nav>
        </div>
      </header>

      {/* Content */}
      <div style={{ padding: '20px', maxWidth: '1200px', margin: '0 auto', fontFamily: 'sans-serif' }}>
        {page === 'home' && (
          <div>
            <h1 style={{ color: '#000000', marginTop: '20px' }}>Weather Forecast</h1>
            {loading && <p>Loading weather from .NET backend...</p>}
            {error && <p style={{ color: '#E64A00' }}>Error: {error}</p>}

            {!loading && !error && (
              <>
                <p style={{ color: '#666666', marginBottom: '20px' }}>Data fetched directly from .NET Web API:</p>
                <table border="1" cellPadding="10" style={{ borderCollapse: 'collapse', marginTop: '20px', width: '100%' }}>
                  <thead>
                    <tr style={{ backgroundColor: '#FF5200', color: '#ffffff' }}>
                      <th style={{ textAlign: 'left' }}>Date</th>
                      <th style={{ textAlign: 'left' }}>Temp (°C)</th>
                      <th style={{ textAlign: 'left' }}>Temp (°F)</th>
                      <th style={{ textAlign: 'left' }}>Summary</th>
                    </tr>
                  </thead>
                  <tbody>
                    {forecasts.map((f, index) => (
                      <tr key={index} style={{ borderBottom: '1px solid #e0e0e0' }}>
                        <td style={{ color: '#000000' }}>{f.date}</td>
                        <td style={{ color: '#000000' }}>{f.temperatureC}°C</td>
                        <td style={{ color: '#000000' }}>{f.temperatureF}°F</td>
                        <td style={{ color: '#000000' }}>{f.summary}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </>
            )}
          </div>
        )}

        {page === 'stories' && <Stories />}
      </div>
    </div>
  );
}

export default App;
