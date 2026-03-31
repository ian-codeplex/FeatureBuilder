# FeatureBuilder

A .NET 8 web application that allows you to log in with your GitHub account, view your issues dashboard, and create new GitHub issues enhanced with AI-powered tools.

## Features

- **GitHub OAuth Login** — Sign in securely using your GitHub account
- **Issues Dashboard** — View all issues you've created across your repositories, filtered by open/closed state
- **AI-Powered Issue Creator** — Create new issues with intelligent assistance:
  - 🤖 **AI Description Improvement** — Automatically enhance your description with structured sections (steps to reproduce, expected vs actual behavior, etc.)
  - 💬 **AI Q&A Assistant** — Ask questions to elaborate your issue based on repository context
  - 🖼️ **Image Analysis** — Upload screenshots and let AI describe them for the issue
  - 📎 **Attachment Parsing** — Upload Word documents, PDFs, text files and extract their content into the issue

## Tech Stack

- **ASP.NET Core 8** with Razor Pages
- **Tailwind CSS** (via CDN)
- **Octokit** — GitHub API .NET client
- **Azure.AI.OpenAI** — OpenAI / Azure OpenAI integration
- **PdfPig** — PDF text extraction
- **DocumentFormat.OpenXml** — Word (.docx) text extraction

## Setup

### 1. Create a GitHub OAuth App

1. Go to [GitHub → Settings → Developer settings → OAuth Apps](https://github.com/settings/applications/new)
2. Set **Homepage URL** to `http://localhost:5246`
3. Set **Authorization callback URL** to `http://localhost:5246/signin-github`
4. Copy the **Client ID** and **Client Secret**

### 2. Configure the Application

Add the following to `src/FeatureBuilder.Web/appsettings.json` or use User Secrets / environment variables:

```json
{
  "GitHub": {
    "ClientId": "your-github-client-id",
    "ClientSecret": "your-github-client-secret"
  },
  "OpenAI": {
    "ApiKey": "your-openai-api-key",
    "Endpoint": "",
    "DeploymentOrModel": "gpt-4o"
  }
}
```

> **For Azure OpenAI:** Set `Endpoint` to your Azure OpenAI endpoint URL (e.g., `https://your-resource.openai.azure.com/`) and `DeploymentOrModel` to your deployment name.

> **Note:** The AI features are optional. The app works without an OpenAI key, but AI-powered features will be disabled.

### 3. Run the Application

```bash
cd src/FeatureBuilder.Web
dotnet run
```

The application will be available at `http://localhost:5246`.

## Project Structure

```
src/
└── FeatureBuilder.Web/
    ├── Controllers/
    │   ├── AiController.cs         # REST API for AI features
    │   └── AttachmentController.cs # REST API for file parsing
    ├── Models/
    │   └── IssueViewModel.cs       # View models
    ├── Pages/
    │   ├── Account/
    │   │   └── Logout.cshtml       # Sign-out page
    │   ├── Issues/
    │   │   └── Create.cshtml       # Issue creator with AI tools
    │   ├── Shared/
    │   │   └── _Layout.cshtml      # Main layout with Tailwind
    │   ├── Dashboard.cshtml        # Issues dashboard
    │   └── Login.cshtml            # GitHub login page
    ├── Services/
    │   ├── GitHubService.cs        # GitHub API integration
    │   ├── OpenAIService.cs        # AI integration (OpenAI/Azure OpenAI)
    │   └── FileParsingService.cs   # PDF/Word/text extraction
    └── Program.cs                  # App configuration
```
