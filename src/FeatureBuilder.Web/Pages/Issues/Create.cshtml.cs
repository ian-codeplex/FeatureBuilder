using FeatureBuilder.Web.Models;
using FeatureBuilder.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FeatureBuilder.Web.Pages.Issues;

[Authorize]
public class CreateModel : PageModel
{
    private readonly IGitHubService _gitHubService;
    private readonly IOpenAIService _openAIService;
    private readonly IFileParsingService _fileParsingService;
    private readonly ILogger<CreateModel> _logger;

    public List<RepositoryViewModel> Repositories { get; private set; } = new();
    public bool IsAiConfigured { get; private set; }

    [BindProperty]
    public CreateIssueModel Input { get; set; } = new();

    [BindProperty]
    public List<IFormFile>? Attachments { get; set; }

    public string? SuccessMessage { get; private set; }
    public string? ErrorMessage { get; private set; }

    public CreateModel(
        IGitHubService gitHubService,
        IOpenAIService openAIService,
        IFileParsingService fileParsingService,
        ILogger<CreateModel> logger)
    {
        _gitHubService = gitHubService;
        _openAIService = openAIService;
        _fileParsingService = fileParsingService;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync(string? repo = null)
    {
        var accessToken = HttpContext.Session.GetString("GitHubAccessToken");
        if (string.IsNullOrEmpty(accessToken))
            return RedirectToPage("/Login");

        IsAiConfigured = _openAIService.IsConfigured;
        Repositories = (await _gitHubService.GetUserRepositoriesAsync(accessToken))
            .Where(r => r.HasIssues)
            .ToList();

        if (!string.IsNullOrEmpty(repo))
            Input.RepositoryFullName = repo;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var accessToken = HttpContext.Session.GetString("GitHubAccessToken");
        if (string.IsNullOrEmpty(accessToken))
            return RedirectToPage("/Login");

        IsAiConfigured = _openAIService.IsConfigured;
        Repositories = (await _gitHubService.GetUserRepositoriesAsync(accessToken))
            .Where(r => r.HasIssues)
            .ToList();

        if (!ModelState.IsValid)
            return Page();

        if (string.IsNullOrWhiteSpace(Input.RepositoryFullName))
        {
            ErrorMessage = "Please select a repository.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(Input.Title))
        {
            ErrorMessage = "Please enter an issue title.";
            return Page();
        }

        var parts = Input.RepositoryFullName.Split('/', 2);
        if (parts.Length != 2)
        {
            ErrorMessage = "Invalid repository name format.";
            return Page();
        }

        try
        {
            var issue = await _gitHubService.CreateIssueAsync(
                accessToken, parts[0], parts[1], Input.Title, Input.Body ?? string.Empty);

            SuccessMessage = $"View it on GitHub: <a href=\"{issue.HtmlUrl}\" target=\"_blank\" rel=\"noopener noreferrer\" class=\"underline font-medium\">{issue.RepositoryFullName}#{issue.Number}</a>";
            Input = new CreateIssueModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating issue");
            ErrorMessage = $"Failed to create issue: {ex.Message}";
        }

        return Page();
    }
}
