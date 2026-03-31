using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FeatureBuilder.Web.Pages;

public class LoginModel : PageModel
{
    private readonly IConfiguration _configuration;

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public bool IsGitHubConfigured { get; private set; }

    public LoginModel(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IActionResult OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        IsGitHubConfigured = !string.IsNullOrWhiteSpace(_configuration["GitHub:ClientId"])
                          && !string.IsNullOrWhiteSpace(_configuration["GitHub:ClientSecret"]);

        if (User.Identity?.IsAuthenticated == true)
            return RedirectToPage("/Dashboard");

        return Page();
    }

    public IActionResult OnGetChallenge(string? returnUrl = null)
    {
        var redirectUrl = returnUrl ?? "/Dashboard";
        return Challenge(new AuthenticationProperties { RedirectUri = redirectUrl }, "GitHub");
    }
}
