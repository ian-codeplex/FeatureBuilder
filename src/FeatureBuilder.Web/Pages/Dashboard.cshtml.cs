using FeatureBuilder.Web.Models;
using FeatureBuilder.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FeatureBuilder.Web.Pages;

[Authorize]
public class DashboardModel : PageModel
{
    private readonly IGitHubService _gitHubService;
    private readonly ILogger<DashboardModel> _logger;

    public List<IssueViewModel> Issues { get; private set; } = new();
    public List<IssueViewModel> FilteredIssues { get; private set; } = new();
    public int OpenCount { get; private set; }
    public int ClosedCount { get; private set; }

    [BindProperty(SupportsGet = true)]
    public string Filter { get; set; } = "all";

    public DashboardModel(IGitHubService gitHubService, ILogger<DashboardModel> logger)
    {
        _gitHubService = gitHubService;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var accessToken = HttpContext.Session.GetString("GitHubAccessToken");
        if (string.IsNullOrEmpty(accessToken))
            return RedirectToPage("/Login");

        Issues = (await _gitHubService.GetUserIssuesAsync(accessToken)).ToList();
        OpenCount = Issues.Count(i => i.State == "open");
        ClosedCount = Issues.Count(i => i.State == "closed");

        FilteredIssues = Filter switch
        {
            "open" => Issues.Where(i => i.State == "open").ToList(),
            "closed" => Issues.Where(i => i.State == "closed").ToList(),
            _ => Issues
        };

        return Page();
    }
}
