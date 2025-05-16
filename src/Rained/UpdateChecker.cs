using System.Text.Json.Serialization;
using System.Text.Json;

namespace Rained;

record class RainedVersionInfo
{
    public readonly string VersionName;
    public readonly string GitHubReleaseUrl;

    public RainedVersionInfo(string name, string url)
    {
        VersionName = name;
        GitHubReleaseUrl = url;
    }
}

static class UpdateChecker
{
    class GitHubResponseJson
    {
        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = "";

        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = "";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
 
        [JsonPropertyName("created_at")]
        public string CreatedAt { get; set; } = "";

        [JsonPropertyName("published_at")]
        public string PublishedAt { get; set; } = "";
    }

    /// <summary>
    /// Uses the GitHub API to fetch the version number
    /// of the latest release of Rained
    /// </summary>
    /// <returns>The version string of the latest release of Rained</returns>
    public static async Task<RainedVersionInfo?> FetchLatestVersion()
    {
        if (!RainEd.Instance.Preferences.CheckForUpdates) return null;
        
        var client = new HttpClient()
        {
            BaseAddress = new Uri("https://api.github.com/"),
        };

        client.DefaultRequestHeaders.Add("user-agent", Util.HttpUserAgent);

        using var response = await client.GetAsync("repos/pkhead/rained/releases/latest");
        response.EnsureSuccessStatusCode();

        var jsonResponse = await response.Content.ReadAsStringAsync();

        // load json response
        var serializeOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = true
        };
        var parsedResponse = JsonSerializer.Deserialize<GitHubResponseJson>(jsonResponse, serializeOptions)!;

        return new RainedVersionInfo(parsedResponse.Name, parsedResponse.HtmlUrl);
    }
}