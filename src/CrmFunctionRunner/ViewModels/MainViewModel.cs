using CrmFunctionRunner.Catalog;
using CrmFunctionRunner.Engine;
using CrmFunctionRunner.Infrastructure;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Threading;

namespace CrmFunctionRunner.ViewModels
{
    public enum Step { Pick, Inputs, DryRun, Confirm, Run, Result }

    public sealed class MainViewModel : Observable
    {
        const int MaxLogLines = 3000;
        const int MaxGridRows = 5000;

        public MainViewModel(Connection connection)
        {
            Connection = connection;
            Functions = CollectionViewSource.GetDefaultView(FunctionCatalog.All);
            Functions.GroupDescriptions.Add(new PropertyGroupDescription(nameof(FunctionInfo.Group)));
            Functions.Filter = o => string.IsNullOrWhiteSpace(Search) || ((FunctionInfo)o).SearchText.Contains(Search.Trim().ToLowerInvariant());

            _flush = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            _flush.Tick += (s, e) => FlushQueues();
            _flush.Start();

            Loc.Instance.LanguageChanged += () =>
            {
                Functions.Refresh();
                foreach (var f in Fields) f.Refresh();
                RaiseAll();
            };
        }

        /* ---------------- connection ---------------- */
        public Connection Connection { get; }
        public string EnvironmentName => Connection.Environment.Name;
        public bool IsProduction => Connection.Environment.IsProduction;
        public string SignedInAs => Connection.UserName;
        public bool AllowNeedsFix => RunSession.Setting("AllowNeedsFixFunctions", false);

        /* ---------------- catalog ---------------- */
        public ICollectionView Functions { get; }

        string _search;
        public string Search { get => _search; set { if (Set(ref _search, value)) Functions.Refresh(); } }

        FunctionInfo _selected;
        public FunctionInfo Selected
        {
            get => _selected;
            set
            {
                if (IsBusy || !Set(ref _selected, value)) return;
                Fields.Clear();
                if (value != null) foreach (var spec in value.Inputs) Fields.Add(new InputField(spec));
                Ticket = ""; Approver = ""; Reviewed = false; ExternalAck = false; ConfirmText = "";
                DryRunResult = null; LiveResult = null; ClearOutput();
                Error = null;
                Step = value == null ? Step.Pick : Step.Inputs;
                RaiseAll();
            }
        }
        public bool HasSelection => Selected != null;
        public bool IsBlocked => Selected != null && Selected.Status == FunctionStatus.NeedsFix && !AllowNeedsFix;
        public bool CanRunDry => Selected != null && Selected.DryRunSupported && !IsBlocked;
        public bool CanSkipDry => Selected != null && !Selected.DryRunSupported && !IsBlocked;
        public bool HasNote => !string.IsNullOrEmpty(Selected?.Note);
        public bool HasExcelColumns => !string.IsNullOrEmpty(Selected?.ExcelColumns);
        public bool HasInputs => Fields.Count > 0;
        public bool HasSop => !string.IsNullOrEmpty(Selected?.SopRef);

        public ObservableCollection<InputField> Fields { get; } = new ObservableCollection<InputField>();

        /* ---------------- step ---------------- */
        Step _step = Step.Pick;
        public Step Step
        {
            get => _step;
            set { if (Set(ref _step, value)) RaiseAll(); }
        }

        string _ticket = "";
        public string Ticket { get => _ticket; set => Set(ref _ticket, value); }

        string _error;
        public string Error { get => _error; set { Set(ref _error, value); Raise(nameof(HasError)); } }
        public bool HasError => !string.IsNullOrEmpty(Error);

        /* ---------------- confirm ---------------- */
        string _approver = "";
        public string Approver { get => _approver; set { Set(ref _approver, value); Raise(nameof(CanRunLive)); } }
        bool _reviewed;
        public bool Reviewed { get => _reviewed; set { Set(ref _reviewed, value); Raise(nameof(CanRunLive)); } }
        bool _externalAck;
        public bool ExternalAck { get => _externalAck; set { Set(ref _externalAck, value); Raise(nameof(CanRunLive)); } }
        string _confirmText = "";
        public string ConfirmText { get => _confirmText; set { Set(ref _confirmText, value); Raise(nameof(CanRunLive)); } }

