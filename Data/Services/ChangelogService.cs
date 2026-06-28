using System.Reflection;
using System.Text.Json;
using Velopack;
using Velopack.Sources;

namespace PokeCollection.Data.Services;

public record ChangelogEntry(string Version, string Date, string? Title, List<string> Items);

public class ChangelogService
{
    private const string GithubRepoUrl = "https://github.com/paulo-h-viana/PokeCollection-TCG";

    private readonly Dictionary<string, ChangelogEntry> _entries;

    public string CurrentVersion { get; }

    public ChangelogService()
    {
        CurrentVersion = ResolveCurrentVersion();
        _entries = LoadEntries();
    }

    public ChangelogEntry? GetEntry(string version) =>
        _entries.TryGetValue(version, out var entry) ? entry : null;

    private static string ResolveCurrentVersion()
    {
        var installed = ResolveInstalledVersion();
        if (installed is not null) return installed;

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version is null ? "0.0.0" : $"{version.Major}.{version.Minor}.{version.Build}";
    }

    private static string? ResolveInstalledVersion()
    {
        try
        {
            var mgr = new UpdateManager(new GithubSource(GithubRepoUrl, accessToken: null, prerelease: false));
            if (!mgr.IsInstalled || mgr.CurrentVersion is null) return null;

            var v = mgr.CurrentVersion;
            return $"{v.Major}.{v.Minor}.{v.Patch}";
        }
        catch
        {
            return null;
        }
    }

    private static Dictionary<string, ChangelogEntry> LoadEntries()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "changelog.json");
            if (!File.Exists(path)) return new();

            var raw = JsonSerializer.Deserialize<Dictionary<string, RawEntry>>(
                File.ReadAllText(path),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

            return raw.ToDictionary(
                kv => kv.Key,
                kv => new ChangelogEntry(kv.Key, kv.Value.Date ?? "", kv.Value.Title, kv.Value.Items ?? new()));
        }
        catch
        {
            return new();
        }
    }

    private class RawEntry
    {
        public string? Date { get; set; }
        public string? Title { get; set; }
        public List<string>? Items { get; set; }
    }
}
