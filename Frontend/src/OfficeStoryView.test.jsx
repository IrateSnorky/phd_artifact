import { fireEvent, render, screen, waitFor, cleanup } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import OfficeStoryView from './OfficeStoryView';
import { AIProviderProvider } from './AIProviderContext';
import { QuotaProvider } from './QuotaContext';

function response(data, ok = true) {
  return {
    ok,
    json: vi.fn().mockResolvedValue(data),
    text: vi.fn().mockResolvedValue(JSON.stringify(data)),
  };
}

function renderOfficeView() {
  return render(
    <AIProviderProvider>
      <QuotaProvider>
        <OfficeStoryView />
      </QuotaProvider>
    </AIProviderProvider>
  );
}

afterEach(() => {
  cleanup();
  vi.restoreAllMocks();
  window.localStorage.clear();
});

describe('OfficeStoryView', () => {
  it('improves the transformed story after a completed survey', async () => {
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockImplementation((url) => {
      if (url.endsWith('/stories')) {
        return Promise.resolve(response([{
          storyId: 7,
          storyPrompt: 'A robot discovers music',
          storyInstructions: 'Write a hopeful story',
          generatedStory: 'The original story.',
        }]));
      }
      if (url.endsWith('/transform-for-office')) {
        return Promise.resolve(response({
          transformedStory: 'The office version.',
          storyVersion: 'version-1',
        }));
      }
      if (url.endsWith('/narrative-transportation')) {
        return Promise.resolve(response({ narrativeTransportationScore: 45, average: 3 }));
      }
      if (url.endsWith('/feedback-insights')) return Promise.resolve(response([]));
      if (url.endsWith('/improve-from-survey')) {
        return Promise.resolve(response({
          transformedStory: 'The improved office version.',
          storyVersion: 'version-2',
        }));
      }
      throw new Error(`Unexpected request: ${url}`);
    });

    renderOfficeView();
    await screen.findByText('The original story.');
    fireEvent.click(screen.getByRole('button', { name: 'Transform story for Law firm' }));
    await screen.findByText('The office version.');

    screen.getAllByRole('radio').filter((_, index) => index % 5 === 0).forEach((radio) => {
      fireEvent.click(radio);
    });
    fireEvent.click(screen.getByRole('button', { name: 'Submit survey' }));

    expect(await screen.findByText('The improved office version.')).toBeInTheDocument();
    await waitFor(() => {
      const improvementRequest = fetchMock.mock.calls.find(([url]) =>
        url.endsWith('/improve-from-survey'));
      expect(improvementRequest).toBeDefined();
    });
  });

  it('shows a useful error when the improvement response has no body', async () => {
    vi.spyOn(globalThis, 'fetch').mockImplementation((url) => {
      if (url.endsWith('/stories')) {
        return Promise.resolve(response([{
          storyId: 7,
          storyPrompt: 'A robot discovers music',
          storyInstructions: 'Write a hopeful story',
          generatedStory: 'The original story.',
        }]));
      }
      if (url.endsWith('/transform-for-office')) {
        return Promise.resolve(response({
          transformedStory: 'The office version.',
          storyVersion: 'version-1',
        }));
      }
      if (url.endsWith('/narrative-transportation')) {
        return Promise.resolve(response({ narrativeTransportationScore: 45, average: 3 }));
      }
      if (url.endsWith('/feedback-insights')) return Promise.resolve(response([]));
      if (url.endsWith('/improve-from-survey')) {
        return {
          ok: true,
          json: () => Promise.reject(new SyntaxError('Unexpected end of JSON input')),
          text: () => Promise.resolve(''),
        };
      }
      throw new Error(`Unexpected request: ${url}`);
    });

    renderOfficeView();
    await screen.findByText('The original story.');
    fireEvent.click(screen.getByRole('button', { name: 'Transform story for Law firm' }));
    await screen.findByText('The office version.');
    screen.getAllByRole('radio').filter((_, index) => index % 5 === 0).forEach((radio) => {
      fireEvent.click(radio);
    });
    fireEvent.click(screen.getByRole('button', { name: 'Submit survey' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('The story could not be improved. Confirm the selected story still exists and restart the backend if it was recently updated.');
  });

  it('shows a safe fallback when the improvement payload is malformed JSON', async () => {
    vi.spyOn(globalThis, 'fetch').mockImplementation((url) => {
      if (url.endsWith('/stories')) {
        return Promise.resolve(response([{
          storyId: 7,
          storyPrompt: 'A robot discovers music',
          storyInstructions: 'Write a hopeful story',
          generatedStory: 'The original story.',
        }]));
      }
      if (url.endsWith('/transform-for-office')) {
        return Promise.resolve(response({
          transformedStory: 'The office version.',
          storyVersion: 'version-1',
        }));
      }
      if (url.endsWith('/narrative-transportation')) {
        return Promise.resolve(response({ narrativeTransportationScore: 45, average: 3 }));
      }
      if (url.endsWith('/feedback-insights')) return Promise.resolve(response([]));
      if (url.endsWith('/improve-from-survey')) {
        return {
          ok: true,
          json: () => Promise.reject(new SyntaxError('Unexpected token')), 
          text: () => Promise.resolve('not-valid-json'),
        };
      }
      throw new Error(`Unexpected request: ${url}`);
    });

    renderOfficeView();
    await screen.findByText('The original story.');
    fireEvent.click(screen.getByRole('button', { name: 'Transform story for Law firm' }));
    await screen.findByText('The office version.');
    screen.getAllByRole('radio').filter((_, index) => index % 5 === 0).forEach((radio) => {
      fireEvent.click(radio);
    });
    fireEvent.click(screen.getByRole('button', { name: 'Submit survey' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('The story could not be improved. Confirm the selected story still exists and restart the backend if it was recently updated.');
  });
});
