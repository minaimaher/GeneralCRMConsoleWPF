using CrmFunctionRunner.Catalog;
using MyConsole.Helpers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CrmFunctionRunner.Engine
{
    public enum RunOutcome { Succeeded, Failed, Cancelled }

    public sealed class RunRequest
    {
        public FunctionInfo Function { get; set; }
        public Dictionary<string, string> Values { get; set; }
        public bool DryRun { get; set; }
        public string Ticket { get; set; }
        public string Approver { get; set; }
        public Connection Connection { get; set; }
        public string DryRunFolder { get; set; }
    }

    public sealed class RunResult
    {
        public RunOutcome Outcome { get; set; }
        public Exception Error { get; set; }
        public string Folder { get; set; }
        public object ReturnValue { get; set; }
        public TimeSpan Duration { get; set; }
        public int Reads { get; set; }
        public List<ChangeRecord> Changes { get; set; }
        public int Count(ChangeStatus s) => Changes.Count(c => c.Status == s);
        public int RecordsTouched => Changes.Where(c => c.RecordId != "").Select(c => c.RecordId).Distinct().Count();
    }

    /// <summary>
    /// Runs one console function on a background thread, captures its console output,
    /// routes its CRM calls through <see cref="RecordingOrganizationService"/>, and writes
    /// the evidence folder: inputs.json, output.log, changes.csv, summary.json, plus any files the function writes.
    /// </summary>
    public static class RunSession
    {
        static readonly SemaphoreSlim OneAtATime = new SemaphoreSlim(1, 1);

        public static string EvidenceRoot
        {
            get
            {
                string configured = ConfigurationManager.AppSettings["EvidenceFolder"];
                string root = string.IsNullOrWhiteSpace(configured)
                    ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CRM Function Runner", "Runs")
                    : Environment.ExpandEnvironmentVariables(configured);
                Directory.CreateDirectory(root);
                return root;
            }
        }

        public static bool Setting(string key, bool fallback) =>
            bool.TryParse(ConfigurationManager.AppSettings[key], out bool v) ? v : fallback;

        public static async Task<RunResult> RunAsync(RunRequest req, Action<string> onLine, Action<ChangeRecord> onChange, CancellationToken cancel)
        {
            if (!await OneAtATime.WaitAsync(0)) throw new InvalidOperationException("Another run is in progress.");
            try
            {
                return await Task.Factory.StartNew(() => Run(req, onLine, onChange, cancel),
                    CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
            finally { OneAtATime.Release(); }
        }

        static RunResult Run(RunRequest req, Action<string> onLine, Action<ChangeRecord> onChange, CancellationToken cancel)
        {
            DateTime started = DateTime.Now;
            string folder = Path.Combine(EvidenceRoot,
                $"{started:yyyy-MM-dd_HHmmss}_{(req.DryRun ? "DRYRUN" : "LIVE")}_{req.Function.Key}_{Safe(req.Ticket)}");
            Directory.CreateDirectory(folder);

            // Keep a copy of the Excel file actually used, as evidence, and read from that copy.
            var values = new Dictionary<string, string>(req.Values);
            if (values.TryGetValue("excel", out string excel) && File.Exists(excel))
            {
                string copy = Path.Combine(folder, "input_" + Path.GetFileName(excel));
                File.Copy(excel, copy, true);
                values["excel"] = copy;
            }
            var input = new InputValues(values);

            var recorder = new RecordingOrganizationService(req.Connection.Create(), req.DryRun, Setting("CaptureBeforeValues", true), cancel);
            recorder.Changed += onChange;

            RunInputs.Reset();
            RunInputs.ExcelPath = values.ContainsKey("excel") ? values["excel"] : null;
            RunInputs.Skip = input.Has("skip") ? input.IntOrNull("skip") : null;
            RunInputs.Take = input.Has("take") ? input.IntOrNull("take") : null;
            RunInputs.Connect = () => recorder; // reconnects inside the console code stay recorded

            var context = new Dictionary<string, object>
            {
                ["function"] = req.Function.Title,
                ["method"] = req.Function.Method,
                ["sopReference"] = req.Function.SopRef,
                ["mode"] = req.DryRun ? "Dry run (nothing saved)" : "Live run",
                ["environment"] = req.Connection.Environment.Name,
                ["production"] = req.Connection.Environment.IsProduction,
                ["ticket"] = req.Ticket,
                ["approvedBy"] = req.Approver,
                ["crmUser"] = req.Connection.UserName,
                ["windowsUser"] = Environment.UserDomainName + "\\" + Environment.UserName,
                ["machine"] = Environment.MachineName,
                ["started"] = started.ToString("yyyy-MM-dd HH:mm:ss"),
                ["dryRunEvidence"] = req.DryRunFolder,
                ["inputs"] = values
            };
            File.WriteAllText(Path.Combine(folder, "inputs.json"), Evidence.Json(context), Encoding.UTF8);

            var result = new RunResult { Folder = folder };
            TextWriter oldOut = Console.Out;
            string oldDir = Environment.CurrentDirectory;
            using (var log = new StreamWriter(Path.Combine(folder, "output.log"), false, new UTF8Encoding(true)))
            {
                var lineWriter = new LineWriter(line => { log.WriteLine(line); onLine?.Invoke(line); });
                Console.SetOut(lineWriter);
                Environment.CurrentDirectory = folder; // trace files the functions write land in the evidence folder
                try
                {
                    Console.WriteLine($"{(req.DryRun ? "DRY RUN" : "LIVE RUN")} · {req.Function.Method} · {req.Connection.Environment.Name} · {req.Ticket}");
                    result.ReturnValue = req.Function.Run(recorder, input);
                    result.Outcome = cancel.IsCancellationRequested ? RunOutcome.Cancelled : RunOutcome.Succeeded;
                    if (result.ReturnValue != null) Console.WriteLine(Convert.ToString(result.ReturnValue));
                }
                catch (Exception e)
                {
                    result.Outcome = cancel.IsCancellationRequested ? RunOutcome.Cancelled : RunOutcome.Failed;
                    result.Error = e;
                    if (result.Outcome == RunOutcome.Failed) Console.WriteLine("ERROR: " + e);
                }
                finally
                {
                    lineWriter.Flush();
                    Console.SetOut(oldOut);
                    Environment.CurrentDirectory = oldDir;
                    RunInputs.Reset();
                    RunInputs.Connect = null;
                }
            }

            result.Duration = DateTime.Now - started;
            result.Reads = recorder.Reads;
            result.Changes = recorder.Changes.ToList();
            Evidence.WriteChangesCsv(Path.Combine(folder, "changes.csv"), result.Changes);

            context["finished"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            context["outcome"] = result.Outcome.ToString();
            context["error"] = result.Error?.Message;
            context["changesPlanned"] = result.Count(ChangeStatus.Planned);
            context["changesDone"] = result.Count(ChangeStatus.Done);
            context["changesFailed"] = result.Count(ChangeStatus.Failed);
            context["recordsTouched"] = result.RecordsTouched;
            context["recordsRead"] = result.Reads;
            context["result"] = result.ReturnValue == null ? null : Convert.ToString(result.ReturnValue);
            context.Remove("inputs");
            File.WriteAllText(Path.Combine(folder, "summary.json"), Evidence.Json(context), Encoding.UTF8);

            // One line per run in a shared audit log next to the run folders.
            context["folder"] = folder;
            File.AppendAllText(Path.Combine(EvidenceRoot, "audit.log"),
                Newtonsoft.Json.JsonConvert.SerializeObject(context) + Environment.NewLine, Encoding.UTF8);
            return result;
        }

        static string Safe(string s)
        {
            var clean = new string((s ?? "").Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray());
            return clean.Length == 0 ? "no-ticket" : clean.Length > 40 ? clean.Substring(0, 40) : clean;
        }

        /// <summary>Turns console writes into whole lines.</summary>
        sealed class LineWriter : TextWriter
        {
            readonly Action<string> _line;
            readonly StringBuilder _buffer = new StringBuilder();
            public LineWriter(Action<string> line) { _line = line; }
            public override Encoding Encoding => Encoding.UTF8;

            public override void Write(char value)
            {
                if (value == '\n') { _line(_buffer.ToString().TrimEnd('\r')); _buffer.Clear(); }
                else _buffer.Append(value);
            }

            public override void Write(string value)
            {
                if (value == null) return;
                foreach (char c in value) Write(c);
            }

            public override void WriteLine(string value) { Write(value); Write('\n'); }

            public override void Flush()
            {
                if (_buffer.Length > 0) { _line(_buffer.ToString()); _buffer.Clear(); }
            }
        }
    }
}
