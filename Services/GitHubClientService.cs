using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using GitHubBlazorApp.Models;

namespace GitHubBlazorApp.Services;

public sealed class GitHubClientService
{
    private readonly HttpClient _httpClient;
    private string? _token;

    public GitHubClientService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://api.github.com/");
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("GitHubBlazorApp/1.0");
    }

    public void SetToken(string token)
    {
        _token = token.Trim();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
    }

    public async Task<GitHubUser> GetCurrentUserAsync()
    {
        EnsureAuthenticated();

        using var response = await _httpClient.GetAsync("user");
        await EnsureSuccessAsync(response);

        var user = await response.Content.ReadFromJsonAsync<GitHubUser>();
        return user ?? throw new InvalidOperationException("GitHub returned an empty user payload.");
    }

    public async Task<List<GitHubRepository>> GetRepositoriesAsync()
    {
        EnsureAuthenticated();

        using var response = await _httpClient.GetAsync("user/repos?per_page=100");
        await EnsureSuccessAsync(response);

        var repos = await response.Content.ReadFromJsonAsync<List<GitHubRepository>>();
        return repos ?? new List<GitHubRepository>();
    }

    public async Task<List<GitHubBranch>> GetBranchesAsync(string owner, string repoName)
    {
        EnsureAuthenticated();

        using var response = await _httpClient.GetAsync($"repos/{owner}/{repoName}/branches");
        await EnsureSuccessAsync(response);

        var branches = await response.Content.ReadFromJsonAsync<List<GitHubBranch>>();
        return branches ?? new List<GitHubBranch>();
    }

    public async Task<GitHubRepository> CreateRepositoryAsync(string name, bool isPrivate, string? description = null)
    {
        EnsureAuthenticated();

        var payload = new
        {
            name,
            description,
            @private = isPrivate,
            auto_init = false
        };

        using var response = await _httpClient.PostAsJsonAsync("user/repos", payload);
        await EnsureSuccessAsync(response);

        var repo = await response.Content.ReadFromJsonAsync<GitHubRepository>();
        return repo ?? throw new InvalidOperationException("GitHub did not return the created repository details.");
    }

    public async Task<GitHubBranch> CreateBranchAsync(string owner, string repoName, string newBranchName, string baseBranch)
    {
        EnsureAuthenticated();

        if (string.IsNullOrWhiteSpace(newBranchName))
        {
            throw new InvalidOperationException("Branch name is required.");
        }

        var trimmedBranchName = newBranchName.Trim();
        if (trimmedBranchName == baseBranch)
        {
            throw new InvalidOperationException("New branch name cannot match the base branch.");
        }

        using var baseRefResponse = await _httpClient.GetAsync($"repos/{owner}/{repoName}/git/ref/heads/{baseBranch}");
        await EnsureSuccessAsync(baseRefResponse);

        var baseRef = await baseRefResponse.Content.ReadFromJsonAsync<GitReferenceResponse>();
        var sha = baseRef?.Object?.Sha ?? throw new InvalidOperationException("Could not resolve the base branch SHA.");

        var newBranchPayload = new
        {
            @ref = $"refs/heads/{trimmedBranchName}",
            sha
        };

        using var createBranchResponse = await _httpClient.PostAsJsonAsync($"repos/{owner}/{repoName}/git/refs", newBranchPayload);
        await EnsureSuccessAsync(createBranchResponse);

        var branch = await createBranchResponse.Content.ReadFromJsonAsync<GitHubBranch>();
        return branch ?? throw new InvalidOperationException("GitHub did not return the new branch.");
    }

    private void EnsureAuthenticated()
    {
        if (string.IsNullOrWhiteSpace(_token))
        {
            throw new InvalidOperationException("Connect with a GitHub personal access token first.");
        }
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var message = await response.Content.ReadAsStringAsync();
        throw new InvalidOperationException($"GitHub request failed with status {(int)response.StatusCode}: {message}");
    }

    private sealed class GitReferenceResponse
    {
        [JsonPropertyName("ref")]
        public string Ref { get; set; } = string.Empty;

        [JsonPropertyName("object")]
        public GitReferenceObject? Object { get; set; }
    }

    private sealed class GitReferenceObject
    {
        [JsonPropertyName("sha")]
        public string Sha { get; set; } = string.Empty;
    }
}
