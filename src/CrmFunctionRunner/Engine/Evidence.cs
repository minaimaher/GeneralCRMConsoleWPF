using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CrmFunctionRunner.Engine
{
    /// <summary>Files written into a run's evidence folder.</summary>
    public static class Evidence
    {
        static readonly Encoding Utf8Bom = new UTF8Encoding(true); // Excel opens Arabic text correctly with a BOM

        public static string Csv(string v)
        {
            v = v ?? "";
            return v.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0 ? "\"" + v.Replace("\"", "\"\"") + "\"" : v;
        }

        public static void WriteChangesCsv(string path, IEnumerable<ChangeRecord> rows)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Seq,Time,Operation,Entity,RecordId,Field,Before,After,Status,Error");
            foreach (var r in rows)
            {
                sb.Append(r.Seq).Append(',')
                  .Append(r.Time.ToString("yyyy-MM-dd HH:mm:ss")).Append(',')
                  .Append(Csv(r.Operation)).Append(',').Append(Csv(r.Entity)).Append(',').Append(Csv(r.RecordId)).Append(',')
                  .Append(Csv(r.Field)).Append(',').Append(Csv(r.Before)).Append(',').Append(Csv(r.After)).Append(',')
                  .Append(r.Status).Append(',').Append(Csv(r.Error)).AppendLine();
            }
            File.WriteAllText(path, sb.ToString(), Utf8Bom);
        }

        /// <summary>Writes records to a CSV in the current directory (the run's evidence folder while a run is active).</summary>
        public static void WriteRecordsCsv(IList<Entity> records, string fileName)
        {
            var columns = records.SelectMany(e => e.Attributes.Keys).Distinct().OrderBy(k => k).ToList();
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", new[] { "id", "entity" }.Concat(columns).Select(Csv)));
            foreach (var e in records)
            {
                var cells = new List<string> { e.Id.ToString(), e.LogicalName };
                cells.AddRange(columns.Select(c => e.Contains(c) ? RecordingOrganizationService.Format(e[c]) : ""));
                sb.AppendLine(string.Join(",", cells.Select(Csv)));
            }
            File.WriteAllText(Path.Combine(Environment.CurrentDirectory, fileName), sb.ToString(), Utf8Bom);
        }

        public static string Json(IDictionary<string, object> values)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(values, Newtonsoft.Json.Formatting.Indented);
        }
    }
}
