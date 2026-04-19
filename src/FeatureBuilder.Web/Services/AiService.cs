using System.ClientModel;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Azure;
using Azure.AI.Inference;
using Azure.AI.OpenAI;
using FeatureBuilder.Web.Models;
using OpenAI.Chat;

namespace FeatureBuilder.Web.Services;

public class AiService : IAiService
{
    private readonly ILogger<AiService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AiProviderType _provider;
    private readonly string? _apiKey;
    private readonly string? _endpoint;
    private readonly string _model;

    // Vision models known to support image input
    private static readonly HashSet<string> KnownVisionModels = new(StringComparer.OrdinalIgnoreCase)
    {
        "gpt-4o", "gpt-4o-mini", "gpt-4-vision-preview", "gpt-4-turbo",
        "claude-3-opus-20240229", "claude-3-sonnet-20240229", "claude-3-haiku-20240307",
        "claude-3-5-sonnet-20241022", "claude-3-5-haiku-20241022",
        "llava", "llava:latest", "llava:7b", "llava:13b", "llava:34b",
        "phi-3.5-vision-instruct", "mistral-small", "pixtral-12b"
    };

    public bool IsConfigured => _provider switch
    {
        AiProviderType.Ollama => true, // Ollama needs no key
        _ => !string.IsNullOrWhiteSpace(_apiKey)
    };

    public AiProviderType Provider => _provider;

    public string ProviderDisplayName => _provider switch
    {
        AiProviderType.OpenAI => "OpenAI",
        AiProviderType.AzureOpenAI => "Azure OpenAI",
        AiProviderType.AzureAIFoundry => "Azure AI Foundry",
        AiProviderType.GitHubModels => "GitHub Models",
        AiProviderType.Anthropic => "Anthropic",
        AiProviderType.Ollama => "Ollama (local)",
        _ => "Unknown"
    };

    public bool SupportsImageAnalysis => _provider switch
    {
        AiProviderType.Anthropic => KnownVisionModels.Contains(_model),
        AiProviderType.Ollama => KnownVisionModels.Any(v => _model.StartsWith(v, StringComparison.OrdinalIgnoreCase)),
        _ => KnownVisionModels.Contains(_model)
    };

    public AiService(ILogger<AiService> logger, IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;

        // Support both new AI:* section and legacy OpenAI:* section for backward compat
        var providerString = configuration["AI:Provider"] ?? configuration["OpenAI:Provider"] ?? "OpenAI";
        _provider = Enum.TryParse<AiProviderType>(providerString, ignoreCase: true, out var parsed)
            ? parsed
            : AiProviderType.OpenAI;

        _apiKey = configuration["AI:ApiKey"] ?? configuration["OpenAI:ApiKey"];
        _endpoint = configuration["AI:Endpoint"] ?? configuration["OpenAI:Endpoint"];
        _model = configuration["AI:Model"]
              ?? configuration["AI:DeploymentOrModel"]
              ?? configuration["OpenAI:DeploymentOrModel"]
              ?? DefaultModelForProvider(_provider);
    }

    private static string DefaultModelForProvider(AiProviderType provider) => provider switch
    {
        AiProviderType.Anthropic => "claude-3-5-sonnet-20241022",
        AiProviderType.Ollama => "llama3.2",
        AiProviderType.GitHubModels => "gpt-4o",
        _ => "gpt-4o"
    };

    // ----------------------------------------------------------------
    // Client factory helpers
    // ----------------------------------------------------------------

    private ChatClient? CreateOpenAICompatibleClient()
    {
        if (!IsConfigured) return null;

        return _provider switch
        {
            AiProviderType.AzureOpenAI => new AzureOpenAIClient(
                    new Uri(_endpoint!), new AzureKeyCredential(_apiKey!))
                .GetChatClient(_model),

            AiProviderType.Ollama => new OpenAI.OpenAIClient(
                    new ApiKeyCredential(_apiKey ?? "ollama"),
                    new OpenAI.OpenAIClientOptions { Endpoint = new Uri(ResolveOllamaEndpoint()) })
                .GetChatClient(_model),

            // OpenAI direct (also default fallback)
            _ => new OpenAI.OpenAIClient(_apiKey!).GetChatClient(_model)
        };
    }

