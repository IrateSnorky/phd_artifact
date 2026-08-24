import { useEffect, useState } from 'react';
import { isQuotaError, useQuotaMessage } from './QuotaContext';

const API = 'http://localhost:5066';

const inputStyle = {
  width: '100%',
  boxSizing: 'border-box',
  border: '2px solid #FF5200',
  borderRadius: '4px',
  padding: '10px',
  fontFamily: 'sans-serif',
  fontSize: '14px',
  color: '#000000',
};

const selectStyle = {
  ...inputStyle,
  padding: '8px',
  backgroundColor: '#ffffff',
};

const primaryButtonStyle = {
  backgroundColor: '#FF5200',
  color: '#ffffff',
  border: 'none',
  padding: '10px 24px',
  borderRadius: '4px',
  cursor: 'pointer',
  fontWeight: 600,
  fontSize: '14px',
};

const secondaryButtonStyle = {
  backgroundColor: '#cccccc',
  color: '#000000',
  border: 'none',
  padding: '6px 12px',
  borderRadius: '4px',
  cursor: 'pointer',
  fontWeight: 600,
  fontSize: '12px',
};

const dangerButtonStyle = {
  ...secondaryButtonStyle,
  backgroundColor: '#E64A00',
  color: '#ffffff',
};

const smallButtonStyle = {
  ...primaryButtonStyle,
  padding: '6px 12px',
  fontSize: '12px',
  marginRight: '4px',
};

function ActionButton({ children, variant = 'primary', style = {}, ...props }) {
  const buttonStyles = {
    primary: smallButtonStyle,
    secondary: secondaryButtonStyle,
    danger: dangerButtonStyle,
  }[variant];

  return (
    <button
      {...props}
      style={{
        ...buttonStyles,
        ...style,
      }}
    >
      {children}
    </button>
  );
}

function TextAreaField({ label, value, onChange, rows = 2, placeholder }) {
  return (
    <div style={{ marginBottom: 12 }}>
      <label style={{ display: 'block', marginBottom: 8, fontWeight: 600, color: '#000000' }}>{label}</label>
      <textarea
        rows={rows}
        value={value}
        onChange={onChange}
        placeholder={placeholder}
        style={inputStyle}
      />
    </div>
  );
}

