import { useEffect, useState } from 'react';

const API = 'http://localhost:5066';

export default function Stories() {
  const [stories, setStories] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [newText, setNewText] = useState('');
  const [editingId, setEditingId] = useState(null);
  const [editingText, setEditingText] = useState('');

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

  useEffect(() => { fetchStories(); }, []);

  const createStory = async () => {
    if (!newText.trim()) return;
    const res = await fetch(`${API}/stories`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ storyInstructions: newText })
    });
    if (!res.ok) { setError('Create failed'); return; }
    setNewText('');
    await fetchStories();
  };

  const startEdit = (s) => {
    setEditingId(s.storyId);
    setEditingText(s.storyInstructions || '');
  };

  const cancelEdit = () => { setEditingId(null); setEditingText(''); };

  const saveEdit = async (id) => {
    const res = await fetch(`${API}/stories/${id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ storyInstructions: editingText })
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

  if (loading) return <p style={{ padding: 20 }}>Loading stories...</p>;
  if (error) return <p style={{ color: 'red', padding: 20 }}>Error: {error}</p>;

  return (
    <div style={{ padding: 20 }}>
      <h2>Stories (CRUD)</h2>

      <div style={{ marginBottom: 16 }}>
        <textarea rows={3} value={newText} onChange={e => setNewText(e.target.value)} placeholder="New story instructions" style={{ width: '100%' }} />
        <button onClick={createStory} style={{ marginTop: 8 }}>Create</button>
      </div>

      <table border="1" cellPadding="8" style={{ borderCollapse: 'collapse', width: '100%' }}>
        <thead>
          <tr>
            <th style={{ width: 80 }}>ID</th>
            <th>Instructions</th>
            <th style={{ width: 220 }}>Actions</th>
          </tr>
        </thead>
        <tbody>
          {stories.map(s => (
            <tr key={s.storyId}>
              <td style={{ textAlign: 'center' }}>{s.storyId}</td>
              <td>
                {editingId === s.storyId ? (
                  <textarea rows={3} value={editingText} onChange={e => setEditingText(e.target.value)} style={{ width: '100%' }} />
                ) : (
                  <div style={{ whiteSpace: 'pre-wrap' }}>{s.storyInstructions}</div>
                )}
              </td>
              <td>
                {editingId === s.storyId ? (
                  <>
                    <button onClick={() => saveEdit(s.storyId)}>Save</button>
                    <button onClick={cancelEdit} style={{ marginLeft: 8 }}>Cancel</button>
                  </>
                ) : (
                  <>
                    <button onClick={() => startEdit(s)}>Edit</button>
                    <button onClick={() => deleteStory(s.storyId)} style={{ marginLeft: 8 }}>Delete</button>
                  </>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
