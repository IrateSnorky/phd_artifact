# Troubleshooting

## Missing AI provider key

If the backend logs:

```text
COHERE_API_KEY environment variable is not set for the selected AI provider
```

or the Gemini equivalent, then:

1. Export the correct key in the shell running the backend.
2. Ensure the browser provider selection matches the exported key.
3. Restart the backend.

Example:

```bash
export COHERE_API_KEY="your_cohere_key"
cd Backend
dotnet run
```

## 404/empty response from stale backend

A stale backend process can still be listening on port 5066 and may not include the newest endpoint. Stop the old process and restart the backend.

## Empty or malformed JSON

The frontend now treats empty or malformed JSON bodies as user-visible errors instead of crashing. This is especially important for failed AI improvement calls.
