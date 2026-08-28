# AI Provider Setup

This app can use either Gemini or Cohere for story transformation and survey-driven improvement. The backend resolves the provider from the request header and falls back to the other configured provider when needed.

## Required environment variables

Set the keys in the same shell session that starts the backend:

```bash
# Gemini
export GEMINI_API_KEY="your_gemini_key"

# Cohere
export COHERE_API_KEY="your_cohere_key"
```

## Browser provider selection

The frontend stores the selected provider in `localStorage` under `storygen-ai-provider`.

```js
localStorage.getItem('storygen-ai-provider')
```

Typical values:

```js
localStorage.setItem('storygen-ai-provider', 'gemini');
localStorage.setItem('storygen-ai-provider', 'cohere');
```

After updating it, reload the page.

## Resolution behavior

The backend checks the selected provider first:

- `gemini` → requires `GEMINI_API_KEY`
- `cohere` → requires `COHERE_API_KEY`

If the selected provider key is missing, the resolver falls back to the other provider when its key is available.

## Troubleshooting

If the backend logs:

```text
COHERE_API_KEY environment variable is not set for the selected AI provider
```

then do one of the following:

1. Export the correct key for the provider you want to use.
2. Change the browser provider selection to match the exported key.
3. Restart the backend after exporting the key.

Example:

```bash
export COHERE_API_KEY="your_cohere_key"
cd Backend
dotnet run
```

If you are using Gemini instead, export `GEMINI_API_KEY` and set the browser value to `gemini`.

## Notes

- The backend does not automatically load `.env` files.
- The same environment variable must be present in the shell that runs the backend process.
- The provider selection is request-scoped and not automatically persisted by the backend.