    private string ResolveOllamaEndpoint()
    {
        var baseEndpoint = string.IsNullOrWhiteSpace(_endpoint) ? "http://localhost:11434" : _endpoint.TrimEnd('/');
        return baseEndpoint.EndsWith("/v1", StringComparison.OrdinalIgnoreCase) ? baseEndpoint : baseEndpoint + "/v1";
    }

    private ChatCompletionsClient? CreateAzureInferenceClient()
    {
        if (!IsConfigured) return null;

        var endpoint = _provider == AiProviderType.GitHubModels
            ? "https://models.inference.ai.azure.com"
            : _endpoint!;

        return new ChatCompletionsClient(new Uri(endpoint), new AzureKeyCredential(_apiKey!));
    }

    // ----------------------------------------------------------------
    // Unified chat dispatch
    // ----------------------------------------------------------------

    private async Task<AiResponse> DispatchChatAsync(
        string systemPrompt, string userText,
        byte[]? imageBytes = null, string? imageMimeType = null)
    {
        if (!IsConfigured)
            return new AiResponse
            {
                Success = false,
                Error = $"AI service ({ProviderDisplayName}) is not configured. Check your AI:ApiKey setting."
            };

        return _provider switch
        {
            AiProviderType.Anthropic =>
                await CallAnthropicAsync(systemPrompt, userText, imageBytes, imageMimeType),

            AiProviderType.AzureAIFoundry or AiProviderType.GitHubModels =>
                await CallAzureInferenceAsync(systemPrompt, userText, imageBytes, imageMimeType),

            _ => // OpenAI, AzureOpenAI, Ollama
                await CallOpenAICompatibleAsync(systemPrompt, userText, imageBytes, imageMimeType)
        };
    }

    // ----------------------------------------------------------------
    // OpenAI-compatible path (OpenAI, Azure OpenAI, Ollama)
    // ----------------------------------------------------------------

