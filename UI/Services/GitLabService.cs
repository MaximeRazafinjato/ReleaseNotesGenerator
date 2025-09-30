using ReleaseNotesGenerator.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace ReleaseNotesGenerator.Services;

public class GitLabService
{
    private readonly RestClient _client;
    private readonly string _token;

    public GitLabService(string baseUrl, string apiToken)
    {
        _client = new RestClient(baseUrl);
        _token = apiToken;
    }

    public async Task<GitLabProject?> GetProjectByIdAsync(int projectId)
    {
        try
        {
            var request = new RestRequest($"/api/v4/projects/{projectId}");
            request.AddHeader("PRIVATE-TOKEN", _token);

            var response = await _client.ExecuteAsync(request);

            if (!response.IsSuccessful || string.IsNullOrEmpty(response.Content))
                return null;

            var item = JObject.Parse(response.Content);

            return new GitLabProject
            {
                Id = item["id"]?.ToObject<int>() ?? 0,
                Name = item["name"]?.ToString() ?? "",
                PathWithNamespace = item["path_with_namespace"]?.ToString() ?? "",
                WebUrl = item["web_url"]?.ToString() ?? "",
                Description = item["description"]?.ToString() ?? ""
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur récupération projet GitLab par ID: {ex.Message}");
            return null;
        }
    }

    public async Task<List<GitLabProject>> SearchProjectsAsync(string searchTerm)
    {
        try
        {
            var request = new RestRequest("/api/v4/projects");
            request.AddHeader("PRIVATE-TOKEN", _token);
            request.AddQueryParameter("search", searchTerm);
            request.AddQueryParameter("per_page", "20");

            var response = await _client.ExecuteAsync(request);

            if (!response.IsSuccessful || string.IsNullOrEmpty(response.Content))
                return new List<GitLabProject>();

            var json = JArray.Parse(response.Content);
            var projects = new List<GitLabProject>();

            foreach (var item in json)
            {
                projects.Add(new GitLabProject
                {
                    Id = item["id"]?.ToObject<int>() ?? 0,
                    Name = item["name"]?.ToString() ?? "",
                    PathWithNamespace = item["path_with_namespace"]?.ToString() ?? "",
                    WebUrl = item["web_url"]?.ToString() ?? "",
                    Description = item["description"]?.ToString() ?? ""
                });
            }

            return projects;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur recherche projets GitLab: {ex.Message}");
            return new List<GitLabProject>();
        }
    }

    public async Task<List<GitLabMergeRequest>> GetMergedMergeRequestsAsync(int projectId, DateTime sinceDate, DateTime untilDate)
    {
        try
        {
            var request = new RestRequest($"/api/v4/projects/{projectId}/merge_requests");
            request.AddHeader("PRIVATE-TOKEN", _token);
            request.AddQueryParameter("state", "merged");
            request.AddQueryParameter("per_page", "100");

            var response = await _client.ExecuteAsync(request);

            if (!response.IsSuccessful || string.IsNullOrEmpty(response.Content))
                return new List<GitLabMergeRequest>();

            var json = JArray.Parse(response.Content);
            var mergeRequests = new List<GitLabMergeRequest>();

            foreach (var item in json)
            {
                var mergedAt = item["merged_at"]?.ToObject<DateTime?>();

                // Filtrer par date
                if (mergedAt.HasValue && mergedAt.Value >= sinceDate && mergedAt.Value <= untilDate)
                {
                    var mr = new GitLabMergeRequest
                    {
                        Iid = item["iid"]?.ToObject<int>() ?? 0,
                        Title = item["title"]?.ToString() ?? "",
                        Description = item["description"]?.ToString() ?? "",
                        State = item["state"]?.ToString() ?? "",
                        MergedAt = mergedAt,
                        WebUrl = item["web_url"]?.ToString() ?? "",
                        SourceBranch = item["source_branch"]?.ToString() ?? "",
                        TargetBranch = item["target_branch"]?.ToString() ?? ""
                    };

                    // Labels
                    var labels = item["labels"]?.ToObject<List<string>>();
                    if (labels != null)
                        mr.Labels = labels;

                    // Author
                    var author = item["author"];
                    if (author != null)
                    {
                        mr.Author = new GitLabUser
                        {
                            Id = author["id"]?.ToObject<int>() ?? 0,
                            Name = author["name"]?.ToString() ?? "",
                            Username = author["username"]?.ToString() ?? ""
                        };
                    }

                    // Merged by
                    var mergedBy = item["merged_by"];
                    if (mergedBy != null)
                    {
                        mr.MergedBy = new GitLabUser
                        {
                            Id = mergedBy["id"]?.ToObject<int>() ?? 0,
                            Name = mergedBy["name"]?.ToString() ?? "",
                            Username = mergedBy["username"]?.ToString() ?? ""
                        };
                    }

                    mergeRequests.Add(mr);
                }
            }

            return mergeRequests;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur récupération MRs GitLab: {ex.Message}");
            return new List<GitLabMergeRequest>();
        }
    }

    public async Task<List<string>> GetTagsAsync(int projectId)
    {
        try
        {
            var request = new RestRequest($"/api/v4/projects/{projectId}/repository/tags");
            request.AddHeader("PRIVATE-TOKEN", _token);
            request.AddQueryParameter("per_page", "100");

            var response = await _client.ExecuteAsync(request);

            if (!response.IsSuccessful || string.IsNullOrEmpty(response.Content))
                return new List<string>();

            var json = JArray.Parse(response.Content);
            var tags = new List<string>();

            foreach (var item in json)
            {
                var tagName = item["name"]?.ToString();
                if (!string.IsNullOrEmpty(tagName))
                    tags.Add(tagName);
            }

            return tags;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur récupération tags GitLab: {ex.Message}");
            return new List<string>();
        }
    }

    public async Task<List<GitCommit>> GetCommitsBetweenRefsAsync(int projectId, string fromRef, string? toRef = null)
    {
        try
        {
            var to = string.IsNullOrEmpty(toRef) ? "HEAD" : toRef;
            var request = new RestRequest($"/api/v4/projects/{projectId}/repository/compare");
            request.AddHeader("PRIVATE-TOKEN", _token);
            request.AddQueryParameter("from", fromRef);
            request.AddQueryParameter("to", to);

            var response = await _client.ExecuteAsync(request);

            if (!response.IsSuccessful || string.IsNullOrEmpty(response.Content))
                return new List<GitCommit>();

            var json = JObject.Parse(response.Content);
            var commitsArray = json["commits"]?.ToObject<JArray>();

            if (commitsArray == null)
                return new List<GitCommit>();

            var commits = new List<GitCommit>();

            foreach (var item in commitsArray)
            {
                var message = item["message"]?.ToString() ?? "";
                var commit = new GitCommit
                {
                    Hash = item["id"]?.ToString() ?? "",
                    ShortHash = item["short_id"]?.ToString() ?? "",
                    Message = message,
                    Author = item["author_name"]?.ToString() ?? "",
                    Date = item["created_at"]?.ToObject<DateTime>() ?? DateTime.Now,
                    Type = DetectCommitType(message),
                    Scope = ExtractScope(message),
                    JiraTickets = ExtractJiraTickets(message)
                };

                commits.Add(commit);
            }

            return commits;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur récupération commits GitLab: {ex.Message}");
            return new List<GitCommit>();
        }
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
        var match = System.Text.RegularExpressions.Regex.Match(message, @"^\w+\(([^)]+)\):");
        return match.Success ? match.Groups[1].Value : "";
    }

    private List<string> ExtractJiraTickets(string message)
    {
        var matches = System.Text.RegularExpressions.Regex.Matches(message, @"\b([A-Z]+)-(\d+)\b");
        return matches.Select(m => m.Value).Distinct().ToList();
    }
}