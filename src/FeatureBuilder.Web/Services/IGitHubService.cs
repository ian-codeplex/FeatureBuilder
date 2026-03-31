using Octokit;
using FeatureBuilder.Web.Models;

namespace FeatureBuilder.Web.Services;

public interface IGitHubService
{
    Task<IReadOnlyList<IssueViewModel>> GetUserIssuesAsync(string accessToken);
    Task<IReadOnlyList<RepositoryViewModel>> GetUserRepositoriesAsync(string accessToken);
    Task<IssueViewModel> CreateIssueAsync(string accessToken, string owner, string repo, string title, string body, List<string>? labels = null);
    Task<string?> GetRepositoryReadmeAsync(string accessToken, string owner, string repo);
    Task<IReadOnlyList<RepositoryContent>> GetRepositoryContentsAsync(string accessToken, string owner, string repo, string path = "");
}
