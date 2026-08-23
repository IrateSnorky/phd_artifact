# Copilot Instructions for FullStackApp

## Project Overview

This is a full-stack application with a **React 19 + Vite frontend** and a **.NET 10 (C#) backend** using ASP.NET Core minimal APIs with SQLite via Entity Framework Core. The application manages stories organized by genres.

## Architecture

### Frontend (`/Frontend`)
- **Framework**: React 19 (functional components with hooks)
- **Build Tool**: Vite 8.2 with React plugin
- **Linting**: Oxlint with React and Oxc rules
- **Key Features**:
  - Multi-page navigation (Home page with weather forecast + Stories management page)
  - Stories component manages CRUD operations for stories
  - Genre dropdown selection for categorizing stories
  - Calls backend API at `http://localhost:5066`

### Backend (`/Backend`)
- **Framework**: ASP.NET Core 10 (minimal APIs)
- **Database**: SQLite with Entity Framework Core 8.0
- **Key Models**:
  - **Story**: `StoryId`, `StoryInstructions` (text), `GenreId` (foreign key)
  - **StoryGenre**: `StoryGenreId`, `Name`, relationship to multiple Stories
- **Database Behavior**: Recreates database on each startup in development (see `Program.cs` around line 23-33)
- **Seeded Data**: Two default genres (Science Fiction, Historical Fiction)
- **API Endpoints**:
  - `GET /genres` - Fetch all genres
  - `GET /stories` - Fetch all stories with genre info
  - `POST /stories` - Create new story
  - `PUT /stories/{id}` - Update story
  - `DELETE /stories/{id}` - Delete story
  - `GET /weatherforecast` - Sample endpoint (legacy, keep for compatibility)
- **CORS Configuration**: Allows requests from React dev server at `http://localhost:5173`

## Build & Run Commands

### Frontend
```bash
cd Frontend

# Development server (HMR enabled)
npm run dev        # Runs at http://localhost:5173

# Production build
npm run build      # Outputs to /dist

# Linting
npm run lint       # Oxlint checks (react and oxc rules)

# Preview production build locally
npm run preview

# Playwright E2E tests (when configured)
npm run test:e2e   # Run end-to-end tests
npx playwright test --ui  # Run with browser UI for debugging
```

### Backend
```bash
cd Backend

# Run (default listens on https://localhost:5066)
dotnet run

# Build
dotnet build

# Restore dependencies
dotnet restore

# Run specific configuration
dotnet run --configuration Release
```

### Full Stack Development
1. Terminal 1: `cd Backend && dotnet run`
2. Terminal 2: `cd Frontend && npm run dev`
3. Open http://localhost:5173

## Browser Testing with Playwright

Playwright MCP integration enables E2E testing automation. Tests verify Stories CRUD operations and UI interactions.

### Setup
```bash
# In Frontend directory
npm install -D @playwright/test
npx playwright install  # Install browser binaries
```

### Test Structure
- Test files: `Frontend/tests/` or `Frontend/e2e/`
- Configuration: `playwright.config.js` in Frontend root
- Target URL: `http://localhost:5173` (ensure backend is running at `localhost:5066`)

### Typical Test Scenarios
1. **Story Creation**: Fill form, submit, verify in list
2. **Genre Selection**: Change genre dropdown, verify API call
3. **Story Deletion**: Delete story, verify removal from UI
4. **Page Navigation**: Switch between Home and Stories pages

### Running Tests with Playwright MCP
- Copilot can generate and execute Playwright test scripts
- Use `npx playwright test --ui` for visual debugging
- Tests auto-wait for elements and network requests

## Key Conventions

### Frontend
- **Component Structure**: React functional components in `/src` (e.g., `App.jsx`, `Stories.jsx`)
- **API Calls**: Direct `fetch()` to backend at `API = 'http://localhost:5066'` (no axios/client library)
- **State Management**: `useState` and `useEffect` hooks, no Redux/Zustand
- **React Hooks Rules**: Oxlint enforces React rules of hooks; see `.oxlintrc.json`
- **Component Exports**: Only export components (enforced by oxlint `react/only-export-components`)
- **CSS**: Global styles in `main.jsx`; component-level in separate files when needed

### Backend
- **Minimal APIs**: All endpoints defined in `Program.cs` (no controller classes)
- **DbContext**: Single `AppDbContext` in `/Data` serving all models
- **Models**: Entity classes in `/Models` directory
- **Database First**: Development setup recreates DB at startup; schema is defined via EF Core models
- **Nullable Reference Types**: Enabled project-wide (`<Nullable>enable</Nullable>`)
- **Foreign Keys**: Story.GenreId can be null; OnDelete behavior is `SetNull` (story persists if genre deleted)

### Shared
- **Port Configuration**: 
  - Frontend dev: `5173` (Vite default)
  - Backend: `5066` (HTTPS)
  - Update both if ports change
- **CORS**: Hardcoded to localhost:5173; modify if frontend deployment URL changes
- **Database File**: `stories.db` in Backend project root (SQLite local file)

## Development Workflow

### Adding a New Story Feature
1. Backend: Update `Story` model in `/Models/Story.cs` if needed
2. Backend: Update `AppDbContext` relationships if needed
3. Backend: Add/modify endpoint in `Program.cs`
4. Frontend: Update `Stories.jsx` to call new endpoint and reflect UI changes
5. Test with both servers running

### Modifying Database Schema
1. Update model classes in `/Backend/Models/`
2. Update `AppDbContext.OnModelCreating()` if relationships change
3. Restart backend (DB auto-recreates on startup in development)

### Testing API Endpoints
- Use the `Backend.http` file with REST client extension (VSCode, JetBrains)
- Or use `curl` from terminal
- Ensure backend is running at `https://localhost:5066`

## Common Issues

- **CORS errors**: Verify `UseCors("AllowReact")` in `Program.cs` matches frontend URL
- **Database not found**: Database recreates on startup; if corrupted, delete `stories.db*` files and restart backend
- **Port conflicts**: Change port in Vite config or `appsettings.json` (backend uses `ApplicationUrl`)
- **Oxlint warnings**: Fix via `npm run lint` output; common: components not exported as default

## Notes for Copilot Sessions

- This is a minimal full-stack template; focus on Story/StoryGenre domain logic
- Frontend: Keep using fetch() and basic hooks (no state management library)
- Backend: Keep using minimal APIs (no controllers)
- Database: Always assumes fresh SQLite; data is ephemeral in development
- Both Frontend and Backend are in separate folders; changes to one don't auto-rebuild the other

