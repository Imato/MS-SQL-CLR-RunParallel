using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Data;
using System.Data.SqlClient;
using System.Xml;
using System.Threading.Tasks;

public class RunParallelProcedure
{
    private static ConcurrentQueue<string> _messages = new ConcurrentQueue<string>();
    private static int _timeOut = 300;
    private static int _maxMessageSize = 3999;

    [SqlProcedure]
    public static void RunParallel(string sqlProcedures, bool noOutput = false, int maxThreads = 5)
    {
        var tasks = new List<Task>();
        var procedures = ParseXmlParameter(sqlProcedures);

        if (maxThreads <= 0)
            maxThreads = 1;

        if (procedures.Count > 0)
        {
            Log($"Start executing {procedures.Count} sql commands in parallel");

            foreach (var p in procedures)
            {
                tasks.Add(RunProcedureAsync(p));
                Wait(tasks, tasks.Count, maxThreads);
            }

            Wait(tasks, maxThreads, maxThreads); 

            Log($"End executing {procedures.Count} sql commands in parallel");

            PrintMessages();

            if (!noOutput)
                SendResultToOutput(procedures);
        }
    }

    private static void Wait(List<Task> tasks, int threadId, int maxThreads)
    {
        if (threadId % maxThreads == 0)
            Task.WaitAll(tasks.ToArray());
    }

    protected static IList<SqlProcedure> ParseXmlParameter(string sqlProcedures)
    {
        if (string.IsNullOrEmpty(sqlProcedures))
            throw new ArgumentNullException(nameof(sqlProcedures));

        var xml = new XmlDocument();
        xml.LoadXml(sqlProcedures);

        var nodes = xml.GetElementsByTagName("Procedure");
        var procedures = new List<SqlProcedure>();

        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i].SelectSingleNode("Id") == null)
                throw new ApplicationException($@"Cannot find attribute ""Id"" in xml list {nameof(sqlProcedures)}");

            if (nodes[i].SelectSingleNode("SqlText") == null)
                throw new ApplicationException($@"Cannot find attribute ""SqlText"" in xml list {nameof(sqlProcedures)}");

            var procedure = new SqlProcedure
            {
                Id = int.Parse(nodes[i].SelectSingleNode("Id").InnerText),
                SqlText = nodes[i].SelectSingleNode("SqlText").InnerText
            };

            procedures.Add(procedure);
        }

        return procedures;
    }


    protected static void SendResultToOutput(IEnumerable<SqlProcedure> procedures)
    {
        foreach (var p in procedures)
        {
            SqlContext.Pipe.Send(GetDataRecord(p));
        }
    }

    protected static SqlDataRecord GetDataRecord(SqlProcedure procedure)
    {
        var record = new SqlDataRecord(new SqlMetaData("Id", SqlDbType.Int),
            new SqlMetaData("SqlText", SqlDbType.NVarChar, 4000),
            new SqlMetaData("IsSuccess", SqlDbType.Bit),
            new SqlMetaData("ErrorText", SqlDbType.NVarChar, 4000));


        record.SetInt32(0, procedure.Id);
        record.SetSqlString(1, procedure.SqlText);
        record.SetBoolean(2, procedure.IsSuccess);
        record.SetSqlString(3, procedure.ErrorText);

        return record;
    }

    protected static Task RunProcedureAsync(SqlProcedure procedure)
    {
        return Task.Factory.StartNew(() => RunProcedure(procedure));
    }

    protected static void RunProcedure(SqlProcedure procedure)
    {
        if (procedure == null || string.IsNullOrEmpty(procedure.SqlText))
            throw new ArgumentNullException(nameof(procedure));

        try
        {
            Log($"Start: {procedure.SqlText}", procedure);

            using (var connection = new SqlConnection("Data Source = localhost; Initial Catalog = master; Integrated Security = True; "))
            {
                connection.Open();
                connection.InfoMessage += Connection_InfoMessage;
                var command = new SqlCommand(procedure.SqlText, connection);
                command.CommandTimeout = _timeOut;
                command.ExecuteNonQuery();

                procedure.IsSuccess = true;
                procedure.ErrorText = "";
            }            
        }

        catch (Exception e)
        {
            procedure.IsSuccess = false;
            procedure.ErrorText = e.Message;

            Log($"Error: {procedure.ErrorText}", procedure);
        }

        finally
        {
            Log("End", procedure);
        }
    }

    private static void Connection_InfoMessage(object sender, SqlInfoMessageEventArgs e)
    {
        /*
        if (e.Errors != null && e.Errors.Count > 0)
        {
            for (int i = 0; i < e.Errors.Count; i++)
            {
                Log($"Error in line {e.Errors[i].LineNumber}: {e.Errors[i].Message}");
            }  
        }
        */

        if (!string.IsNullOrEmpty(e.Message) && !e.Message.Contains("row affected"))
            Log(e.Message);
    }

    protected static void Log(string message, SqlProcedure procedure = null)
    {
        var command = "";
        if (procedure != null)
            command = $" SQL command {procedure.Id}";

        var m = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}{command}: {message}";

        _messages.Enqueue(m);
    }

    private static void PrintMessages()
    {
        while (!_messages.IsEmpty)
        {
            string message = null;
            _messages.TryDequeue(out message);
            if(message.Length <= _maxMessageSize)
                SqlContext.Pipe.Send(message);
            else
                SqlContext.Pipe.Send(message.Substring(0, _maxMessageSize - 1));
        }
    }
}
