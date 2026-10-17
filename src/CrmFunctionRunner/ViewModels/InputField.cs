using CrmFunctionRunner.Catalog;
using CrmFunctionRunner.Infrastructure;
using MyConsole;
using System;
using System.Data;
using System.IO;
using System.Linq;

namespace CrmFunctionRunner.ViewModels
{
    /// <summary>One input on the form. The view picks an editor from <see cref="Kind"/>.</summary>
    public sealed class InputField : Observable
    {
        public InputSpec Spec { get; }
        public InputField(InputSpec spec)
        {
            Spec = spec;
            _value = spec.Default ?? "";
        }

        public InputKind Kind => Spec.Kind;
        public string Label => Spec.Label + (Spec.Required ? " *" : "");
        public string Help => Spec.Help;
        public bool HasHelp => !string.IsNullOrEmpty(Help);

        string _value;
        public string Value
        {
            get => _value;
            set { if (Set(ref _value, value)) { Raise(nameof(BoolValue)); if (Kind == InputKind.ExcelFile) LoadPreview(); } }
        }

        public bool BoolValue
        {
            get => Value == "true";
            set => Value = value ? "true" : "false";
        }

        // Excel preview
        DataView _preview;
        public DataView Preview { get => _preview; private set => Set(ref _preview, value); }
        string _previewText;
        public string PreviewText { get => _previewText; private set => Set(ref _previewText, value); }

        void LoadPreview()
        {
            Preview = null;
            PreviewText = null;
            if (string.IsNullOrWhiteSpace(Value) || !File.Exists(Value)) return;
            try
            {
                DataTable table = Applications.ReadExcelFiles(Value);
                if (table == null) { PreviewText = "The file has no sheets."; return; }
                var top = table.Clone();
                foreach (DataRow row in table.Rows.Cast<DataRow>().Take(5)) top.ImportRow(row);
                Preview = top.DefaultView;
                PreviewText = string.Format(Loc.T("RowsInFile"), table.Rows.Count);
            }
            catch (Exception e)
            {
                PreviewText = "Could not read the file: " + e.Message;
            }
        }

        public void Refresh() => RaiseAll();

        /// <summary>Returns an error message, or null when the value is valid.</summary>
        public string Validate()
        {
            string v = (Value ?? "").Trim();
            if (Kind == InputKind.YesNo) return null;
            if (v.Length == 0) return Spec.Required ? string.Format(Loc.T("Err.Required"), Spec.Label) : null;
            switch (Kind)
            {
                case InputKind.Guid:
                    return Guid.TryParse(v, out _) ? null : string.Format(Loc.T("Err.Guid"), Spec.Label);
                case InputKind.GuidList:
                    string bad = InputValues.SplitList(v).FirstOrDefault(x => !Guid.TryParse(x, out _));
                    return bad == null ? null : string.Format(Loc.T("Err.GuidList"), Spec.Label, bad);
                case InputKind.Integer:
                    return int.TryParse(v, out _) ? null : string.Format(Loc.T("Err.Int"), Spec.Label);
                case InputKind.ExcelFile:
                    return File.Exists(v) ? null : string.Format(Loc.T("Err.File"), Spec.Label);
                default:
                    return null;
            }
        }
    }
}
