# StoryGen Wiki

## Overview

StoryGen is a full-stack application for creating, reviewing, and improving stories in different professional office contexts. The project combines a .NET minimal API backend with a React frontend and SQLite persistence.

## Architecture

- Frontend: React 19 + Vite
- Backend: ASP.NET Core minimal APIs on .NET 10
- Database: SQLite via EF Core
- AI providers: Gemini and Cohere

## Key commands

### Backend

```bash
cd Backend
export GEMINI_API_KEY="your_key"
# or
export COHERE_API_KEY="your_key"
dotnet run
```

### Frontend

```bash
cd Frontend
npm install
npm run dev
```

### Validation

```bash
cd Backend
dotnet test
cd Frontend
npm run test -- --run
npm run lint
npm run build
```

## Features

- Story CRUD with genre grouping
- Office View story transformation
- Narrative transportation survey and guardrail-driven improvement
- AI provider fallback logic
- Safe UI handling for empty or malformed backend responses

## AI provider setup

Set the provider key in the backend shell, and match the provider in the browser via `localStorage`:

```js
localStorage.setItem('storygen-ai-provider', 'gemini');
// or
localStorage.setItem('storygen-ai-provider', 'cohere');
```

See `docs/AI_PROVIDER_SETUP.md` for the full troubleshooting guide.

## Project docs

- `README.md`
- `docs/AI_PROVIDER_SETUP.md`
- `docs/superpowers/plans/2026-08-27-survey-driven-story-improvement.md`

## Contributing

- Keep backend and frontend changes aligned.
- Prefer focused tests for changed behavior.
- Do not overwrite the saved story during Office View transformation.
