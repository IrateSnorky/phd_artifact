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

const narrativeTransportationItems = [
  'While I was reading the narrative, I could easily picture the events taking place.',
  'I was mentally involved in the narrative while reading it.',
  'The narrative affected me emotionally.',
  'I found myself thinking of ways the narrative could have turned out differently.',
  'While reading the narrative I had a vivid image of the characters.',
  'I wanted to learn how the narrative ended.',
  'I found my mind wandering while reading the narrative.',
  'The events in the narrative are relevant to my everyday life.',
  'The narrative changed my understanding of things.',
  'I felt like I was physically present in the setting of the narrative.',
  'I had a sense of being in the story world.',
  'The narrative had an emotional pull on me.',
  'I thought about how the story might have unfolded differently.',
  'I was absorbed in the narrative and lost track of time.',
  'I felt fully immersed in the characters’ world.',
];

export default function OfficeStoryView() {
  const [stories, setStories] = useState([]);
  const [selectedStoryId, setSelectedStoryId] = useState('');
  const [selectedOfficeId, setSelectedOfficeId] = useState(officeSettings[0].id);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [transformedStory, setTransformedStory] = useState(null);
  const [transforming, setTransforming] = useState(false);
  const [transformError, setTransformError] = useState(null);
  const [surveyResponses, setSurveyResponses] = useState(Array(15).fill(null));
  const [surveySubmitting, setSurveySubmitting] = useState(false);
  const [surveyError, setSurveyError] = useState(null);
  const [surveyResult, setSurveyResult] = useState(null);

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

  const selectStory = (storyId) => {
    setSelectedStoryId(storyId);
    setTransformedStory(null);
    setTransformError(null);
    setSurveyResponses(Array(15).fill(null));
    setSurveyError(null);
    setSurveyResult(null);
  };

  const selectOffice = (officeId) => {
    setSelectedOfficeId(officeId);
    setTransformedStory(null);
    setTransformError(null);
    setSurveyResponses(Array(15).fill(null));
    setSurveyError(null);
    setSurveyResult(null);
  };

  const handleSurveyResponse = (index, value) => {
    setSurveyResponses((current) => {
      const next = [...current];
      next[index] = Number(value);
      return next;
    });
    setSurveyError(null);
  };

  const transformStoryForOffice = async () => {
    if (!selectedStory || !storyText) return;

    setTransforming(true);
    setTransformError(null);
    try {
      const response = await fetch(`${API}/stories/${selectedStory.storyId}/transform-for-office`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          officeName: selectedOffice.name,
          officeDescription: selectedOffice.description,
        }),
      });
      const data = await response.json();
      if (!response.ok) {
        throw new Error(data.detail || data || 'Failed to transform story');
      }
      setTransformedStory(data.transformedStory);
      setSurveyResponses(Array(15).fill(null));
      setSurveyResult(null);
      setSurveyError(null);
    } catch (err) {
      setTransformError(err.message || String(err));
    } finally {
      setTransforming(false);
    }
  };

  const submitSurvey = async () => {
    if (!selectedStory || !transformedStory) return;

    const missing = surveyResponses.some((value) => value === null || Number.isNaN(Number(value)));
    if (missing) {
      setSurveyError('Please answer every question before submitting the survey.');
      return;
    }

    setSurveySubmitting(true);
    setSurveyError(null);

    try {
      const response = await fetch(`${API}/stories/${selectedStory.storyId}/narrative-transportation`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ responses: surveyResponses.map((value) => Number(value)) }),
      });

      if (!response.ok) {
        const data = await response.json().catch(() => null);
        throw new Error(data?.detail || data?.title || 'Failed to save survey responses');
      }

      const data = await response.json();
      setSurveyResult({
        total: data.narrativeTransportationScore,
        average: data.average,
      });
    } catch (err) {
      setSurveyError(err.message || String(err));
    } finally {
      setSurveySubmitting(false);
    }
  };

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
            onChange={(event) => selectStory(event.target.value)}
            disabled={transforming}
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
            onChange={(event) => selectOffice(event.target.value)}
            disabled={transforming}
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
            <button
              type="button"
              onClick={transformStoryForOffice}
              disabled={!storyText || transforming}
              style={{
                backgroundColor: transforming ? '#9ca3af' : selectedOffice.accent,
                border: 'none',
                borderRadius: 6,
                color: '#ffffff',
                cursor: !storyText || transforming ? 'not-allowed' : 'pointer',
                fontWeight: 600,
                marginBottom: 16,
                padding: '10px 16px',
              }}
            >
              {transforming ? 'Transforming...' : `Transform story for ${selectedOffice.name}`}
            </button>
            {transformError && (
              <p role="alert" style={{ color: '#b91c1c', marginTop: 0, marginBottom: 16 }}>
                {transformError}
              </p>
            )}
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
                {transformedStory || storyText || 'This story does not yet have content to display.'}
              </p>
            </div>

            {transformedStory && (
              <div style={{
                marginTop: 24,
                backgroundColor: '#ffffff',
                border: `1px solid ${selectedOffice.accent}`,
                borderRadius: 12,
                padding: 20,
              }}>
                <h3 style={{ margin: '0 0 16px', color: '#111827' }}>Narrative Transportation Survey</h3>
                <p style={{ margin: '0 0 20px', color: '#4b5563' }}>
                  After reading the transformed story, rate how strongly each statement reflects your experience.
                </p>

                {surveyResult ? (
                  <div style={{
                    backgroundColor: '#f5f3ff',
                    borderRadius: 8,
                    padding: 16,
                    border: `1px solid ${selectedOffice.accent}`,
                  }}>
                    <p style={{ margin: 0, fontWeight: 700, color: '#111827' }}>
                      Score: {surveyResult.total}/105
                    </p>
                    <p style={{ margin: '8px 0 0', color: '#374151' }}>
                      Average: {surveyResult.average.toFixed(2)}/7
                    </p>
                  </div>
                ) : (
                  <div>
                    {narrativeTransportationItems.map((item, index) => (
                      <div key={item} style={{ marginBottom: 18, paddingBottom: 12, borderBottom: '1px solid #e5e7eb' }}>
                        <p style={{ margin: '0 0 8px', color: '#111827', lineHeight: 1.5 }}>
                          {index + 1}. {item}
                        </p>
                        <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8 }}>
                          {[1, 2, 3, 4, 5, 6, 7].map((value) => (
                            <label key={value} style={{ display: 'flex', alignItems: 'center', gap: 4, fontSize: 14, color: '#374151' }}>
                              <input
                                type="radio"
                                name={`transport-${index}`}
                                value={value}
                                checked={surveyResponses[index] === value}
                                onChange={(event) => handleSurveyResponse(index, event.target.value)}
                              />
                              {value}
                            </label>
                          ))}
                        </div>
                      </div>
                    ))}

                    {surveyError && (
                      <p role="alert" style={{ color: '#b91c1c', marginTop: 16 }}>{surveyError}</p>
                    )}

                    <button
                      type="button"
                      onClick={submitSurvey}
                      disabled={surveySubmitting}
                      style={{
                        marginTop: 12,
                        backgroundColor: surveySubmitting ? '#9ca3af' : selectedOffice.accent,
                        color: '#ffffff',
                        border: 'none',
                        borderRadius: 6,
                        padding: '10px 16px',
                        cursor: surveySubmitting ? 'not-allowed' : 'pointer',
                        fontWeight: 700,
                      }}
                    >
                      {surveySubmitting ? 'Submitting survey...' : 'Submit survey'}
                    </button>
                  </div>
                )}
              </div>
            )}
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
