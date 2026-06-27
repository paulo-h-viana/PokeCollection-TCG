using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace PokeCollection.Data.Services;

public class BackupConfig
{
    public bool AutoBackupEnabled { get; set; }
    public string? BackupFolder { get; set; }
    public int MaxBackups { get; set; } = 10;
    public DateTime? LastBackupUtc { get; set; }
}

public class BackupService
{
    private readonly string _dbPath;
    private readonly string _configPath;
    private static readonly object _lock = new();

    public BackupService(string dbPath, string appDataFolder)
    {
        _dbPath = dbPath;
        _configPath = Path.Combine(appDataFolder, "backup_settings.json");
    }

    public BackupConfig GetConfig()
    {
        lock (_lock)
        {
            if (!File.Exists(_configPath)) return new BackupConfig();
            try
            {
                var json = File.ReadAllText(_configPath);
                return JsonSerializer.Deserialize<BackupConfig>(json) ?? new BackupConfig();
            }
            catch
            {
                return new BackupConfig();
            }
        }
    }

    private void SaveConfig(BackupConfig config)
    {
        lock (_lock)
        {
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_configPath, json);
        }
    }

    public void UpdateFolder(string? folder)
    {
        var config = GetConfig();
        config.BackupFolder = folder;
        SaveConfig(config);
    }

    public void SetAutoBackup(bool enabled)
    {
        var config = GetConfig();
        config.AutoBackupEnabled = enabled;
        SaveConfig(config);
    }

    public string CreateBackupToFolder(string folder)
    {
        Directory.CreateDirectory(folder);
        var fileName = $"PokeCollection_backup_{DateTime.Now:yyyy-MM-dd_HHmmss}.db";
        var destPath = Path.Combine(folder, fileName);

        CopyDatabaseTo(destPath);
        ApplyRetention(folder);

        var config = GetConfig();
        config.LastBackupUtc = DateTime.UtcNow;
        SaveConfig(config);

        return destPath;
    }

    public void CreateBackupToFile(string destFilePath)
    {
        CopyDatabaseTo(destFilePath);

        var config = GetConfig();
        config.LastBackupUtc = DateTime.UtcNow;
        SaveConfig(config);
    }

    public void RunAutoBackup()
    {
        var config = GetConfig();
        if (!config.AutoBackupEnabled) return;
        if (string.IsNullOrWhiteSpace(config.BackupFolder)) return;
        if (!File.Exists(_dbPath)) return;

        try
        {
            CreateBackupToFolder(config.BackupFolder);
        }
        catch
        {
        }
    }

    public void RestoreFromFile(string sourcePath)
    {
        SqliteConnection.ClearAllPools();

        foreach (var suffix in new[] { "-wal", "-shm" })
        {
            var sidecar = _dbPath + suffix;
            if (File.Exists(sidecar)) File.Delete(sidecar);
        }

        File.Copy(sourcePath, _dbPath, overwrite: true);
    }

    private void CopyDatabaseTo(string destPath)
    {
        var dir = Path.GetDirectoryName(destPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        var tempDest = destPath + ".tmp";
        if (File.Exists(tempDest)) File.Delete(tempDest);

        var sourceCs = new SqliteConnectionStringBuilder
        {
            DataSource = _dbPath,
            Pooling = false
        }.ToString();

        var destCs = new SqliteConnectionStringBuilder
        {
            DataSource = tempDest,
            Pooling = false
        }.ToString();

        using (var source = new SqliteConnection(sourceCs))
        using (var dest = new SqliteConnection(destCs))
        {
            source.Open();
            dest.Open();
            source.BackupDatabase(dest);
        }

        File.Move(tempDest, destPath, overwrite: true);
    }

    private void ApplyRetention(string folder)
    {
        var config = GetConfig();
        var max = config.MaxBackups <= 0 ? 10 : config.MaxBackups;

        var stale = new DirectoryInfo(folder)
            .GetFiles("PokeCollection_backup_*.db")
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .Skip(max)
            .ToList();

        foreach (var file in stale)
        {
            try { file.Delete(); } catch { }
        }
    }

    public static string? SuggestGoogleDriveFolder()
    {
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string[] candidates =
        {
            Path.Combine(profile, "Google Drive"),
            Path.Combine(profile, "My Drive")
        };
        return candidates.FirstOrDefault(Directory.Exists);
    }
}
