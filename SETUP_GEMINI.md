# Setup Google Gemini API for Story Generation

## Step 1: Get a Free API Key

1. Go to **https://aistudio.google.com/app/apikey**
2. Click **"Create API key"** (or **"Get API key"**)
3. A new key will be generated - copy it immediately (you won't see it again)

> **Note:** The free tier includes generous usage limits (60 requests per minute)

## Step 2: Set Environment Variable (macOS/Linux)

Add this to your terminal session or `.env` file:

```bash
export GEMINI_API_KEY="your_api_key_here"
```

Alternatively, add to `~/.zshrc` or `~/.bash_profile` for persistence:
```bash
echo 'export GEMINI_API_KEY="your_api_key_here"' >> ~/.zshrc
source ~/.zshrc
```

## Step 3: Restart Backend

```bash
cd Backend
dotnet run
```

The environment variable will be picked up automatically.

## Step 4: Test story generation in the UI

1. Create a story with:
   - **Instructions:** "Write a dramatic scene"
   - **Prompt:** "A mysterious visitor arrives at midnight"
   - **Genre:** Science Fiction

2. Click the **"Generate"** button
3. Watch it create a one-paragraph story!

## Transforming a story for an office setting

The same `GEMINI_API_KEY` is used by the **Office View**. After creating or generating a story:

1. Open **Office View**.
2. Select the story and an office setting.
3. Click **Transform story for [office name]**.

The transformation changes the displayed backdrop to fit the selected office while preserving the saved original story. It is temporary and is cleared when you select a different story or office setting.

## Troubleshooting

- **"GEMINI_API_KEY not set"** → Make sure the environment variable is exported in your terminal
- **API errors** → Check the browser console (F12) for error details
- **Rate limited** → Free tier has 60 requests/minute. Wait a minute before retrying

## Example Output

Input:
- Instructions: Write a tense mystery
- Prompt: Detective finds a cryptic note
- Genre: Historical Fiction

Generated story might be:
> "Detective Morrison's weathered hands trembled as he held the yellowed parchment, its edges frayed with time. The cryptic words scrawled in faded ink—'The truth lies beneath the clocktower at midnight'—sent shivers down his spine. He glanced out the rain-streaked window at the silhouette of the tower against the storm, knowing that whatever secrets it held were about to upend his entire understanding of the city's past."
