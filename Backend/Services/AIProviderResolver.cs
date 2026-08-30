namespace Backend.Services;

using Microsoft.AspNetCore.Http;

public static class AIProviderResolver
{
    public static bool Resolve(
        HttpRequest request,
        out IAIProvider? provider,
        out string error,
        Func<string?, IAIProvider?>? providerFactory = null)
    {
        var requestedProvider = request.Headers["X-AI-Provider"].FirstOrDefault();
        return TryResolve(requestedProvider, out provider, out error, providerFactory);
    }

    public static bool TryResolve(
        string? requestedProvider,
        out IAIProvider? provider,
        out string error,
        Func<string?, IAIProvider?>? providerFactory = null)
    {
        requestedProvider = string.IsNullOrWhiteSpace(requestedProvider)
            ? "gemini"
            : requestedProvider.Trim().ToLowerInvariant();

        var geminiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        var cohereKey = Environment.GetEnvironmentVariable("COHERE_API_KEY");
        var claudeKey = Environment.GetEnvironmentVariable("CLAUDE_API_KEY");

        providerFactory ??= providerName =>
        {
            if (providerName is "gemini" && !string.IsNullOrWhiteSpace(geminiKey))
                return new GeminiProvider(geminiKey);

            if (providerName is "cohere" && !string.IsNullOrWhiteSpace(cohereKey))
                return new CohereProvider(cohereKey);

            if (providerName is "claude" && !string.IsNullOrWhiteSpace(claudeKey))
                return new ClaudeProvider(claudeKey, cohereKey);

            if (providerName is not ("gemini" or "cohere" or "claude"))
                return !string.IsNullOrWhiteSpace(geminiKey)
                    ? new GeminiProvider(geminiKey)
                    : !string.IsNullOrWhiteSpace(cohereKey)
                        ? new CohereProvider(cohereKey)
                        : !string.IsNullOrWhiteSpace(claudeKey)
                            ? new ClaudeProvider(claudeKey, cohereKey)
                            : null;

            if (!string.IsNullOrWhiteSpace(geminiKey) && providerName == "cohere")
                return new GeminiProvider(geminiKey);

            if (!string.IsNullOrWhiteSpace(cohereKey) && providerName == "gemini")
                return new CohereProvider(cohereKey);

            if (!string.IsNullOrWhiteSpace(claudeKey) && providerName == "gemini")
                return new ClaudeProvider(claudeKey, cohereKey);

            if (!string.IsNullOrWhiteSpace(claudeKey) && providerName == "cohere")
                return new ClaudeProvider(claudeKey, cohereKey);

            return null;
        };

        provider = providerFactory(requestedProvider);
        if (provider is not null)
        {
            error = string.Empty;
            return true;
        }

        if (requestedProvider is not ("gemini" or "cohere" or "claude"))
        {
            error = "Unsupported AI provider. Choose Gemini, Cohere, or Claude.";
            return false;
        }

        error = requestedProvider switch
        {
            "gemini" => "GEMINI_API_KEY environment variable is not set for the selected AI provider.",
            "cohere" => "COHERE_API_KEY environment variable is not set for the selected AI provider.",
            "claude" => "CLAUDE_API_KEY environment variable is not set for the selected AI provider.",
            _ => "No AI provider API key is configured."
        };
        return false;
    }
}
