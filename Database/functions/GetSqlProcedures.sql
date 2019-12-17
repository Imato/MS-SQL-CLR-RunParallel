CREATE  FUNCTION dbo.GetSqlProcedures
  (@tableProcedures dbo.SqlProcedure READONLY)
RETURNS NVARCHAR(MAX)
AS
/*
  Need to prepare XML parameter @sqlProcedure in procedure dbo.RunParallel
*/
BEGIN
  RETURN (
    CAST((SELECT t.Id, t.SqlText
          FROM @tableProcedures t
          FOR XML PATH('Procedure'), ROOT('Root')) AS NVARCHAR(MAX)));
END
