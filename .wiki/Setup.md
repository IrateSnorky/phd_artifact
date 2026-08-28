# Setup

## Requirements

- .NET 10 SDK
- Node.js 20+
- A valid API key for either Gemini or Cohere

## Backend

```bash
cd Backend
export GEMINI_API_KEY="your_gemini_key"
# or
export COHERE_API_KEY="your_cohere_key"
dotnet run
```

The app listens on `http://localhost:5066`.

## Frontend

```bash
cd Frontend
npm install
npm run dev
```

The app is served at `http://localhost:5173`.

## Provider selection

The frontend keeps the active provider in localStorage:

```js
localStorage.setItem('storygen-ai-provider', 'gemini');
// or
localStorage.setItem('storygen-ai-provider', 'cohere');
```

## Validation

```bash
cd Backend
dotnet test
cd Frontend
npm run test -- --run
npm run lint
npm run build
```
