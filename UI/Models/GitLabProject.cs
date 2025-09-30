namespace ReleaseNotesGenerator.Models;

public class GitLabProject
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PathWithNamespace { get; set; } = string.Empty;
    public string WebUrl { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}