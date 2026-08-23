import { useEffect, useState } from 'react';

const API = 'http://localhost:5066';

export default function Stories() {
  const [stories, setStories] = useState([]);
  const [genres, setGenres] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [generating, setGenerating] = useState(null);
  const [newText, setNewText] = useState('');
  const [newPrompt, setNewPrompt] = useState('');
  const [newGenre, setNewGenre] = useState('');
  const [editingId, setEditingId] = useState(null);
  const [editingText, setEditingText] = useState('');
  const [editingPrompt, setEditingPrompt] = useState('');
  const [editingGenre, setEditingGenre] = useState(null);

  const fetchGenres = async () => {
    try {
      const res = await fetch(`${API}/genres`);
      if (!res.ok) throw new Error('Failed to load genres');
      const data = await res.json();
      setGenres(data);
      if (!newGenre && data.length) setNewGenre(data[0].id);
    } catch (err) {
      setError(err.message || String(err));
    }
  };

  const fetchStories = async () => {
    try {
      setLoading(true);
      const res = await fetch(`${API}/stories`);
      if (!res.ok) throw new Error('Failed to load stories');
      const data = await res.json();
      setStories(data);
    } catch (err) {
      setError(err.message || String(err));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { fetchGenres(); fetchStories(); }, []);

  const createStory = async () => {
    if (!newText.trim()) return;
    const payload = { storyInstructions: newText, storyPrompt: newPrompt, genreId: newGenre || null };
    const res = await fetch(`${API}/stories`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    });
    if (!res.ok) { setError('Create failed'); return; }
    setNewText('');
    setNewPrompt('');
    await fetchStories();
  };

  const startEdit = (s) => {
    setEditingId(s.storyId);
    setEditingText(s.storyInstructions || '');
    setEditingPrompt(s.storyPrompt || '');
    setEditingGenre(s.genreId || '');
  };

  const cancelEdit = () => { setEditingId(null); setEditingText(''); setEditingPrompt(''); setEditingGenre(null); };

  const saveEdit = async (id) => {
    const payload = { storyInstructions: editingText, storyPrompt: editingPrompt, genreId: editingGenre || null };
    const res = await fetch(`${API}/stories/${id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    });
    if (!res.ok) { setError('Update failed'); return; }
    cancelEdit();
    await fetchStories();
  };

  const deleteStory = async (id) => {
    if (!confirm('Delete this story?')) return;
    const res = await fetch(`${API}/stories/${id}`, { method: 'DELETE' });
    if (!res.ok) { setError('Delete failed'); return; }
    await fetchStories();
  };

  const generateStory = async (id) => {
    setGenerating(id);
    try {
      const res = await fetch(`${API}/stories/${id}/generate`, { method: 'POST' });
      if (!res.ok) {
        const errorData = await res.json();
        setError(`Generation failed: ${errorData.detail || 'Unknown error'}`);
        return;
      }
      await fetchStories();
    } catch (err) {
      setError(`Error: ${err.message}`);
    } finally {
      setGenerating(null);
    }
  };

  if (loading) return <p style={{ padding: 20, color: '#000000' }}>Loading stories...</p>;
  if (error) return <p style={{ color: '#E64A00', padding: 20, fontWeight: 'bold' }}>Error: {error}</p>;

  return (
    <div style={{ padding: 20, backgroundColor: '#ffffff' }}>
      <h2 style={{ color: '#000000', marginBottom: 24 }}>Create & Manage Stories</h2>

      <div style={{ 
        marginBottom: 24, 
        padding: 20, 
        backgroundColor: '#f9f9f9', 
        borderRadius: '8px', 
        border: '1px solid #e0e0e0' 
      }}>
        <div style={{ marginBottom: 12 }}>
          <label style={{ display: 'block', marginBottom: 8, fontWeight: 600, color: '#000000' }}>Instructions:</label>
          <textarea 
            rows={3} 
            value={newText} 
            onChange={e => setNewText(e.target.value)} 
            placeholder="Describe the story you want to create" 
            style={{ 
              width: '100%',
              boxSizing: 'border-box',
              border: '2px solid #FF5200',
              borderRadius: '4px',
              padding: '10px',
              fontFamily: 'sans-serif',
              fontSize: '14px',
              color: '#000000'
            }} 
          />
        </div>
        <div style={{ marginBottom: 12 }}>
          <label style={{ display: 'block', marginBottom: 8, fontWeight: 600, color: '#000000' }}>Prompt:</label>
          <textarea 
            rows={2} 
            value={newPrompt} 
            onChange={e => setNewPrompt(e.target.value)} 
            placeholder="The main prompt or idea for the story" 
            style={{ 
              width: '100%',
              boxSizing: 'border-box',
              border: '2px solid #FF5200',
              borderRadius: '4px',
              padding: '10px',
              fontFamily: 'sans-serif',
              fontSize: '14px',
              color: '#000000'
            }} 
          />
        </div>
        <div style={{ display: 'flex', gap: '12px', alignItems: 'flex-end' }}>
          <div style={{ flex: 1 }}>
            <label style={{ display: 'block', marginBottom: 8, fontWeight: 600, color: '#000000' }}>Genre:</label>
            <select 
              value={newGenre} 
              onChange={e => setNewGenre(e.target.value)}
              style={{
                width: '100%',
                boxSizing: 'border-box',
                border: '2px solid #FF5200',
                borderRadius: '4px',
                padding: '8px',
                fontFamily: 'sans-serif',
                fontSize: '14px',
                color: '#000000',
                backgroundColor: '#ffffff'
              }}
            >
              <option value="">-- Select Genre --</option>
              {genres.map(g => (
                <option key={g.id} value={g.id}>{g.name}</option>
              ))}
            </select>
          </div>
          <button 
            onClick={createStory}
            style={{
              backgroundColor: '#FF5200',
              color: '#ffffff',
              border: 'none',
              padding: '10px 24px',
              borderRadius: '4px',
              cursor: 'pointer',
              fontWeight: 600,
              fontSize: '14px',
              transition: 'background-color 0.3s'
            }}
            onMouseEnter={(e) => e.target.style.backgroundColor = '#E64A00'}
            onMouseLeave={(e) => e.target.style.backgroundColor = '#FF5200'}
          >
            Create Story
          </button>
        </div>
      </div>

      <div style={{ overflowX: 'auto' }}>
        <table border="1" cellPadding="8" style={{ 
          borderCollapse: 'collapse', 
          width: '100%',
          border: '1px solid #e0e0e0',
          borderRadius: '4px'
        }}>
          <thead>
            <tr style={{ backgroundColor: '#FF5200', color: '#ffffff' }}>
              <th style={{ width: 60, textAlign: 'left', fontWeight: 600, color: '#ffffff' }}>ID</th>
              <th style={{ textAlign: 'left', fontWeight: 600, color: '#ffffff' }}>Instructions</th>
              <th style={{ textAlign: 'left', fontWeight: 600, color: '#ffffff' }}>Prompt</th>
              <th style={{ textAlign: 'left', fontWeight: 600, color: '#ffffff' }}>Generated Story</th>
              <th style={{ textAlign: 'left', fontWeight: 600, color: '#ffffff' }}>Genre</th>
              <th style={{ width: 280, textAlign: 'left', fontWeight: 600, color: '#ffffff' }}>Actions</th>
            </tr>
          </thead>
        <tbody>
          {stories.map(s => (
            <tr key={s.storyId} style={{ borderBottom: '1px solid #e0e0e0', backgroundColor: s.storyId % 2 === 0 ? '#f9f9f9' : '#ffffff' }}>
              <td style={{ textAlign: 'center', color: '#000000', fontWeight: 600 }}>{s.storyId}</td>
              <td style={{ color: '#000000' }}>
                {editingId === s.storyId ? (
                  <textarea 
                    rows={3} 
                    value={editingText} 
                    onChange={e => setEditingText(e.target.value)} 
                    style={{ 
                      width: '100%',
                      boxSizing: 'border-box',
                      border: '2px solid #FF5200',
                      borderRadius: '4px',
                      padding: '8px',
                      fontFamily: 'sans-serif',
                      color: '#000000'
                    }} 
                  />
                ) : (
                  <div style={{ whiteSpace: 'pre-wrap', fontSize: '0.95em' }}>{s.storyInstructions}</div>
                )}
              </td>
              <td style={{ color: '#000000' }}>
                {editingId === s.storyId ? (
                  <textarea 
                    rows={2} 
                    value={editingPrompt} 
                    onChange={e => setEditingPrompt(e.target.value)} 
                    style={{ 
                      width: '100%',
                      boxSizing: 'border-box',
                      border: '2px solid #FF5200',
                      borderRadius: '4px',
                      padding: '8px',
                      fontFamily: 'sans-serif',
                      color: '#000000'
                    }} 
                  />
                ) : (
                  <div style={{ whiteSpace: 'pre-wrap', fontSize: '0.95em' }}>{s.storyPrompt}</div>
                )}
              </td>
              <td style={{ color: '#666666' }}>
                <div style={{ whiteSpace: 'pre-wrap', fontSize: '0.9em', fontStyle: 'italic' }}>
                  {s.generatedStory ? s.generatedStory.substring(0, 80) + '...' : '(none)'}
                </div>
              </td>
              <td style={{ color: '#000000' }}>
                {editingId === s.storyId ? (
                  <select 
                    value={editingGenre || ''} 
                    onChange={e => setEditingGenre(e.target.value)}
                    style={{
                      border: '2px solid #FF5200',
                      borderRadius: '4px',
                      padding: '6px',
                      fontFamily: 'sans-serif',
                      color: '#000000',
                      backgroundColor: '#ffffff'
                    }}
                  >
                    <option value="">-- none --</option>
                    {genres.map(g => (
                      <option key={g.id} value={g.id}>{g.name}</option>
                    ))}
                  </select>
                ) : (
                  <div style={{ fontWeight: 500 }}>{s.genreName || '-'}</div>
                )}
              </td>
              <td>
                {editingId === s.storyId ? (
                  <>
                    <button 
                      onClick={() => saveEdit(s.storyId)}
                      style={{
                        backgroundColor: '#FF5200',
                        color: '#ffffff',
                        border: 'none',
                        padding: '6px 12px',
                        borderRadius: '4px',
                        cursor: 'pointer',
                        fontWeight: 600,
                        fontSize: '12px',
                        marginRight: '4px'
                      }}
                    >
                      Save
                    </button>
                    <button 
                      onClick={cancelEdit}
                      style={{
                        backgroundColor: '#cccccc',
                        color: '#000000',
                        border: 'none',
                        padding: '6px 12px',
                        borderRadius: '4px',
                        cursor: 'pointer',
                        fontWeight: 600,
                        fontSize: '12px'
                      }}
                    >
                      Cancel
                    </button>
                  </>
                ) : (
                  <>
                    <button 
                      onClick={() => startEdit(s)}
                      style={{
                        backgroundColor: '#FF5200',
                        color: '#ffffff',
                        border: 'none',
                        padding: '6px 12px',
                        borderRadius: '4px',
                        cursor: 'pointer',
                        fontWeight: 600,
                        fontSize: '12px',
                        marginRight: '4px'
                      }}
                    >
                      Edit
                    </button>
                    <button 
                      onClick={() => generateStory(s.storyId)} 
                      disabled={generating === s.storyId}
                      style={{
                        backgroundColor: generating === s.storyId ? '#cccccc' : '#FF5200',
                        color: '#ffffff',
                        border: 'none',
                        padding: '6px 12px',
                        borderRadius: '4px',
                        cursor: generating === s.storyId ? 'not-allowed' : 'pointer',
                        fontWeight: 600,
                        fontSize: '12px',
                        marginRight: '4px'
                      }}
                    >
                      {generating === s.storyId ? 'Generating...' : 'Generate'}
                    </button>
                    <button 
                      onClick={() => deleteStory(s.storyId)}
                      style={{
                        backgroundColor: '#E64A00',
                        color: '#ffffff',
                        border: 'none',
                        padding: '6px 12px',
                        borderRadius: '4px',
                        cursor: 'pointer',
                        fontWeight: 600,
                        fontSize: '12px'
                      }}
                    >
                      Delete
                    </button>
                  </>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      </div>
    </div>
  );
}
