using Octokit;
using FeatureBuilder.Web.Models;

namespace FeatureBuilder.Web.Services;

public class GitHubService : IGitHubService
{
    private readonly ILogger<GitHubService> _logger;
    private const string ProductHeader = "FeatureBuilder";

    public GitHubService(ILogger<GitHubService> logger)
    {
        _logger = logger;
    }

    private GitHubClient CreateClient(string accessToken)
    {
        var client = new GitHubClient(new ProductHeaderValue(ProductHeader));
        client.Credentials = new Credentials(accessToken);
        return client;
    }

    public async Task<IReadOnlyList<IssueViewModel>> GetUserIssuesAsync(string accessToken)
    {
        try
        {
            var client = CreateClient(accessToken);
            var request = new IssueRequest
            {
                Filter = IssueFilter.Created,
                State = ItemStateFilter.All,
                SortProperty = IssueSort.Created,
                SortDirection = SortDirection.Descending
            };
            var issues = await client.Issue.GetAllForCurrent(request);
            return issues.Select(i => MapIssue(i)).ToList().AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user issues");
            return Array.Empty<IssueViewModel>();
        }
    }

    public async Task<IReadOnlyList<RepositoryViewModel>> GetUserRepositoriesAsync(string accessToken)
    {
        try
        {
            var client = CreateClient(accessToken);
            var repos = await client.Repository.GetAllForCurrent(new RepositoryRequest
            {
                Sort = RepositorySort.Updated,
                Direction = SortDirection.Descending,
                Type = RepositoryType.All
            });
            return repos.Select(r => new RepositoryViewModel
            {
                Id = r.Id,
                Name = r.Name,
                FullName = r.FullName,
                Description = r.Description ?? string.Empty,
                IsPrivate = r.Private,
                HasIssues = r.HasIssues
            }).ToList().AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user repositories");
            return Array.Empty<RepositoryViewModel>();
        }
    }

    public async Task<IssueViewModel> CreateIssueAsync(string accessToken, string owner, string repo, string title, string body, List<string>? labels = null)
    {
        var client = CreateClient(accessToken);
        var newIssue = new NewIssue(title) { Body = body };
        if (labels != null)
        {
            foreach (var label in labels)
                newIssue.Labels.Add(label);
        }
        var issue = await client.Issue.Create(owner, repo, newIssue);
        return MapIssue(issue, $"{owner}/{repo}");
    }

    public async Task<string?> GetRepositoryReadmeAsync(string accessToken, string owner, string repo)
    {
        try
        {
            var client = CreateClient(accessToken);
            var readme = await client.Repository.Content.GetReadme(owner, repo);
            return readme.Content;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not retrieve README for {Owner}/{Repo}", owner, repo);
            return null;
        }
    }

    public async Task<IReadOnlyList<RepositoryContent>> GetRepositoryContentsAsync(string accessToken, string owner, string repo, string path = "")
    {
        try
        {
            var client = CreateClient(accessToken);
            return string.IsNullOrEmpty(path)
                ? await client.Repository.Content.GetAllContents(owner, repo)
                : await client.Repository.Content.GetAllContents(owner, repo, path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not retrieve repository contents for {Owner}/{Repo}/{Path}", owner, repo, path);
            return Array.Empty<RepositoryContent>();
        }
    }

    private static IssueViewModel MapIssue(Issue issue, string? repoFullName = null)
    {
        var repoName = repoFullName ?? issue.Repository?.FullName ?? string.Empty;
        return new IssueViewModel
        {
            Id = issue.Id,
            Number = issue.Number,
            Title = issue.Title,
            Body = issue.Body ?? string.Empty,
            State = issue.State.StringValue,
            HtmlUrl = issue.HtmlUrl,
            RepositoryName = repoName.Contains('/') ? repoName.Split('/')[1] : repoName,
            RepositoryFullName = repoName,
            CreatedAt = issue.CreatedAt,
            UpdatedAt = issue.UpdatedAt,
            CommentsCount = issue.Comments,
            Labels = issue.Labels?.Select(l => l.Name).ToList() ?? new List<string>()
        };
    }
}
