using ReleaseNotesGenerator.Models;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ReleaseNotesGenerator.Services;

public class GitService
{
    private readonly string _repositoryPath;

    public GitService(string? repositoryPath = null)
    {
        _repositoryPath = repositoryPath ?? Directory.GetCurrentDirectory();
    }

    public async Task<List<string>> GetTagsAsync()
    {
        var output = await ExecuteGitCommandAsync("tag --sort=-creatordate");
        return output.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    public async Task<List<GitCommit>> GetCommitsBetweenTagsAsync(string? fromTag, string? toTag = null)
    {
        var range = string.IsNullOrEmpty(fromTag)
            ? "HEAD"
            : string.IsNullOrEmpty(toTag)
                ? $"{fromTag}..HEAD"
                : $"{fromTag}..{toTag}";

        var format = "%H|%h|%s|%an|%ai";
        var output = await ExecuteGitCommandAsync($"log {range} --pretty=format:\"{format}\"");

        var commits = new List<GitCommit>();
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var parts = line.Split('|');
            if (parts.Length >= 5)
            {
                var message = parts[2];
                var commit = new GitCommit
                {
                    Hash = parts[0],
                    ShortHash = parts[1],
                    Message = message,
                    Author = parts[3],
                    Date = DateTime.Parse(parts[4]),
                    Type = DetectCommitType(message),
                    Scope = ExtractScope(message),
                    JiraTickets = ExtractJiraTickets(message)
                };

                commits.Add(commit);
            }
        }

        return commits;
    }

    public async Task<DateTime?> GetTagDateAsync(string tag)
    {
        try
        {
            var output = await ExecuteGitCommandAsync($"log -1 --format=%ai {tag}");
            if (!string.IsNullOrEmpty(output))
            {
                return DateTime.Parse(output.Trim());
            }
        }
        catch
        {
            // Ignore
        }

        return null;
    }

    private async Task<string> ExecuteGitCommandAsync(string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = _repositoryPath
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0 && !string.IsNullOrEmpty(error))
        {
            throw new Exception($"Git error: {error}");
        }

        return output;
    }

    private CommitType DetectCommitType(string message)
    {
        var lowerMessage = message.ToLower();

        if (lowerMessage.StartsWith("feat:") || lowerMessage.StartsWith("feature:"))
            return CommitType.Feature;

        if (lowerMessage.StartsWith("fix:") || lowerMessage.StartsWith("bugfix:"))
            return CommitType.BugFix;

        if (lowerMessage.StartsWith("refactor:"))
            return CommitType.Refactor;

        if (lowerMessage.StartsWith("docs:") || lowerMessage.StartsWith("doc:"))
            return CommitType.Documentation;

        if (lowerMessage.StartsWith("test:") || lowerMessage.StartsWith("tests:"))
            return CommitType.Test;

        if (lowerMessage.StartsWith("chore:") || lowerMessage.StartsWith("build:") || lowerMessage.StartsWith("ci:"))
            return CommitType.Chore;

        return CommitType.Other;
    }

    private string ExtractScope(string message)
    {
        // Format: feat(scope): message
        var match = Regex.Match(message, @"^\w+\(([^)]+)\):");
        return match.Success ? match.Groups[1].Value : "";
    }

    private List<string> ExtractJiraTickets(string message)
    {
        // Recherche des patterns comme FTELINFO-123, SAS-456, etc.
        var matches = Regex.Matches(message, @"\b([A-Z]+)-(\d+)\b");
        return matches.Select(m => m.Value).Distinct().ToList();
    }
}