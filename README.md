### MS-SQL-CLR-RunParallel
Helpers for start same procedures in parallel

#### How to use
1. Compile CLR.RunParallel
2. Register new assembly [CLR.RunParallel] in DB 
3. Create type SqlProcedure
4. Create function GetSqlProcedures
5. Create procedure RunParallel
6. Use it for start others procedures in parallel

#### Example

`
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
`