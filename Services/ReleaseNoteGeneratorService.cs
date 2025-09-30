using ReleaseNotesGenerator.Models;
using System.Text;

namespace ReleaseNotesGenerator.Services;

public class ReleaseNoteGeneratorService
{
    public ReleaseNote GenerateReleaseNote(
        List<GitCommit> commits,
        List<GitLabMergeRequest> mergeRequests,
        string fromTag,
        string? toTag = null)
    {
        var releaseNote = new ReleaseNote
        {
            FromTag = fromTag,
            ToTag = toTag ?? "HEAD",
            Version = toTag ?? "Unreleased",
            Date = DateTime.Now,
            TotalCommits = commits.Count,
            MergeRequests = mergeRequests
        };

        // Grouper par type
        var sections = new Dictionary<CommitType, (string Title, string Emoji)>
        {
            { CommitType.Feature, ("Features", "🎉") },
            { CommitType.BugFix, ("Bug Fixes", "🐛") },
            { CommitType.Refactor, ("Refactoring", "🔧") },
            { CommitType.Documentation, ("Documentation", "📝") },
            { CommitType.Test, ("Tests", "🧪") },
            { CommitType.Chore, ("Chore", "⚙️") },
            { CommitType.Other, ("Other", "📦") }
        };

        foreach (var (commitType, (title, emoji)) in sections)
        {
            var commitsOfType = commits.Where(c => c.Type == commitType).ToList();
            if (!commitsOfType.Any())
                continue;

            var section = new ReleaseNoteSection
            {
                Title = title,
                Emoji = emoji
            };

            foreach (var commit in commitsOfType)
            {
                // Trouver la MR associée (si elle mentionne le commit ou vice-versa)
                var relatedMr = FindRelatedMergeRequest(commit, mergeRequests);

                var item = new ReleaseNoteItem
                {
                    Description = CleanCommitMessage(commit.Message),
                    JiraTickets = commit.JiraTickets,
                    CommitHash = commit.ShortHash,
                    RelatedMergeRequest = relatedMr
                };

                section.Items.Add(item);
            }

            releaseNote.Sections.Add(section);
        }

        return releaseNote;
    }

    public string GenerateMarkdown(ReleaseNote releaseNote)
    {
        var sb = new StringBuilder();

        // En-tête
        sb.AppendLine($"# Release Notes - {releaseNote.Version}");
        sb.AppendLine();
        sb.AppendLine($"**Date**: {releaseNote.Date:dd MMMM yyyy}");
        sb.AppendLine($"**Range**: `{releaseNote.FromTag}` → `{releaseNote.ToTag}`");
        sb.AppendLine($"**Commits**: {releaseNote.TotalCommits}");

        if (releaseNote.MergeRequests.Any())
        {
            sb.AppendLine($"**Merge Requests**: {releaseNote.MergeRequests.Count}");
        }

        sb.AppendLine();

        // Sections
        foreach (var section in releaseNote.Sections)
        {
            sb.AppendLine($"## {section.Emoji} {section.Title}");
            sb.AppendLine();

            foreach (var item in section.Items)
            {
                var prefix = item.JiraTickets.Any()
                    ? $"**[{string.Join(", ", item.JiraTickets)}]** "
                    : "";

                sb.Append($"- {prefix}{item.Description}");

                if (item.RelatedMergeRequest != null)
                {
                    var mr = item.RelatedMergeRequest;
                    sb.Append($" ([!{mr.Iid}]({mr.WebUrl}))");

                    if (mr.Author != null)
                    {
                        sb.Append($" by @{mr.Author.Username}");
                    }
                }
                else
                {
                    sb.Append($" (`{item.CommitHash}`)");
                }

                sb.AppendLine();
            }

            sb.AppendLine();
        }

        // MRs orphelines (sans commit associé trouvé)
        var orphanMrs = releaseNote.MergeRequests
            .Where(mr => !releaseNote.Sections
                .SelectMany(s => s.Items)
                .Any(i => i.RelatedMergeRequest?.Iid == mr.Iid))
            .ToList();

        if (orphanMrs.Any())
        {
            sb.AppendLine("## 🔗 Other Merge Requests");
            sb.AppendLine();

            foreach (var mr in orphanMrs)
            {
                sb.Append($"- **!{mr.Iid}**: {mr.Title}");

                if (mr.Author != null)
                {
                    sb.Append($" by @{mr.Author.Username}");
                }

                if (mr.Labels.Any())
                {
                    sb.Append($" `{string.Join("`, `", mr.Labels)}`");
                }

                sb.AppendLine();
            }

            sb.AppendLine();
        }

        // Footer
        sb.AppendLine("---");
        sb.AppendLine($"*Generated on {DateTime.Now:yyyy-MM-dd HH:mm:ss}*");

        return sb.ToString();
    }

    private GitLabMergeRequest? FindRelatedMergeRequest(GitCommit commit, List<GitLabMergeRequest> mergeRequests)
    {
        // Chercher une MR qui mentionne le même ticket JIRA
        foreach (var ticket in commit.JiraTickets)
        {
            var mr = mergeRequests.FirstOrDefault(m =>
                m.Title.Contains(ticket, StringComparison.OrdinalIgnoreCase) ||
                m.Description.Contains(ticket, StringComparison.OrdinalIgnoreCase) ||
                m.SourceBranch.Contains(ticket, StringComparison.OrdinalIgnoreCase));

            if (mr != null)
                return mr;
        }

        // Chercher par message de commit similaire
        var cleanCommitMsg = CleanCommitMessage(commit.Message).ToLower();
        return mergeRequests.FirstOrDefault(m =>
            m.Title.ToLower().Contains(cleanCommitMsg) ||
            cleanCommitMsg.Contains(m.Title.ToLower()));
    }

    private string CleanCommitMessage(string message)
    {
        // Enlever le prefix type: (feat:, fix:, etc.)
        var cleaned = System.Text.RegularExpressions.Regex.Replace(message, @"^\w+(\([^)]+\))?:\s*", "");

        // Capitaliser la première lettre
        if (!string.IsNullOrEmpty(cleaned))
        {
            cleaned = char.ToUpper(cleaned[0]) + cleaned.Substring(1);
        }

        return cleaned;
    }
}