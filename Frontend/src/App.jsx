import { useState, useEffect } from 'react';

function App() {
  const [forecasts, setForecasts] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    // Note: Verify your backend HTTPS port in Backend/Properties/launchSettings.json
    // It is typically 7001 or 7233. Update the URL below to match yours!
    fetch('https://localhost:7215/weatherforecast')
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

  if (loading) return <p style={{ padding: '20px' }}>Loading weather from .NET backend...</p>;
  if (error) return <p style={{ color: 'red', padding: '20px' }}>Error: {error}</p>;

  return (
    <div style={{ padding: '40px', fontFamily: 'sans-serif' }}>
      <h1>M3 Mac Full-Stack Connection</h1>
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
    </div>
  );
}

export default App;
