### MS-SQL CLR - Imato.CLR.ParallelRunner
Helpers for start same procedures in parallel

#### How to use
1. Compile Imato.CLR.ParallelRunner or dounload 
2. Register new assembly [Imato.CLR.ParallelRunner] in DB 
``` sql 
CREATE ASSEMBLY [Imato.CLR.ParallelRunner]
FROM 'PathToDll\Imato.CLR.ParallelRunner.dll'
WITH PERMISSION_SET = UNSAFE;
go
```
3. Create type SqlProcedure
``` sql
CREATE TYPE dbo.SqlProcedure AS TABLE
(Id INT NOT NULL IDENTITY(1, 1) PRIMARY KEY,
SqlText NVARCHAR(MAX) NOT NULL,
IsSuccess BIT,
ErrorText NVARCHAR(MAX))
```
4. Create function GetSqlProcedures
``` sql
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
```
5. Create procedure RunParallel
```sql
CREATE PROCEDURE dbo.RunParallel
  @sqlProcedures NVARCHAR(MAX),     -- List of procedures
  @noOutput      BIT            = 0, -- don't return result of execution,
  @maxThreads    INT            = 10

AS EXTERNAL NAME [Imato.CLR.ParallelRunner].[RunParallelProcedure].[RunParallel];
```
6. Use it for start others procedures in parallel

#### Example
```
-- 1. Create list 
DECLARE @commands dbo.SqlProcedure, 

insert into @commands 
(SqlText) 
values 
('print ''Test 1''; waitfor delay ''00:00:02'';'), 
('waitfor delay ''00:00:02'';'); 

-- 2. Get parameter @sqlProcedures
DECLARE  @sqlProcedures nvarchar(max); 
set @sqlProcedures = dbo.GetSqlProcedures(@commands); 

-- 3. Execute 
EXEC dbo.RunParallel @sqlProcedures = @sqlProcedures 
    -- @noOutput = 0, 
    -- @maxThreads = 10
;
```
