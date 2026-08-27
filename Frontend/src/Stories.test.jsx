import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import Stories from './Stories';
import { AIProviderProvider } from './AIProviderContext';
import { QuotaProvider } from './QuotaContext';

const genres = [
  { id: 1, name: 'Science Fiction' },
  { id: 2, name: 'Historical Fiction' },
];

const initialStories = [
  {
    storyId: 7,
    storyInstructions: 'A robot discovers music',
    storyPrompt: 'Write a hopeful story',
    genreId: 1,
    genreName: 'Science Fiction',
    generatedStory: null,
    narrativeTransportationScore: null,
  },
];

function renderStories() {
  return render(
    <AIProviderProvider>
      <QuotaProvider>
        <Stories />
      </QuotaProvider>
    </AIProviderProvider>,
  );
}

function response(data, ok = true, status = 200) {
  return {
    ok,
    status,
    json: vi.fn().mockResolvedValue(data),
    text: vi.fn().mockResolvedValue(typeof data === 'string' ? data : JSON.stringify(data)),
  };
}

afterEach(() => {
  cleanup();
  vi.restoreAllMocks();
  window.localStorage.clear();
});

describe('Stories', () => {
  it('loads genres and stories', async () => {
    vi.spyOn(globalThis, 'fetch').mockImplementation((url) => {
      if (url.endsWith('/genres')) return Promise.resolve(response(genres));
      return Promise.resolve(response(initialStories));
    });

    renderStories();

    expect(screen.getByText('Loading stories...')).toBeInTheDocument();
    expect(await screen.findByText('A robot discovers music')).toBeInTheDocument();
    expect(screen.getAllByText('Science Fiction').length).toBeGreaterThan(0);
    expect(screen.getByRole('button', { name: 'Create Story' })).toBeInTheDocument();
  });

  it('creates a story and refreshes the list', async () => {
    let stories = [];
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockImplementation((url, options = {}) => {
      if (url.endsWith('/genres')) return Promise.resolve(response(genres));
      if (options.method === 'POST') {
        stories = [{ ...initialStories[0], storyInstructions: 'A new adventure' }];
        return Promise.resolve(response(stories));
      }
      return Promise.resolve(response(stories));
    });

    renderStories();
    await screen.findByText('Create & Manage Stories');

    fireEvent.change(screen.getByPlaceholderText('Describe the story you want to create'), {
      target: { value: 'A new adventure' },
    });
    fireEvent.change(screen.getByPlaceholderText('The main prompt or idea for the story'), {
      target: { value: 'Make it exciting' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Create Story' }));

    expect(await screen.findByText('A new adventure')).toBeInTheDocument();
    const createRequest = fetchMock.mock.calls.find(([, options]) => options?.method === 'POST');
    expect(JSON.parse(createRequest[1].body)).toEqual({
      storyInstructions: 'A new adventure',
      storyPrompt: 'Make it exciting',
      genreId: '1',
    });
  });

  it('edits and deletes a story', async () => {
    let stories = [...initialStories];
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockImplementation((url, options = {}) => {
      if (url.endsWith('/genres')) return Promise.resolve(response(genres));
      if (options.method === 'PUT') {
        stories = [{ ...stories[0], storyInstructions: 'Updated instructions' }];
        return Promise.resolve(response(stories[0]));
      }
      if (options.method === 'DELETE') {
        stories = [];
        return Promise.resolve({
          ok: true,
          status: 204,
          json: vi.fn().mockRejectedValue(new SyntaxError('Unexpected end of JSON input')),
          text: vi.fn().mockResolvedValue(''),
        });
      }
      return Promise.resolve(response([...stories]));
    });
    vi.spyOn(window, 'confirm').mockReturnValue(true);

    renderStories();
    await screen.findByText('A robot discovers music');

    fireEvent.click(screen.getByRole('button', { name: 'Edit' }));
    const editArea = screen.getByDisplayValue('A robot discovers music');
    fireEvent.change(editArea, { target: { value: 'Updated instructions' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save' }));
    await waitFor(() => expect(screen.queryByRole('button', { name: 'Save' })).not.toBeInTheDocument());
    expect(await screen.findByText('Updated instructions')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Delete' }));
    await waitFor(() => expect(screen.queryByText('Updated instructions')).not.toBeInTheDocument());
    expect(fetchMock.mock.calls.some(([, options]) => options?.method === 'DELETE')).toBe(true);
  });

  it('shows a quota alert when generation reaches the AI quota', async () => {
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockImplementation((url, options = {}) => {
      if (url.endsWith('/genres')) return Promise.resolve(response(genres));
      if (options.method === 'POST' && url.endsWith('/generate')) {
        return Promise.resolve(response({ detail: 'AI quota exceeded' }, false, 429));
      }
      return Promise.resolve(response(initialStories));
    });

    renderStories();
    await screen.findByText('A robot discovers music');
    fireEvent.click(screen.getByRole('button', { name: 'Generate' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('free-tier AI quota has been reached');
    expect(fetchMock.mock.calls.some(([, options]) => options?.method === 'POST')).toBe(true);
  });
});
