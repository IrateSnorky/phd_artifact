import { useState, useEffect } from 'react';
import Stories from './Stories';

function App() {
  const [page, setPage] = useState('home');
  const [forecasts, setForecasts] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    // Backend weather endpoint (update if your backend uses another port)
    fetch('http://localhost:5066/weatherforecast')
      .then((response) => {
        if (!response.ok) {
          throw new Error('Failed to fetch data from .NET Backend');
        }
        return response.json();
      })
      .then((data) => {
        setForecasts(data);
        setLoading(false);
      })
      .catch((err) => {
        setError(err.message);
        setLoading(false);
      });
  }, []);

  return (
    <div style={{ padding: '20px', fontFamily: 'sans-serif' }}>
      <nav style={{ marginBottom: 20 }}>
        <button onClick={() => setPage('home')} style={{ marginRight: 8 }}>Home</button>
        <button onClick={() => setPage('stories')}>Stories (CRUD)</button>
      </nav>

      {page === 'home' && (
        <div>
          <h1>M3 Mac Full-Stack Connection</h1>
          {loading && <p>Loading weather from .NET backend...</p>}
          {error && <p style={{ color: 'red' }}>Error: {error}</p>}

          {!loading && !error && (
            <>
              <h3>Data fetched directly from .NET Web API:</h3>
              <table border="1" cellPadding="10" style={{ borderCollapse: 'collapse', marginTop: '20px' }}>
                <thead>
                  <tr style={{ backgroundColor: '#f2f2f2' }}>
                    <th>Date</th>
                    <th>Temp (°C)</th>
                    <th>Temp (°F)</th>
                    <th>Summary</th>
                  </tr>
                </thead>
                <tbody>
                  {forecasts.map((f, index) => (
                    <tr key={index}>
                      <td>{f.date}</td>
                      <td>{f.temperatureC}°C</td>
                      <td>{f.temperatureF}°F</td>
                      <td>{f.summary}</td>
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
  );
}

export default App;
