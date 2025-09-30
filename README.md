# 📝 Release Notes Generator

Automatic release notes generator from GitLab with smart project search.

> **🎨 Two modes available**: Modern GUI (Avalonia) and CLI (Console)

## 🎯 Features

### Core Features
- ✅ **GitLab Project Search**: Automatically search and select project by name or ID
- ✅ **GitLab Commits Analysis**: Parse commits between 2 tags
- ✅ **Automatic Categorization**: feat, fix, refactor, docs, test, chore
- ✅ **JIRA Ticket Extraction**: Automatically detects FTELINFO-123, SAS-456, etc.
- ✅ **GitLab MRs Integration**: Retrieves associated Merge Requests
- ✅ **Smart Tag Selection**: Automatically suggests last 2 releases
- ✅ **Markdown Generation**: Copy/paste ready format
- ✅ **Automatic Clipboard Copy**

### GUI Mode (UI/)
- ✅ **Modern Dark Interface** with Avalonia UI
- ✅ **Real-time Search** with Enter key support
- ✅ **Two-panel Layout**: Raw and AI-improved markdown
- ✅ **AI Integration**: Mistral AI for automatic improvement
- ✅ **Keyboard Navigation**: Full keyboard support
- ✅ **French Interface**: Complete French translation
- ✅ **MR Filtering**: Smart merge request filtering by commit dates

### CLI Mode (ReleaseNotesGenerator/)
- ✅ **Interactive Menu** with Spectre.Console
- ✅ **Command Line Arguments**: Batch mode support

## 📋 Prerequisites

- .NET 9.0 SDK
- GitLab API Token
- Mistral AI API Key (optional, for GUI AI improvement)
- ✅ **No local Git needed** - Everything fetched from GitLab!

## 🔐 Configuration

### 1. Get GitLab API Token

1. Go to your GitLab instance: `https://your-gitlab.com/-/profile/personal_access_tokens`
2. Create a new token with scopes:
   - `read_api`
   - `read_repository`
3. Copy the token

### 2. Get Mistral AI API Key (Optional - GUI only)

