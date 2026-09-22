using ControleMateriais.Data.Common;

namespace ControleMateriais.Data.Services;

public static class BackupService
{
    private const int MaxBackups = 7;

    public static void CreateBackup()
    {
        Directory.CreateDirectory(DbSettings.BackupFolder);

        if (!File.Exists(DbSettings.DatabasePath))
            return;

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
        string backupPath = Path.Combine(DbSettings.BackupFolder, $"database_{timestamp}.db");

        File.Copy(DbSettings.DatabasePath, backupPath, overwrite: true);

        CleanupOldBackups();
    }

    public static void RestoreBackup(string backupPath)
    {
        if (!File.Exists(backupPath))
            throw new FileNotFoundException("Backup não encontrado.", backupPath);

        File.Copy(backupPath, DbSettings.DatabasePath, overwrite: true);
    }

    public static IEnumerable<string> ListBackups()
    {
        Directory.CreateDirectory(DbSettings.BackupFolder);
        return Directory
            .GetFiles(DbSettings.BackupFolder, "database_*.db")
            .OrderByDescending(f => f)
            .ToList();
    }

    private static void CleanupOldBackups()
    {
        var backups = ListBackups().ToList();

        while (backups.Count > MaxBackups)
        {
            File.Delete(backups.Last());
            backups.RemoveAt(backups.Count - 1);
        }
    }
}
