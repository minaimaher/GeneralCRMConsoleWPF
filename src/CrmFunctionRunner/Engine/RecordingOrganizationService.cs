using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace CrmFunctionRunner.Engine
{
    public enum ChangeStatus { Planned, Done, Failed }

    /// <summary>One changed field (or one operation without fields) — a row in the evidence CSV.</summary>
    public sealed class ChangeRecord
    {
        public int Seq { get; set; }
        public DateTime Time { get; set; }
        public string Operation { get; set; }
        public string Entity { get; set; }
        public string RecordId { get; set; }
        public string Field { get; set; }
        public string Before { get; set; }
        public string After { get; set; }
        public ChangeStatus Status { get; set; }
        public string Error { get; set; }
        public string StatusText => Status.ToString();
    }

    /// <summary>
    /// Wraps the CRM connection used by a run.
    /// Reads pass straight through. Every write (create, update, delete, associate, status change,
    /// workflow, transaction…) is recorded with before/after values. In a dry run, writes are recorded
    /// and not sent. A Stop request ends the run at the next CRM call.
    /// </summary>
    public sealed class RecordingOrganizationService : IOrganizationService
    {
        readonly IOrganizationService _inner;
        readonly CancellationToken _cancel;
        readonly bool _captureBefore;
        readonly object _lock = new object();
        int _seq;

        public bool DryRun { get; }
        public int Reads { get; private set; }
        public List<ChangeRecord> Changes { get; } = new List<ChangeRecord>();
        public event Action<ChangeRecord> Changed;

        public RecordingOrganizationService(IOrganizationService inner, bool dryRun, bool captureBefore, CancellationToken cancel)
        {
            _inner = inner;
            DryRun = dryRun;
            _captureBefore = captureBefore;
            _cancel = cancel;
        }

        /* ---------------- reads ---------------- */

        public Entity Retrieve(string entityName, Guid id, ColumnSet columnSet)
        {
            Check(); Reads++;
            return _inner.Retrieve(entityName, id, columnSet);
        }

        public EntityCollection RetrieveMultiple(QueryBase query)
        {
            Check(); Reads++;
            return _inner.RetrieveMultiple(query);
        }

        /* ---------------- writes ---------------- */

        public Guid Create(Entity entity)
        {
            Check();
            var rows = FieldRows("Create", entity, null);
            if (DryRun) { Finish(rows, ChangeStatus.Planned); return entity.Id != Guid.Empty ? entity.Id : Guid.NewGuid(); }
            try
            {
                Guid id = _inner.Create(entity);
                foreach (var r in rows) r.RecordId = id.ToString();
                Finish(rows, ChangeStatus.Done);
                return id;
            }
            catch (Exception e) { Finish(rows, ChangeStatus.Failed, e); throw; }
        }

        public void Update(Entity entity)
        {
            Check();
            var rows = FieldRows("Update", entity, ReadBefore(entity));
            if (DryRun) { Finish(rows, ChangeStatus.Planned); return; }
            try { _inner.Update(entity); Finish(rows, ChangeStatus.Done); }
            catch (Exception e) { Finish(rows, ChangeStatus.Failed, e); throw; }
        }

        public void Delete(string entityName, Guid id)
        {
            Check();
            var rows = new List<ChangeRecord> { Row("Delete", entityName, id, "(record)", "exists", "deleted") };
            if (DryRun) { Finish(rows, ChangeStatus.Planned); return; }
            try { _inner.Delete(entityName, id); Finish(rows, ChangeStatus.Done); }
            catch (Exception e) { Finish(rows, ChangeStatus.Failed, e); throw; }
        }

        public void Associate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
        {
            Check();
            var rows = relatedEntities.Select(r => Row("Associate", entityName, entityId, relationship.SchemaName, "", Describe(r))).ToList();
            if (DryRun) { Finish(rows, ChangeStatus.Planned); return; }
            try { _inner.Associate(entityName, entityId, relationship, relatedEntities); Finish(rows, ChangeStatus.Done); }
            catch (Exception e) { Finish(rows, ChangeStatus.Failed, e); throw; }
        }

        public void Disassociate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
        {
            Check();
            var rows = relatedEntities.Select(r => Row("Disassociate", entityName, entityId, relationship.SchemaName, Describe(r), "")).ToList();
            if (DryRun) { Finish(rows, ChangeStatus.Planned); return; }
            try { _inner.Disassociate(entityName, entityId, relationship, relatedEntities); Finish(rows, ChangeStatus.Done); }
            catch (Exception e) { Finish(rows, ChangeStatus.Failed, e); throw; }
        }

        public OrganizationResponse Execute(OrganizationRequest request)
        {
            Check();
            if (IsRead(request)) { Reads++; return _inner.Execute(request); }

            var rows = RowsFor(request);
            if (DryRun) { Finish(rows, ChangeStatus.Planned); return FakeResponse(request); }
            try
            {
                var response = _inner.Execute(request);
                Finish(rows, ChangeStatus.Done);
                return response;
            }
            catch (Exception e) { Finish(rows, ChangeStatus.Failed, e); throw; }
        }

        /* ---------------- helpers ---------------- */

        void Check() => _cancel.ThrowIfCancellationRequested();

        static bool IsRead(OrganizationRequest r)
        {
            string n = r.RequestName ?? r.GetType().Name;
            return n.StartsWith("Retrieve", StringComparison.OrdinalIgnoreCase)
                || n == "WhoAmI" || n == "FetchXmlToQueryExpression" || n == "QueryExpressionToFetchXml"
                || r is RetrieveRequest || r is RetrieveMultipleRequest || r is WhoAmIRequest;
        }

        Entity ReadBefore(Entity target)
        {
            if (!_captureBefore || target.Id == Guid.Empty || target.Attributes.Count == 0) return null;
            try
            {
                var cols = target.Attributes.Keys.Where(k => k != target.LogicalName + "id").ToArray();
                if (cols.Length == 0) return null;
                Reads++;
                return _inner.Retrieve(target.LogicalName, target.Id, new ColumnSet(cols));
            }
            catch { return null; }
        }

        List<ChangeRecord> FieldRows(string op, Entity e, Entity before)
        {
            var rows = new List<ChangeRecord>();
            foreach (var kv in e.Attributes)
            {
                if (kv.Key == e.LogicalName + "id") continue;
                string b = before == null ? (op == "Create" ? "" : "?") : (before.Contains(kv.Key) ? Format(before[kv.Key]) : "");
                rows.Add(Row(op, e.LogicalName, e.Id, kv.Key, b, Format(kv.Value)));
            }
            if (rows.Count == 0) rows.Add(Row(op, e.LogicalName, e.Id, "(record)", "", ""));
            return rows;
        }

        List<ChangeRecord> RowsFor(OrganizationRequest request)
        {
            switch (request)
            {
                case CreateRequest c: return FieldRows("Create", c.Target, null);
                case UpdateRequest u: return FieldRows("Update", u.Target, ReadBefore(u.Target));
                case DeleteRequest d: return new List<ChangeRecord> { Row("Delete", d.Target.LogicalName, d.Target.Id, "(record)", "exists", "deleted") };
                case SetStateRequest s:
                    return new List<ChangeRecord> { Row("SetState", s.EntityMoniker.LogicalName, s.EntityMoniker.Id, "statecode / statuscode", "", $"{s.State?.Value} / {s.Status?.Value}") };
                case ExecuteWorkflowRequest w:
                    return new List<ChangeRecord> { Row("RunWorkflow", "", w.EntityId, "workflow", "", w.WorkflowId.ToString()) };
                case AssignRequest a:
                    return new List<ChangeRecord> { Row("Assign", a.Target.LogicalName, a.Target.Id, "ownerid", "", Describe(a.Assignee)) };
                case AssociateRequest ar:
                    return ar.RelatedEntities.Select(r => Row("Associate", ar.Target.LogicalName, ar.Target.Id, ar.Relationship.SchemaName, "", Describe(r))).ToList();
                case DisassociateRequest dr:
                    return dr.RelatedEntities.Select(r => Row("Disassociate", dr.Target.LogicalName, dr.Target.Id, dr.Relationship.SchemaName, Describe(r), "")).ToList();
                case AddMembersTeamRequest t:
                    return t.MemberIds.Select(m => Row("AddTeamMember", "team", t.TeamId, "member", "", m.ToString())).ToList();
                case ExecuteTransactionRequest tx:
                    return tx.Requests.SelectMany(RowsFor).ToList();
                case ExecuteMultipleRequest mx:
                    return mx.Requests.SelectMany(RowsFor).ToList();
                default:
                    var target = request.Parameters.Contains("Target") ? request.Parameters["Target"] : null;
                    var er = target as EntityReference ?? (target as Entity)?.ToEntityReference();
                    return new List<ChangeRecord> { Row(request.RequestName ?? request.GetType().Name, er?.LogicalName ?? "", er?.Id ?? Guid.Empty, "(request)", "", "") };
            }
        }

        ChangeRecord Row(string op, string entity, Guid id, string field, string before, string after) => new ChangeRecord
        {
            Operation = op, Entity = entity, RecordId = id == Guid.Empty ? "" : id.ToString(),
            Field = field, Before = before, After = after
        };

        void Finish(List<ChangeRecord> rows, ChangeStatus status, Exception error = null)
        {
            lock (_lock)
            {
                foreach (var r in rows)
                {
                    r.Seq = ++_seq;
                    r.Time = DateTime.Now;
                    r.Status = status;
                    r.Error = error?.Message;
                    Changes.Add(r);
                }
            }
            foreach (var r in rows) Changed?.Invoke(r);
        }

        /// <summary>Response returned in a dry run so the calling code can carry on.</summary>
        static OrganizationResponse FakeResponse(OrganizationRequest request)
        {
            switch (request)
            {
                case CreateRequest c:
                    var cr = new CreateResponse();
                    cr.Results["id"] = c.Target.Id != Guid.Empty ? c.Target.Id : Guid.NewGuid();
                    return cr;
                case ExecuteTransactionRequest tx:
                    var tr = new ExecuteTransactionResponse();
                    var responses = new OrganizationResponseCollection();
                    foreach (var inner in tx.Requests) responses.Add(FakeResponse(inner));
                    tr.Results["Responses"] = responses;
                    return tr;
                case ExecuteMultipleRequest _:
                    var mr = new ExecuteMultipleResponse();
                    mr.Results["Responses"] = new ExecuteMultipleResponseItemCollection();
                    mr.Results["IsFaulted"] = false;
                    return mr;
            }
            // XxxRequest → XxxResponse from the same SDK assembly, when it exists.
            var type = request.GetType();
            var responseType = type.Assembly.GetType(type.FullName.Substring(0, type.FullName.Length - "Request".Length) + "Response", false);
            if (responseType != null && typeof(OrganizationResponse).IsAssignableFrom(responseType))
            {
                try { return (OrganizationResponse)Activator.CreateInstance(responseType); } catch { }
            }
            return new OrganizationResponse { ResponseName = request.RequestName };
        }

        public static string Describe(EntityReference r) =>
            r == null ? "" : $"{r.LogicalName}:{r.Id}" + (string.IsNullOrEmpty(r.Name) ? "" : $" ({r.Name})");

        public static string Format(object v)
        {
            switch (v)
            {
                case null: return "";
                case EntityReference r: return Describe(r);
                case OptionSetValue o: return o.Value.ToString(CultureInfo.InvariantCulture);
                case OptionSetValueCollection oc: return string.Join(";", oc.Select(o => o.Value));
                case Money m: return m.Value.ToString(CultureInfo.InvariantCulture);
                case DateTime d: return d.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                case decimal dec: return dec.ToString(CultureInfo.InvariantCulture);
                case double dbl: return dbl.ToString(CultureInfo.InvariantCulture);
                case AliasedValue a: return Format(a.Value);
                case EntityCollection ec: return $"({ec.Entities.Count} records)";
                case Entity e: return $"{e.LogicalName}:{e.Id}";
                default: return Convert.ToString(v, CultureInfo.InvariantCulture);
            }
        }
    }
}
