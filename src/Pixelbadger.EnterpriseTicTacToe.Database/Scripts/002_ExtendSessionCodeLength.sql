IF COL_LENGTH(N'dbo.GameSessions', N'SessionCode') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[GameSessions]
        ALTER COLUMN [SessionCode] NVARCHAR(8) NOT NULL;
END;
GO
