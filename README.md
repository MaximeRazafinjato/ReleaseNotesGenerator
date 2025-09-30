# 📝 Release Notes Generator

Automatic release notes generator from GitLab with smart project search.

## 🎯 Features

- ✅ **GitLab Project Search**: Automatically search and select project by name or ID
- ✅ **GitLab Commits Analysis**: Parse commits between 2 tags
- ✅ **Automatic Categorization**: feat, fix, refactor, docs, test, chore
- ✅ **JIRA Ticket Extraction**: Automatically detects FTELINFO-123, SAS-456, etc.
- ✅ **GitLab MRs Integration**: Retrieves associated Merge Requests
- ✅ **Smart Tag Selection**: Automatically suggests last 2 releases
- ✅ **Markdown Generation**: Copy/paste ready format
- ✅ **Automatic Clipboard Copy**
- ✅ **Interactive Menu** with Spectre.Console

## 📋 Prerequisites

- .NET 9.0 SDK
- GitLab API Token
- ✅ **No local Git needed** - Everything fetched from GitLab!

## 🔐 Configuration

### 1. Get GitLab API Token

1. Go to your GitLab instance: `https://your-gitlab.com/-/profile/personal_access_tokens`
2. Create a new token with scopes:
   - `read_api`
   - `read_repository`
3. Copy the token

### 2. Configure the tool

1. Copy `appsettings.example.json` to `appsettings.json`
2. Edit `appsettings.json` with your parameters:

```json
{
  "GitLab": {
    "BaseUrl": "https://your-gitlab.com",
    "ApiToken": "YOUR_GITLAB_API_TOKEN"
  }
}
```

## 🚀 Usage

### Interactive mode (recommended)

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
├── Models/              # DTOs (GitCommit, GitLabProject, ReleaseNote, etc.)
├── Services/
│   ├── GitLabService.cs           # GitLab API
│   ├── ReleaseNoteGeneratorService.cs  # Markdown generation
│   └── MenuService.cs             # Interactive menu
├── Program.cs                     # Entry point
├── appsettings.json              # Configuration
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

- `Newtonsoft.Json` (13.0.3) - JSON serialization
- `RestSharp` (112.1.0) - HTTP REST client
- `TextCopy` (6.2.1) - Clipboard copy
- `Spectre.Console` (0.48.0) - Interactive CLI interface
- `System.CommandLine` (2.0.0-beta4) - Argument parsing
- `Microsoft.Extensions.Configuration.Json` (9.0.0) - Configuration

## 💡 Tips

1. **Fast mode**: In interactive mode without arguments, tool automatically suggests comparing last 2 releases
2. **Search by ID**: Use project ID for faster selection (visible in GitLab)
3. **Commit convention**: Use prefixes (`feat:`, `fix:`, etc.) for better categorization
4. **JIRA tickets**: Always mention ticket in commit message
5. **Regular tags**: Tag your versions regularly to facilitate generation
6. **Descriptive MRs**: Fill titles and descriptions of your Merge Requests properly

## 🤝 Contribution

Contributions are welcome! Feel free to open an issue or pull request.

## 📜 License

MIT