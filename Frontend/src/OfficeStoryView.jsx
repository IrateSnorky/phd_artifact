import { useEffect, useState } from 'react';

const API = 'http://localhost:5066';

const officeSettings = [
  {
    id: 'law-firm',
    name: 'Law firm',
    label: 'Client matter briefing',
    description: 'A focused setting for reviewing a story alongside case notes and client objectives.',
    accent: '#7c2d12',
    background: '#fff7ed',
  },
  {
    id: 'software-startup',
    name: 'Software startup company',
    label: 'Product team review',
    description: 'A collaborative setting for sharing a narrative with a fast-moving product team.',
    accent: '#4338ca',
    background: '#f5f3ff',
  },
  {
    id: 'accounting-business',
    name: 'Accounting business',
    label: 'Client portfolio review',
    description: 'A clear, structured setting for considering a story with a client services team.',
    accent: '#047857',
    background: '#ecfdf5',
  },
];

export default function OfficeStoryView() {
  const [stories, setStories] = useState([]);
  const [selectedStoryId, setSelectedStoryId] = useState('');
  const [selectedOfficeId, setSelectedOfficeId] = useState(officeSettings[0].id);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    const controller = new AbortController();

    async function loadStories() {
      try {
        const response = await fetch(`${API}/stories`, { signal: controller.signal });
        if (!response.ok) {
          throw new Error('Failed to load stories');
        }
        setStories(await response.json());
      } catch (err) {
        if (err.name !== 'AbortError') {
          setError(err.message || String(err));
        }
      } finally {
        if (!controller.signal.aborted) {
          setLoading(false);
        }
      }
    }

    loadStories();
    return () => controller.abort();
  }, []);

  const selectedStory = stories.find((story) => story.storyId === Number(selectedStoryId)) ?? stories[0];
  const selectedOffice = officeSettings.find((office) => office.id === selectedOfficeId) ?? officeSettings[0];
  const storyText = selectedStory?.generatedStory || selectedStory?.storyPrompt || selectedStory?.storyInstructions;

  if (loading) {
    return <p style={{ padding: 20, color: '#000000' }}>Loading stories...</p>;
  }

  if (error) {
    return <p style={{ color: '#E64A00', padding: 20, fontWeight: 'bold' }}>Error: {error}</p>;
  }

  return (
    <section style={{ padding: 20, backgroundColor: '#ffffff' }}>
      <h2 style={{ color: '#000000', marginBottom: 8 }}>Office Story View</h2>
      <p style={{ color: '#4b5563', marginBottom: 24 }}>
        Choose a saved story and the office setting where it will be reviewed.
      </p>

      <div style={{
        display: 'grid',
        gridTemplateColumns: 'repeat(auto-fit, minmax(260px, 1fr))',
        gap: 16,
        marginBottom: 24,
      }}>
        <div>
          <label htmlFor="story-select" style={{ display: 'block', marginBottom: 8 }}>Story</label>
          <select
            id="story-select"
            value={selectedStory?.storyId ?? ''}
            onChange={(event) => setSelectedStoryId(event.target.value)}
            style={{ boxSizing: 'border-box', width: '100%' }}
          >
            {stories.length === 0 && <option value="">No stories available</option>}
            {stories.map((story) => (
              <option key={story.storyId} value={story.storyId}>
                Story {story.storyId}: {story.storyPrompt || story.storyInstructions || 'Untitled story'}
              </option>
            ))}
          </select>
        </div>

        <div>
          <label htmlFor="office-select" style={{ display: 'block', marginBottom: 8 }}>Office setting</label>
          <select
            id="office-select"
            value={selectedOffice.id}
            onChange={(event) => setSelectedOfficeId(event.target.value)}
            style={{ boxSizing: 'border-box', width: '100%' }}
          >
            {officeSettings.map((office) => (
              <option key={office.id} value={office.id}>{office.name}</option>
            ))}
          </select>
        </div>
      </div>

      {selectedStory ? (
        <article style={{
          backgroundColor: selectedOffice.background,
          border: `1px solid ${selectedOffice.accent}`,
          borderRadius: 12,
          overflow: 'hidden',
        }}>
          <header style={{
            backgroundColor: selectedOffice.accent,
            color: '#ffffff',
            padding: '20px 24px',
          }}>
            <p style={{ color: '#ffffff', fontSize: 14, fontWeight: 600, marginBottom: 4 }}>
              {selectedOffice.label}
            </p>
            <h3 style={{ color: '#ffffff', fontSize: 24, margin: 0 }}>
              {selectedOffice.name}
            </h3>
          </header>

          <div style={{ padding: 24 }}>
            <p style={{ color: '#374151', marginBottom: 20 }}>{selectedOffice.description}</p>
            <div style={{
              backgroundColor: '#ffffff',
              borderRadius: 8,
              padding: 20,
              borderLeft: `5px solid ${selectedOffice.accent}`,
            }}>
              <p style={{ color: '#6b7280', fontSize: 14, fontWeight: 600, marginBottom: 8 }}>
                STORY {selectedStory.storyId}{selectedStory.genreName ? ` · ${selectedStory.genreName}` : ''}
              </p>
              <p style={{ color: '#111827', lineHeight: 1.7, whiteSpace: 'pre-wrap' }}>
                {storyText || 'This story does not yet have content to display.'}
              </p>
            </div>
          </div>
        </article>
      ) : (
        <div style={{
          padding: 24,
          border: '1px solid #e0e0e0',
          borderRadius: 8,
          color: '#4b5563',
        }}>
          Create a story on the Stories page to view it in an office setting.
        </div>
      )}
    </section>
  );
}
