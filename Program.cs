using ReleaseNotesGenerator.Models;
using ReleaseNotesGenerator.Services;
using Microsoft.Extensions.Configuration;
using Spectre.Console;
using TextCopy;
using System.CommandLine;

namespace ReleaseNotesGenerator;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // Configuration
        var config = LoadConfiguration();

        // Arguments
        var projectOption = new Option<string?>(
            aliases: new[] { "--project", "-p" },
            description: "Nom ou ID du projet GitLab");

        var fromOption = new Option<string?>(
            aliases: new[] { "--from", "-f" },
            description: "Tag de début (depuis)");

        var toOption = new Option<string?>(
            aliases: new[] { "--to", "-t" },
            description: "Tag de fin (jusqu'à). Si omis, utilise HEAD");

        var outputOption = new Option<string?>(
            aliases: new[] { "--output", "-o" },
            description: "Fichier de sortie (optionnel)");

        var rootCommand = new RootCommand("Générateur de Release Notes depuis Git et GitLab")
        {
            projectOption,
            fromOption,
            toOption,
            outputOption
        };

        rootCommand.SetHandler(async (project, from, to, output) =>
        {
            await GenerateReleaseNotesAsync(config, project, from, to, output);
        }, projectOption, fromOption, toOption, outputOption);

        return await rootCommand.InvokeAsync(args);
    }

    static async Task GenerateReleaseNotesAsync(AppSettings config, string? projectName, string? fromTag, string? toTag, string? outputFile)
    {
        try
        {
            AnsiConsole.Write(new FigletText("Release Notes").Color(Color.Blue));
            AnsiConsole.WriteLine();

            // Validation de la config
            if (string.IsNullOrEmpty(config.GitLab.BaseUrl) || string.IsNullOrEmpty(config.GitLab.ApiToken))
            {
                AnsiConsole.MarkupLine("[red]❌ Configuration GitLab manquante dans appsettings.json[/]");
                return;
            }

            // Services
            var gitLabService = new GitLabService(config.GitLab.BaseUrl, config.GitLab.ApiToken);
            var menuService = new MenuService();
            var generatorService = new ReleaseNoteGeneratorService();

            // 1. Sélection du projet GitLab
            GitLabProject? selectedProject = null;

            // Demander le nom ou l'ID du projet si non fourni
            if (string.IsNullOrEmpty(projectName))
            {
                AnsiConsole.WriteLine();
                var wantProject = AnsiConsole.Confirm("[cyan]Voulez-vous rechercher un projet GitLab (pour inclure les MRs) ?[/]", defaultValue: true);

                if (wantProject)
                {
                    projectName = AnsiConsole.Ask<string>("[cyan]Entrez le nom ou l'ID du projet:[/]");
                }
            }

            if (!string.IsNullOrEmpty(projectName))
            {
                // Vérifier si c'est un ID (nombre) ou un nom
                if (int.TryParse(projectName, out int projectId))
                {
                    // Recherche par ID
                    AnsiConsole.MarkupLine($"[cyan]🔍 Récupération du projet ID {projectId} depuis GitLab...[/]");

                    await menuService.ShowProgressAsync("Récupération en cours...", async () =>
                    {
                        selectedProject = await gitLabService.GetProjectByIdAsync(projectId);
                    });

                    if (selectedProject != null)
                    {
                        AnsiConsole.MarkupLine($"[green]✓[/] Projet trouvé: [cyan]{selectedProject.PathWithNamespace}[/]");
                    }
                }
                else
                {
                    // Recherche par nom
                    AnsiConsole.MarkupLine($"[cyan]🔍 Recherche du projet '{projectName}' dans GitLab...[/]");

                    List<GitLabProject> projects = new();
                    await menuService.ShowProgressAsync("Recherche en cours...", async () =>
                    {
                        projects = await gitLabService.SearchProjectsAsync(projectName);
                    });

                    selectedProject = menuService.SelectProject(projects);
                }

                if (selectedProject == null)
                {
                    AnsiConsole.MarkupLine("[red]❌ Projet non trouvé ou sélectionné.[/]");
                    return;
                }
            }
            else
            {
                AnsiConsole.MarkupLine("[red]❌ Vous devez sélectionner un projet GitLab.[/]");
                return;
            }

            // 2. Récupération des tags depuis GitLab
            AnsiConsole.MarkupLine("[cyan]📋 Récupération des tags depuis GitLab...[/]");

            List<string> tags = new();
            await menuService.ShowProgressAsync("Récupération des tags...", async () =>
            {
                tags = await gitLabService.GetTagsAsync(selectedProject.Id);
            });

            if (!tags.Any())
            {
                AnsiConsole.MarkupLine("[red]❌ Aucun tag trouvé dans ce projet GitLab.[/]");
                return;
            }

            AnsiConsole.MarkupLine($"[green]✓[/] {tags.Count} tags trouvés");

            // 3. Sélection des tags
            string? selectedFromTag = fromTag;
            string? selectedToTag = toTag;

            // Si aucun tag n'est spécifié en ligne de commande, proposer les 2 derniers tags par défaut
            if (string.IsNullOrEmpty(selectedFromTag) && string.IsNullOrEmpty(selectedToTag))
            {
                if (tags.Count >= 2)
                {
                    var latestTag = tags[0];        // Tag le plus récent (release de fin)
                    var previousTag = tags[1];      // Tag précédent (release de départ)

                    AnsiConsole.WriteLine();
                    var useDefault = AnsiConsole.Confirm(
                        $"[cyan]Comparer les 2 dernières releases : [yellow]{previousTag}[/] → [yellow]{latestTag}[/] ?[/]",
                        defaultValue: true);

                    if (useDefault)
                    {
                        selectedFromTag = previousTag;
                        selectedToTag = latestTag;
                    }
                }
            }

            // Si toujours pas de tags sélectionnés, afficher les menus de sélection
            if (string.IsNullOrEmpty(selectedFromTag))
            {
                selectedFromTag = menuService.SelectTag(tags, "[cyan]Sélectionnez le tag de départ:[/]");
                if (selectedFromTag == null)
                {
                    AnsiConsole.MarkupLine("[red]❌ Tag de départ requis.[/]");
                    return;
                }
            }

            if (string.IsNullOrEmpty(selectedToTag))
            {
                selectedToTag = menuService.SelectTag(tags, "[cyan]Sélectionnez le tag de fin (ou HEAD):[/]");
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[cyan]📊 Génération des release notes:[/]");
            AnsiConsole.MarkupLine($"   De: [yellow]{selectedFromTag}[/]");
            AnsiConsole.MarkupLine($"   À:  [yellow]{selectedToTag ?? "HEAD"}[/]");
            AnsiConsole.WriteLine();

            // 4. Récupération des commits depuis GitLab
            List<GitCommit> commits = new();
            await menuService.ShowProgressAsync("📝 Récupération des commits depuis GitLab...", async () =>
            {
                commits = await gitLabService.GetCommitsBetweenRefsAsync(selectedProject.Id, selectedFromTag, selectedToTag);
            });

            AnsiConsole.MarkupLine($"[green]✓[/] {commits.Count} commits trouvés");

            // 5. Récupération des MRs GitLab
            List<GitLabMergeRequest> mergeRequests = new();

            await menuService.ShowProgressAsync("🔗 Récupération des Merge Requests GitLab...", async () =>
            {
                // Utiliser une plage de dates large (6 mois en arrière)
                var fromDate = DateTime.Now.AddMonths(-6);
                var toDate = DateTime.Now;

                mergeRequests = await gitLabService.GetMergedMergeRequestsAsync(selectedProject.Id, fromDate, toDate);
            });

            AnsiConsole.MarkupLine($"[green]✓[/] {mergeRequests.Count} MRs mergées trouvées");

            // 6. Génération des release notes
            var releaseNote = generatorService.GenerateReleaseNote(commits, mergeRequests, selectedFromTag, selectedToTag);
            var markdown = generatorService.GenerateMarkdown(releaseNote);

            // 7. Affichage
            AnsiConsole.WriteLine();
            var panel = new Panel(Markup.Escape(markdown))
            {
                Header = new PanelHeader("📄 [bold yellow]Release Notes[/]"),
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Green)
            };
            AnsiConsole.Write(panel);

            // 8. Copie dans le presse-papier
            await ClipboardService.SetTextAsync(markdown);
            AnsiConsole.MarkupLine("[green]✅ Copié dans le presse-papier ![/]");

            // 9. Sauvegarde dans un fichier (si spécifié)
            if (!string.IsNullOrEmpty(outputFile))
            {
                await File.WriteAllTextAsync(outputFile, markdown);
                AnsiConsole.MarkupLine($"[green]✅ Sauvegardé dans: {outputFile}[/]");
            }
            else
            {
                // Proposer de sauvegarder
                if (AnsiConsole.Confirm("Voulez-vous sauvegarder dans un fichier ?"))
                {
                    var fileName = $"RELEASE_NOTES_{releaseNote.Version}_{DateTime.Now:yyyyMMdd_HHmmss}.md";
                    await File.WriteAllTextAsync(fileName, markdown);
                    AnsiConsole.MarkupLine($"[green]✅ Sauvegardé dans: {fileName}[/]");
                }
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            AnsiConsole.MarkupLine($"[red]❌ Erreur: {ex.Message}[/]");
        }
    }

    static AppSettings LoadConfiguration()
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

        if (!File.Exists(configPath))
        {
            AnsiConsole.MarkupLine($"[yellow]⚠ appsettings.json introuvable, création du template...[/]");
            CreateDefaultConfig(configPath);
            AnsiConsole.MarkupLine($"[green]✓ Fichier créé: {configPath}[/]");
            AnsiConsole.MarkupLine("[yellow]Veuillez remplir la configuration GitLab et relancer.[/]");
            Environment.Exit(0);
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        return configuration.Get<AppSettings>() ?? new AppSettings();
    }

    static void CreateDefaultConfig(string path)
    {
        var defaultSettings = new AppSettings
        {
            GitLab = new GitLabSettings
            {
                BaseUrl = "https://gitlab.ftel.fr",
                ApiToken = "VOTRE_GITLAB_API_TOKEN"
            }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(defaultSettings, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(path, json);
    }
}