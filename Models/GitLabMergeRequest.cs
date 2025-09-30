namespace ReleaseNotesGenerator.Models;

public class GitLabMergeRequest
{
    public int Iid { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public DateTime? MergedAt { get; set; }
    public string WebUrl { get; set; } = string.Empty;
    public List<string> Labels { get; set; } = new();
    public string SourceBranch { get; set; } = string.Empty;
    public string TargetBranch { get; set; } = string.Empty;
    public GitLabUser? Author { get; set; }
    public GitLabUser? MergedBy { get; set; }
}