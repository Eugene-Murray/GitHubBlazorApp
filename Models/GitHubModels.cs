using System.Text.Json.Serialization;

namespace GitHubBlazorApp.Models;

public sealed class GitHubUser
{
    [JsonPropertyName("login")]
    public string Login { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("public_repos")]
    public int PublicRepos { get; set; }
}

public sealed class GitHubRepository
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("private")]
    public bool IsPrivate { get; set; }

    [JsonPropertyName("default_branch")]
    public string DefaultBranch { get; set; } = "main";

    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; } = string.Empty;

    [JsonIgnore]
    public string Owner => FullName.Contains('/') ? FullName.Split('/')[0] : "";
}

public sealed class GitHubBranch
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("commit")]
    public CommitInfo? Commit { get; set; }

    public sealed class CommitInfo
    {
        [JsonPropertyName("sha")]
        public string Sha { get; set; } = string.Empty;
    }
}
