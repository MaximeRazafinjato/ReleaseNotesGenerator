using ReleaseNotesGenerator.Models;
using Spectre.Console;

namespace ReleaseNotesGenerator.Services;

public class MenuService
{
    public GitLabProject? SelectProject(List<GitLabProject> projects)
    {
        if (!projects.Any())
        {
            AnsiConsole.MarkupLine("[red]Aucun projet trouvé.[/]");
            return null;
        }

        if (projects.Count == 1)
        {
            var project = projects[0];
            AnsiConsole.MarkupLine($"[green]✓[/] Projet unique trouvé: [cyan]{project.PathWithNamespace}[/]");
            return project;
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[yellow]{projects.Count} projets trouvés:[/]");
        AnsiConsole.WriteLine();

        var selection = AnsiConsole.Prompt(
            new SelectionPrompt<GitLabProject>()
                .Title("[cyan]Sélectionnez un projet:[/]")
                .PageSize(10)
                .AddChoices(projects)
                .UseConverter(p => $"{p.PathWithNamespace} [dim](ID: {p.Id})[/]"));

        return selection;
    }

    public string? SelectTag(List<string> tags, string prompt)
    {
        if (!tags.Any())
        {
            AnsiConsole.MarkupLine("[red]Aucun tag Git trouvé dans ce repository.[/]");
            return null;
        }

        AnsiConsole.WriteLine();

        var choices = new List<string> { "[Pas de tag / HEAD]" };
        choices.AddRange(tags.Take(50)); // Limiter à 50 tags

        var selection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title(prompt)
                .PageSize(15)
                .AddChoices(choices)
                .UseConverter(tag => Markup.Escape(tag)));

        return selection == "[Pas de tag / HEAD]" ? null : selection;
    }

    public void ShowProgress(string message, Func<Task> action)
    {
        AnsiConsole.Status()
            .Start(message, ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots);
                ctx.SpinnerStyle(Style.Parse("green"));
                action().Wait();
            });
    }

    public async Task ShowProgressAsync(string message, Func<Task> action)
    {
        await AnsiConsole.Status()
            .StartAsync(message, async ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots);
                ctx.SpinnerStyle(Style.Parse("green"));
                await action();
            });
    }
}