using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using ReleaseNotesGenerator.Models;
using ReleaseNotesGenerator.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TextCopy;

namespace ReleaseNotesGenerator.UI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly GitLabService _gitLabService;
    private readonly ReleaseNoteGeneratorService _generatorService;
    private readonly MistralService? _mistralService;

    [ObservableProperty]
    private string _projectNameOrId = string.Empty;

    [ObservableProperty]
    private ObservableCollection<GitLabProject> _projects = new();

    [ObservableProperty]
    private GitLabProject? _selectedProject;

    [ObservableProperty]
    private ObservableCollection<string> _tags = new();

    [ObservableProperty]
    private string? _selectedFromTag;

    [ObservableProperty]
    private string? _selectedToTag;

    [ObservableProperty]
    private string _markdownOutput = string.Empty;

    [ObservableProperty]
    private string _improvedMarkdownOutput = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "Prêt";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _useDefaultTags = true;

    [ObservableProperty]
    private bool _isProjectDropDownOpen = false;

    [ObservableProperty]
    private bool _focusProjectComboBox = false;

    public MainWindowViewModel()
    {
        var config = LoadConfiguration();

        if (string.IsNullOrEmpty(config.GitLab.BaseUrl) || string.IsNullOrEmpty(config.GitLab.ApiToken))
        {
            StatusMessage = "⚠️ Veuillez configurer appsettings.json avec vos identifiants GitLab";
            _gitLabService = new GitLabService("", "");
        }
        else
        {
            _gitLabService = new GitLabService(config.GitLab.BaseUrl, config.GitLab.ApiToken);
            StatusMessage = "✅ Prêt - Entrez un nom ou un ID de projet";
        }

        _generatorService = new ReleaseNoteGeneratorService();

        // Initialize Mistral service if API key is configured
        if (!string.IsNullOrEmpty(config.Mistral.ApiKey))
        {
            _mistralService = new MistralService(config.Mistral.ApiKey, config.Mistral.Model);
        }
    }

    [RelayCommand]
    private async Task SearchProject()
    {
        if (string.IsNullOrWhiteSpace(ProjectNameOrId))
        {
            StatusMessage = "Veuillez entrer un nom ou un ID de projet";
            return;
        }

        IsLoading = true;
        StatusMessage = "🔍 Recherche du projet...";
        Projects.Clear();
        Tags.Clear();
        MarkdownOutput = string.Empty;
        IsProjectDropDownOpen = false;
        FocusProjectComboBox = false;

        try
        {
            // Check if it's an ID or name
            if (int.TryParse(ProjectNameOrId, out int projectId))
            {
                var project = await _gitLabService.GetProjectByIdAsync(projectId);
                if (project != null)
                {
                    Projects.Add(project);
                    SelectedProject = project;
                    StatusMessage = $"✅ Projet trouvé : {project.PathWithNamespace}";
                    await LoadTags();
                }
                else
                {
                    StatusMessage = $"❌ Projet avec l'ID {projectId} non trouvé";
                }
            }
            else
            {
                var projects = await _gitLabService.SearchProjectsAsync(ProjectNameOrId);
                foreach (var p in projects)
                {
                    Projects.Add(p);
                }

                if (Projects.Count == 1)
                {
                    SelectedProject = Projects[0];
                    StatusMessage = $"✅ Projet trouvé : {SelectedProject.PathWithNamespace}";
                    await LoadTags();
                }
                else if (Projects.Count > 1)
                {
                    StatusMessage = $"✅ {Projects.Count} projets trouvés - Sélectionnez-en un";
                    // Delay to ensure ComboBox is visible before focusing
                    await Task.Delay(100);
                    FocusProjectComboBox = true;
                    IsProjectDropDownOpen = true;
                }
                else
                {
                    StatusMessage = "❌ Aucun projet trouvé";
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Erreur : {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedProjectChanged(GitLabProject? value)
    {
        if (value != null)
        {
            _ = LoadTags();
        }
        OnPropertyChanged(nameof(IsProjectSelected));
    }

    public bool IsProjectSelected => SelectedProject != null;

    private async Task LoadTags()
    {
        if (SelectedProject == null) return;

        IsLoading = true;
        StatusMessage = "📋 Chargement des tags...";
        Tags.Clear();

        try
        {
            var tags = await _gitLabService.GetTagsAsync(SelectedProject.Id);
            foreach (var tag in tags)
            {
                Tags.Add(tag);
            }

            StatusMessage = $"✅ {Tags.Count} tags chargés";

            // Suggest last 2 tags by default
            if (UseDefaultTags && Tags.Count >= 2)
            {
                SelectedFromTag = Tags[1]; // Previous tag
                SelectedToTag = Tags[0]; // Latest tag
                StatusMessage += " - Les 2 derniers tags sélectionnés par défaut";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Erreur lors du chargement des tags : {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task GenerateReleaseNotes()
    {
        if (SelectedProject == null)
        {
            StatusMessage = "❌ Veuillez sélectionner un projet d'abord";
            return;
        }

        if (string.IsNullOrEmpty(SelectedFromTag))
        {
            StatusMessage = "❌ Veuillez sélectionner un tag de départ";
            return;
        }

        IsLoading = true;
        StatusMessage = "📝 Génération des release notes...";
        MarkdownOutput = string.Empty;
        ImprovedMarkdownOutput = string.Empty; // Reset improved version

        try
        {
            // Get commits
            var commits = await _gitLabService.GetCommitsBetweenRefsAsync(
                SelectedProject.Id,
                SelectedFromTag,
                SelectedToTag);

            StatusMessage = $"✅ {commits.Count} commits trouvés";

            // Get merge requests between tags (using commit dates)
            DateTime fromDate;
            DateTime toDate;

            if (commits.Count > 0)
            {
                // Les commits sont triés du plus récent au plus ancien
                // On prend la date du plus ancien et du plus récent
                fromDate = commits.Min(c => c.Date).AddDays(-1); // -1 jour de marge
                toDate = commits.Max(c => c.Date).AddDays(1); // +1 jour de marge
            }
            else
            {
                // Fallback si pas de commits
                fromDate = DateTime.Now.AddMonths(-6);
                toDate = DateTime.Now;
            }

            var mergeRequests = await _gitLabService.GetMergedMergeRequestsAsync(
                SelectedProject.Id,
                fromDate,
                toDate);

            StatusMessage = $"✅ {commits.Count} commits et {mergeRequests.Count} MRs trouvés";

            // Generate release notes
            var releaseNote = _generatorService.GenerateReleaseNote(
                commits,
                mergeRequests,
                SelectedFromTag,
                SelectedToTag);

            MarkdownOutput = _generatorService.GenerateMarkdown(releaseNote);

            StatusMessage = $"✅ Release notes générées - {commits.Count} commits traités";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Erreur : {ex.Message}";
            MarkdownOutput = $"Erreur lors de la génération des release notes :\n{ex.Message}\n\n{ex.StackTrace}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CopyToClipboard()
    {
        if (string.IsNullOrWhiteSpace(MarkdownOutput))
        {
            StatusMessage = "❌ Aucune release note à copier";
            return;
        }

        try
        {
            await ClipboardService.SetTextAsync(MarkdownOutput);
            StatusMessage = "✅ Version brute copiée dans le presse-papiers !";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Erreur lors de la copie : {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task CopyImprovedToClipboard()
    {
        if (string.IsNullOrWhiteSpace(ImprovedMarkdownOutput))
        {
            StatusMessage = "❌ Aucune version améliorée à copier";
            return;
        }

        try
        {
            await ClipboardService.SetTextAsync(ImprovedMarkdownOutput);
            StatusMessage = "✅ Version améliorée copiée dans le presse-papiers !";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Erreur lors de la copie : {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SaveToFile()
    {
        // Prioriser la version améliorée si elle existe
        var contentToSave = !string.IsNullOrWhiteSpace(ImprovedMarkdownOutput)
            ? ImprovedMarkdownOutput
            : MarkdownOutput;

        if (string.IsNullOrWhiteSpace(contentToSave))
        {
            StatusMessage = "❌ Aucune release note à sauvegarder";
            return;
        }

        try
        {
            var suffix = !string.IsNullOrWhiteSpace(ImprovedMarkdownOutput) ? "_improved" : "";
            var fileName = $"RELEASE_NOTES_{SelectedToTag ?? "HEAD"}_{DateTime.Now:yyyyMMdd_HHmmss}{suffix}.md";
            await File.WriteAllTextAsync(fileName, contentToSave);
            StatusMessage = $"✅ Sauvegardé dans {fileName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Erreur lors de la sauvegarde : {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task GenerateImprovedVersion()
    {
        if (string.IsNullOrWhiteSpace(MarkdownOutput))
        {
            StatusMessage = "❌ Aucune release note à améliorer";
            return;
        }

        if (_mistralService == null)
        {
            StatusMessage = "❌ Service Mistral non configuré. Vérifiez appsettings.json";
            return;
        }

        IsLoading = true;
        StatusMessage = "🤖 Génération de la version améliorée avec Mistral AI...";

        try
        {
            ImprovedMarkdownOutput = await _mistralService.ImproveReleaseNotesAsync(MarkdownOutput);
            StatusMessage = "✅ Version améliorée générée avec succès !";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Erreur Mistral AI : {ex.Message}";
            ImprovedMarkdownOutput = string.Empty;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CopyForAI()
    {
        if (string.IsNullOrWhiteSpace(MarkdownOutput))
        {
            StatusMessage = "❌ Aucune release note à copier";
            return;
        }

        try
        {
            var prompt = $@"Je veux que tu améliores ces release notes GitLab pour les rendre plus professionnelles et claires.

Objectifs :
1. Réécrire les commits en descriptions claires et compréhensibles
2. Grouper les changements similaires ensemble
3. Garder la structure markdown existante (titres, sections, etc.)
4. Ajouter des emojis appropriés
5. Être concis mais informatif
6. Garder les références aux tickets JIRA et MRs

Voici les release notes brutes :

---

{MarkdownOutput}

---

Merci de me retourner uniquement le markdown amélioré, sans commentaires additionnels.";

            await ClipboardService.SetTextAsync(prompt);
            StatusMessage = "✅ Prompt copié ! Collez-le dans Claude ou ChatGPT, puis collez la réponse dans la zone 'Version Améliorée'";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Erreur lors de la copie : {ex.Message}";
        }
    }

    private AppSettings LoadConfiguration()
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

        if (!File.Exists(configPath))
        {
            return new AppSettings
            {
                GitLab = new GitLabSettings
                {
                    BaseUrl = "",
                    ApiToken = ""
                }
            };
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        return configuration.Get<AppSettings>() ?? new AppSettings();
    }
}