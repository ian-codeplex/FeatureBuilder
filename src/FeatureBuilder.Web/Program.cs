using FeatureBuilder.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Register application services
builder.Services.AddHttpClient();
builder.Services.AddScoped<IGitHubService, GitHubService>();
builder.Services.AddScoped<IOpenAIService, OpenAIService>();
builder.Services.AddScoped<IFileParsingService, FileParsingService>();

var gitHubClientId = builder.Configuration["GitHub:ClientId"] ?? string.Empty;
var gitHubClientSecret = builder.Configuration["GitHub:ClientSecret"] ?? string.Empty;
var gitHubConfigured = !string.IsNullOrWhiteSpace(gitHubClientId) && !string.IsNullOrWhiteSpace(gitHubClientSecret);

// Configure GitHub OAuth authentication
var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = gitHubConfigured ? "GitHub" : CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/Login";
    options.LogoutPath = "/Account/Logout";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

if (gitHubConfigured)
{
    authBuilder.AddOAuth("GitHub", options =>
    {
        options.ClientId = gitHubClientId;
        options.ClientSecret = gitHubClientSecret;
        options.CallbackPath = new PathString("/signin-github");

        options.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
        options.TokenEndpoint = "https://github.com/login/oauth/access_token";
        options.UserInformationEndpoint = "https://api.github.com/user";

        options.Scope.Add("user:email");
        options.Scope.Add("repo");
        options.Scope.Add("read:org");

        options.SaveTokens = true;

        options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
        options.ClaimActions.MapJsonKey(ClaimTypes.Name, "login");
        options.ClaimActions.MapJsonKey("urn:github:name", "name");
        options.ClaimActions.MapJsonKey("urn:github:email", "email");
        options.ClaimActions.MapJsonKey("urn:github:avatar", "avatar_url");
        options.ClaimActions.MapJsonKey("urn:github:url", "html_url");

        options.Events = new OAuthEvents
        {
            OnCreatingTicket = async context =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
                request.Headers.UserAgent.Add(new ProductInfoHeaderValue("FeatureBuilder", "1.0"));

                var response = await context.Backchannel.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.HttpContext.RequestAborted);
                response.EnsureSuccessStatusCode();

                var user = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                context.RunClaimActions(user.RootElement);

                // Store the access token in session so we can use it for API calls
                if (context.AccessToken != null)
                {
                    context.HttpContext.Session.SetString("GitHubAccessToken", context.AccessToken);
                }
            }
        };
    });
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

// GitHub OAuth challenge endpoint
app.MapGet("/challenge-github", (HttpContext ctx, string? returnUrl) =>
{
    var redirectUrl = string.IsNullOrEmpty(returnUrl) ? "/Dashboard" : returnUrl;
    return Results.Challenge(new AuthenticationProperties { RedirectUri = redirectUrl }, new[] { "GitHub" });
});

app.Run();
