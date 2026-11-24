using Microsoft.Data.SqlClient;

namespace NexusCopia;

internal sealed class HunterProgress
{
    public string HunterId { get; set; } = "soren";
    public int Experience { get; set; }
    public int Credits { get; set; }
    public int ClearedDungeons { get; set; }
    public int BestScore { get; set; }
    public string Rank => Experience >= 1000 ? "S" : Experience >= 700 ? "A" : Experience >= 450 ? "B" : Experience >= 250 ? "C" : Experience >= 100 ? "D" : "E";
    public int NextRankExperience => Experience >= 1000 ? 1000 : new[] { 100, 250, 450, 700, 1000 }.First(x => x > Experience);
    public HunterArchetype Hunter => HunterCatalog.All.FirstOrDefault(x => x.Id == HunterId) ?? HunterCatalog.All[0];
}

/// <summary>All player progression is stored in SQL Server. No file-based save is used.</summary>
internal static class ProgressStore
{
    private const string DefaultConnectionString = "Server=(localdb)\\MSSQLLocalDB;Initial Catalog=NexusCopyDb;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=15";
    private static readonly string ConnectionString = Environment.GetEnvironmentVariable("NEXUS_SQLSERVER_CONNECTION") ?? DefaultConnectionString;
    private static readonly object Gate = new();
    private static bool _schemaReady;

    public static bool SqlAvailable { get; private set; }
    public static string StorageStatus => SqlAvailable ? "SQL SERVER CONECTADO" : "SQL SERVER DESCONECTADO";

