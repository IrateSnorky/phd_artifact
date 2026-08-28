# Architecture

## High-level structure

StoryGen is composed of:

- `Backend/` — ASP.NET Core minimal API server, EF Core models, SQLite storage, AI provider integrations
- `Frontend/` — React 19 client served by Vite
- `Backend.Tests/` — backend unit tests
- `docs/` — repository documentation and plans

## Backend

The backend exposes story, genre, knowledge-base, and transformation endpoints in `Backend/Program.cs`.

Core responsibilities:

- story CRUD
- genre lookup
- Office View transformation
- narrative transportation scoring
- survey-driven story improvement
- AI provider selection and fallback

## Frontend

The frontend uses React functional components and fetch-based API calls to the backend.

Main responsibilities:

- story listing and editing
- generation flow
- Office View transformation UI
- survey capture and response handling
- user-visible error messaging

## Data flow

1. User creates or edits a story in the frontend.
2. Backend stores the story in SQLite.
3. Office View transforms the story temporarily for a selected business setting.
4. The survey is submitted to the backend.
5. The backend scores responses, derives guardrails, and improves the temporary story.
6. The source story remains unchanged unless a user explicitly saves or updates it through normal CRUD paths.
