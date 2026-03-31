using FeatureBuilder.Web.Models;

namespace FeatureBuilder.Web.Services;

public interface IOpenAIService
{
    Task<AiResponse> ImproveIssueDescriptionAsync(AiImproveRequest request);
    Task<AiResponse> AnswerQuestionAsync(AiQuestionRequest request);
    Task<AiResponse> AnalyzeImageAsync(AiImageAnalysisRequest request);
    bool IsConfigured { get; }
}