    public static HunterProgress Load()
    {
        lock (Gate)
        {
            try
            {
                using var connection = OpenDatabase();
                using var command = new SqlCommand("SELECT HunterId, Experience, Credits, ClearedDungeons, BestScore FROM dbo.HunterProgress WHERE ProfileId = 1", connection);
                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    SqlAvailable = true;
                    return new HunterProgress
                    {
                        HunterId = reader.GetString(0), Experience = reader.GetInt32(1), Credits = reader.GetInt32(2),
                        ClearedDungeons = reader.GetInt32(3), BestScore = reader.GetInt32(4)
                    };
                }

                // First launch: create the only player profile directly in SQL Server.
                var profile = new HunterProgress();
                reader.Close();
                SaveSql(connection, profile);
                SqlAvailable = true;
                return profile;
            }
            catch (Exception ex) when (IsSqlFailure(ex))
            {
                SqlAvailable = false;
                throw CreateConnectionError(ex);
            }
        }
    }

    public static (int Experience, int Credits, string RankBefore, string RankAfter) Complete(HunterProgress profile, int score, int shards)
    {
        string before = profile.Rank;
        int oldExperience = profile.Experience, oldCredits = profile.Credits;
        int oldCleared = profile.ClearedDungeons, oldBest = profile.BestScore;
        int xp = 140 + Math.Max(0, shards) * 5 + Math.Max(0, score) / 80;
        int credits = 60 + Math.Max(0, shards) * 2;
        profile.Experience += xp;
        profile.Credits += credits;
        profile.ClearedDungeons++;
        profile.BestScore = Math.Max(profile.BestScore, score);
        try { Save(profile); }
        catch
        {
            profile.Experience = oldExperience;
            profile.Credits = oldCredits;
            profile.ClearedDungeons = oldCleared;
            profile.BestScore = oldBest;
            throw;
        }
        return (xp, credits, before, profile.Rank);
    }

    public static void Save(HunterProgress profile)
    {
        lock (Gate)
        {
            try
            {
                using var connection = OpenDatabase();
                SaveSql(connection, profile);
                SqlAvailable = true;
            }
            catch (Exception ex) when (IsSqlFailure(ex))
            {
                SqlAvailable = false;
                throw CreateConnectionError(ex);
            }
        }
    }

    private static SqlConnection OpenDatabase()
    {
        var target = new SqlConnectionStringBuilder(ConnectionString);
        string database = string.IsNullOrWhiteSpace(target.InitialCatalog) ? "NexusCopyDb" : target.InitialCatalog;
        if (!_schemaReady)
        {
            var master = new SqlConnectionStringBuilder(target.ConnectionString) { InitialCatalog = "master" };
            using var adminConnection = new SqlConnection(master.ConnectionString);
            adminConnection.Open();
            using (var createDb = new SqlCommand("IF DB_ID(@database) IS NULL BEGIN DECLARE @sql nvarchar(max) = N'CREATE DATABASE ' + QUOTENAME(@database); EXEC sys.sp_executesql @sql; END", adminConnection))
            {
                createDb.Parameters.AddWithValue("@database", database);
                createDb.ExecuteNonQuery();
            }

            target.InitialCatalog = database;
            using var schemaConnection = new SqlConnection(target.ConnectionString);
            schemaConnection.Open();
            const string schema = """
                IF OBJECT_ID(N'dbo.HunterProgress', N'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.HunterProgress
                    (
                        ProfileId tinyint NOT NULL CONSTRAINT PK_HunterProgress PRIMARY KEY,
                        HunterId nvarchar(32) NOT NULL,
                        Experience int NOT NULL CONSTRAINT DF_HunterProgress_Experience DEFAULT (0),
                        Credits int NOT NULL CONSTRAINT DF_HunterProgress_Credits DEFAULT (0),
                        ClearedDungeons int NOT NULL CONSTRAINT DF_HunterProgress_Cleared DEFAULT (0),
                        BestScore int NOT NULL CONSTRAINT DF_HunterProgress_Best DEFAULT (0),
                        UpdatedAt datetime2 NOT NULL CONSTRAINT DF_HunterProgress_Updated DEFAULT (SYSUTCDATETIME()),
                        CONSTRAINT CK_HunterProgress_ProfileId CHECK (ProfileId = 1)
                    );
                END;
                """;
            using var schemaCommand = new SqlCommand(schema, schemaConnection);
            schemaCommand.ExecuteNonQuery();
            _schemaReady = true;
        }

        target.InitialCatalog = database;
        var connection = new SqlConnection(target.ConnectionString);
        connection.Open();
        return connection;
    }

    private static void SaveSql(SqlConnection connection, HunterProgress profile)
    {
        const string sql = """
            UPDATE dbo.HunterProgress
               SET HunterId = @hunter, Experience = @xp, Credits = @credits,
                   ClearedDungeons = @cleared, BestScore = @best, UpdatedAt = SYSUTCDATETIME()
             WHERE ProfileId = 1;
            IF @@ROWCOUNT = 0
                INSERT dbo.HunterProgress (ProfileId, HunterId, Experience, Credits, ClearedDungeons, BestScore)
                VALUES (1, @hunter, @xp, @credits, @cleared, @best);
            """;
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@hunter", profile.HunterId);
        command.Parameters.AddWithValue("@xp", profile.Experience);
        command.Parameters.AddWithValue("@credits", profile.Credits);
        command.Parameters.AddWithValue("@cleared", profile.ClearedDungeons);
        command.Parameters.AddWithValue("@best", profile.BestScore);
        command.ExecuteNonQuery();
    }

    private static InvalidOperationException CreateConnectionError(Exception inner) => new(
        "Nexus Copy necesita SQL Server para cargar y guardar el progreso. Instala o inicia SQL Server LocalDB, " +
        "o configura la variable NEXUS_SQLSERVER_CONNECTION. La base NexusCopyDb y la tabla de progreso se crean automáticamente " +
        "si la cuenta tiene permisos. Consulta la sección Base de datos SQL Server del README.md.\n\nDetalle: " + inner.Message, inner);

    private static bool IsSqlFailure(Exception ex) => ex is SqlException or InvalidOperationException or ArgumentException;
}
