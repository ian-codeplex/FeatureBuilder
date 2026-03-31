namespace FeatureBuilder.Web.Models;

/// <summary>
/// Supported AI provider backends for issue description enhancement.
/// </summary>
public enum AiProviderType
{
    /// <summary>OpenAI API (api.openai.com)</summary>
    OpenAI,

    /// <summary>Azure OpenAI Service (your-resource.openai.azure.com)</summary>
    AzureOpenAI,

    /// <summary>Azure AI Foundry inference endpoint (your-project.services.ai.azure.com/models)</summary>
    AzureAIFoundry,

    /// <summary>GitHub Models — Azure AI Inference endpoint (models.inference.ai.azure.com)</summary>
    GitHubModels,

    /// <summary>Anthropic Claude API (api.anthropic.com)</summary>
    Anthropic,

    /// <summary>Ollama local inference server (OpenAI-compatible, e.g. http://localhost:11434)</summary>
    Ollama
}
