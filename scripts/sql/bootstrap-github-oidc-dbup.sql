/*
Bootstrap Azure SQL access for the GitHub OIDC deploy principal used by DbUp.

Run this script against the application database as an Entra admin account.
Set @principalName to the service principal display name or object id before running.
*/

DECLARE @principalName sysname = N'__GITHUB_OIDC_PRINCIPAL_NAME__';

IF @principalName = N'__GITHUB_OIDC_PRINCIPAL_NAME__'
BEGIN
    THROW 50001, 'Set @principalName before running this script.', 1;
END;

DECLARE @quotedPrincipal nvarchar(260) = N'[' + REPLACE(@principalName, N']', N']]') + N']';

IF NOT EXISTS (
    SELECT 1
    FROM sys.database_principals
    WHERE name = @principalName)
BEGIN
    EXEC(N'CREATE USER ' + @quotedPrincipal + N' FROM EXTERNAL PROVIDER;');
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.database_role_members roleMembers
    INNER JOIN sys.database_principals rolePrincipal
        ON rolePrincipal.principal_id = roleMembers.role_principal_id
    INNER JOIN sys.database_principals memberPrincipal
        ON memberPrincipal.principal_id = roleMembers.member_principal_id
    WHERE rolePrincipal.name = N'db_ddladmin'
      AND memberPrincipal.name = @principalName)
BEGIN
    EXEC(N'ALTER ROLE [db_ddladmin] ADD MEMBER ' + @quotedPrincipal + N';');
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.database_role_members roleMembers
    INNER JOIN sys.database_principals rolePrincipal
        ON rolePrincipal.principal_id = roleMembers.role_principal_id
    INNER JOIN sys.database_principals memberPrincipal
        ON memberPrincipal.principal_id = roleMembers.member_principal_id
    WHERE rolePrincipal.name = N'db_datareader'
      AND memberPrincipal.name = @principalName)
BEGIN
    EXEC(N'ALTER ROLE [db_datareader] ADD MEMBER ' + @quotedPrincipal + N';');
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.database_role_members roleMembers
    INNER JOIN sys.database_principals rolePrincipal
        ON rolePrincipal.principal_id = roleMembers.role_principal_id
    INNER JOIN sys.database_principals memberPrincipal
        ON memberPrincipal.principal_id = roleMembers.member_principal_id
    WHERE rolePrincipal.name = N'db_datawriter'
      AND memberPrincipal.name = @principalName)
BEGIN
    EXEC(N'ALTER ROLE [db_datawriter] ADD MEMBER ' + @quotedPrincipal + N';');
END;

PRINT N'Bootstrap complete for principal: ' + @principalName;