        /// <summary>In production the user types the number of planned changes (or RUN when there was no dry run).</summary>
        public string ConfirmWord => DryRunResult != null ? DryRunResult.Count(ChangeStatus.Planned).ToString() : "RUN";
        public string ConfirmPrompt => string.Format(Loc.T("TypeToConfirm"), ConfirmWord);
        public bool NeedsTypedConfirm => IsProduction && Selected != null && Selected.ChangesData;
        public bool NeedsExternalAck => Selected != null && !Selected.DryRunSupported;
        public string RunButtonText => string.Format(Loc.T("RunOn"), EnvironmentName);

        public bool CanRunLive =>
            Step == Step.Confirm && !IsBusy && !string.IsNullOrWhiteSpace(Approver)
            && (Reviewed || DryRunResult == null)
            && (!NeedsExternalAck || ExternalAck)
            && (!NeedsTypedConfirm || ConfirmText.Trim() == ConfirmWord);

        public string ConfirmSummary
        {
            get
            {
                if (Selected == null) return "";
                var sb = new StringBuilder();
                sb.AppendLine($"{Loc.T("Function")}: {Selected.Title}  ({Selected.Method})");
                sb.AppendLine($"{Loc.T("Environment")}: {EnvironmentName}");
                sb.AppendLine($"{Loc.T("Ticket")}: {Ticket}");
                foreach (var f in Fields.Where(f => !string.IsNullOrWhiteSpace(f.Value)))
                {
                    string v = f.Value.Trim();
                    if (v.Length > 160) v = v.Substring(0, 160) + "…";
                    sb.AppendLine($"{f.Spec.Label}: {v.Replace(Environment.NewLine, ", ")}");
                }
                if (DryRunResult != null) sb.AppendLine($"{Loc.T("PlannedSummary")}: {DryRunSummary}");
                return sb.ToString().TrimEnd();
            }
        }

        /* ---------------- running ---------------- */
        bool _busy;
        public bool IsBusy { get => _busy; set { Set(ref _busy, value); Raise(nameof(CanRunLive)); Raise(nameof(IsIdle)); } }
        public bool IsIdle => !IsBusy;
        CancellationTokenSource _cts;

        RunResult _dry;
        public RunResult DryRunResult { get => _dry; set { Set(ref _dry, value); Raise(nameof(DryRunSummary)); Raise(nameof(HasDryRun)); } }
        public bool HasDryRun => DryRunResult != null;
        RunResult _live;
        public RunResult LiveResult { get => _live; set => Set(ref _live, value); }

        public string DryRunSummary => DryRunResult == null ? "" :
            string.Format(Loc.T("DryCounts"), DryRunResult.Count(ChangeStatus.Planned), DryRunResult.RecordsTouched, DryRunResult.Reads)
            + (DryRunResult.Outcome == RunOutcome.Succeeded ? "" : " · " + Loc.T("Outcome." + DryRunResult.Outcome));

        public RunResult ShownResult => Step == Step.Result ? LiveResult : DryRunResult;
        public string ResultTitle => LiveResult == null ? "" : Loc.T("Outcome." + LiveResult.Outcome);
        public bool ResultSucceeded => LiveResult?.Outcome == RunOutcome.Succeeded;
        public string ResultCounts => LiveResult == null ? "" :
            string.Format(Loc.T("Counts"), LiveResult.Count(ChangeStatus.Done), LiveResult.Count(ChangeStatus.Failed), LiveResult.Reads);
        public string ResultValue => LiveResult?.ReturnValue == null ? null : Convert.ToString(LiveResult.ReturnValue);
        public bool HasResultValue => !string.IsNullOrEmpty(ResultValue);
        public string ResultError => LiveResult?.Error?.Message;
        public bool HasResultError => !string.IsNullOrEmpty(ResultError);
        public string EvidenceFolder => (LiveResult ?? DryRunResult)?.Folder;

        // output and change grid, filled in batches so long runs do not freeze the window
        public ObservableCollection<ChangeRecord> Changes { get; } = new ObservableCollection<ChangeRecord>();
        readonly ConcurrentQueue<string> _lines = new ConcurrentQueue<string>();
        readonly ConcurrentQueue<ChangeRecord> _changes = new ConcurrentQueue<ChangeRecord>();
        readonly LinkedList<string> _logLines = new LinkedList<string>();
        readonly DispatcherTimer _flush;
        string _logText = "";
        public string LogText { get => _logText; private set => Set(ref _logText, value); }
        int _changeCount;
        public string ChangeCountText => _changeCount > Changes.Count ? $"{Changes.Count} / {_changeCount}" : _changeCount.ToString();

