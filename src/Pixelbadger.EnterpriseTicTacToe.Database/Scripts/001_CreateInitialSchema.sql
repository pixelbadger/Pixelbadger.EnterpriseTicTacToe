IF SCHEMA_ID(N'dbo') IS NULL
BEGIN
    EXEC(N'CREATE SCHEMA [dbo]');
END;
GO

IF OBJECT_ID(N'[dbo].[GameSessions]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[GameSessions]
    (
        [Id] UNIQUEIDENTIFIER NOT NULL,
        [SessionCode] NVARCHAR(6) NOT NULL,
        [Status] NVARCHAR(32) NOT NULL,
        [BoardState] NCHAR(9) NOT NULL,
        [CurrentTurn] NVARCHAR(1) NULL,
        [Winner] NVARCHAR(1) NULL,
        [RematchXReady] BIT NOT NULL,
        [RematchOReady] BIT NOT NULL,
        [CreatedUtc] DATETIME2 NOT NULL,
        [UpdatedUtc] DATETIME2 NOT NULL,
        [LastActivityUtc] DATETIME2 NOT NULL,
        [ExpiresAtUtc] DATETIME2 NOT NULL,
        [RowVersion] ROWVERSION NOT NULL,
        CONSTRAINT [PK_GameSessions] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [CK_GameSessions_Status] CHECK ([Status] IN (N'WaitingForOpponent', N'InProgress', N'Won', N'Draw', N'Expired')),
        CONSTRAINT [CK_GameSessions_CurrentTurn] CHECK ([CurrentTurn] IS NULL OR [CurrentTurn] IN (N'X', N'O')),
        CONSTRAINT [CK_GameSessions_Winner] CHECK ([Winner] IS NULL OR [Winner] IN (N'X', N'O'))
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM [sys].[indexes]
    WHERE [name] = N'IX_GameSessions_SessionCode'
      AND [object_id] = OBJECT_ID(N'[dbo].[GameSessions]')
)
BEGIN
    CREATE UNIQUE INDEX [IX_GameSessions_SessionCode]
        ON [dbo].[GameSessions] ([SessionCode]);
END;
GO

IF OBJECT_ID(N'[dbo].[Players]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Players]
    (
        [Id] UNIQUEIDENTIFIER NOT NULL,
        [GameSessionId] UNIQUEIDENTIFIER NOT NULL,
        [Username] NVARCHAR(80) NOT NULL,
        [NormalizedUsername] NVARCHAR(80) NOT NULL,
        [ClientIdentityHash] NVARCHAR(96) NOT NULL,
        [Mark] NVARCHAR(1) NOT NULL,
        [IsOnline] BIT NOT NULL,
        [JoinedUtc] DATETIME2 NOT NULL,
        [LastSeenUtc] DATETIME2 NOT NULL,
        CONSTRAINT [PK_Players] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_Players_GameSessions_GameSessionId]
            FOREIGN KEY ([GameSessionId]) REFERENCES [dbo].[GameSessions]([Id]) ON DELETE CASCADE,
        CONSTRAINT [CK_Players_Mark] CHECK ([Mark] IN (N'X', N'O'))
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM [sys].[indexes]
    WHERE [name] = N'IX_Players_GameSessionId_Mark'
      AND [object_id] = OBJECT_ID(N'[dbo].[Players]')
)
BEGIN
    CREATE UNIQUE INDEX [IX_Players_GameSessionId_Mark]
        ON [dbo].[Players] ([GameSessionId], [Mark]);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM [sys].[indexes]
    WHERE [name] = N'IX_Players_GameSessionId_NormalizedUsername'
      AND [object_id] = OBJECT_ID(N'[dbo].[Players]')
)
BEGIN
    CREATE UNIQUE INDEX [IX_Players_GameSessionId_NormalizedUsername]
        ON [dbo].[Players] ([GameSessionId], [NormalizedUsername]);
END;
GO
