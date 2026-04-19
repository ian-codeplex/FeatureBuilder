# FeatureBuilder

A .NET 8 web application that allows you to log in with your GitHub account, view your issues dashboard, and create new GitHub issues enhanced with AI-powered tools.

## Features

- **GitHub OAuth Login** — Sign in securely using your GitHub account
- **Issues Dashboard** — View all issues you've created across your repositories, filtered by open/closed state
- **AI-Powered Issue Creator** — Create new issues with intelligent assistance:
  - 🤖 **AI Description Improvement** — Automatically enhance your description with structured sections (steps to reproduce, expected vs actual behavior, etc.)
  - 💬 **AI Q&A Assistant** — Ask questions to elaborate your issue based on repository context
  - 🖼️ **Image Analysis** — Upload screenshots and let AI describe them for the issue (vision-capable models)
  - 📎 **Attachment Parsing** — Upload Word documents, PDFs, text files and extract their content into the issue

## Tech Stack

- **ASP.NET Core 8** with Razor Pages
- **Tailwind CSS** (via CDN)
- **Octokit** — GitHub API .NET client
- **Azure.AI.OpenAI** — OpenAI and Azure OpenAI integration
- **Azure.AI.Inference** — Azure AI Foundry and GitHub Models integration
- **PdfPig** — PDF text extraction
- **DocumentFormat.OpenXml** — Word (.docx) text extraction

## Setup

### 1. Create a GitHub OAuth App

1. Go to [GitHub → Settings → Developer settings → OAuth Apps](https://github.com/settings/applications/new)
2. Set **Homepage URL** to `http://localhost:5246`
3. Set **Authorization callback URL** to `http://localhost:5246/signin-github`
4. Copy the **Client ID** and **Client Secret**

### 2. Configure the Application

Add the following to `src/FeatureBuilder.Web/appsettings.json` or use [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) / environment variables:

```json
{
  "GitHub": {
    "ClientId": "your-github-client-id",
    "ClientSecret": "your-github-client-secret"
  },
  "AI": {
    "Provider": "OpenAI",
    "ApiKey": "your-api-key",
    "Endpoint": "",
    "Model": "gpt-4o"
  }
}
```

#### Supported AI Providers

| Provider | `AI:Provider` value | `AI:ApiKey` | `AI:Endpoint` | `AI:Model` example | Vision |
|---|---|---|---|---|---|
| **OpenAI** | `OpenAI` | OpenAI API key (`sk-…`) | *(leave blank)* | `gpt-4o` | ✅ |
| **Azure OpenAI** | `AzureOpenAI` | Azure resource key | `https://your-resource.openai.azure.com/` | `gpt-4o` (deployment name) | ✅ |
| **Azure AI Foundry** | `AzureAIFoundry` | AI Foundry project key | `https://your-project.services.ai.azure.com/models` | `gpt-4o` | ✅ |
| **GitHub Models** | `GitHubModels` | GitHub PAT (`ghp_…`) | *(leave blank)* | `gpt-4o` | ✅ |
| **Anthropic** | `Anthropic` | Anthropic API key (`sk-ant-…`) | *(leave blank)* | `claude-3-5-sonnet-20241022` | ✅ |
| **Ollama** (local) | `Ollama` | *(leave blank)* | `http://localhost:11434` | `llama3.2` | ✅ (llava) |

#### Provider-specific examples

<details>
<summary>OpenAI</summary>

```json
"AI": { "Provider": "OpenAI", "ApiKey": "sk-...", "Model": "gpt-4o" }
```
</details>

<details>
<summary>Azure OpenAI</summary>

```json
"AI": {
  "Provider": "AzureOpenAI",
  "ApiKey": "your-azure-key",
  "Endpoint": "https://your-resource.openai.azure.com/",
  "Model": "gpt-4o"
}
```
</details>

<details>
<summary>Azure AI Foundry</summary>

Deploy a model in [Azure AI Foundry](https://ai.azure.com), then copy the endpoint and key from the project settings.

```json
"AI": {
  "Provider": "AzureAIFoundry",
  "ApiKey": "your-project-key",
  "Endpoint": "https://your-project.services.ai.azure.com/models",
  "Model": "gpt-4o"
}
```
</details>

<details>
<summary>GitHub Models</summary>

Create a [GitHub Personal Access Token](https://github.com/settings/tokens) with the `models:read` permission (or a classic PAT).

```json
"AI": {
  "Provider": "GitHubModels",
  "ApiKey": "ghp_your_token",
  "Model": "gpt-4o"
}
```
</details>

<details>
<summary>Anthropic Claude</summary>

```json
"AI": {
  "Provider": "Anthropic",
  "ApiKey": "sk-ant-...",
  "Model": "claude-3-5-sonnet-20241022"
}
```
</details>

<details>
<summary>Ollama (local)</summary>

Start Ollama locally (`ollama serve`) and pull a model (`ollama pull llama3.2`). For image analysis, pull a vision model (`ollama pull llava`).

```json
"AI": {
  "Provider": "Ollama",
  "Endpoint": "http://localhost:11434",
  "Model": "llama3.2"
}
```
</details>

> **Note:** The AI features are optional. The app works without an AI key, but AI-powered features will be disabled.

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
    │   ├── AiProviderType.cs       # Enum of supported AI providers
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
    │   ├── AiService.cs            # Multi-provider AI integration
    │   ├── IAiService.cs           # AI service interface
    │   ├── GitHubService.cs        # GitHub API integration
    │   └── FileParsingService.cs   # PDF/Word/text extraction
    └── Program.cs                  # App configuration
```
