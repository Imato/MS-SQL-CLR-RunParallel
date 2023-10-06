CREATE PROCEDURE dbo.RunParallel
  @sqlProcedures NVARCHAR(MAX),     -- List of procedures
  @noOutput      BIT            = 0, -- don't return result of execution,
  @maxThreads    INT            = 10
/*

  Input: XML structure with type dbo.SqlProcedure (Id, SqlText, IsSuccess, ErrorText)
  Output: Result of execution with type dbo.SqlProcedure

  Start list of SQL procedures in parallel. Return list of procedures with status and error text.
  Use function dbo.Get_SqlProcedures_Text for prepare structure dbo.SqlProcedure for execution.

  Example:
  1. Create list
  DECLARE @commands dbo.SqlProcedure,

  insert into @commands
  (SqlText)
  values
  ('print ''Test 1''; waitfor delay ''00:00:02'';'),
  ('waitfor delay ''00:00:02'';');

  2. Get parameter @sqlProcedures
  DECLARE  @sqlProcedures nvarchar(max);
  set @sqlProcedures = dbo.GetSqlProcedures(@commands);

  3. Execute
  EXEC dbo.RunParallel @sqlProcedures = @sqlProcedures;

*/
AS EXTERNAL NAME [Imato.CLR.ParallelRunner].[RunParallelProcedure].[RunParallel];