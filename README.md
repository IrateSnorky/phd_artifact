# StoryGen

StoryGen is a full-stack story-writing application. It provides story and genre management, Gemini-powered generation with optional knowledge-base context, and an Office View that can adapt a story's backdrop for a selected professional setting.

## Requirements

- .NET 10 SDK
- Node.js 20 or later
- A Google Gemini API key for generation and Office View transformations

## Run locally

Start the backend:

```bash
cd Backend
export GEMINI_API_KEY="your_api_key_here"
dotnet run
```

Start the frontend in another terminal:

```bash
cd Frontend
npm install
npm run dev
```

Open http://localhost:5173. The backend runs at http://localhost:5066.

## Features

- Create, edit, delete, and generate stories grouped by genre.
- Add knowledge-base content and guardrails for Gemini-backed story generation.
- Review a saved story in one of three Office View settings: Law firm, Software startup company, or Accounting business.
- Transform a story's backdrop for the selected office while preserving its original saved content.

## Office View

1. Create a story and generate it from the **Stories** page, or provide a prompt/instructions.
2. Open **Office View** and choose a story and office setting.
3. Select **Transform story for [office name]**.

The transformed text is displayed only in the Office View. It is not saved over the source story, and changing the story or office setting clears the temporary transformation.

Office transformations require an AI provider key. The app supports both Gemini and Cohere, and the selected provider is resolved from the browser header state plus the matching environment variable in the backend shell. See [docs/AI_PROVIDER_SETUP.md](docs/AI_PROVIDER_SETUP.md) for setup and troubleshooting.

## AI Provider Setup

The backend expects the active provider’s API key to be exported in the shell that runs `dotnet run`.

```bash
# Gemini
export GEMINI_API_KEY="your_gemini_key"

# Cohere
export COHERE_API_KEY="your_cohere_key"
```

If the browser is configured for a provider whose key is not available, the resolver falls back to the other configured provider when possible. If neither key is set, the app returns a clear provider-missing error instead of failing with a generic JSON parsing problem.

## Survey-Driven Improvement Flow

The Office View survey can improve a temporary transformed story using feedback from the Narrative Transportation scale.

1. Transform a story for an office setting.
2. Answer all 15 survey questions.
3. Submit the survey.
4. The backend calculates a score, builds guardrails from low-scoring items, and regenerates the temporary story.
5. The saved story is unchanged; only the temporary office-view version is replaced.

## API

| Method | Path | Purpose |
| --- | --- | --- |
| `GET` | `/genres` | List story genres |
| `GET` | `/stories` | List stories with genre details |
| `POST` | `/stories` | Create a story |
| `PUT` | `/stories/{id}` | Update a story |
| `DELETE` | `/stories/{id}` | Delete a story |
| `POST` | `/stories/{id}/generate` | Generate a story using Gemini |
| `POST` | `/stories/{id}/transform-for-office` | Transform a story for an office setting |
| `POST` | `/stories/{id}/narrative-transportation` | Save narrative transportation survey responses |
| `POST` | `/stories/{id}/improve-from-survey` | Regenerate a temporary transformed story using survey feedback guardrails |
| `GET` | `/knowledge` | List knowledge-base chunks |
| `POST` | `/knowledge` | Add knowledge-base content |
| `PUT` | `/knowledge/{id}` | Update knowledge-base content |
| `DELETE` | `/knowledge/{id}` | Delete knowledge-base content |

`POST /stories/{id}/transform-for-office` accepts:

```json
{
  "officeName": "Law firm",
  "officeDescription": "A focused setting for reviewing a story alongside case notes and client objectives."
}
```

It returns `{ "transformedStory": "..." }` and does not modify the stored story.

## Validation

```bash
cd Backend && dotnet build
cd Frontend && npm run lint && npm run build
```
