using FeatureBuilder.Web.Models;

namespace FeatureBuilder.Web.Services;

public interface IAiService
{
    /// <summary>Whether the service has a valid configuration and can make AI calls.</summary>
    bool IsConfigured { get; }

    /// <summary>The active provider type.</summary>
    AiProviderType Provider { get; }

    /// <summary>Human-readable name for the active provider (e.g. "Azure AI Foundry").</summary>
    string ProviderDisplayName { get; }

    /// <summary>Whether the active provider/model supports image (vision) analysis.</summary>
    bool SupportsImageAnalysis { get; }

    Task<AiResponse> ImproveIssueDescriptionAsync(AiImproveRequest request);
    Task<AiResponse> AnswerQuestionAsync(AiQuestionRequest request);
    Task<AiResponse> AnalyzeImageAsync(AiImageAnalysisRequest request);
}
