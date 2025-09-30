namespace ReleaseNotesGenerator.Models;

public class AppSettings
{
    public GitLabSettings GitLab { get; set; } = new();
}

public class GitLabSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiToken { get; set; } = string.Empty;
}