    private async Task<AiResponse> CallOpenAICompatibleAsync(
        string systemPrompt, string userText,
        byte[]? imageBytes, string? imageMimeType)
    {
        var client = CreateOpenAICompatibleClient();
        if (client == null)
            return Error("Could not create OpenAI-compatible client.");

        try
        {
            List<ChatMessage> messages;
            if (imageBytes != null && imageMimeType != null)
            {
                messages = new List<ChatMessage>
                {
                    new SystemChatMessage(systemPrompt),
                    new UserChatMessage(
                        ChatMessageContentPart.CreateTextPart(userText),
                        ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(imageBytes), imageMimeType))
                };
            }
            else
            {
                messages = new List<ChatMessage>
                {
                    new SystemChatMessage(systemPrompt),
                    new UserChatMessage(userText)
                };
            }

            var response = await client.CompleteChatAsync(messages);
            return new AiResponse { Success = true, Content = response.Value.Content[0].Text };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Provider}] Chat completion failed", ProviderDisplayName);
            return Error(ex.Message);
        }
    }

    // ----------------------------------------------------------------
    // Azure AI Inference path (Azure AI Foundry, GitHub Models)
    // ----------------------------------------------------------------

    private async Task<AiResponse> CallAzureInferenceAsync(
        string systemPrompt, string userText,
        byte[]? imageBytes, string? imageMimeType)
    {
        var client = CreateAzureInferenceClient();
        if (client == null)
            return Error("Could not create Azure AI Inference client.");

        try
        {
            var messages = new List<ChatRequestMessage>
            {
                new ChatRequestSystemMessage(systemPrompt)
            };

            if (imageBytes != null && imageMimeType != null)
            {
                var userMsg = new ChatRequestUserMessage(new ChatMessageContentItem[]
                {
                    new ChatMessageTextContentItem(userText),
                    new ChatMessageImageContentItem(
                        new BinaryData(imageBytes),
                        imageMimeType)
                });
                messages.Add(userMsg);
            }
            else
            {
                messages.Add(new ChatRequestUserMessage(userText));
            }

            var options = new ChatCompletionsOptions(messages)
            {
                Model = _model,
                MaxTokens = 4096
            };

            var response = await client.CompleteAsync(options);
            return new AiResponse { Success = true, Content = response.Value.Content };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Provider}] Azure AI Inference call failed", ProviderDisplayName);
            return Error(ex.Message);
        }
    }

    // ----------------------------------------------------------------
    // Anthropic path (direct HTTP, avoids third-party SDK)
    // ----------------------------------------------------------------

    private async Task<AiResponse> CallAnthropicAsync(
        string systemPrompt, string userText,
        byte[]? imageBytes, string? imageMimeType)
    {
        try
        {
            var http = _httpClientFactory.CreateClient("Anthropic");
            http.DefaultRequestHeaders.Clear();
            http.DefaultRequestHeaders.Add("x-api-key", _apiKey);
            http.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            // Build content array
            var content = new JsonArray();

            if (imageBytes != null && imageMimeType != null)
            {
                content.Add(new JsonObject
                {
                    ["type"] = "image",
                    ["source"] = new JsonObject
                    {
                        ["type"] = "base64",
                        ["media_type"] = imageMimeType,
                        ["data"] = Convert.ToBase64String(imageBytes)
                    }
                });
            }

            content.Add(new JsonObject { ["type"] = "text", ["text"] = userText });

            var body = new JsonObject
            {
                ["model"] = _model,
                ["max_tokens"] = 4096,
                ["system"] = systemPrompt,
                ["messages"] = new JsonArray
                {
                    new JsonObject { ["role"] = "user", ["content"] = content }
                }
            };

            var httpResponse = await http.PostAsync(
                "https://api.anthropic.com/v1/messages",
                new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"));

            var json = await httpResponse.Content.ReadAsStringAsync();

            if (!httpResponse.IsSuccessStatusCode)
            {
                _logger.LogError("[Anthropic] API error {Status}: {Body}", httpResponse.StatusCode, json);
                var errDoc = JsonDocument.Parse(json);
                var errMsg = errDoc.RootElement.TryGetProperty("error", out var errEl)
                    ? errEl.GetProperty("message").GetString() ?? json
                    : json;
                return Error(errMsg);
            }

            using var doc = JsonDocument.Parse(json);
            var text = doc.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString() ?? string.Empty;

            return new AiResponse { Success = true, Content = text };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Anthropic] API call failed");
            return Error(ex.Message);
        }
    }

    // ----------------------------------------------------------------
    // IAiService implementation
    // ----------------------------------------------------------------

    public async Task<AiResponse> ImproveIssueDescriptionAsync(AiImproveRequest request)
    {
        var system = $"""
            You are an expert software engineer helping to write clear, comprehensive GitHub issues.
            The issue is for the repository: {request.RepositoryFullName}.
            Your task is to improve the issue description to be more detailed, clear, and actionable.
            Include sections like: Description, Steps to Reproduce (if applicable), Expected Behavior, Actual Behavior, and Additional Context.
            Return only the improved issue body in Markdown format without any preamble or explanation.
            """;

        var user = $"""
            Issue Title: {request.Title}

            Current Description:
            {request.Body}

            {(string.IsNullOrWhiteSpace(request.AdditionalContext) ? "" : $"Additional Context:\n{request.AdditionalContext}")}

            Please improve this issue description.
            """;

        return await DispatchChatAsync(system, user);
    }

    public async Task<AiResponse> AnswerQuestionAsync(AiQuestionRequest request)
    {
        var system = $"""
            You are an expert software engineer helping to elaborate on a GitHub issue.
            The issue is for the repository: {request.RepositoryFullName}.
            You help users think through their issues by answering questions that help them provide more context and detail.
            Current issue title: {request.IssueTitle}
            Current issue body: {request.IssueBody}
            """;

        return await DispatchChatAsync(system, request.Question);
    }

    public async Task<AiResponse> AnalyzeImageAsync(AiImageAnalysisRequest request)
    {
        if (!SupportsImageAnalysis)
            return new AiResponse
            {
                Success = false,
                Error = $"The current model ({_model}) does not appear to support image analysis. Try gpt-4o, claude-3-5-sonnet, or a llava model."
            };

        var system = $"""
            You are an expert software engineer analyzing images to help write GitHub issues.
            The issue is for the repository: {request.RepositoryFullName}.
            Analyze the provided image and describe what you see in the context of the issue.
            Provide a detailed description that can be added to the issue body to clarify the problem.
            """;

        var user = $"Issue Title: {request.IssueTitle}\n\nCurrent Issue Body: {request.IssueBody}\n\nPlease analyze this image and provide a description for the GitHub issue:";

        var imageBytes = Convert.FromBase64String(request.ImageBase64);
        return await DispatchChatAsync(system, user, imageBytes, request.MimeType);
    }

    private static AiResponse Error(string message) => new() { Success = false, Error = message };
}
