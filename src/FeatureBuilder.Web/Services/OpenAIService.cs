using Azure;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using FeatureBuilder.Web.Models;

namespace FeatureBuilder.Web.Services;

public class OpenAIService : IOpenAIService
{
    private readonly ILogger<OpenAIService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string? _apiKey;
    private readonly string? _endpoint;
    private readonly string _deploymentOrModel;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    public OpenAIService(ILogger<OpenAIService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _apiKey = configuration["OpenAI:ApiKey"];
        _endpoint = configuration["OpenAI:Endpoint"];
        _deploymentOrModel = configuration["OpenAI:DeploymentOrModel"] ?? "gpt-4o";
    }

    private ChatClient? CreateChatClient()
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            return null;

        if (!string.IsNullOrWhiteSpace(_endpoint))
        {
            // Azure OpenAI
            var azureClient = new AzureOpenAIClient(new Uri(_endpoint), new AzureKeyCredential(_apiKey));
            return azureClient.GetChatClient(_deploymentOrModel);
        }
        else
        {
            // OpenAI direct
            var openAiClient = new OpenAI.OpenAIClient(_apiKey);
            return openAiClient.GetChatClient(_deploymentOrModel);
        }
    }

    public async Task<AiResponse> ImproveIssueDescriptionAsync(AiImproveRequest request)
    {
        var client = CreateChatClient();
        if (client == null)
            return new AiResponse { Success = false, Error = "AI service is not configured. Please add OpenAI:ApiKey to your configuration." };

        try
        {
            var systemPrompt = $"""
                You are an expert software engineer helping to write clear, comprehensive GitHub issues.
                The issue is for the repository: {request.RepositoryFullName}.
                Your task is to improve the issue description to be more detailed, clear, and actionable.
                Include sections like: Description, Steps to Reproduce (if applicable), Expected Behavior, Actual Behavior, and Additional Context.
                Return only the improved issue body in Markdown format without any preamble or explanation.
                """;

            var userPrompt = $"""
                Issue Title: {request.Title}
                
                Current Description:
                {request.Body}
                
                {(string.IsNullOrWhiteSpace(request.AdditionalContext) ? "" : $"Additional Context:\n{request.AdditionalContext}")}
                
                Please improve this issue description.
                """;

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            };

            var response = await client.CompleteChatAsync(messages);
            return new AiResponse { Success = true, Content = response.Value.Content[0].Text };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error improving issue description");
            return new AiResponse { Success = false, Error = ex.Message };
        }
    }

    public async Task<AiResponse> AnswerQuestionAsync(AiQuestionRequest request)
    {
        var client = CreateChatClient();
        if (client == null)
            return new AiResponse { Success = false, Error = "AI service is not configured. Please add OpenAI:ApiKey to your configuration." };

        try
        {
            var systemPrompt = $"""
                You are an expert software engineer helping to elaborate on a GitHub issue.
                The issue is for the repository: {request.RepositoryFullName}.
                You help users think through their issues by answering questions that help them provide more context and detail.
                Current issue title: {request.IssueTitle}
                Current issue body: {request.IssueBody}
                """;

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(request.Question)
            };

            var response = await client.CompleteChatAsync(messages);
            return new AiResponse { Success = true, Content = response.Value.Content[0].Text };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error answering question");
            return new AiResponse { Success = false, Error = ex.Message };
        }
    }

    public async Task<AiResponse> AnalyzeImageAsync(AiImageAnalysisRequest request)
    {
        var client = CreateChatClient();
        if (client == null)
            return new AiResponse { Success = false, Error = "AI service is not configured. Please add OpenAI:ApiKey to your configuration." };

        try
        {
            var systemPrompt = $"""
                You are an expert software engineer analyzing images to help write GitHub issues.
                The issue is for the repository: {request.RepositoryFullName}.
                Analyze the provided image and describe what you see in the context of the issue.
                Provide a detailed description that can be added to the issue body to clarify the problem.
                """;

            var imageBytes = Convert.FromBase64String(request.ImageBase64);
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(
                    ChatMessageContentPart.CreateTextPart($"Issue Title: {request.IssueTitle}\n\nCurrent Issue Body: {request.IssueBody}\n\nPlease analyze this image and provide a description for the GitHub issue:"),
                    ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(imageBytes), request.MimeType)
                )
            };

            var response = await client.CompleteChatAsync(messages);
            return new AiResponse { Success = true, Content = response.Value.Content[0].Text };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing image");
            return new AiResponse { Success = false, Error = ex.Message };
        }
    }
}
