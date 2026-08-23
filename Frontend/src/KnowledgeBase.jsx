import { useEffect, useState } from 'react';

const API = 'http://localhost:5066';

export default function KnowledgeBase() {
  const [chunks, setChunks] = useState([]);
  const [genres, setGenres] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [content, setContent] = useState('');
  const [source, setSource] = useState('');
  const [alwaysInclude, setAlwaysInclude] = useState(false);
  const [genreId, setGenreId] = useState('');
  const [saving, setSaving] = useState(false);
  const [editingId, setEditingId] = useState(null);
  const [editContent, setEditContent] = useState('');
  const [editSource, setEditSource] = useState('');
  const [editAlwaysInclude, setEditAlwaysInclude] = useState(false);
  const [editGenreId, setEditGenreId] = useState('');
  const [editSaving, setEditSaving] = useState(false);

  const fetchGenres = async () => {
    try {
      const res = await fetch(`${API}/genres`);
      if (!res.ok) throw new Error('Failed to load genres');
      setGenres(await res.json());
    } catch (err) {
      setError(err.message || String(err));
    }
  };

  const fetchChunks = async () => {
    try {
      setLoading(true);
      const res = await fetch(`${API}/knowledge`);
      if (!res.ok) throw new Error('Failed to load knowledge base');
      setChunks(await res.json());
    } catch (err) {
      setError(err.message || String(err));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { fetchGenres(); fetchChunks(); }, []);

  const addDocument = async () => {
    if (!content.trim()) return;
    setSaving(true);
    setError(null);
    try {
      const res = await fetch(`${API}/knowledge`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ content, source: source || null, alwaysInclude, genreId: genreId || null }),
      });
      if (!res.ok) {
        const errorText = await res.text();
        throw new Error(errorText || 'Failed to add document');
      }
      setContent('');
      setSource('');
      setAlwaysInclude(false);
      setGenreId('');
      await fetchChunks();
    } catch (err) {
      setError(err.message || String(err));
    } finally {
      setSaving(false);
    }
  };

  const deleteChunk = async (id) => {
    if (!confirm('Delete this reference chunk?')) return;
    const res = await fetch(`${API}/knowledge/${id}`, { method: 'DELETE' });
    if (!res.ok) { setError('Delete failed'); return; }
    await fetchChunks();
  };

  const startEdit = (chunk) => {
    setEditingId(chunk.id);
    setEditContent(chunk.content);
    setEditSource(chunk.source || '');
    setEditAlwaysInclude(chunk.alwaysInclude);
    setEditGenreId(chunk.genreId ?? '');
  };

  const cancelEdit = () => {
    setEditingId(null);
  };

  const saveEdit = async (id) => {
    if (!editContent.trim()) return;
    setEditSaving(true);
    setError(null);
    try {
      const res = await fetch(`${API}/knowledge/${id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          content: editContent,
          source: editSource || null,
          alwaysInclude: editAlwaysInclude,
          genreId: editGenreId || null,
        }),
      });
      if (!res.ok) {
        const errorText = await res.text();
        throw new Error(errorText || 'Failed to update document');
      }
      setEditingId(null);
      await fetchChunks();
    } catch (err) {
      setError(err.message || String(err));
    } finally {
      setEditSaving(false);
    }
  };


  return (
    <section style={{ padding: 20, backgroundColor: '#ffffff' }}>
      <h2 style={{ color: '#000000', marginBottom: 8 }}>Knowledge Base</h2>
      <p style={{ color: '#4b5563', marginBottom: 24 }}>
        Add reference documents (lore, style guides, character bios). Relevant excerpts are
        automatically retrieved and included when generating stories.
      </p>

      {error && <p style={{ color: '#E64A00', fontWeight: 'bold' }}>Error: {error}</p>}

      <div style={{
        marginBottom: 24,
        padding: 20,
        backgroundColor: '#f9f9f9',
        borderRadius: '8px',
        border: '1px solid #e0e0e0',
      }}>
        <div style={{ marginBottom: 12 }}>
          <label style={{ display: 'block', marginBottom: 8, fontWeight: 600 }}>Source label (optional):</label>
          <input
            type="text"
            value={source}
            onChange={(e) => setSource(e.target.value)}
            placeholder="e.g. Character bios, Style guide"
            style={{ width: '100%', boxSizing: 'border-box' }}
          />
        </div>
        <div style={{ marginBottom: 12 }}>
          <label style={{ display: 'block', marginBottom: 8, fontWeight: 600 }}>Document content:</label>
          <textarea
            rows={6}
            value={content}
            onChange={(e) => setContent(e.target.value)}
            placeholder="Paste reference text here. Separate distinct ideas with a blank line so they can be retrieved independently."
            style={{ width: '100%', boxSizing: 'border-box' }}
          />
        </div>
        <div style={{ marginBottom: 12, display: 'flex', alignItems: 'center', gap: 8 }}>
          <input
            id="always-include"
            type="checkbox"
            checked={alwaysInclude}
            onChange={(e) => setAlwaysInclude(e.target.checked)}
            style={{ width: 'auto' }}
          />
          <label htmlFor="always-include" style={{ fontWeight: 600, margin: 0 }}>
            Treat as a guardrail (always applied when generating a matching story)
          </label>
        </div>
        {alwaysInclude && (
          <div style={{ marginBottom: 12 }}>
            <label style={{ display: 'block', marginBottom: 8, fontWeight: 600 }}>
              Apply only to genre (optional — leave blank to apply to all genres):
            </label>
            <select
              value={genreId}
              onChange={(e) => setGenreId(e.target.value)}
              style={{ width: '100%', boxSizing: 'border-box' }}
            >
              <option value="">-- All genres --</option>
              {genres.map((g) => (
                <option key={g.id} value={g.id}>{g.name}</option>
              ))}
            </select>
          </div>
        )}
        <button type="button" onClick={addDocument} disabled={saving}>
          {saving ? 'Embedding & saving...' : alwaysInclude ? 'Add Guardrail' : 'Add to Knowledge Base'}
        </button>
      </div>

      {loading ? (
        <p>Loading knowledge base...</p>
      ) : chunks.length === 0 ? (
        <p style={{ color: '#4b5563' }}>No reference documents yet.</p>
      ) : (
        <ul style={{ listStyle: 'none', margin: 0, padding: 0 }}>
          {chunks.map((chunk) => (
            <li
              key={chunk.id}
              style={{
                border: chunk.alwaysInclude ? '2px solid #FF5200' : '1px solid #e0e0e0',
                borderRadius: 8,
                padding: 16,
                marginBottom: 12,
                display: 'flex',
                justifyContent: 'space-between',
                gap: 16,
                alignItems: 'flex-start',
              }}
            >
              {editingId === chunk.id ? (
                <div style={{ width: '100%' }}>
                  <div style={{ marginBottom: 12 }}>
                    <label style={{ display: 'block', marginBottom: 8, fontWeight: 600 }}>Source label (optional):</label>
                    <input
                      type="text"
                      value={editSource}
                      onChange={(e) => setEditSource(e.target.value)}
                      style={{ width: '100%', boxSizing: 'border-box' }}
                    />
                  </div>
                  <div style={{ marginBottom: 12 }}>
                    <label style={{ display: 'block', marginBottom: 8, fontWeight: 600 }}>Document content:</label>
                    <textarea
                      rows={6}
                      value={editContent}
                      onChange={(e) => setEditContent(e.target.value)}
                      style={{ width: '100%', boxSizing: 'border-box' }}
                    />
                  </div>
                  <div style={{ marginBottom: 12, display: 'flex', alignItems: 'center', gap: 8 }}>
                    <input
                      id={`edit-always-include-${chunk.id}`}
                      type="checkbox"
                      checked={editAlwaysInclude}
                      onChange={(e) => setEditAlwaysInclude(e.target.checked)}
                      style={{ width: 'auto' }}
                    />
                    <label htmlFor={`edit-always-include-${chunk.id}`} style={{ fontWeight: 600, margin: 0 }}>
                      Treat as a guardrail (always applied when generating a matching story)
                    </label>
                  </div>
                  {editAlwaysInclude && (
                    <div style={{ marginBottom: 12 }}>
                      <label style={{ display: 'block', marginBottom: 8, fontWeight: 600 }}>
                        Apply only to genre (optional — leave blank to apply to all genres):
                      </label>
                      <select
                        value={editGenreId}
                        onChange={(e) => setEditGenreId(e.target.value)}
                        style={{ width: '100%', boxSizing: 'border-box' }}
                      >
                        <option value="">-- All genres --</option>
                        {genres.map((g) => (
                          <option key={g.id} value={g.id}>{g.name}</option>
                        ))}
                      </select>
                    </div>
                  )}
                  <div style={{ display: 'flex', gap: 8 }}>
                    <button type="button" onClick={() => saveEdit(chunk.id)} disabled={editSaving}>
                      {editSaving ? 'Saving...' : 'Save'}
                    </button>
                    <button type="button" onClick={cancelEdit} disabled={editSaving}>
                      Cancel
                    </button>
                  </div>
                </div>
              ) : (
                <>
                  <div>
                    <p style={{ color: '#6b7280', fontSize: 13, fontWeight: 600, marginBottom: 4 }}>
                      {chunk.alwaysInclude && (
                        <span style={{
                          backgroundColor: '#FF5200',
                          color: '#ffffff',
                          borderRadius: 4,
                          padding: '2px 8px',
                          marginRight: 8,
                          fontSize: 11,
                        }}>
                          GUARDRAIL{chunk.genreName ? ` · ${chunk.genreName}` : ''}
                        </span>
                      )}
                      {chunk.source}
                    </p>
                    <p style={{ color: '#111827', whiteSpace: 'pre-wrap' }}>{chunk.content}</p>
                  </div>
                  <div style={{ display: 'flex', gap: 8, flexShrink: 0 }}>
                    <button type="button" onClick={() => startEdit(chunk)}>
                      Edit
                    </button>
                    <button type="button" onClick={() => deleteChunk(chunk.id)}>
                      Delete
                    </button>
                  </div>
                </>
              )}
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
