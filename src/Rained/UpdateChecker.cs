using System.Text.Json.Serialization;
using System.Text.Json;

namespace RainEd;

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
    public static async Task<RainedVersionInfo> FetchLatestVersion()
    {
        var client = new HttpClient()
        {
            BaseAddress = new Uri("https://api.github.com/"),
        };

        var os = Environment.OSVersion.ToString();
        var clr = Environment.Version.ToString();

        client.DefaultRequestHeaders.Add("user-agent", $"Mozilla/4.0 (compatible; MSIE 6.0; {os}; .NET CLR {clr};)");

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