        void ClearOutput()
        {
            while (_lines.TryDequeue(out _)) { }
            while (_changes.TryDequeue(out _)) { }
            _logLines.Clear(); LogText = "";
            Changes.Clear(); _changeCount = 0; Raise(nameof(ChangeCountText));
        }

        void FlushQueues()
        {
            bool any = false;
            while (_lines.TryDequeue(out string line))
            {
                _logLines.AddLast(line);
                if (_logLines.Count > MaxLogLines) _logLines.RemoveFirst();
                any = true;
            }
            if (any) LogText = string.Join(Environment.NewLine, _logLines);
            bool anyChange = false;
            while (_changes.TryDequeue(out var c))
            {
                _changeCount++;
                if (Changes.Count < MaxGridRows) Changes.Add(c);
                anyChange = true;
            }
            if (anyChange) Raise(nameof(ChangeCountText));
        }

        /// <summary>Checks the ticket and inputs. Returns false and sets <see cref="Error"/> when something is missing.</summary>
        public bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(Ticket)) { Error = Loc.T("Err.Ticket"); return false; }
            foreach (var f in Fields)
            {
                string err = f.Validate();
                if (err != null) { Error = err; return false; }
            }
            Error = null;
            return true;
        }

        RunRequest Request(bool dry) => new RunRequest
        {
            Function = Selected,
            Values = Fields.ToDictionary(f => f.Spec.Key, f => f.Value ?? ""),
            DryRun = dry,
            Ticket = Ticket.Trim(),
            Approver = Approver?.Trim(),
            Connection = Connection,
            DryRunFolder = DryRunResult?.Folder
        };

        public async Task StartDryRunAsync()
        {
            if (!ValidateInputs()) return;
            DryRunResult = null;
            Step = Step.DryRun;
            DryRunResult = await ExecuteAsync(Request(true));
            RaiseAll();
        }

        public void GoToConfirm()
        {
            if (DryRunResult == null && !ValidateInputs()) return;
            Reviewed = false; ExternalAck = false; ConfirmText = "";
            Step = Step.Confirm;
        }

        public async Task StartLiveRunAsync()
        {
            if (!CanRunLive) return;
            Step = Step.Run;
            LiveResult = await ExecuteAsync(Request(false));
            Step = Step.Result;
        }

        async Task<RunResult> ExecuteAsync(RunRequest request)
        {
            ClearOutput();
            Error = null;
            IsBusy = true;
            _cts = new CancellationTokenSource();
            try
            {
                return await RunSession.RunAsync(request, line => _lines.Enqueue(line), c => _changes.Enqueue(c), _cts.Token);
            }
            catch (Exception e)
            {
                Error = e.Message;
                return null;
            }
            finally
            {
                FlushQueues();
                IsBusy = false;
                _cts.Dispose(); _cts = null;
            }
        }

        public void Stop() => _cts?.Cancel();

        public void BackToInputs() { if (!IsBusy) Step = Step.Inputs; }

        public void Reset() { if (!IsBusy) Selected = null; }

        /// <summary>Closure comment in the CRM-INC-05 service request format.</summary>
        public string ClosureComment()
        {
            if (LiveResult == null || Selected == null) return "";
            var r = LiveResult;
            int records = r.RecordsTouched;
            if (Loc.IsArabic)
                return $"الاعتماد: {Approver}\n" +
                       $"الإجراء المتخذ: {Selected.Title} ({Selected.Method}) على بيئة {EnvironmentName}{(string.IsNullOrEmpty(Selected.SopRef) ? "" : " — " + Selected.SopRef)}\n" +
                       $"السجلات المتأثرة: {r.Count(ChangeStatus.Done)} تغييرًا على {records} سجلًا؛ فشل {r.Count(ChangeStatus.Failed)}\n" +
                       $"الأدلة: {r.Folder}\n" +
                       $"دالة تطبيق الكونسول: {Selected.Method}";
            return $"Approval: {Approver}\n" +
                   $"Action taken: {Selected.Title} ({Selected.Method}) on {EnvironmentName}{(string.IsNullOrEmpty(Selected.SopRef) ? "" : " — " + Selected.SopRef)}\n" +
                   $"Records affected: {r.Count(ChangeStatus.Done)} changes on {records} records; {r.Count(ChangeStatus.Failed)} failed\n" +
                   $"Evidence: {r.Folder}\n" +
                   $"Console function: {Selected.Method}";
        }
    }
}
