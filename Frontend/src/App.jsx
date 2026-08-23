import Stories from './Stories';

function App() {
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
        </div>
      </header>

      <main style={{ padding: '20px', maxWidth: '1200px', margin: '0 auto', fontFamily: 'sans-serif' }}>
        <Stories />
      </main>
    </div>
  );
}

export default App;
