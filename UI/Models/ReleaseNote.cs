namespace ReleaseNotesGenerator.Models;

public class ReleaseNote
{
    public string Version { get; set; } = string.Empty;
    public string FromTag { get; set; } = string.Empty;
    public string ToTag { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int TotalCommits { get; set; }
    public List<ReleaseNoteSection> Sections { get; set; } = new();
    public List<GitLabMergeRequest> MergeRequests { get; set; } = new();
}

public class ReleaseNoteSection
{
    public string Title { get; set; } = string.Empty;
    public string Emoji { get; set; } = string.Empty;
    public List<ReleaseNoteItem> Items { get; set; } = new();
}

public class ReleaseNoteItem
{
    public string Description { get; set; } = string.Empty;
    public List<string> JiraTickets { get; set; } = new();
    public string CommitHash { get; set; } = string.Empty;
    public GitLabMergeRequest? RelatedMergeRequest { get; set; }
}