1. Go to [Mistral AI Console](https://console.mistral.ai/)
2. Create an account and generate an API key
3. Copy the key

### 3. Configure the tool

**For CLI mode** (`ReleaseNotesGenerator/`):
1. Copy `appsettings.example.json` to `appsettings.json`
2. Edit with your GitLab parameters:

```json
{
  "GitLab": {
    "BaseUrl": "https://your-gitlab.com",
    "ApiToken": "YOUR_GITLAB_API_TOKEN"
  }
}
```

**For GUI mode** (`UI/`):
1. Copy `UI/appsettings.example.json` to `UI/appsettings.json`
2. Edit with your parameters:

```json
{
  "GitLab": {
    "BaseUrl": "https://your-gitlab.com",
    "ApiToken": "YOUR_GITLAB_API_TOKEN"
  },
  "Mistral": {
    "ApiKey": "YOUR_MISTRAL_API_KEY",
    "Model": "mistral-large-latest"
  }
}
```

## 🚀 Usage

### 🎨 GUI Mode (Modern Interface)

```bash
cd UI
dotnet run
```

**Features:**
1. **Search Project**: Enter project name or ID, press Enter
2. **Select Project**: If multiple results, select from dropdown
3. **Tag Selection**:
   - Default: Uses last 2 tags automatically
   - Manual: Uncheck "Use last 2 tags" to select specific tags
4. **Generate**: Click "Générer les Release Notes" or press Enter
5. **AI Improvement**: Click "Générer avec IA" for Mistral AI enhancement
6. **Copy/Save**: Copy to clipboard or save to file

**Keyboard Shortcuts:**
- `Enter` in search field → Search project
- `Enter` on project selection → Generate release notes
- Full keyboard navigation support

### 📟 CLI Mode (Interactive mode)

```bash
cd ReleaseNotesGenerator
dotnet run

# Or with project specified
dotnet run -- --project "Ftello"

# Or with project ID
dotnet run -- --project "123"
```

**The tool will:**
1. Search project in GitLab (by name or ID)
2. List available GitLab tags
3. Suggest by default to compare last 2 releases
   - If you accept, automatically uses these tags
   - If you decline, lets you choose manually
4. Generate release notes
5. Copy to clipboard

### Command line mode

```bash
# Between 2 tags with GitLab project (by name)
dotnet run -- --project "Ftello" --from v1.0.0 --to v1.1.0

# Between 2 tags with GitLab project (by ID)
dotnet run -- --project "123" --from v1.0.0 --to v1.1.0

# From a tag to HEAD
dotnet run -- --project "Hub3E" --from v2.0.0

# With save to file
dotnet run -- --project "Commerce" --from v1.0.0 --output RELEASE_v1.1.0.md
```

### Available Arguments

- `--project`, `-p`: GitLab project name or ID
  - Name: search by name (e.g., "Ftello", "Hub3E")
  - ID: direct search by numeric ID (e.g., "123")
  - ID is found in GitLab under the project name
- `--from`, `-f`: Start tag (required)
- `--to`, `-t`: End tag (optional, default: HEAD)
- `--output`, `-o`: Output file (optional)

## 📊 Example Output

```markdown
# Release Notes - v1.1.0

**Date**: September 30, 2025
**Range**: `v1.0.0` → `v1.1.0`
**Commits**: 42
**Merge Requests**: 8

## 🎉 Features
- **[FTELINFO-123]** Add user management module ([!456](https://gitlab.com/mr/456)) by @mrazafinjato
- **[SAS-789]** Integrate HUB3E API (`a1b2c3d`)

## 🐛 Bug Fixes
- **[COMMERCE-456]** Fix VAT calculation ([!457](https://gitlab.com/mr/457))
- Fix crash on Windows 11 startup (`d4e5f6g`)

## 🔧 Refactoring
- Simplify service layer (`h7i8j9k`)
- Migrate to .NET 9 (`k1l2m3n`)
```

## 📁 Project Structure

```
ReleaseNotesGenerator/
├── ReleaseNotesGenerator/         # CLI Console Application
│   ├── Models/                    # DTOs (GitCommit, GitLabProject, etc.)
│   ├── Services/
│   │   ├── GitLabService.cs       # GitLab API
│   │   ├── ReleaseNoteGeneratorService.cs  # Markdown generation
│   │   └── MenuService.cs         # Interactive menu
│   ├── Program.cs                 # Entry point
│   └── appsettings.json          # Configuration
│
├── UI/                            # GUI Avalonia Application
│   ├── Models/                    # DTOs (GitCommit, GitLabProject, etc.)
│   ├── Services/
│   │   ├── GitLabService.cs       # GitLab API
│   │   ├── ReleaseNoteGeneratorService.cs  # Markdown generation
│   │   └── MistralService.cs      # Mistral AI integration
│   ├── ViewModels/
│   │   └── MainWindowViewModel.cs # Main view logic
│   ├── Views/
│   │   └── MainWindow.axaml       # Main UI layout
│   ├── Behaviors/
│   │   └── FocusBehavior.cs       # Keyboard navigation
│   ├── App.axaml                  # Application styles
│   ├── Program.cs                 # Entry point
│   └── appsettings.json          # Configuration (with Mistral)
│
└── README.md                      # This file
```

## 🔍 Automatic Detection

### Commit Types

The tool automatically detects commit types:

- `feat:`, `feature:` → 🎉 Features
- `fix:`, `bugfix:` → 🐛 Bug Fixes
- `refactor:` → 🔧 Refactoring
- `docs:`, `doc:` → 📝 Documentation
- `test:`, `tests:` → 🧪 Tests
- `chore:`, `build:`, `ci:` → ⚙️ Chore
- Other → 📦 Other

### JIRA Tickets

Automatically extracts references like:
- `FTELINFO-123`
- `SAS-456`
- `COMMERCE-789`
- `MEDIA-012`
- Etc.

### Merge Requests

Automatically associates commits to GitLab MRs by searching:
- Same JIRA ticket
- Similar title
- Description containing the commit

## 🛠️ Development

### Build

```bash
dotnet build
```

### Publish (standalone executable)

```bash
dotnet publish -c Release -r win-x64 --self-contained
```

The executable will be in: `bin/Release/net9.0/win-x64/publish/`

## 🐛 Troubleshooting

### Error "GitLab configuration missing"

➡️ Check that `appsettings.json` exists and contains your token.

### Error "No GitLab tags found"

➡️ Ensure the project has tags in GitLab.

### Error "401 Unauthorized" (GitLab)

➡️ Verify:
- Your GitLab API token
- Token scopes (`read_api`, `read_repository`)
- GitLab base URL

### No project found

➡️ Verify:
- Project name (try broader search)
- You have access to the project in GitLab
- Token has sufficient rights

## 📦 Packages Used

### CLI Application
- `Newtonsoft.Json` (13.0.3) - JSON serialization
- `RestSharp` (112.1.0) - HTTP REST client
- `TextCopy` (6.2.1) - Clipboard copy
- `Spectre.Console` (0.48.0) - Interactive CLI interface
- `System.CommandLine` (2.0.0-beta4) - Argument parsing
- `Microsoft.Extensions.Configuration.Json` (9.0.0) - Configuration

### GUI Application
- `Avalonia` (11.3.6) - Cross-platform UI framework
- `Avalonia.Themes.Fluent` (11.3.6) - Modern Fluent theme
- `CommunityToolkit.Mvvm` (8.4.0) - MVVM implementation
- `Newtonsoft.Json` (13.0.3) - JSON serialization
- `RestSharp` (112.1.0) - HTTP REST client
- `TextCopy` (6.2.1) - Clipboard copy
- `Microsoft.Extensions.Configuration.Json` (9.0.0) - Configuration

## 💡 Tips

1. **GUI for daily use**: Use the modern UI for quick, visual release notes generation
2. **CLI for automation**: Use command line mode for scripts and CI/CD pipelines
3. **AI improvement**: Let Mistral AI polish your release notes for professional communication
4. **Fast mode**: Tool automatically suggests comparing last 2 releases
5. **Search by ID**: Use project ID for faster selection (visible in GitLab)
6. **Commit convention**: Use prefixes (`feat:`, `fix:`, etc.) for better categorization
7. **JIRA tickets**: Always mention ticket in commit message
8. **Regular tags**: Tag your versions regularly to facilitate generation
9. **Descriptive MRs**: Fill titles and descriptions of your Merge Requests properly
10. **Keyboard navigation**: Use Enter key and Tab for faster workflow in GUI

## 🤝 Contribution

Contributions are welcome! Feel free to open an issue or pull request.

## 📜 License

MIT