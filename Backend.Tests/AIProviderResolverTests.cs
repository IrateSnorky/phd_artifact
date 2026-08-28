using Backend.Services;
using Microsoft.AspNetCore.Http;

namespace Backend.Tests;

public class AIProviderResolverTests
{
    [Fact]
    public void Resolve_UsesFallbackProvider_WhenSelectedProviderKeyMissing()
    {
        var previousGemini = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        var previousCohere = Environment.GetEnvironmentVariable("COHERE_API_KEY");

        try
        {
            Environment.SetEnvironmentVariable("GEMINI_API_KEY", "gemini-test-key");
            Environment.SetEnvironmentVariable("COHERE_API_KEY", null);

            var resolved = AIProviderResolver.TryResolve("cohere", out var provider, out var error);

            Assert.True(resolved);
            Assert.NotNull(provider);
            Assert.Equal("gemini", provider.Name);
            Assert.Equal(string.Empty, error);
        }
        finally
        {
            Environment.SetEnvironmentVariable("GEMINI_API_KEY", previousGemini);
            Environment.SetEnvironmentVariable("COHERE_API_KEY", previousCohere);
        }
    }

    [Fact]
    public void Resolve_ReturnsClearError_WhenNoProviderKeysExist()
    {
        var previousGemini = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        var previousCohere = Environment.GetEnvironmentVariable("COHERE_API_KEY");

        try
        {
            Environment.SetEnvironmentVariable("GEMINI_API_KEY", null);
            Environment.SetEnvironmentVariable("COHERE_API_KEY", null);

            var context = new DefaultHttpContext();
            context.Request.Headers["X-AI-Provider"] = "cohere";

            var success = AIProviderResolver.Resolve(context.Request, out var provider, out var error);

            Assert.False(success);
            Assert.Null(provider);
            Assert.Contains("COHERE_API_KEY", error);
        }
        finally
        {
            Environment.SetEnvironmentVariable("GEMINI_API_KEY", previousGemini);
            Environment.SetEnvironmentVariable("COHERE_API_KEY", previousCohere);
        }
    }
}
