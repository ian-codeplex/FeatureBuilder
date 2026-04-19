namespace FeatureBuilder.Web.Models;

public class IssueViewModel
{
    public long Id { get; set; }
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string HtmlUrl { get; set; } = string.Empty;
    public string RepositoryName { get; set; } = string.Empty;
    public string RepositoryFullName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public int CommentsCount { get; set; }
    public List<string> Labels { get; set; } = new();
}

public class RepositoryViewModel
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsPrivate { get; set; }
    public bool HasIssues { get; set; }
}

public class CreateIssueModel
{
    public string RepositoryFullName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public List<string> Labels { get; set; } = new();
}

public class AiImproveRequest
{
    public string RepositoryFullName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? AdditionalContext { get; set; }
}

public class AiQuestionRequest
{
    public string RepositoryFullName { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public string IssueTitle { get; set; } = string.Empty;
    public string IssueBody { get; set; } = string.Empty;
}

public class AiImageAnalysisRequest
{
    public string RepositoryFullName { get; set; } = string.Empty;
    public string IssueTitle { get; set; } = string.Empty;
    public string IssueBody { get; set; } = string.Empty;
    public string ImageBase64 { get; set; } = string.Empty;
    public string MimeType { get; set; } = "image/png";
}

public class AiResponse
{
    public bool Success { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Error { get; set; }
}
