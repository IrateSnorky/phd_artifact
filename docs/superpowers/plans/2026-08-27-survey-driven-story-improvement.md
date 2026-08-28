# Survey-Driven Story Improvement Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a survey-driven story improvement flow that turns narrative-transportation feedback into guardrails, regenerates a temporary office-view story, and preserves the original saved story while keeping the app resilient to provider configuration issues.

**Architecture:** The backend will score survey responses, derive improvement guardrails, resolve the active AI provider from request context and environment variables, and expose dedicated improvement endpoints. The frontend will capture the survey, call the backend endpoints, render the transformed story in Office View, and handle empty/invalid responses without crashing the UI. This keeps the source story immutable while enabling iterative temporary rewrites.

**Tech Stack:** .NET 10, ASP.NET Core minimal APIs, EF Core SQLite, React 19, Vite, Vitest + Testing Library, Gemini and Cohere provider integrations.

**Spec:** `README.md`

## Global Constraints

- .NET 10 SDK required
- Node.js 20 or later required
- Backend uses ASP.NET Core minimal APIs and SQLite via EF Core
- Frontend uses React 19 + Vite with no Redux/Zustand
- Story data must remain mutable only through normal story CRUD; Office View transformation must not overwrite the saved story
- AI provider resolution must respect the request header and available environment variables (`GEMINI_API_KEY`, `COHERE_API_KEY`)
- All new behavior must be validated with focused unit tests plus backend/frontend checks

---

### Task 1: Add provider abstraction and resolver fallback

**Files:**
- Create: `Backend/Services/IAIProvider.cs`
- Create: `Backend/Services/AIProviderResolver.cs`
- Modify: `Backend/Program.cs`
- Test: `Backend.Tests/AIProviderResolverTests.cs`

**Interfaces:**
- Consumes: environment variables and request header values from `HttpRequest`
- Produces: `IAIProvider` implementation selected for use by story improvement and transformation endpoints

- [ ] **Step 1: Write the failing test**

```csharp
using Backend.Services;

namespace Backend.Tests;

public class AIProviderResolverTests
{
    [Fact]
    public void Resolve_UsesFallbackProvider_WhenSelectedProviderKeyMissing()
    {
        Environment.SetEnvironmentVariable("GEMINI_API_KEY", "gemini-key");
        Environment.SetEnvironmentVariable("COHERE_API_KEY", null);

        var resolved = AIProviderResolver.TryResolve("cohere", out var provider, out var error);

        Assert.True(resolved);
        Assert.NotNull(provider);
        Assert.Equal("gemini", provider.Name);
        Assert.Null(error);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd Backend && dotnet test Backend.Tests --filter AIProviderResolverTests -v minimal`
Expected: FAIL because `AIProviderResolver` and `IAIProvider` do not exist yet.

- [ ] **Step 3: Write minimal implementation**

```csharp
public interface IAIProvider
{
    string Name { get; }
    Task<string> ImproveStoryAsync(string originalStory, List<string> guardrails, string officeName, string officeDescription);
}

public static class AIProviderResolver
{
    public static bool Resolve(HttpRequest request, out IAIProvider? provider, out string? error)
    {
        var chosen = request.Headers["X-AI-Provider"].FirstOrDefault() ?? "gemini";
        provider = chosen switch
        {
            "cohere" when !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("COHERE_API_KEY")) => new CohereProvider(),
            "gemini" when !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GEMINI_API_KEY")) => new GeminiProvider(),
            _ when !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GEMINI_API_KEY")) => new GeminiProvider(),
            _ when !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("COHERE_API_KEY")) => new CohereProvider(),
            _ => null
        };

        error = provider is null
            ? "COHERE_API_KEY environment variable is not set for the selected AI provider."
            : null;
        return provider is not null;
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `cd Backend && dotnet test Backend.Tests --filter AIProviderResolverTests -v minimal`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add Backend/Services/IAIProvider.cs Backend/Services/AIProviderResolver.cs Backend/Program.cs Backend.Tests/AIProviderResolverTests.cs
git commit -m "feat: add AI provider resolver fallback"
```

### Task 2: Add narrative transportation scoring and persistence

**Files:**
- Modify: `Backend/Program.cs`
- Modify: `Backend/Models/Story.cs` (if needed)
- Modify: `Backend/Data/AppDbContext.cs` (if needed)
- Test: `Backend.Tests/NarrativeTransportationScoringTests.cs`

**Interfaces:**
- Consumes: 15 survey response values, transformed story text, office metadata, story version
- Produces: `totalScore`, `adjustedResponses`, and persisted evaluation record for later debugging/auditing

- [ ] **Step 1: Write the failing test**