function StoryModal({ story, onClose }) {
  if (!story || !story.generatedStory) return null;

  return (
    <div style={{
      position: 'fixed',
      top: 0,
      left: 0,
      right: 0,
      bottom: 0,
      backgroundColor: 'rgba(0, 0, 0, 0.5)',
      display: 'flex',
      justifyContent: 'center',
      alignItems: 'center',
      zIndex: 1000,
    }}>
      <div style={{
        backgroundColor: '#ffffff',
        borderRadius: '12px',
        padding: '32px',
        maxWidth: '700px',
        width: '90%',
        maxHeight: '80vh',
        overflowY: 'auto',
        boxShadow: '0 10px 40px rgba(0, 0, 0, 0.2)',
        border: '3px solid #FF5200',
      }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
          <h2 style={{ color: '#FF5200', margin: 0 }}>Generated Story</h2>
          <button
            type="button"
            onClick={onClose}
            style={{
              backgroundColor: '#E64A00',
              color: '#ffffff',
              border: 'none',
              width: '32px',
              height: '32px',
              borderRadius: '50%',
              cursor: 'pointer',
              fontSize: '18px',
              fontWeight: 'bold',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
            }}
          >
            ✕
          </button>
        </div>

        <div style={{ marginBottom: 16 }}>
          <label style={{ fontWeight: 600, color: '#666666', fontSize: '12px', textTransform: 'uppercase' }}>Genre</label>
          <p style={{ color: '#000000', fontSize: '16px', margin: '4px 0 0 0' }}>{story.genreName || '-'}</p>
        </div>

        <div style={{ marginBottom: 16 }}>
          <label style={{ fontWeight: 600, color: '#666666', fontSize: '12px', textTransform: 'uppercase' }}>Instructions</label>
          <p style={{ color: '#000000', fontSize: '14px', margin: '4px 0 0 0', whiteSpace: 'pre-wrap' }}>{story.storyInstructions}</p>
        </div>

        <div style={{ marginBottom: 24 }}>
          <label style={{ fontWeight: 600, color: '#666666', fontSize: '12px', textTransform: 'uppercase' }}>Prompt</label>
          <p style={{ color: '#000000', fontSize: '14px', margin: '4px 0 0 0', whiteSpace: 'pre-wrap' }}>{story.storyPrompt}</p>
        </div>

        <div style={{
          backgroundColor: '#f9f9f9',
          border: '2px solid #FF5200',
          borderRadius: '8px',
          padding: '20px',
          marginBottom: 24,
        }}>
          <label style={{ fontWeight: 600, color: '#FF5200', fontSize: '12px', textTransform: 'uppercase', display: 'block', marginBottom: 12 }}>Full Story</label>
          <p style={{
            color: '#000000',
            fontSize: '16px',
            lineHeight: '1.6',
            margin: 0,
            whiteSpace: 'pre-wrap',
            wordWrap: 'break-word',
          }}>
            {story.generatedStory}
          </p>
        </div>

        <button
          type="button"
          onClick={onClose}
          style={{
            ...primaryButtonStyle,
            width: '100%',
          }}
        >
          Close
        </button>
      </div>
    </div>
  );
}

export default function Stories() {
  const [stories, setStories] = useState([]);
  const [genres, setGenres] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [generating, setGenerating] = useState(null);
  const [viewingStoryId, setViewingStoryId] = useState(null);
  const [newText, setNewText] = useState('');
  const [newPrompt, setNewPrompt] = useState('');
  const [newGenre, setNewGenre] = useState('');
  const [editingId, setEditingId] = useState(null);
  const [editingText, setEditingText] = useState('');
  const [editingPrompt, setEditingPrompt] = useState('');
  const [editingGenre, setEditingGenre] = useState(null);
  const { showQuotaMessage } = useQuotaMessage();

  const getErrorMessage = async (response) => {
    const rawText = await response.text();
    let parsedText = rawText.trim();

    try {
      const parsedJson = JSON.parse(rawText);
      parsedText = parsedJson.detail || parsedJson.title || parsedJson.message || parsedText;
    } catch {
      // ASP.NET minimal APIs may return plain text.
    }

    return parsedText || `Request failed (${response.status})`;
  };

  const requestJson = async (path, options = {}) => {
    const response = await fetch(`${API}${path}`, options);
    if (!response.ok) {
      throw new Error(await getErrorMessage(response));
    }
    return response.json();
  };

  const fetchGenres = async () => {
    const data = await requestJson('/genres');
    // oxlint-disable-next-line react/set-state-in-effect
    setGenres(data);

    if (!newGenre && data.length) {
      setNewGenre(String(data[0].id));
    }
  };

  const fetchStories = async () => {
    const data = await requestJson('/stories');
    // oxlint-disable-next-line react/set-state-in-effect
    setStories(data);
  };

  useEffect(() => {
    const loadData = async () => {
      try {
        setLoading(true);
        setError(null);
        await Promise.all([fetchGenres(), fetchStories()]);
      } catch (err) {
        setError(err.message || String(err));
      } finally {
        setLoading(false);
      }
    };

    loadData();
  }, []);

  const createStory = async () => {
    if (!newText.trim()) return;

    try {
      await requestJson('/stories', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          storyInstructions: newText,
          storyPrompt: newPrompt,
          genreId: newGenre || null,
        }),
      });

      setNewText('');
      setNewPrompt('');
      await fetchStories();
    } catch (err) {
      setError(err.message || String(err));
    }
  };

  const startEdit = (story) => {
    setEditingId(story.storyId);
    setEditingText(story.storyInstructions || '');
    setEditingPrompt(story.storyPrompt || '');
    setEditingGenre(story.genreId || '');
  };

  const cancelEdit = () => {
    setEditingId(null);
    setEditingText('');
    setEditingPrompt('');
    setEditingGenre(null);
  };

  const saveEdit = async (id) => {
    try {
      await requestJson(`/stories/${id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          storyInstructions: editingText,
          storyPrompt: editingPrompt,
          genreId: editingGenre || null,
        }),
      });

      cancelEdit();
      await fetchStories();
    } catch (err) {
      setError(err.message || String(err));
    }
  };

  const deleteStory = async (id) => {
    if (!window.confirm('Delete this story?')) return;

    try {
      await requestJson(`/stories/${id}`, { method: 'DELETE' });
      await fetchStories();
    } catch (err) {
      setError(err.message || String(err));
    }
  };

  const generateStory = async (id) => {
    setGenerating(id);

    try {
      await requestJson(`/stories/${id}/generate`, { method: 'POST' });
      await fetchStories();
    } catch (err) {
      const message = err.message || String(err);
      if (isQuotaError(message)) {
        showQuotaMessage();
      } else {
        setError(message);
      }
    } finally {
      setGenerating(null);
    }
  };

  if (loading) return <p style={{ padding: 20, color: '#000000' }}>Loading stories...</p>;
  if (error) return <p style={{ color: '#E64A00', padding: 20, fontWeight: 'bold' }}>Error: {error}</p>;

  const viewingStory = viewingStoryId ? stories.find((story) => story.storyId === viewingStoryId) : null;

  return (
    <div style={{ padding: 20, backgroundColor: '#ffffff' }}>
      <h2 style={{ color: '#000000', marginBottom: 24 }}>Create & Manage Stories</h2>

      <div style={{
        marginBottom: 24,
        padding: 20,
        backgroundColor: '#f9f9f9',
        borderRadius: '8px',
        border: '1px solid #e0e0e0',
      }}>
        <TextAreaField
          label="Instructions:"
          rows={3}
          value={newText}
          onChange={(event) => setNewText(event.target.value)}
          placeholder="Describe the story you want to create"
        />
        <TextAreaField
          label="Prompt:"
          rows={2}
          value={newPrompt}
          onChange={(event) => setNewPrompt(event.target.value)}
          placeholder="The main prompt or idea for the story"
        />

        <div style={{ display: 'flex', gap: '12px', alignItems: 'flex-end' }}>
          <div style={{ flex: 1 }}>
            <label style={{ display: 'block', marginBottom: 8, fontWeight: 600, color: '#000000' }}>Genre:</label>
            <select
              value={newGenre}
              onChange={(event) => setNewGenre(event.target.value)}
              style={selectStyle}
            >
              <option value="">-- Select Genre --</option>
              {genres.map((genre) => (
                <option key={genre.id} value={genre.id}>{genre.name}</option>
              ))}
            </select>
          </div>

          <button type="button" onClick={createStory} style={primaryButtonStyle}>
            Create Story
          </button>
        </div>
      </div>

      <div style={{ overflowX: 'auto' }}>
        <table border="1" cellPadding="8" style={{ borderCollapse: 'collapse', width: '100%', border: '1px solid #e0e0e0', borderRadius: '4px' }}>
          <thead>
            <tr style={{ backgroundColor: '#FF5200', color: '#ffffff' }}>
              <th style={{ width: 60, textAlign: 'left', fontWeight: 600, color: '#ffffff' }}>ID</th>
              <th style={{ textAlign: 'left', fontWeight: 600, color: '#ffffff' }}>Instructions</th>
              <th style={{ textAlign: 'left', fontWeight: 600, color: '#ffffff' }}>Prompt</th>
              <th style={{ textAlign: 'left', fontWeight: 600, color: '#ffffff' }}>Generated Story</th>
              <th style={{ textAlign: 'left', fontWeight: 600, color: '#ffffff' }}>Genre</th>
              <th style={{ textAlign: 'left', fontWeight: 600, color: '#ffffff' }}>Narrative Transport</th>
              <th style={{ width: 280, textAlign: 'left', fontWeight: 600, color: '#ffffff' }}>Actions</th>
            </tr>
          </thead>
          <tbody>
            {stories.map((story) => (
              <tr key={story.storyId} style={{ borderBottom: '1px solid #e0e0e0', backgroundColor: story.storyId % 2 === 0 ? '#f9f9f9' : '#ffffff' }}>
                <td style={{ textAlign: 'center', color: '#000000', fontWeight: 600 }}>{story.storyId}</td>
                <td style={{ color: '#000000' }}>
                  {editingId === story.storyId ? (
                    <textarea
                      rows={3}
                      value={editingText}
                      onChange={(event) => setEditingText(event.target.value)}
                      style={inputStyle}
                    />
                  ) : (
                    <div style={{ whiteSpace: 'pre-wrap', fontSize: '0.95em' }}>{story.storyInstructions}</div>
                  )}
                </td>
                <td style={{ color: '#000000' }}>
                  {editingId === story.storyId ? (
                    <textarea
                      rows={2}
                      value={editingPrompt}
                      onChange={(event) => setEditingPrompt(event.target.value)}
                      style={inputStyle}
                    />
                  ) : (
                    <div style={{ whiteSpace: 'pre-wrap', fontSize: '0.95em' }}>{story.storyPrompt}</div>
                  )}
                </td>
                <td style={{ color: '#666666', cursor: story.generatedStory ? 'pointer' : 'default' }}>
                  <div
                    onClick={() => story.generatedStory && setViewingStoryId(story.storyId)}
                    style={{
                      whiteSpace: 'pre-wrap',
                      fontSize: '0.9em',
                      fontStyle: 'italic',
                      padding: '4px',
                      borderRadius: '4px',
                      backgroundColor: story.generatedStory ? 'rgba(255, 82, 0, 0.05)' : 'transparent',
                      transition: 'all 0.2s',
                    }}
                  >
                    {story.generatedStory ? `${story.generatedStory.substring(0, 80)}... (click to view)` : '(none)'}
                  </div>
                </td>
                <td style={{ color: '#000000' }}>
                  {editingId === story.storyId ? (
                    <select
                      value={editingGenre || ''}
                      onChange={(event) => setEditingGenre(event.target.value)}
                      style={{ ...selectStyle, padding: '6px' }}
                    >
                      <option value="">-- none --</option>
                      {genres.map((genre) => (
                        <option key={genre.id} value={genre.id}>{genre.name}</option>
                      ))}
                    </select>
                  ) : (
                    <div style={{ fontWeight: 500 }}>{story.genreName || '-'}</div>
                  )}
                </td>
                <td style={{ color: '#000000', fontWeight: 600 }}>
                  {story.narrativeTransportationScore == null ? '—' : `${story.narrativeTransportationScore}/105`}
                </td>
                <td>
                  {editingId === story.storyId ? (
                    <>
                      <ActionButton onClick={() => saveEdit(story.storyId)}>Save</ActionButton>
                      <ActionButton variant="secondary" onClick={cancelEdit}>Cancel</ActionButton>
                    </>
                  ) : (
                    <>
                      <ActionButton onClick={() => startEdit(story)}>Edit</ActionButton>
                      <ActionButton
                        onClick={() => generateStory(story.storyId)}
                        disabled={generating === story.storyId}
                        style={{
                          backgroundColor: generating === story.storyId ? '#cccccc' : '#FF5200',
                          cursor: generating === story.storyId ? 'not-allowed' : 'pointer',
                        }}
                      >
                        {generating === story.storyId ? 'Generating...' : 'Generate'}
                      </ActionButton>
                      <ActionButton variant="danger" onClick={() => deleteStory(story.storyId)}>Delete</ActionButton>
                    </>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <StoryModal story={viewingStory} onClose={() => setViewingStoryId(null)} />
    </div>
  );
}
