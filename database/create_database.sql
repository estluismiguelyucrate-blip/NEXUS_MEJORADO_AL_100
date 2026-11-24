/*
   Nexus Copy progression database.
   Run with SQL Server Management Studio or sqlcmd while connected to your local SQL Server.
   The app also creates this database and table automatically when LocalDB is available.
*/
IF DB_ID(N'NexusCopyDb') IS NULL
    CREATE DATABASE NexusCopyDb;
GO

USE NexusCopyDb;
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

IF NOT EXISTS (SELECT 1 FROM dbo.HunterProgress WHERE ProfileId = 1)
    INSERT dbo.HunterProgress (ProfileId, HunterId) VALUES (1, N'soren');
GO
