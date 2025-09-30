namespace ReleaseNotesGenerator.Models;

public class AppSettings
{
    public GitLabSettings GitLab { get; set; } = new();
    public MistralSettings Mistral { get; set; } = new();
}

public class GitLabSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiToken { get; set; } = string.Empty;
}

public class MistralSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "mistral-large-latest";
}