```csharp
[Fact]
public void Calculate_ReturnsAdjustedResponses_AndTotalScore()
{
    var responses = new[] { 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5 };
    var score = NarrativeTransportationScoring.Calculate(responses);

    Assert.Equal(75, score.Total);
    Assert.Equal(15, score.AdjustedResponses.Count);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd Backend && dotnet test Backend.Tests --filter NarrativeTransportationScoringTests -v minimal`
Expected: FAIL because scoring logic does not exist yet.

- [ ] **Step 3: Write minimal implementation**

```csharp
public static class NarrativeTransportationScoring
{
    public static NarrativeTransportationScore Calculate(IReadOnlyList<int> responses)
    {
        if (responses.Count != 15)
            throw new ArgumentException("Narrative transportation responses must contain 15 values.");

        var adjusted = responses
            .Select((value, index) => index == 6 ? 7 - value : value)
            .ToList();

        var total = adjusted.Sum();
        return new NarrativeTransportationScore(total, adjusted);
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `cd Backend && dotnet test Backend.Tests --filter NarrativeTransportationScoringTests -v minimal`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add Backend/Program.cs Backend/Models/Story.cs Backend/Data/AppDbContext.cs Backend.Tests/NarrativeTransportationScoringTests.cs
git commit -m "feat: add narrative transportation scoring"
```

### Task 3: Generate improvement guardrails from survey feedback

**Files:**
- Create: `Backend/Services/FeedbackInsightService.cs`
- Modify: `Backend/Program.cs`
- Test: `Backend.Tests/FeedbackInsightServiceTests.cs`

**Interfaces:**
- Consumes: list of survey responses and current transformed story context
- Produces: list of guardrail strings for use by the AI improvement call

- [ ] **Step 1: Write the failing test**

```csharp
[Fact]
public void BuildImprovementGuardrails_UsesLowScoresToPrioritizeFixes()
{
    var responses = new[] { 1, 1, 1, 1, 1, 1, 5, 1, 1, 1, 1, 1, 1, 1, 1 };
    var guardrails = FeedbackInsightService.BuildImprovementGuardrails(responses);

    Assert.Contains("clarity", guardrails.Select(g => g.ToLowerInvariant()).FirstOrDefault() ?? string.Empty);
    Assert.NotEmpty(guardrails);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd Backend && dotnet test Backend.Tests --filter FeedbackInsightServiceTests -v minimal`
Expected: FAIL because `FeedbackInsightService` does not exist yet.

- [ ] **Step 3: Write minimal implementation**

```csharp
public static class FeedbackInsightService
{
    public static IReadOnlyList<string> BuildImprovementGuardrails(IReadOnlyList<int> responses)
    {
        if (responses.Count != 15)
            throw new ArgumentException("Responses must contain 15 values.");

        var lowScores = responses
            .Select((value, index) => new { value, index })
            .Where(x => x.value <= 2)
            .Select(x => x.index)
            .ToList();

        var guardrails = new List<string>();
        if (lowScores.Contains(0) || lowScores.Contains(1) || lowScores.Contains(2))
            guardrails.Add("Improve clarity and narrative accessibility for the reader.");
        if (lowScores.Contains(6))
            guardrails.Add("Reduce attention drift by tightening pacing and transitions.");
        if (lowScores.Contains(3) || lowScores.Contains(4) || lowScores.Contains(5))
            guardrails.Add("Preserve emotional resonance while strengthening scene specificity.");

        return guardrails.Count == 0
            ? new[] { "Keep the story vivid, emotionally grounded, and cleanly paced." }
            : guardrails;
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `cd Backend && dotnet test Backend.Tests --filter FeedbackInsightServiceTests -v minimal`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add Backend/Services/FeedbackInsightService.cs Backend/Program.cs Backend.Tests/FeedbackInsightServiceTests.cs
git commit -m "feat: add survey feedback guardrails"
```

### Task 4: Add Office View endpoints and AI improvement flow

**Files:**
- Modify: `Backend/Program.cs`
- Modify: `Backend/Services/GeminiProvider.cs`
- Modify: `Backend/Services/CohereProvider.cs`
- Test: `Backend.Tests/OfficeImprovementFlowTests.cs`

**Interfaces:**
- Consumes: story ID, transformed story text, office metadata, and guardrails
- Produces: improved temporary transformed story payload and version metadata

- [ ] **Step 1: Write the failing test**

```csharp
[Fact]
public async Task ImproveFromSurvey_ReturnsImprovedStoryPayload()
{
    var request = new NarrativeTransportationImprovementRequest
    {
        Responses = Enumerable.Repeat(4, 15).ToList(),
        TransformedStory = "A story in a law office.",
        OfficeName = "Law firm",
        OfficeDescription = "Case review context.",
        StoryVersion = "v1"
    };

    var result = await new StoryImprovementEndpointHarness().Call(request);

    Assert.NotNull(result);
    Assert.False(string.IsNullOrWhiteSpace(result.TransformedStory));
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd Backend && dotnet test Backend.Tests --filter OfficeImprovementFlowTests -v minimal`
Expected: FAIL because the endpoint and provider plumbing do not exist or are not wired together.

