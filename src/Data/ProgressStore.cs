using Microsoft.Data.SqlClient;

namespace NexusCopia;

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

    public static ExpeditionSave? LoadExpedition()
    {
        lock (Gate)
        {
            try
            {
                using var connection = OpenDatabase();
                ExpeditionSave? save;
                using (var command = new SqlCommand("SELECT DungeonIndex, DifficultyIndex, PlayerX, PlayerY, VelocityX, VelocityY, CheckpointX, Score, Lives, BossHealth, BossAwakened, BossDefeated, WorldTicks, FacingRight, Grounded FROM dbo.ExpeditionSave WHERE ProfileId = 1", connection))
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read()) { SqlAvailable = true; return null; }
                    save = new ExpeditionSave(
                        reader.GetByte(0), reader.GetByte(1), reader.GetFloat(2), reader.GetFloat(3), reader.GetFloat(4), reader.GetFloat(5),
                        reader.GetFloat(6), reader.GetInt32(7), reader.GetByte(8), reader.GetInt32(9), reader.GetBoolean(10), reader.GetBoolean(11),
                        reader.GetInt32(12), reader.GetBoolean(13), reader.GetBoolean(14), [], [], []);
                }
                var shards = new List<int>();
                using (var command = new SqlCommand("SELECT ShardIndex FROM dbo.ExpeditionShardSave WHERE ProfileId = 1 AND DungeonIndex = @dungeon ORDER BY ShardIndex", connection))
                {
                    command.Parameters.AddWithValue("@dungeon", save.DungeonIndex);
                    using var reader = command.ExecuteReader(); while (reader.Read()) shards.Add(reader.GetInt32(0));
                }
                var enemies = new List<SavedEnemy>();
                using (var command = new SqlCommand("SELECT X, Speed, Health, AttackCooldown, Alive FROM dbo.ExpeditionEnemySave WHERE ProfileId = 1 AND DungeonIndex = @dungeon ORDER BY EnemyIndex", connection))
                {
                    command.Parameters.AddWithValue("@dungeon", save.DungeonIndex);
                    using var reader = command.ExecuteReader(); while (reader.Read()) enemies.Add(new(reader.GetFloat(0), reader.GetFloat(1), reader.GetInt32(2), reader.GetInt32(3), reader.GetBoolean(4)));
                }
                var traps = new List<int>();
                using (var command = new SqlCommand("SELECT Phase FROM dbo.ExpeditionTrapSave WHERE ProfileId = 1 AND DungeonIndex = @dungeon ORDER BY TrapIndex", connection))
                {
                    command.Parameters.AddWithValue("@dungeon", save.DungeonIndex);
                    using var reader = command.ExecuteReader(); while (reader.Read()) traps.Add(reader.GetInt32(0));
                }
                SqlAvailable = true;
                return save with { CollectedShards = shards.ToArray(), Enemies = enemies.ToArray(), TrapPhases = traps.ToArray() };
            }
            catch (Exception ex) when (IsSqlFailure(ex))
            {
                SqlAvailable = false;
                throw CreateConnectionError(ex);
            }
        }
    }

    public static void SaveExpedition(ExpeditionSave save)
    {
        lock (Gate)
        {
            try
            {
                using var connection = OpenDatabase();
                using var transaction = connection.BeginTransaction();
                const string upsert = """
                    UPDATE dbo.ExpeditionSave SET DungeonIndex=@dungeon, DifficultyIndex=@difficulty, PlayerX=@x, PlayerY=@y,
                        VelocityX=@vx, VelocityY=@vy, CheckpointX=@checkpoint, Score=@score, Lives=@lives, BossHealth=@bossHealth,
                        BossAwakened=@bossAwakened, BossDefeated=@bossDefeated, WorldTicks=@ticks, FacingRight=@facing, Grounded=@grounded,
                        SavedAt=SYSUTCDATETIME() WHERE ProfileId=1;
                    IF @@ROWCOUNT=0 INSERT dbo.ExpeditionSave
                        (ProfileId,DungeonIndex,DifficultyIndex,PlayerX,PlayerY,VelocityX,VelocityY,CheckpointX,Score,Lives,BossHealth,BossAwakened,BossDefeated,WorldTicks,FacingRight,Grounded)
                        VALUES (1,@dungeon,@difficulty,@x,@y,@vx,@vy,@checkpoint,@score,@lives,@bossHealth,@bossAwakened,@bossDefeated,@ticks,@facing,@grounded);
                    DELETE dbo.ExpeditionShardSave WHERE ProfileId=1;
                    DELETE dbo.ExpeditionEnemySave WHERE ProfileId=1;
                    DELETE dbo.ExpeditionTrapSave WHERE ProfileId=1;
                    """;
                using (var command = new SqlCommand(upsert, connection, transaction))
                {
                    command.Parameters.AddWithValue("@dungeon", save.DungeonIndex); command.Parameters.AddWithValue("@difficulty", save.DifficultyIndex);
                    command.Parameters.AddWithValue("@x", save.PlayerX); command.Parameters.AddWithValue("@y", save.PlayerY);
                    command.Parameters.AddWithValue("@vx", save.VelocityX); command.Parameters.AddWithValue("@vy", save.VelocityY);
                    command.Parameters.AddWithValue("@checkpoint", save.CheckpointX); command.Parameters.AddWithValue("@score", save.Score);
                    command.Parameters.AddWithValue("@lives", save.Lives); command.Parameters.AddWithValue("@bossHealth", save.BossHealth);
                    command.Parameters.AddWithValue("@bossAwakened", save.BossAwakened); command.Parameters.AddWithValue("@bossDefeated", save.BossDefeated);
                    command.Parameters.AddWithValue("@ticks", save.WorldTicks); command.Parameters.AddWithValue("@facing", save.FacingRight);
                    command.Parameters.AddWithValue("@grounded", save.Grounded); command.ExecuteNonQuery();
                }
                for (int i = 0; i < save.CollectedShards.Length; i++)
                {
                    using var command = new SqlCommand("INSERT dbo.ExpeditionShardSave (ProfileId,DungeonIndex,ShardIndex) VALUES (1,@dungeon,@index)", connection, transaction);
                    command.Parameters.AddWithValue("@dungeon", save.DungeonIndex); command.Parameters.AddWithValue("@index", save.CollectedShards[i]); command.ExecuteNonQuery();
                }
                for (int i = 0; i < save.Enemies.Length; i++)
                {
                    SavedEnemy enemy = save.Enemies[i];
                    using var command = new SqlCommand("INSERT dbo.ExpeditionEnemySave (ProfileId,DungeonIndex,EnemyIndex,X,Speed,Health,AttackCooldown,Alive) VALUES (1,@dungeon,@index,@x,@speed,@health,@cooldown,@alive)", connection, transaction);
                    command.Parameters.AddWithValue("@dungeon", save.DungeonIndex); command.Parameters.AddWithValue("@index", i);
                    command.Parameters.AddWithValue("@x", enemy.X); command.Parameters.AddWithValue("@speed", enemy.Speed);
                    command.Parameters.AddWithValue("@health", enemy.Health); command.Parameters.AddWithValue("@cooldown", enemy.AttackCooldown);
                    command.Parameters.AddWithValue("@alive", enemy.Alive); command.ExecuteNonQuery();
                }
                for (int i = 0; i < save.TrapPhases.Length; i++)
                {
                    using var command = new SqlCommand("INSERT dbo.ExpeditionTrapSave (ProfileId,DungeonIndex,TrapIndex,Phase) VALUES (1,@dungeon,@index,@phase)", connection, transaction);
                    command.Parameters.AddWithValue("@dungeon", save.DungeonIndex); command.Parameters.AddWithValue("@index", i);
                    command.Parameters.AddWithValue("@phase", save.TrapPhases[i]); command.ExecuteNonQuery();
                }
                transaction.Commit();
                SqlAvailable = true;
            }
            catch (Exception ex) when (IsSqlFailure(ex))
            {
                SqlAvailable = false;
                throw CreateConnectionError(ex);
            }
        }
    }

    public static void DeleteExpedition()
    {
        lock (Gate)
        {
            try
            {
                using var connection = OpenDatabase();
                using var command = new SqlCommand("DELETE dbo.ExpeditionSave WHERE ProfileId=1; DELETE dbo.ExpeditionShardSave WHERE ProfileId=1; DELETE dbo.ExpeditionEnemySave WHERE ProfileId=1; DELETE dbo.ExpeditionTrapSave WHERE ProfileId=1;", connection);
                command.ExecuteNonQuery(); SqlAvailable = true;
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
            SqlServerSchema.Ensure(target.ConnectionString, database);
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
