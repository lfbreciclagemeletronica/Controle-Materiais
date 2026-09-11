using Microsoft.Data.Sqlite;

namespace ControleMateriais.Data.Common;

public static class DbSettings
{
    public static string DatabaseFolder =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads",
            "ControleMateriaisLFB",
            "banco-de-dados");

    public static string DatabasePath =>
        Path.Combine(DatabaseFolder, "database.db");

    public static string BackupFolder =>
        Path.Combine(DatabaseFolder, "backups");

    public static string LogFolder =>
        Path.Combine(DatabaseFolder, "logs");

    public static SqliteConnectionStringBuilder CreateConnectionString()
    {
        return new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate
        };
    }

    public static string GetConnectionString() =>
        CreateConnectionString().ConnectionString;
}
