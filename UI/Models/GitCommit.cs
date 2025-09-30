namespace ReleaseNotesGenerator.Models;

public class GitCommit
{
    public string Hash { get; set; } = string.Empty;
    public string ShortHash { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public CommitType Type { get; set; }
    public string Scope { get; set; } = string.Empty;
    public List<string> JiraTickets { get; set; } = new();
}

public enum CommitType
{
    Feature,
    BugFix,
    Refactor,
    Documentation,
    Test,
    Chore,
    Other
}