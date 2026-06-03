using Velopack;
using Velopack.Sources;

namespace PokeCollection.Data.Services;

public class UpdateService
{
    private const string GithubRepoUrl = "https://github.com/paulo-h-viana/PokeCollection-TCG";
    private bool _checked;

    public UpdateInfo? AvailableUpdate { get; private set; }
    public bool IsUpdating { get; private set; }

    public event Action? OnStateChanged;

    public async Task CheckOnceAsync()
    {
        if (_checked) return;
        _checked = true;

        try
        {
            var source = new GithubSource(GithubRepoUrl, accessToken: null, prerelease: false);
            var mgr = new UpdateManager(source);
            if (!mgr.IsInstalled) return;
            AvailableUpdate = await mgr.CheckForUpdatesAsync();
        }
        catch { }
        finally
        {
            OnStateChanged?.Invoke();
        }
    }

    public async Task ApplyUpdateAsync()
    {
        if (AvailableUpdate is null) return;
        IsUpdating = true;
        OnStateChanged?.Invoke();

        try
        {
            var source = new GithubSource(GithubRepoUrl, accessToken: null, prerelease: false);
            var mgr = new UpdateManager(source);
            await mgr.DownloadUpdatesAsync(AvailableUpdate);
            mgr.ApplyUpdatesAndRestart(AvailableUpdate);
        }
        catch
        {
            IsUpdating = false;
            OnStateChanged?.Invoke();
        }
    }
}
