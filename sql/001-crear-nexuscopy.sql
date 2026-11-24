/* Execute this script in SQL Server Management Studio using a login that can
   create the database and tables. The default application database is
   NexusCopyDb. The application can also create this schema on first launch. */
IF DB_ID(N'NexusCopyDb') IS NULL
    CREATE DATABASE [NexusCopyDb];
GO

USE [NexusCopyDb];
GO

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
GO

IF OBJECT_ID(N'dbo.ExpeditionSave', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ExpeditionSave
    (
        ProfileId tinyint NOT NULL CONSTRAINT PK_ExpeditionSave PRIMARY KEY,
        DungeonIndex tinyint NOT NULL,
        DifficultyIndex tinyint NOT NULL,
        PlayerX real NOT NULL,
        PlayerY real NOT NULL,
        VelocityX real NOT NULL,
        VelocityY real NOT NULL,
        CheckpointX real NOT NULL,
        Score int NOT NULL,
        Lives tinyint NOT NULL,
        BossHealth int NOT NULL,
        BossAwakened bit NOT NULL,
        BossDefeated bit NOT NULL,
        WorldTicks int NOT NULL,
        FacingRight bit NOT NULL,
        Grounded bit NOT NULL,
        SavedAt datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT CK_ExpeditionSave_ProfileId CHECK (ProfileId = 1)
    );
END;
GO

IF OBJECT_ID(N'dbo.ExpeditionShardSave', N'U') IS NULL
    CREATE TABLE dbo.ExpeditionShardSave
    (
        ProfileId tinyint NOT NULL,
        DungeonIndex tinyint NOT NULL,
        ShardIndex int NOT NULL,
        CONSTRAINT PK_ExpeditionShardSave PRIMARY KEY (ProfileId, DungeonIndex, ShardIndex)
    );
GO

IF OBJECT_ID(N'dbo.ExpeditionEnemySave', N'U') IS NULL
    CREATE TABLE dbo.ExpeditionEnemySave
    (
        ProfileId tinyint NOT NULL,
        DungeonIndex tinyint NOT NULL,
        EnemyIndex int NOT NULL,
        X real NOT NULL,
        Speed real NOT NULL,
        Health int NOT NULL,
        AttackCooldown int NOT NULL,
        Alive bit NOT NULL,
        CONSTRAINT PK_ExpeditionEnemySave PRIMARY KEY (ProfileId, DungeonIndex, EnemyIndex)
    );
GO

IF OBJECT_ID(N'dbo.ExpeditionTrapSave', N'U') IS NULL
    CREATE TABLE dbo.ExpeditionTrapSave
    (
        ProfileId tinyint NOT NULL,
        DungeonIndex tinyint NOT NULL,
        TrapIndex int NOT NULL,
        Phase int NOT NULL,
        CONSTRAINT PK_ExpeditionTrapSave PRIMARY KEY (ProfileId, DungeonIndex, TrapIndex)
    );
GO

