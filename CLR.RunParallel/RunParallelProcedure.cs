using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Xml;
using System.Threading.Tasks;
using Imato.CLR.Logger;

public class RunParallelProcedure
{
    private static readonly int _timeOut = 300;

    [SqlProcedure]
    public static void RunParallel(string sqlProcedures, bool noOutput = false, int maxThreads = 5)
    {
        var log = new Logger(LogLevel.Info);

        var tasks = new List<Task>();
        var procedures = ParseXmlParameter(sqlProcedures);

        if (maxThreads <= 0)
            maxThreads = 1;

        if (procedures.Count > 0)
        {
            log.Info($"Start executing {procedures.Count} sql commands in parallel");

            foreach (var p in procedures)
            {
                tasks.Add(RunProcedureAsync(p, log));
                Wait(tasks, tasks.Count, maxThreads);
            }

            Wait(tasks, maxThreads, maxThreads);

            log.Info($"End executing {procedures.Count} sql commands in parallel");

            if (!noOutput)
            {
                SendResultToOutput(procedures, log);
            }
        }

        log.Dispose();
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
                Text = nodes[i].SelectSingleNode("SqlText").InnerText
            };

            procedures.Add(procedure);
        }

        return procedures;
    }

    protected static void SendResultToOutput(IEnumerable<SqlProcedure> procedures, Logger log)
    {
        foreach (var p in procedures)
        {
            log.Output(p);
        }
    }

    protected static Task RunProcedureAsync(SqlProcedure procedure, Logger log)
    {
        return Task.Factory.StartNew(() => RunProcedure(procedure, log));
    }

    protected static void RunProcedure(SqlProcedure procedure, Logger log)
    {
        if (procedure == null || string.IsNullOrEmpty(procedure.Text))
            throw new ArgumentNullException(nameof(procedure));

        try
        {
            log.Info($"Start: {procedure.Text}", procedure);

            using (var connection = new SqlConnection("Data Source = localhost; Initial Catalog = master; Integrated Security = True; "))
            {
                connection.Open();
                connection.InfoMessage += (s, e) => Connection_InfoMessage(s, e, log);
                var command = new SqlCommand(procedure.Text, connection);
                command.CommandTimeout = _timeOut;
                command.ExecuteNonQuery();

                procedure.IsSuccess = true;
                procedure.Error = "";
            }
        }
        catch (Exception e)
        {
            procedure.IsSuccess = false;
            procedure.Error = e.Message;

            log.Error($"{procedure.Error}", procedure);
        }
        finally
        {
            log.Info("End", procedure);
        }
    }

    private static void Connection_InfoMessage(object sender, SqlInfoMessageEventArgs e, Logger log)
    {
        if (!string.IsNullOrEmpty(e.Message) && !e.Message.Contains("row affected"))
        {
            log.Info(e.Message);
        }
    }
}