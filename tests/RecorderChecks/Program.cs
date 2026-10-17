using CrmFunctionRunner.Engine;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

class Fake : IOrganizationService {
  public Dictionary<Guid, Entity> Db = new();
  public List<string> Sent = new();
  public Guid Create(Entity e){ Sent.Add("Create"); var id=Guid.NewGuid(); e.Id=id; Db[id]=e; return id; }
  public Entity Retrieve(string n, Guid id, ColumnSet c){ var e=Db[id]; var r=new Entity(n,id); foreach(var k in c.Columns) if(e.Contains(k)) r[k]=e[k]; return r; }
  public void Update(Entity e){ Sent.Add("Update"); foreach(var a in e.Attributes) Db[e.Id][a.Key]=a.Value; }
  public void Delete(string n, Guid id){ Sent.Add("Delete"); Db.Remove(id); }
  public OrganizationResponse Execute(OrganizationRequest r){ Sent.Add("Execute:"+r.RequestName); return new OrganizationResponse(); }
  public void Associate(string n, Guid id, Relationship r, EntityReferenceCollection c){ Sent.Add("Associate"); }
  public void Disassociate(string n, Guid id, Relationship r, EntityReferenceCollection c){ Sent.Add("Disassociate"); }
  public EntityCollection RetrieveMultiple(QueryBase q){ return new EntityCollection(Db.Values.ToList()); }
}
static class P {
  static int fails=0;
  static void Check(bool ok, string what){ Console.WriteLine((ok?"PASS ":"FAIL ")+what); if(!ok) fails++; }
  static int Main(){
    var fake=new Fake(); var id=Guid.NewGuid(); fake.Db[id]=new Entity("contact",id){ ["firstname"]="Old", ["ntw_status"]=new OptionSetValue(1) };
    // dry run update
    var dry=new RecordingOrganizationService(fake,true,true,CancellationToken.None);
    var upd=new Entity("contact",id){ ["firstname"]="New", ["ntw_status"]=new OptionSetValue(2) };
    dry.Update(upd);
    Check(fake.Sent.Count==0, "dry run: update not sent to CRM");
    Check((string)fake.Db[id]["firstname"]=="Old", "dry run: record unchanged");
    var row=dry.Changes.First(c=>c.Field=="firstname");
    Check(row.Before=="Old" && row.After=="New" && row.Status==ChangeStatus.Planned, "dry run: before/after captured as Planned");
    Check(dry.Changes.First(c=>c.Field=="ntw_status").Before=="1", "dry run: option set before value formatted");
    // dry set state + workflow + transaction
    var ss=(OrganizationResponse)dry.Execute(new SetStateRequest{EntityMoniker=new EntityReference("email",Guid.NewGuid()),State=new OptionSetValue(2),Status=new OptionSetValue(5)});
    Check(fake.Sent.Count==0 && ss is SetStateResponse, "dry run: SetState recorded, typed fake response returned");
    var tx=new ExecuteTransactionRequest{Requests=new OrganizationRequestCollection{ new UpdateRequest{Target=new Entity("contact",id){["firstname"]="Tx"}}, new CreateRequest{Target=new Entity("ntw_licenserenewrequest"){["ntw_failurereason"]="No Failure"}} }};
    var txr=(ExecuteTransactionResponse)dry.Execute(tx);
    Check(fake.Sent.Count==0 && txr.Responses.Count==2, "dry run: transaction not sent; fake response has 2 inner responses");
    Check(dry.Changes.Any(c=>c.Operation=="Create" && c.Entity=="ntw_licenserenewrequest"), "dry run: inner create of a transaction recorded");
    var newId=dry.Create(new Entity("account"){["name"]="X"});
    Check(newId!=Guid.Empty && fake.Sent.Count==0, "dry run: create returns a placeholder id and is not sent");
    dry.Delete("contact", id);
    Check(fake.Db.ContainsKey(id), "dry run: delete not sent");
    // reads pass through
    var r=dry.Retrieve("contact", id, new ColumnSet("firstname"));
    Check((string)r["firstname"]=="Old" && dry.Reads>=2, "reads pass through and are counted");
    var who=dry.Execute(new RetrieveMultipleRequest{Query=new QueryExpression("contact")});
    Check(fake.Sent.Contains("Execute:RetrieveMultiple"), "read requests through Execute are passed to CRM");
    // live
    fake.Sent.Clear();
    var live=new RecordingOrganizationService(fake,false,true,CancellationToken.None);
    live.Update(new Entity("contact",id){["firstname"]="Live"});
    Check(fake.Sent.SequenceEqual(new[]{"Update"}) && (string)fake.Db[id]["firstname"]=="Live", "live: update sent");
    var lrow=live.Changes.Single();
    Check(lrow.Before=="Old" && lrow.After=="Live" && lrow.Status==ChangeStatus.Done, "live: evidence row Done with before/after");
    // failure recorded
    try { live.Update(new Entity("contact",Guid.NewGuid()){["firstname"]="x"}); } catch {}
    Check(live.Changes.Last().Status==ChangeStatus.Failed && live.Changes.Last().Error!=null, "live: failed write recorded with error");
    // cancellation
    var cts=new CancellationTokenSource(); var c2=new RecordingOrganizationService(fake,false,true,cts.Token); cts.Cancel();
    bool threw=false; try{ c2.Retrieve("contact",id,new ColumnSet(true)); }catch(OperationCanceledException){threw=true;}
    Check(threw, "stop: next CRM call throws OperationCanceledException");
    // csv
    var path=Path.GetTempFileName();
    Evidence.WriteChangesCsv(path, new[]{ new ChangeRecord{Seq=1,Operation="Update",Entity="contact",RecordId="1",Field="name",Before="أحمد, علي",After="say \"hi\"",Status=ChangeStatus.Done} });
    var csv=File.ReadAllText(path);
    Check(csv.Contains("\"أحمد, علي\"") && csv.Contains("\"say \"\"hi\"\"\""), "csv: Arabic text kept, commas and quotes escaped");
    Console.WriteLine(fails==0?"ALL PASSED":$"{fails} FAILED");
    return fails;
  }
}
