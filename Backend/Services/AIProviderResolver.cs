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

        providerFactory ??= providerName =>
        {
            if (providerName is "gemini" && !string.IsNullOrWhiteSpace(geminiKey))
                return new GeminiProvider(geminiKey);

            if (providerName is "cohere" && !string.IsNullOrWhiteSpace(cohereKey))
                return new CohereProvider(cohereKey);

            if (providerName is not ("gemini" or "cohere"))
                return !string.IsNullOrWhiteSpace(geminiKey)
                    ? new GeminiProvider(geminiKey)
                    : !string.IsNullOrWhiteSpace(cohereKey)
                        ? new CohereProvider(cohereKey)
                        : null;

            if (!string.IsNullOrWhiteSpace(geminiKey) && providerName == "cohere")
                return new GeminiProvider(geminiKey);

            if (!string.IsNullOrWhiteSpace(cohereKey) && providerName == "gemini")
                return new CohereProvider(cohereKey);

            return null;
        };

        provider = providerFactory(requestedProvider);
        if (provider is not null)
        {
            error = string.Empty;
            return true;
        }

        if (requestedProvider is not ("gemini" or "cohere"))
        {
            error = "Unsupported AI provider. Choose Gemini or Cohere.";
            return false;
        }

        error = requestedProvider == "gemini"
            ? "GEMINI_API_KEY environment variable is not set for the selected AI provider."
            : "COHERE_API_KEY environment variable is not set for the selected AI provider.";
        return false;
    }
}