- [ ] **Step 3: Write minimal implementation**

```csharp
app.MapPost("/stories/{id}/improve-from-survey", async (int id, NarrativeTransportationImprovementRequest request, HttpRequest httpRequest, AppDbContext db) =>
{
    var story = await db.Stories.FindAsync(id);
    if (story is null) return Results.NotFound();

    var guardrails = FeedbackInsightService.BuildImprovementGuardrails(request.Responses ?? []);
    if (!AIProviderResolver.Resolve(httpRequest, out var provider, out var providerError))
        return Results.BadRequest(providerError);

    var improvedStory = await provider!.ImproveStoryAsync(
        request.TransformedStory,
        guardrails.ToList(),
        request.OfficeName,
        request.OfficeDescription);

    return Results.Ok(new { transformedStory = improvedStory, storyVersion = Guid.NewGuid().ToString("N"), guardrails });
});
```

- [ ] **Step 4: Run test to verify it passes**

Run: `cd Backend && dotnet test Backend.Tests --filter OfficeImprovementFlowTests -v minimal`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add Backend/Program.cs Backend/Services/GeminiProvider.cs Backend/Services/CohereProvider.cs Backend.Tests/OfficeImprovementFlowTests.cs
git commit -m "feat: add survey-driven story improvement endpoint"
```

### Task 5: Wire the frontend survey flow and safe parsing

**Files:**
- Modify: `Frontend/src/OfficeStoryView.jsx`
- Modify: `Frontend/src/Stories.jsx`
- Test: `Frontend/src/OfficeStoryView.test.jsx`
- Test: `Frontend/src/Stories.test.jsx`

**Interfaces:**
- Consumes: selected story, office configuration, survey responses, and backend payloads
- Produces: transformed story state, feedback insights, and user-visible error handling

- [ ] **Step 1: Write the failing test**

```jsx
it('handles empty JSON body without crashing', async () => {
  global.fetch = vi.fn().mockResolvedValue({
    ok: false,
    text: async () => '',
  });

  const { getByText } = render(<OfficeStoryView />);
  await waitFor(() => expect(getByText(/Failed to improve story/i)).toBeInTheDocument());
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd Frontend && npm run test -- --run src/OfficeStoryView.test.jsx`
Expected: FAIL because the UI currently tries to parse invalid or empty JSON directly.

- [ ] **Step 3: Write minimal implementation**

```jsx
const responseText = await response.text();
let data = null;
if (responseText.trim()) {
  try {
    data = JSON.parse(responseText);
  } catch {
    data = responseText;
  }
}

if (!response.ok) {
  throw new Error(data?.detail || data?.title || data?.message || data || 'Failed to improve story');
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `cd Frontend && npm run test -- --run src/OfficeStoryView.test.jsx src/Stories.test.jsx`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add Frontend/src/OfficeStoryView.jsx Frontend/src/Stories.jsx Frontend/src/OfficeStoryView.test.jsx Frontend/src/Stories.test.jsx
git commit -m "feat: add front-end survey handling and safe API parsing"
```

### Task 6: End-to-end validation and release checks

**Files:**
- Modify: `README.md`
- Validate: `Backend`, `Frontend`, and any relevant test/build commands

**Interfaces:**
- Consumes: validated implementation from prior tasks
- Produces: verified story-improvement workflow with clear developer setup notes

- [ ] **Step 1: Update docs**

Add a short section to `README.md` describing the Office View survey flow, the required provider environment variables, and the command to verify app health.

- [ ] **Step 2: Run targeted validation**

```bash
cd Backend && dotnet test
cd Frontend && npm run test -- --run
cd Frontend && npm run lint
cd Frontend && npm run build
```

- [ ] **Step 3: Verify behavior manually**

1. Start backend with the environment variable for the selected provider.
2. Start frontend with `npm run dev`.
3. Create a story, transform it in Office View, submit survey responses, and confirm the improved story appears without overwriting the original.
4. Confirm a stale backend process is not masking the new endpoint and that empty-body failures are handled gracefully.

- [ ] **Step 4: Commit**

```bash
git add README.md
git commit -m "docs: document survey-driven story improvement"
```

---

### Implementation Notes

- Keep the temporary transformed story flow isolated to Office View; never rewrite the `Stories` table from the improvement endpoint.
- The survey question set should remain stable across UI and backend contracts to avoid mismatch bugs.
- Provider selection should be resolved once per request, with fallback rules applied only when the preferred provider key is absent.
- Empty HTTP bodies should be treated as valid no-content or error states depending on response status, not as JSON parse failures.
- Commit after each task to preserve a clean review trail and make review gates obvious.
