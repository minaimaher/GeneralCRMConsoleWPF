using ExcelDataReader;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.SqlServer.Server;
using Microsoft.Xrm.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Discovery;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.PluginTelemetry;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Xrm.Tooling.Connector;
using MyConsole.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;
using System;
using System.Activities;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.Drawing;
using System.Globalization;
using System.IdentityModel.Metadata;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Contexts;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Security.Cryptography;
using System.Security.Principal;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Web.Services.Description;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using System.Xml.Linq;
using static MyConsole.Applications;
using static System.Net.WebRequestMethods;

namespace MyConsole
{
    internal class Applications
    {
        public static void CloseJanaLoans(IOrganizationService service)
        {
            //get loan ids
            List<string> ids = new List<string>();
            ids.Add("1079100069");
            ids.Add("1029613559");
            ids.Add("1086891338");
            ids.Add("1053878961");
            ids.Add("1049484270");
            ids.Add("1092885753");
            ids.Add("1109575629");
            ids.Add("1088446057");
            ids.Add("1142890340");
            ids.Add("1043928892");
            ids.Add("1064417593");
            ids.Add("1047370885");
            ids.Add("1003985254");
            ids.Add("1015127119");
            ids.Add("1103101844");
            ids.Add("1041870146");
            ids.Add("1095918353");
            ids.Add("1042002475");

            for (int i = 0; i < ids.Count; i++)
            {
                Console.WriteLine(i);
                string loansFetch = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                    <entity name='ntw_loan'>
                    <attribute name='ntw_name' />
                    <attribute name='ntw_partnerid' />
                    <attribute name='ntw_loanamount' />
                    <attribute name='ntw_familyid' />
                    <attribute name='ntw_pfpmigratedrowid' />
                    <attribute name='ntw_loanstatus' />
                    <attribute name='ntw_migratedcreatedon' />
                    <attribute name='ntw_fundingwallet' />
                    <attribute name='createdon' />
                    <attribute name='ntw_remainingamount' />
                    <attribute name='ntw_paidamount' />
                    <attribute name='ntw_contactid' />
                    <attribute name='sdb_issuccessfulstory' />
                    <attribute name='sdb_loandisbursedon' />
                    <attribute name='ntw_loanid' />
                    <attribute name='ntw_requesttype' />
                    <order attribute='createdon' descending='true' />
                    <order attribute='ntw_migratedcreatedon' descending='true' />
                    <filter type='and'>
                        <filter type='and'>
                        <condition attribute='ntw_partnerid' operator='eq' uiname='مؤسسة سليمان الراجحي للتمويل التنموي' uitype='account' value='924D0140-1882-EC11-B817-00505692B4E1' />
                        </filter>
                        <filter type='and'>
                        <condition attribute='ntw_loanstatus' operator='eq' uiname='مفعل' uitype='ntw_loanstatus' value='636D5B7B-0EE8-EC11-B826-00505692A571' />
                        </filter>
                    </filter>
                    <link-entity name='contact' from='contactid' to='ntw_contactid' link-type='inner' alias='a_a9177eda3981ec11b81700505692b4e1'>
                        <attribute name='ntw_nationalidtext' />
                        <filter type='and'>
                        <condition attribute='ntw_nationalidtext' operator='eq' value='" + ids[i] + @"' />
                        </filter>
                    </link-entity>
                    </entity>
                </fetch>";
                var loans = service.RetrieveMultiple(new FetchExpression(loansFetch));

                foreach (var loan in loans.Entities)
                {
                    Console.WriteLine(loan.Id + " with nat id " + ids[i]);
                    loan["ntw_loanstatus"] = new EntityReference("ntw_loanstatus", new Guid("7b5d506f-0ee8-ec11-b826-00505692a571"));
                    service.Update(loan);
                }
            }
        }
        public static void UpdateContactWithDOB(IOrganizationService service)
        {

            //get contact ids
            List<List<string>> ids = GetIdsFromExcel("Ids.xlsx");
            var SkipValue = int.Parse(System.Configuration.ConfigurationManager.AppSettings["Skip"]);
            var TakeValue = int.Parse(System.Configuration.ConfigurationManager.AppSettings["Take"]);
            var contactsIds = ids[0].Skip(SkipValue).Take(TakeValue).ToList();
            var dates = ids[1].Skip(SkipValue).Take(TakeValue).ToList();
            for (int i = 0; i < contactsIds.Count; i++)
            {
                Console.WriteLine(i);
                Guid contactId = new Guid(contactsIds[i]);
                string hijryDate = dates[i];
                Console.WriteLine(contactId + " " + hijryDate);
                //update contact birthdate georgian
                Entity updatedId = new Entity("contact");
                updatedId.Id = contactId;
                updatedId["birthdate"] = ConvertHijriToGregorian(hijryDate);
                try
                {
                    service.Update(updatedId);
                }
                catch
                {
                    Console.WriteLine("error with contact id : " + contactId);
                }
            }
        }
        public static void UpdateContactWithIndvNo(IOrganizationService service)
        {

            //get contact ids
            List<List<string>> ids = GetIdsFromExcel("Ids.xlsx");

            var contactsIds = ids[0];
            var Indvs = ids[1];
            int updated = 0;
            for (int i = 0; i < contactsIds.Count; i++)
            {
                QueryExpression query = new QueryExpression("contact");
                query.ColumnSet = new ColumnSet("contactid", "ntw_indvno");
                query.Criteria.AddCondition("ntw_nationalidtext", ConditionOperator.Equal, contactsIds[i]);
                EntityCollection contactid = service.RetrieveMultiple(query);
                if (contactid.Entities.Count<1)
                {
                    Console.WriteLine("contact nationalid : " + contactsIds[i] + " does not exist index : " + i);
                    continue;
                }
                Guid contactId = new Guid(contactid.Entities[0]["contactid"].ToString());

                //update contact birthdate georgian
                Entity updatedId = new Entity("contact");
                updatedId.Id = contactId;
                updatedId["ntw_indvno"] = Indvs[i];
                try
                {
                    service.Update(updatedId);
                    updated++;
                    Console.WriteLine("updated contacts : " + updated);
                    Console.WriteLine("contact id : " + contactId + " is updated index : " + i);
                }
                catch
                {
                    Console.WriteLine("error with contact id : " + contactId + " index : " + i);
                }
            }
        }

        public static void FixContacts(IOrganizationService service)
        {
            //QueryExpression query = new QueryExpression()
            //        {
            //            EntityName = "contact",
            //            ColumnSet = new ColumnSet(false)
            //            {
            //                AttributeExpressions = {
            //        {
            //            new XrmAttributeExpression(
            //                attributeName: "ntw_nationalidtext",
            //                alias: "Ids",
            //                aggregateType: XrmAggregateType.Count)
            //        },
            //        {
            //            new XrmAttributeExpression(
            //                attributeName: "ntw_nationalidtext",
            //                alias: "Ids",
            //                aggregateType: XrmAggregateType.None){
            //                HasGroupBy = true
            //                }
            //            }
            //        }
            //    }
            //};
            //query.Orders.Add(new OrderExpression(
            //            attributeName: "ntw_nationalidtext",
            //            alias: "Ids",
            //            orderType: OrderType.Ascending));
            //query.TopCount = 10;
           
            string contactFetch = @"<fetch  aggregate='true' aggregatelimit = '10'>
                                      <entity name='contact' alias='c'>
                                        <attribute name='contactid' alias='countIdsIds' aggregate='count' />
                                        <attribute name='ntw_nationalidtext' alias='nationalId' groupby='true' />
                                        <filter type='and'>
                                            <condition entityname='c' attribute='countIdsIds' operator='gt' value='1' />
                                        </filter>
                                      </entity>
                                    </fetch>";
            var contacts = service.RetrieveMultiple(new FetchExpression(contactFetch));
            foreach (var contact in contacts.Entities)
            {
                if (contact.Attributes["countIds"] != null && contact.Attributes["nationalId"] != null)
                {
                    var nationalId = ((Microsoft.Xrm.Sdk.AliasedValue)contact.Attributes["nationalId"]).Value;
                    var countIds = ((Microsoft.Xrm.Sdk.AliasedValue)contact.Attributes["countIdsIds"]).Value;
                    Console.WriteLine(nationalId + " - " + countIds);
                }
               
            }
        }
        public static void Calculator()
        {
            int baseNum = 1003;
            Console.WriteLine(baseNum);
            int mod = baseNum % 4;
            Console.WriteLine(mod);
            int skipnum1 = baseNum * 0 / 4;
            int takenum1 = baseNum / 4;
            Console.WriteLine(skipnum1 + " : " + takenum1);
            int skipnum2 = baseNum * 1 / 4;
            int takenum2 = baseNum / 4;
            Console.WriteLine(skipnum2 + " : " + takenum2);
            int skipnum3 = baseNum / 4;
            skipnum3 = skipnum3 * 2;
            int takenum3 = baseNum / 4;
            Console.WriteLine(skipnum3 + " : " + takenum3);
            int skipnum4 = baseNum / 4;
            skipnum4 = skipnum4 * 3;
            int takenum4 = baseNum / 4 + mod;
            Console.WriteLine(skipnum4 + " : " + takenum4);
        }
        public static void SettleLoans(IOrganizationService service)
        {
            List<Guid> loanIds = new List<Guid>();
            bool isAhyaaLoan = true;

            //List<List<string>> ids = GetIdsFromExcel("Ids.xlsx");
            //foreach (var id in ids[0])
            //{
            //    loanIds.Add(new Guid(id));
            //}

            loanIds.Add(new Guid("d279c866-dfd0-ee11-b857-00505692a571"));
            
            int loanCount = 0;
            var FilteredloanIds = new List<Guid>();
            //var SkipValue = int.Parse(System.Configuration.ConfigurationManager.AppSettings["Skip"]);
            //var TakeValue = int.Parse(System.Configuration.ConfigurationManager.AppSettings["Take"]);
            //FilteredloanIds = (List<Guid>)loanIds.Skip(SkipValue).Take(TakeValue);
            FilteredloanIds = loanIds;
            foreach (var loanId in FilteredloanIds)
            {
                StreamWriter w = System.IO.File.AppendText($"{++loanCount} log {loanId} trace.txt");
                Log($"Start {loanId}", w, true);
                Log(loanCount + " settle loan Id : " + loanId, w, true);
                try
                {
                    int reqsCount = 0;
                    Console.WriteLine(loanCount + " settle loan Id : " + loanId);
                    //retrieve all installments
                    string loanInstallmentsFetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                      <entity name='ntw_loaninstallmentschedule'>
                        <attribute name='ntw_loaninstallmentscheduleid' />
                        <attribute name='createdon' />
                        <attribute name='ntw_installmentno' />
                        <attribute name='ntw_remainingamount' />
                        <attribute name='ntw_paidamount' />
                        <attribute name='ntw_amount' />
                        <order attribute='ntw_duedate' descending='false' />
                        <filter type='and'>
                          <condition attribute='ntw_loan' operator='eq' uitype='ntw_loan' value='" + loanId + @"' />
                          <condition attribute='statuscode' operator='ne' value='961110003' />
                        </filter>
                      </entity>
                    </fetch>";
                    EntityCollection loanInstallments = service.RetrieveMultiple(new FetchExpression(loanInstallmentsFetchXml));

                    Console.WriteLine("loan Installments retrieved");
                    Log("loan Installments retrieved", w);
                    //clear all installments

                    foreach (Entity installment in loanInstallments.Entities)
                    {
                        //string clearInstallmentWorkflowId = "D273BF69-7C13-49C9-B575-BED6BFD36730";
                        //var executeWorkflowRequest = new ExecuteWorkflowRequest()
                        //{
                        //    WorkflowId = Guid.Parse(clearInstallmentWorkflowId),
                        //    EntityId = Guid.Parse(installment.Id.ToString())
                        //};
                        //var executeWorkflowResponse = (ExecuteWorkflowResponse)service.Execute(executeWorkflowRequest);

                        //clear installament
                        installment["ntw_paidamount"] = decimal.Zero;
                        installment["ntw_paymentdate"] = null;
                        installment["statecode"] = new OptionSetValue(0);
                        installment["statuscode"] = new OptionSetValue(1);
                        installment["ntw_saddadinvoice"] = null;
                        installment["ntw_installmentstatus"] = new EntityReference("ntw_migratedinstallmentstatus", new Guid("2aa7363f-cce8-ec11-b826-00505692a571"));

                        service.Update(installment);
                    }
                    Console.WriteLine("loan Installments cleared");
                    //retrieve all completed settlement requests
                    string loanCompletedSettlementRequestsFetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                      <entity name='ntw_productivefamilyloansettlement'>
                        <attribute name='createdon'/>
                        <attribute name='ntw_paidamount'/>
                        <attribute name='ntw_paymentmethod'/>
                        <attribute name='ntw_partner'/>
                        <attribute name='ntw_loan'/>
                        <attribute name='ntw_family'/>
                        <attribute name='ntw_settlementdate'/>
                        <attribute name='ntw_productivefamilyloansettlementid'/>
                        <order attribute='ntw_settlementdate' descending='false'/>
                        <filter type='and'>
                            <filter type='or'>
                                <condition attribute='ntw_submit' operator='eq' value='1' />
                                <filter type='and'>
                                    <condition attribute='ntw_ismigrated' operator='eq' value='1'/>
                                    <condition attribute='statuscode' operator='ne' value='749190000'/>
                                </filter>
                            </filter>
                            <condition attribute='ntw_loan' operator='eq' uiname='00065489' uitype='ntw_loan' value='" + loanId + @"' />
                        </filter>
                      </entity>
                    </fetch>";
                    EntityCollection loanCompletedSettlementRequests = service.RetrieveMultiple(new FetchExpression(loanCompletedSettlementRequestsFetchXml));

                    Console.WriteLine("loan settlement requests retrieved");
                    Log("loan settlement requests retrieved", w);
                    //get installments to be settled
                    foreach (Entity settlementRequest in loanCompletedSettlementRequests.Entities)
                    {
                        //delete associated installements
                        QueryExpression query = new QueryExpression("ntw_loansettlementinstallments");
                        query.ColumnSet = new ColumnSet(true);
                        query.Criteria.AddCondition("ntw_loansettlement", ConditionOperator.Equal, settlementRequest.Id);
                        EntityCollection pendingInstallmentsCollection = service.RetrieveMultiple(query);
                        if (pendingInstallmentsCollection.Entities.Any())
                        {
                            foreach (var item in pendingInstallmentsCollection.Entities)
                            {
                                service.Delete("ntw_loansettlementinstallments", item.Id);
                            }
                        }
                        Console.WriteLine("association cleared for request no. " + ++reqsCount);
                        Log("association cleared for request no. " + reqsCount, w);
                        int paymentMethod = 0;
                        //retrieve request info
                        if (!isAhyaaLoan)
                        {
                            paymentMethod = settlementRequest.GetAttributeValue<OptionSetValue>("ntw_paymentmethod").Value;
                        }
                        var settlmentAmount = settlementRequest.GetAttributeValue<Money>("ntw_paidamount").Value;
                        var settlementDate = settlementRequest.GetAttributeValue<DateTime>("ntw_settlementdate");
                        var remainingTemp = settlmentAmount;
                        //retrieve loan info
                        Entity loanInfo = service.Retrieve("ntw_loan", loanId, new ColumnSet(new string[] { "ntw_remainingamount" }));
                        decimal remainingLoanAmount = loanInfo.GetAttributeValue<Money>("ntw_remainingamount").Value;

                       
                        //reassociate installemts : Call Get Loan Installments Wokflow code
                        string fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                            <entity name='ntw_loaninstallmentschedule'>
                                            <attribute name='ntw_loaninstallmentscheduleid' />
                                            <attribute name='createdon' />
                                            <attribute name='ntw_installmentno' />
                                            <attribute name='ntw_remainingamount' />
                                            <attribute name='ntw_paidamount' />
                                            <attribute name='ntw_amount' />
                                            <order attribute='ntw_duedate' descending='false' />
                                            <filter type='and'>
                                                <condition attribute='ntw_loan' operator='eq' uitype='ntw_loan' value='" + loanId + @"' />
                                                <condition attribute='statuscode' operator='not-in'>
                                                <value>961110000</value>
                                                <value>2</value>
                                                <value>961110003</value>
                                                </condition>
                                                <condition attribute='statuscode' operator='ne' value='961110003' />
                                            </filter>
                                            <link-entity name='ntw_migratedinstallmentstatus' from='ntw_migratedinstallmentstatusid' to='ntw_installmentstatus' link-type='inner' alias='ab'>
                                                <filter type='and'>
                                                    <condition attribute='ntw_installmentstatusid' operator='ne' value='2' />
                                                </filter>
                                            </link-entity>
                                            </entity>
                                        </fetch>";
                        //decimal tempAmount = 0;

                        EntityCollection InstallmentsCollection = service.RetrieveMultiple(new FetchExpression(fetchXml));

                        var loanInstallmentAmountCheck = InstallmentsCollection.Entities[0].Attributes.Contains("ntw_amount") ?
                                                    InstallmentsCollection.Entities[0].GetAttributeValue<Money>("ntw_amount").Value : 0;


                        List<Entity> installmentDataCollection = new List<Entity> { };

                        if (settlmentAmount <= loanInstallmentAmountCheck)
                        {
                            decimal diffRemaining = settlmentAmount;
                            for (int item = 0; item <= InstallmentsCollection.Entities.Count; item++)
                            {

                                decimal installmentRemaining = InstallmentsCollection.Entities[item].GetAttributeValue<Money>("ntw_remainingamount").Value;

                                if (diffRemaining > installmentRemaining)
                                {
                                    diffRemaining = diffRemaining - installmentRemaining;
                                    installmentDataCollection.Add(InstallmentsCollection.Entities[item]);
                                }
                                else if (diffRemaining <= installmentRemaining)
                                {
                                    diffRemaining = 0;
                                    installmentDataCollection.Add(InstallmentsCollection.Entities[item]);
                                    break;
                                }
                            }
                        }
                        else
                        {
                            decimal diffRemaining = settlmentAmount;
                            for (int item = 0; item <= InstallmentsCollection.Entities.Count; item++)
                            {

                                decimal installmentRemaining = InstallmentsCollection.Entities[item].GetAttributeValue<Money>("ntw_remainingamount").Value;

                                if (diffRemaining > installmentRemaining)
                                {
                                    diffRemaining = diffRemaining - installmentRemaining;
                                    installmentDataCollection.Add(InstallmentsCollection.Entities[item]);
                                }
                                else if (diffRemaining <= installmentRemaining)
                                {
                                    diffRemaining = 0;
                                    installmentDataCollection.Add(InstallmentsCollection.Entities[item]);
                                    break;
                                }
                            }
                            //var installmentPaymentCount = Math.Ceiling(settlmentAmount / loanAmountCheck);

                            //for (int i = 0; i <= installmentPaymentCount; i++)
                            //{
                            //    installmentDataCollection.Add(InstallmentsCollection.Entities[i]);
                            //}

                        }

                        foreach (var installment in installmentDataCollection)
                        {
                            //create many to many relation record on Loan Settelment Installments

                            if (!IsExisted(service, settlementRequest.Id, installment.Id))
                            {
                                Entity loanSettelmentInstallment = new Entity("ntw_loansettlementinstallments");

                                loanSettelmentInstallment["ntw_loansettlement"] = new EntityReference("ntw_productivefamilyloansettlement", settlementRequest.Id);
                                loanSettelmentInstallment["ntw_loaninstallment"] = new EntityReference("ntw_loaninstallmentschedule", installment.Id);

                                service.Create(loanSettelmentInstallment);
                            }
                        }
                        Console.WriteLine("re-association done");
                        Log("re-association done", w);
                        //string getLoanInstallmentsWokflowId = "DC41E95D-3078-4D10-9B76-AE9DF77774CF";
                        //var executegetLoanInstallmentsWokflow = new ExecuteWorkflowRequest()
                        //{
                        //    WorkflowId = Guid.Parse(getLoanInstallmentsWokflowId),
                        //    EntityId = Guid.Parse(settlementRequest.Id.ToString())
                        //};


                        //settle installemts : Call Loan Installments Settlement workflow code
                        var fetch = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                    <entity name='ntw_loansettlementinstallments'>
                                    <attribute name='ntw_loansettlementinstallmentsid' />
                                    <attribute name='ntw_loaninstallment' />
                                    <order attribute='ntw_loaninstallment' descending='false' />
                                    <filter type='and'>
                                        <condition attribute='ntw_loansettlement' operator='eq' uitype='ntw_productivefamilyloansettlement' value='" + settlementRequest.Id.ToString() + @"' />
                                    </filter>
                                    <link-entity name='ntw_loaninstallmentschedule' from='ntw_loaninstallmentscheduleid' to='ntw_loaninstallment' visible='false' link-type='outer' alias='Inst'>
                                        <attribute name='ntw_remainingamount' />
                                        <attribute name='ntw_paymentdate' />
                                        <attribute name='ntw_paidamount' />
                                        <attribute name='ntw_amount' />
                                        <attribute name='ntw_duedate' />
                                    </link-entity>                            
                                    </entity>
                                </fetch>";
                        var InstallmentCollection = service.RetrieveMultiple(new FetchExpression(fetch));

                        List<Entity> OrderedInstallmentCollection = InstallmentCollection.Entities.OrderBy(e => (DateTime)e.GetAttributeValue<AliasedValue>("Inst.ntw_duedate").Value).ToList();
                        Console.WriteLine("installments retrieved");
                        Log("installments retrieved", w);
                        foreach (var installment in OrderedInstallmentCollection)
                        {
                            var installmentId = installment.GetAttributeValue<EntityReference>("ntw_loaninstallment").Id;
                            var amount = installment.Attributes.Contains("Inst.ntw_amount") ?
                                                    ((Money)installment.GetAttributeValue<AliasedValue>("Inst.ntw_amount").Value).Value : 0;
                            var remainingAmount = installment.Attributes.Contains("Inst.ntw_remainingamount") ?
                                                    ((Money)installment.GetAttributeValue<AliasedValue>("Inst.ntw_remainingamount").Value).Value : 0;



                            if (remainingTemp >= remainingAmount)
                            {
                                Entity installmentPaymentHistory = new Entity("ntw_installmentpayment");
                                installmentPaymentHistory["ntw_loaninstallment"] = new EntityReference("ntw_loaninstallmentschedule", installmentId);
                                installmentPaymentHistory["ntw_paidamount"] = new Money(remainingAmount);
                                if (!isAhyaaLoan)
                                {
                                    installmentPaymentHistory["ntw_paymentmethod"] = new OptionSetValue(paymentMethod);
                                }
                                installmentPaymentHistory["ntw_paymentdate"] = settlementDate;
                                //ntw_paymentdate
                                service.Create(installmentPaymentHistory);
                                remainingTemp = remainingTemp - remainingAmount;
                            }
                            else
                            {
                                //settlment amount greater than remaining amount//

                                Entity installmentPaymentHistory = new Entity("ntw_installmentpayment");
                                installmentPaymentHistory["ntw_loaninstallment"] = new EntityReference("ntw_loaninstallmentschedule", installmentId);
                                installmentPaymentHistory["ntw_paidamount"] = new Money(remainingTemp);
                                if (!isAhyaaLoan)
                                {
                                    installmentPaymentHistory["ntw_paymentmethod"] = new OptionSetValue(paymentMethod);
                                }

                                installmentPaymentHistory["ntw_paymentdate"] = settlementDate;
                                service.Create(installmentPaymentHistory);

                            }

                        }
                        Console.WriteLine("installments settled for request : " + settlementRequest.Id);
                        Log("installments settled for request : " + settlementRequest.Id, w);

                        //string settleLoanInstallmentsWokflowId = "8B546F57-2677-4286-B297-5397B18DE434";
                        //var executesettleLoanInstallmentsWokflow = new ExecuteWorkflowRequest()
                        //{
                        //    WorkflowId = Guid.Parse(settleLoanInstallmentsWokflowId),
                        //    EntityId = Guid.Parse(settlementRequest.Id.ToString())
                        //};
                        

                    }

                    Console.WriteLine("loan with id : " + loanId + " is done");
                    Log("loan with id : " + loanId + " is done", w);
                    Console.WriteLine();
                    w.Close();
                }
                catch (Exception e)
                {

                    Log("Error in Loan Id : " + loanId +" :- "+ e.Message, w);
                    w.Close();
                    break;
                }


                if (loanCount%10 == 0)
                {
                    //Reset Token
                    Console.WriteLine("Reset token : " + ((OrganizationServiceProxy)service).SecurityTokenResponse.Token);
                    service = null;
                    service = CRMHelper.ConnectToCRM();
                }
            }
        }
        public static void Log(string logMessage, TextWriter w, bool logTime = false)
        {
            if (logTime == true)
            {
                w.Write("\r\nLog Entry : ");
                w.WriteLine($"{DateTime.Now.ToLongTimeString()} {DateTime.Now.ToLongDateString()}");
                w.WriteLine($"{logMessage}");
                w.WriteLine("-------------------------------");
            }
            else
            {
                w.WriteLine($"{logMessage}");
                w.WriteLine("-------------------------------");
            }
        }
        private static bool IsExisted(IOrganizationService service, Guid loansettlement, Guid loaninstallment)
        {
            var fetch = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                          <entity name='ntw_loansettlementinstallments'>
                            <attribute name='ntw_loansettlementinstallmentsid' />
                            <attribute name='ntw_name' />
                            <attribute name='createdon' />
                            <order attribute='ntw_name' descending='false' />
                            <filter type='and'>
                              <condition attribute='ntw_loaninstallment' operator='eq' uitype='ntw_loaninstallmentschedule' value='" + loaninstallment.ToString() + @"' />
                              <condition attribute='ntw_loansettlement' operator='eq' uitype='ntw_productivefamilyloansettlement' value='" + loansettlement.ToString() + @"' />
                            </filter>
                          </entity>
                        </fetch>";

            var result = service.RetrieveMultiple(new FetchExpression(fetch));
            if (result.Entities != null && result.Entities.Count > 0)
            {
                return true;
            }
            return false;
        }
        public static void CancelEmails(IOrganizationService service)
        {
            StreamWriter w = System.IO.File.AppendText($"cancel emails trace.txt");
            Log($"Start cancelling", w, true);
            //fetch emails
            string pendingSendEmails = @"<fetch>
                                            <entity name='email'>
                                                <attribute name='statuscode' />
                                                <attribute name='activityid' />
                                                <filter type='and'>
                                                    <condition attribute='statuscode' operator='in'>
                                                    <value>6</value>
                                                    <value>7</value>
                                                    </condition>
                                                </filter>
                                            </entity>
                                        </fetch>";
            List<Entity> pendingSendEmailsList = GetRecordsByFetchXml(service, pendingSendEmails, w);
            int x = 0;
            //update status and status code

            foreach (Entity email in pendingSendEmailsList)
            {
                try
                {
                    if (service != null)
                    {
                        service.Execute(new SetStateRequest
                        {
                            EntityMoniker = new EntityReference("email", email.Id),
                            State = new OptionSetValue(2), //Status
                            Status = new OptionSetValue(5) //Status reason
                        });
                        Console.WriteLine("email no. " + (x + 1) + " with id : " + email.Id + " is cancelled");
                        Log("email no. " + (x + 1) + " with id : " + email.Id + " is cancelled", w);
                    }
                    x++;
                }
                catch (Exception e)
                {
                   Log(email.Id + " " + e.Message, w);
                    w.Close();
                }
               
            }
            w.Close();
        }
        public static List<Entity> GetRecordsByFetchXml(IOrganizationService service, String fetchXML, StreamWriter w = null)
        {
            List<Entity> objList = new List<Entity>();
            try
            {
                // Define the fetch attributes.
                // Set the number of records per page to retrieve.
                int fetchCount = 500;
                // Initialize the page number.
                int pageNumber = 1;
                // Specify the current paging cookie. For retrieving the first page,
                // pagingCookie should be null.
                String pagingCookie = null;
                while (objList.Count < 1100000)
                {
                    Console.WriteLine("list count : " + objList.Count);
                    if (w != null)
                    {
                        Log("list count : " + objList.Count, w);
                    }
                    // Build fetchXml String with the placeholders.
                    String xml = CreateXml(fetchXML, pagingCookie, pageNumber, fetchCount);
                    // Excute the fetch query and get the xml result.
                    RetrieveMultipleRequest fetchRequest1 = new RetrieveMultipleRequest
                    {
                        Query = new FetchExpression(xml)
                    };
                    EntityCollection returnCollection = ((RetrieveMultipleResponse)service.Execute(fetchRequest1)).EntityCollection;
                    foreach (var entity in returnCollection.Entities)
                    {
                        objList.Add(entity);
                    }
                    // Check for morerecords, if it returns 1.
                    if (returnCollection.MoreRecords)
                    {
                        // Increment the page number to retrieve the next page.
                        pageNumber++;
                    }
                    else
                    {
                        // If no more records in the result nodes, exit the loop.
                        return objList;
                        //  break;
                    }
                }
                return objList;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static String CreateXml(String xml, String cookie, int page, int count)
        {
            XmlDocument doc = new XmlDocument();
            try
            {
                StringReader StReader = new StringReader(xml);
                XmlTextReader reader = new XmlTextReader(StReader);
                // Load document
                doc.Load(reader);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return CreateXml(doc, cookie, page, count);
        }
        public static String CreateXml(XmlDocument doc, String cookie, int page, int count)
        {
            try
            {
                XmlAttributeCollection attrs = doc.DocumentElement.Attributes;
                if (cookie != null)
                {
                    XmlAttribute pagingAttr = doc.CreateAttribute("paging-cookie");
                    pagingAttr.Value = cookie;
                    attrs.Append(pagingAttr);
                }
                XmlAttribute pageAttr = doc.CreateAttribute("page");
                pageAttr.Value = System.Convert.ToString(page);
                attrs.Append(pageAttr);
                XmlAttribute countAttr = doc.CreateAttribute("count");
                countAttr.Value = System.Convert.ToString(count);
                attrs.Append(countAttr);
                StringBuilder sb = new StringBuilder(1024);
                StringWriter StringWriter = new StringWriter(sb);
                XmlTextWriter writer = new XmlTextWriter(StringWriter);
                doc.WriteTo(writer);
                writer.Close();
                return sb.ToString();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static DataTable ReadExcelFiles(string filePath)
        {
            using (var stream = System.IO.File.Open(filePath, FileMode.Open, FileAccess.Read))
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                var encoding = Encoding.GetEncoding("UTF-8");
                using (var reader = ExcelReaderFactory.CreateReader(stream,
                  new ExcelReaderConfiguration() { FallbackEncoding = encoding }))
                {
                    var result = reader.AsDataSet(new ExcelDataSetConfiguration
                    {
                        ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
                    });

                    if (result.Tables.Count > 0)
                    {
                        return result.Tables[0];
                    }
                }
            }
            return null;
        }
        public static List<List<string>> GetIdsFromExcel(string fileName)
        {
            //This will give us the full name path of the executable file:
            //i.e. C:\Program Files\MyApplication\MyApplication.exe
            string strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            //This will strip just the working path name:
            //C:\Program Files\MyApplication
            string strWorkPath = System.IO.Path.GetDirectoryName(strExeFilePath);
            string fullPathToExcel = System.IO.Path.Combine(strWorkPath, fileName);
            //string connString = string.Format("Provider=Microsoft.ACE.OLEDB.12.0;Data Source={0};Extended Properties='Excel 12.0;HDR=yes'", fullPathToExcel);
            DataTable dt = ReadExcelFiles(fullPathToExcel);
            List<string> ids1 = new List<string>();
            foreach (DataRow dr in dt.Rows)
            {
                ids1.Add(dr[0].ToString());
            }
            //List<string> ids2 = new List<string>();
            //foreach (DataRow dr in dt.Rows)
            //{
            //    ids2.Add(dr[1].ToString());
            //}
            //List<string> ids3 = new List<string>();
            //foreach (DataRow dr in dt.Rows)
            //{
            //    ids3.Add(dr[2].ToString());
            //}
            //List<string> ids4 = new List<string>();
            //foreach (DataRow dr in dt.Rows)
            //{
            //    ids4.Add(dr[3].ToString());
            //}
            //List<string> ids5 = new List<string>();
            //foreach (DataRow dr in dt.Rows)
            //{
            //    ids5.Add(dr[4].ToString());
            //}
            //List<string> ids6 = new List<string>();
            //foreach (DataRow dr in dt.Rows)
            //{
            //    ids6.Add(dr[5].ToString());
            //}
            //List<string> ids7 = new List<string>();
            //foreach (DataRow dr in dt.Rows)
            //{
            //    ids7.Add(dr[6].ToString());
            //}
            //List<string> ids8 = new List<string>();
            //foreach (DataRow dr in dt.Rows)
            //{
            //    ids8.Add(dr[7].ToString());
            //}
            //List<string> ids9 = new List<string>();
            //foreach (DataRow dr in dt.Rows)
            //{
            //    ids9.Add(dr[8].ToString());
            //}
            //List<string> ids10 = new List<string>();
            //foreach (DataRow dr in dt.Rows)
            //{
            //    ids10.Add(dr[9].ToString());
            //}

            List<List<string>> ids = new List<List<string>>();

            ids.Add(ids1);
            //ids.Add(ids2);
            //ids.Add(ids3);
            //ids.Add(ids4);
            //ids.Add(ids5);
            //ids.Add(ids6);
            //ids.Add(ids7);
            //ids.Add(ids8);
            //ids.Add(ids9);
            //ids.Add(ids10);
            return ids;
        }
        public static void UpdateIds(IOrganizationService service)
        {
            List<List<string>> ids = GetIdsFromExcel("Ids.xlsx");
            for (int i = 0; i < ids[0].Count; i++)
            {
                ////get audit for 4 fields
                //string auditFetch = @"<fetch>
                //                  <entity name='audit' >
                //                    <filter type='and' >
                //                      <condition attribute='operation' operator='eq' value='2' />
                //                      <condition attribute='objectid' operator='eq' value='{0}' />
                //                    </filter>
                //                  </entity>
                //                </fetch>";
                //auditFetch = String.Format(auditFetch, ids[0][i]);
                //var auditId = service.RetrieveMultiple(new FetchExpression(auditFetch)).Entities.FirstOrDefault().Id;

                Console.WriteLine("sdbqabooltemplate id is updated;");
                var changeRequest = new RetrieveRecordChangeHistoryRequest();
                changeRequest.Target = new EntityReference("los_sdbqabooltemplate", new Guid(ids[0][i]));

                var changeResponse = (RetrieveRecordChangeHistoryResponse)service.Execute(changeRequest);
                var details = changeResponse.AuditDetailCollection;
                details.AuditDetails.OrderByDescending(d => d.AuditRecord.Attributes["createdon"]);
                
                if (typeof(AttributeAuditDetail).Name == details[0].GetType().Name)
                {
                    AttributeAuditDetail detail = details[0] as AttributeAuditDetail;
                    DateTime detailDate = ((DateTime)detail.AuditRecord["createdon"]).Date;

                    Entity oldEntity = detail.OldValue;
                    Entity newEntity = detail.NewValue;
                    //Guid oldEntityTeamId = ((EntityReference)oldEntity.Attributes["ownerid"]).Id;
                    //Guid newEntityTeamId = ((EntityReference)newEntity.Attributes["ownerid"]).Id;


                    //if (newEntityTeamId == new Guid("73ecf52a-36f1-ec11-b828-00505692a571")
                    //    && oldEntityTeamId ==  new Guid("3043ada2-84ec-ec11-b827-00505692a571"))
                    //{
                    Entity updatedId = new Entity("los_sdbqabooltemplate");
                    updatedId.Id = new Guid(ids[0][i]);
                    if(oldEntity.Contains("los_name"))
                        updatedId.Attributes["los_name"] =  oldEntity.Attributes["los_name"];
                    if (oldEntity.Contains("los_nameen"))
                        updatedId.Attributes["los_nameen"] = oldEntity.Attributes["los_nameen"];
                    if (oldEntity.Contains("los_code"))
                        updatedId.Attributes["los_code"] = oldEntity.Attributes["los_code"];
                    if (oldEntity.Contains("los_isactive"))
                        updatedId.Attributes["los_isactive"] = oldEntity.Attributes["los_isactive"];
                    if (oldEntity.Contains("los_numberoflimittosend"))
                        updatedId.Attributes["los_numberoflimittosend"] = oldEntity.Attributes["los_numberoflimittosend"];
                    if (oldEntity.Contains("los_expiryindays"))
                        updatedId.Attributes["los_expiryindays"] = oldEntity.Attributes["los_expiryindays"];
                    if (oldEntity.Contains("los_purposecode"))
                        updatedId.Attributes["los_purposecode"] = oldEntity.Attributes["los_purposecode"];
                    if (oldEntity.Contains("los_templatebody"))
                        updatedId.Attributes["los_templatebody"] = oldEntity.Attributes["los_templatebody"];
                    if (oldEntity.Contains("los_templatenumber"))
                        updatedId.Attributes["los_templatenumber"] = oldEntity.Attributes["los_templatenumber"];
                    if (oldEntity.Contains("los_termsandconditiondraftcode"))
                        updatedId.Attributes["los_termsandconditiondraftcode"] = oldEntity.Attributes["los_termsandconditiondraftcode"];
                    service.Update(updatedId);
                    //}
                  
                  
                }

            }
           

            //for (int i = 0; i < ids[0].Count; i++) 
            //{
            //    var query = new QueryExpression("ntw_loan");
            //    query.ColumnSet = new ColumnSet(true);
            //    query.Criteria.AddCondition("ntw_pfpmigratedrowid", ConditionOperator.Equal, ids[0][i]);
            //    query.Criteria.AddCondition("ntw_migrateddatasource", ConditionOperator.Equal, 4);
            //    EntityCollection loansCollection = service.RetrieveMultiple(query);

            //    Entity updatedDisbursement = new Entity("ntw_createnewpaymentrequest");
            //    updatedDisbursement.Id = new Guid(ids[1][i]);
            //    updatedDisbursement.Attributes["ntw_loan"] = new EntityReference("ntw_loan", loansCollection[0].Id);
            //    service.Update(updatedDisbursement);
            //    Console.WriteLine("updated Disbursement ID : " + updatedDisbursement.Id + "order : " + (i+1));
            //}

        }
        public static void DeleteBPFRecords(IOrganizationService service)
        {
            //retrieve associated records for specific bpf record

            var fetch = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                          <entity name='new_bpf_7013d7c4826745e182fabb09ef464219'>
                            <filter type='and'>
                              <condition attribute='bpf_incidentid' operator='eq'  value='9e88ae11-c80e-f011-b87b-00505692a571' />
                            </filter>
                          </entity>
                        </fetch>";

            var result = service.RetrieveMultiple(new FetchExpression(fetch));

            service.Delete("new_bpf_7013d7c4826745e182fabb09ef464219",result[0].Id);

        }
        public static void RenewLicense(IOrganizationService service)
        {
            List<List<string>> ids = GetIdsFromExcel("Ids.xlsx");
            for (int i = 0; i < ids[0].Count; i++)
            {
                var transactionRequest = new ExecuteTransactionRequest()
                {
                    Requests = new OrganizationRequestCollection(),
                    ReturnResponses = true
                };
                //update license
                Entity updatedLicense = new Entity("ntw_license");
                updatedLicense.Id = new Guid(ids[0][i]);
                updatedLicense.Attributes["ntw_expirydate"] = DateTime.Parse(ids[1][i].Split(' ')[0]).AddYears(3);
                UpdateRequest updatedLicenseRequest = new UpdateRequest() { Target = updatedLicense };
                transactionRequest.Requests.Add(updatedLicenseRequest);
                //create renew request
                Entity renewRequest = new Entity("ntw_licenserenewrequest");
                renewRequest.Attributes["ntw_requester"] = new EntityReference("contact", new Guid(ids[2][i]));
                renewRequest.Attributes["ntw_license"] = new EntityReference("ntw_license", new Guid(ids[0][i]));
                renewRequest.Attributes["ntw_failurereason"] = "No Failure";
                CreateRequest renewRequestRequest = new CreateRequest() { Target = renewRequest };
                transactionRequest.Requests.Add(renewRequestRequest);
                service.Execute(transactionRequest);
                Console.WriteLine("updated License ID : " + updatedLicense.Id + " order : " + (i + 1));

                if (i%1000 ==0 && i != 0)
                {
                    service = null;
                    service = CRMConnector.ConnectToCRM();
                }
            }
        }
        public static void ReadJsonFile(IOrganizationService service)
        {
            using (WebClient client = new WebClient())
            using (Stream stream = client.OpenRead("JanaRepayments.json"))
            using (StreamReader streamReader = new StreamReader(stream))
            using (JsonTextReader reader = new JsonTextReader(streamReader))
            {
                reader.SupportMultipleContent = true;

                var serializer = new JsonSerializer();
                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.StartObject)
                    {
                        object c = serializer.Deserialize<object>(reader);
                        Console.WriteLine(c.ToString() + " ");
                    }
                }
            }
        }
        public static bool StopDuplicationCase(IOrganizationService service)
        {
            Entity targetEntity = new Entity("incident");
            targetEntity.Attributes["ntw_newcustomer"] = new EntityReference("contact", new Guid("59fcffec-009c-ef11-b86d-00505692a571"));
            targetEntity.Attributes["ownerid"] = new EntityReference("systemuser", new Guid("ac5f552b-0827-ed11-b82b-00505692a571"));
            targetEntity.Attributes["ntw_requestsource"] = new OptionSetValue(2);
            try
            {
                Console.WriteLine("StopDuplicationCase method is hit successfully");

                // Get the ntw_newcustomer value from PreEntityImage
                string CustomerId = targetEntity.Attributes.Contains("ntw_newcustomer") ? targetEntity.GetAttributeValue<EntityReference>("ntw_newcustomer").Id.ToString() : string.Empty;
                Console.WriteLine("customer value" + CustomerId);
                Guid ownerId = targetEntity.Attributes.Contains("ownerid") ? targetEntity.GetAttributeValue<EntityReference>("ownerid").Id : Guid.Empty;
                Console.WriteLine("Owner Of the record" + ownerId);
                int RequestSourceValue = targetEntity.GetAttributeValue<OptionSetValue>("ntw_requestsource").Value;
                Console.WriteLine("Request Source Value" + RequestSourceValue);
                bool IsAccessAbleTOCreate = Applications.CheckRequestSource(RequestSourceValue);
                if (!IsAccessAbleTOCreate)
                {
                    var pendingCases = GetPendingCases(CustomerId, service);
                    Console.WriteLine("GetPendingCases method responed successfully");
                    if (pendingCases.Count > 0)
                    { return true; }
                    else
                    { return false; }
                }
                else { return false; }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public static DataCollection<Entity> GetPendingCases(string contactId, IOrganizationService service)
        {
            Console.WriteLine("GetPendingCases method is hit successfully");

            if (contactId == string.Empty)
                return null;

            string fetchXML = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' no-lock='false'>
                                  <entity name='incident'>
                                    <attribute name='incidentid' />
                                    <attribute name='statuscode' />
                                    <filter type='and'>
                                      <condition attribute='ntw_newcustomer' operator='eq' value='" + contactId + @"' />
                                      <condition attribute='statuscode' operator='not-in'>
                                        <value>961110004</value>
                                        <value>5</value>
                                        <value>1000</value>
                                        <value>2</value>
                                        <value>2000</value>
                                        <value>961110003</value>
                                      </condition>
                                    </filter>
                                  </entity>
                                </fetch>";
            var Collection = service.RetrieveMultiple(new FetchExpression(fetchXML));
            return Collection.Entities;
        }
        public static bool CheckRequestSource(int RequestSourceValue)
        {
            string config = "1";
            return config.Trim().Split(',').ToList().Contains(RequestSourceValue.ToString());
        }
        public static void UpdateFundingWalletSettlement(IOrganizationService service)
        {
            Console.WriteLine("UpdateFundingWalletSettlement");

            //get bad requests
            string settlementsFetch = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                            <entity name = 'ntw_fundingwalletsettlementrequest'>
                                                <attribute name = 'ntw_partner'/>
                                                <attribute name = 'ntw_fundingwalletsettlementrequestid'/>
                                                <order attribute = 'createdon' descending = 'true'/>
                                                <filter type = 'and'>
                                                    <condition attribute = 'ntw_partner' operator = 'not-null'/>
                                                    <condition attribute = 'ntw_fundingwallet' operator = 'null'/>
                                                    <condition attribute = 'statuscode' operator = 'eq' value = '1'/>
                                                </filter>
                                            </entity>
                                        </fetch >";
            var settlementsIds = service.RetrieveMultiple(new FetchExpression(settlementsFetch));
            foreach (var settlement in settlementsIds.Entities)
            {
                Console.WriteLine("new request update with id : " + settlement.Id);
                //get wallet from partner
                string walletFetch = @"<fetch version = '1.0' output-format = 'xml-platform' mapping = 'logical' distinct = 'false'>
                                                <entity name = 'account'>
                                                <attribute name = 'name'/>
                                                <attribute name = 'primarycontactid'/>
                                                <attribute name = 'telephone1'/>
                                                <attribute name = 'ntw_nationalid'/>
                                                <attribute name = 'accountid'/>
                                                <attribute name = 'new_fundingwallet'/>
                                                <order attribute = 'name' descending = 'false'/>
                                                <filter type = 'and'>
                                                <condition attribute = 'accountid' operator = 'eq' uitype = 'account' value = '{0}'/>
                                                </filter>
                                                </entity>
                                            </fetch>";
                walletFetch = String.Format(walletFetch, ((EntityReference)settlement.Attributes["ntw_partner"]).Id.ToString());

                var walletId = ((EntityReference)(service.RetrieveMultiple(new FetchExpression(walletFetch)).Entities.FirstOrDefault().Attributes["new_fundingwallet"])).Id;

                //update settlement with wallet
                settlement["ntw_fundingwallet"] = new EntityReference("ntw_fundingwallet",walletId);
                service.Update(settlement);

                ////call process on request completed

                //var executeWorkflowRequest = new ExecuteWorkflowRequest()
                //{
                //    WorkflowId = Guid.Parse("094EA7CA-E359-4FCF-9FD4-0239774DD5D9"), // Guid of workflow  
                //    EntityId = settlement.Id
                //};
                //var executeWorkflowResponse = (ExecuteWorkflowResponse)service.Execute(executeWorkflowRequest);
            }
                
        }
        public static void SLAMatixForCases(IOrganizationService service)
        {
            // < condition attribute = 'productserialnumber' operator= 'eq' value = '1-357390' />
            string fetchXmlFromCaseEntity = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                          <entity name='incident'>
                                            <attribute name='title' />
                                            <attribute name='ticketnumber' />
                                            <attribute name='createdon' />
                                            <attribute name='ntw_assigneddate' />
                                            <attribute name='incidentid' />
                                            <attribute name='caseorigincode' />
                                            <attribute name='ntw_communicationtype' />
                                            <attribute name='ntw_communicationreason' />
                                            <attribute name='productserialnumber' />
                                            <attribute name='statuscode' />
                                            <order attribute='title' descending='false' />
                                            <filter type='and'>
                                                <condition attribute='ownerid' operator='eq'  uitype='team' value='FCD0D336-63D5-EE11-B85A-00505692A571' />
                                            </filter>
                                          </entity>
                                        </fetch>";
            var Cases = service.RetrieveMultiple(new FetchExpression(fetchXmlFromCaseEntity));

            if (Cases.Entities != null && Cases.Entities.Count > 0)
            {
                foreach (var Case in Cases.Entities)
                {

                    EntityReference _communicationReason = Case.GetAttributeValue<EntityReference>("ntw_communicationreason");

                    int _communicationType = Case.Attributes.Contains("ntw_communicationtype") ? Case.GetAttributeValue<OptionSetValue>("ntw_communicationtype").Value : 0;

                    //var _createdOn = Case.GetAttributeValue<DateTime>("createdon").Date;
                    //var CurrentDate = DateTime.Now;
                    EntityCollection calendar = GetCustomerServiceCalender(service);
                    var calendarRules = calendar.Entities[0].GetAttributeValue<EntityCollection>("calendarrules");
                    var firstRulePattern = calendarRules[0].GetAttributeValue<string>("pattern");

                    var innerCalendarId = calendarRules[0].GetAttributeValue<EntityReference>("innercalendarid").Id;
                    var innerCalendar = service.Retrieve("calendar", innerCalendarId, new ColumnSet(true));
                    var innnerCalendarRule = innerCalendar.GetAttributeValue<EntityCollection>("calendarrules").Entities.FirstOrDefault();

                    //get working hours per day  = duration(mins)/60 

                    var duration = innnerCalendarRule.Attributes["duration"];
                    var workingHoursPerDay = (int)duration / 60;

                    //get working days

                    var workingDays = firstRulePattern.Substring(29).Split(',');
                    //DateTime createdOn = new DateTime(2022, 5, 22, 19, 0, 0);
                    DateTime createdOn = Case.GetAttributeValue<DateTime>("ntw_assigneddate");

                    //DateTime createdOn = Case.GetAttributeValue<DateTime>("ntw_assigneddate").Date;

                    DateTime workingDay = createdOn;
                    DateTime now = DateTime.Now;

                    //calculate no. of working days between created on of the case and current date

                    double totalTime = 0;

                    var diffDays = (now - createdOn).Days + 1;

                    for (int day = 1; day <= diffDays; day++)
                    {

                        bool isWorkingDay = false;
                        //ايام العمل ما عدا الجمعه والسبت
                        var pointer = workingDay.DayOfWeek.ToString().ToUpper().Substring(0, 2);
                        //من ضمن ايام العمل workingDays
                        if (workingDays.Contains(pointer))
                        {
                            isWorkingDay = true;
                        }
                        if (isWorkingDay)
                        {
                            //check if case opened within working hours
                            if (day == 1)
                            {
                                DateTime endOfWorkingDay = new DateTime(workingDay.Year, workingDay.Month, workingDay.Day, 17, 0, 0); //5 PM
                                DateTime startOfWorkingDay = new DateTime(workingDay.Year, workingDay.Month, workingDay.Day, 8, 0, 0); //8 AM


                                //DateTime endOfWorkingDay = DateTime.Parse("5:00:00 PM");
                                //int endTime = DateTime.Parse("5:00:00 PM").Hour;
                                DateTime startTimeOfCase = new DateTime(workingDay.Year, workingDay.Month, workingDay.Day, workingDay.TimeOfDay.Hours, workingDay.TimeOfDay.Minutes, workingDay.TimeOfDay.Seconds);

                                var isBeforeEndHour = DateTime.Compare(endOfWorkingDay, startTimeOfCase);
                                var isAfterStartHour = DateTime.Compare(startOfWorkingDay, startTimeOfCase);
                                if (isBeforeEndHour > 0) //1
                                {
                                    if (isAfterStartHour > 0) //1
                                    {
                                        totalTime += workingHoursPerDay;

                                    }
                                    //endtimeofworkingday-starttimeofcase
                                    else if (isAfterStartHour < 0)//-1
                                    {
                                        totalTime += endOfWorkingDay.Hour - startTimeOfCase.Hour;
                                        //totalTime += startTimeOfCase.Hour - startOfWorkingDay.Hour;
                                    }
                                }

                            }
                            else if (day == diffDays)
                            {

                                DateTime startOfWorkingDay = new DateTime(workingDay.Year, workingDay.Month, workingDay.Day, 8, 0, 0); //8 AM
                                DateTime currentWorkingDayTime = new DateTime(workingDay.Year, workingDay.Month, workingDay.Day, 13, 0, 0); //1 PM
                                                                                                                                            //now
                                                                                                                                            // DateTime currentWorkingDayTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0); //1 PM
                                var result = DateTime.Compare(now, startOfWorkingDay);

                                if (result > 0)
                                {
                                    totalTime += currentWorkingDayTime.Hour - startOfWorkingDay.Hour;
                                }

                            }
                            else
                            {
                                totalTime += workingHoursPerDay;
                            }

                        }
                        workingDay = workingDay.AddDays(1);
                    }

                    double NumberOfDifferentDays = totalTime / workingHoursPerDay;
                    //var NumberOfDifferentDays = CurrentDate.Subtract(_createdOn).TotalDays;//(CurrentDate-_createdOn).Days;

                    string fetchXmlFromEscalationMatrix = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                        <entity name='ntw_slamatrix' >
                                        <attribute name = 'ntw_slamatrixid' />
                                        <attribute name = 'ntw_name' />     
                                        <attribute name = 'createdon' />      
                                        <attribute name = 'ntw_communicationtype' />       
                                        <attribute name = 'ntw_escalationlevel4' />       
                                        <attribute name = 'ntw_escalationlevel2' />        
                                        <attribute name = 'ntw_escalationlevel3'/>          
                                        <attribute name = 'ntw_esclationlevel' />         
                                        <attribute name = 'ntw_escalationperiod4' />           
                                        <attribute name = 'ntw_escalationperiod2' />             
                                        <attribute name = 'ntw_escalationperiod3' />             
                                        <attribute name = 'ntw_escalationvalue' />              
                                        <attribute name = 'ntw_communicationreason' />                
                                        <order attribute = 'ntw_name' descending = 'false' />                 
                                        <filter type = 'and' >   
                                        <condition attribute='statecode' operator='eq' value='0' />
                                        <condition attribute = 'ntw_communicationreason' operator= 'eq' uitype = 'ntw_communicationreason' value = '" + _communicationReason.Id.ToString() + @"' />                           
                                        <condition attribute = 'ntw_communicationtype' operator= 'eq' value = '" + _communicationType.ToString() + @"' />                               
                                                   
                                        </filter >
                                        </entity >
                                        </fetch>";


                    var fetchXmlFromEscalationMatrixResult = service.RetrieveMultiple(new FetchExpression(fetchXmlFromEscalationMatrix));
                    if (fetchXmlFromEscalationMatrixResult.Entities != null && fetchXmlFromEscalationMatrixResult.Entities.Count > 0)
                    {
                        var EscalationMatrix = fetchXmlFromEscalationMatrixResult.Entities[0];
                        var ntw_escalationvalue1 = EscalationMatrix.GetAttributeValue<int>("ntw_escalationvalue");
                        var ntw_escalationperiod2 = EscalationMatrix.GetAttributeValue<int>("ntw_escalationperiod2");
                        var ntw_escalationperiod3 = EscalationMatrix.GetAttributeValue<int>("ntw_escalationperiod3");
                        var ntw_escalationperiod4 = EscalationMatrix.GetAttributeValue<int>("ntw_escalationperiod4");
                        var _escalationLevel1 = EscalationMatrix.GetAttributeValue<EntityReference>("ntw_esclationlevel");
                        var _escalationLevel2 = EscalationMatrix.GetAttributeValue<EntityReference>("ntw_escalationlevel2");
                        var _escalationLevel3 = EscalationMatrix.GetAttributeValue<EntityReference>("ntw_escalationlevel3");
                        var _escalationLevel4 = EscalationMatrix.GetAttributeValue<EntityReference>("ntw_escalationlevel4");

                        if (NumberOfDifferentDays > (double)ntw_escalationvalue1)
                        {

                            Case["ntw_escalationlevel"] = new EntityReference(_escalationLevel1.LogicalName, _escalationLevel1.Id);

                        }
                        if (NumberOfDifferentDays > (double)ntw_escalationperiod2)
                        {

                            Case["ntw_escalationlevel"] = new EntityReference(_escalationLevel2.LogicalName, _escalationLevel2.Id);

                        }
                        if (NumberOfDifferentDays > (double)ntw_escalationperiod3)
                        {

                            Case["ntw_escalationlevel"] = new EntityReference(_escalationLevel3.LogicalName, _escalationLevel3.Id);

                        }
                        if (NumberOfDifferentDays > (double)ntw_escalationperiod4)
                        {

                            Case["ntw_escalationlevel"] = new EntityReference(_escalationLevel4.LogicalName, _escalationLevel4.Id);

                        }


                        service.Update(Case);

                    }

                }
            }
        }
        public static EntityCollection GetCustomerServiceCalender(IOrganizationService service)
        {
            var slaCalender = GetConfigurationValue(service, "SLA_CALENDER").StringVal;
            QueryExpression cal = new QueryExpression("calendar") { ColumnSet = { AllColumns = true } };
            cal.Criteria.AddCondition("name", ConditionOperator.Equal, slaCalender);
            return service.RetrieveMultiple(cal);
        }
        public static ConfigurationValue GetConfigurationValue(IOrganizationService service, string code)
        {
            ConfigurationValue configurationValue = new ConfigurationValue();

            List<CrmCondition> conditions = new List<CrmCondition>();
            conditions.Add(new CrmCondition
            {
                Key = "ntw_configurationcode",
                Value = code,
                ConditionOperator = ConditionOperator.Equal
            });

            var entityCollection = GetEntityData(service, "ntw_sdbconfiguratin", conditions, true, null);
            if (entityCollection.Entities.Count > 0)
            {
                var entity = entityCollection.Entities[0];
                if (entity.Attributes.Contains("ntw_textvalue"))
                {
                    configurationValue.StringVal = entity.GetAttributeValue<string>("ntw_textvalue");
                }
                if (entity.Attributes.Contains("ntw_numbervalue"))
                {
                    configurationValue.IntVal = entity.GetAttributeValue<int>("ntw_numbervalue");
                }
                if (entity.Attributes.Contains("ntw_datevalue"))
                {
                    configurationValue.DateVal = entity.GetAttributeValue<DateTime>("ntw_datevalue");
                }
            }
            return configurationValue;
        }
        public static EntityCollection GetEntityData(IOrganizationService service, string entityName, List<CrmCondition> conditionAttributes,
                                                        bool retrieveAllColumns, params string[] columns)
        {
            QueryExpression MyQuery = new QueryExpression();

            MyQuery.EntityName = entityName;
            if (retrieveAllColumns)
                MyQuery.ColumnSet = new ColumnSet(true);
            else
                MyQuery.ColumnSet = new ColumnSet(columns);

            MyQuery.Criteria = new FilterExpression();
            MyQuery.Criteria.FilterOperator = LogicalOperator.And;
            foreach (var attribute in conditionAttributes)
            {
                ConditionExpression Condition = new ConditionExpression();
                Condition.AttributeName = attribute.Key;
                Condition.Operator = attribute.ConditionOperator;
                Condition.Values.Add(attribute.Value);
                MyQuery.Criteria.Conditions.Add(Condition);
            }
            return service.RetrieveMultiple(MyQuery);
        }
        public static void UpdateLoanRequests(IOrganizationService service)
        {
            List<List<string>> ids = GetIdsFromExcel("Ids.xlsx");
            for (int i = 0; i < ids[0].Count; i++)
            {
                EntityReference rightWalletId = new EntityReference("ntw_fundingwallet", new Guid(ids[1][i]));
                EntityReference loanRequestId = new EntityReference("ntw_productivefamilyloanrequest", new Guid(ids[0][i]));

                //get right funding wallet type if not virtual clear Fees info if Virtual set fees based on the configurations

                //get wallet type
                Entity rightWallet = service.Retrieve("ntw_fundingwallet", rightWalletId.Id, new ColumnSet("ntw_wallettype"));

                //get request amount
                Entity loanRequest = service.Retrieve("ntw_productivefamilyloanrequest", loanRequestId.Id, new ColumnSet("ntw_loanamounts"));
                EntityReference ntw_loanamounts = (EntityReference)loanRequest.Attributes["ntw_loanamounts"];
                Entity loanRequestAmount = service.Retrieve("ntw_loanconfiguration", ntw_loanamounts.Id, new ColumnSet("ntw_loanamount"));

                OptionSetValue ntw_wallettype = (OptionSetValue) rightWallet.Attributes["ntw_wallettype"];
                if (ntw_wallettype.Value == 1) // تمويلية 1
                {
                    //clear fees
                    loanRequest.Attributes["ntw_payfees"] = new decimal(0.00);
                }
                else if (ntw_wallettype.Value == 2) //إفتراضية	2
                {
                    decimal loanRequestAmountValue = ((Money)loanRequestAmount.Attributes["ntw_loanamount"]).Value;
                    //set fees
                    if (loanRequestAmountValue > 20000)
                    {
                        //set fees to 750
                        loanRequest.Attributes["ntw_payfees"] = new decimal(750.00);
                    }
                    else if (loanRequestAmountValue <= 20000)
                    {
                        //set fees to 500
                        loanRequest.Attributes["ntw_payfees"] = new decimal(500);
                    }
                }
                //set right wallet
                loanRequest.Attributes["ntw_fundingwallet"] = rightWalletId;

                service.Update(loanRequest);

                //var executeWorkflowRequest = new ExecuteWorkflowRequest() 
                //{
                //    WorkflowId = Guid.Parse(updateIbanWFId),
                //    EntityId = Guid.Parse(ids[0][i])
                //};
                //var executeWorkflowResponse = (ExecuteWorkflowResponse)service.Execute(executeWorkflowRequest);

                Console.WriteLine(Guid.Parse(ids[0][i]) + " # " + (i+1) );
            }
        }
        public static void UpdateLoanPartner(IOrganizationService service)
        {
            List<List<string>> ids = GetIdsFromExcel("Ids.xlsx");
            for (int i = 0; i < ids[0].Count; i++)
            {
                EntityReference partnerid = new EntityReference("account", new Guid("6fdcba20-9276-f011-913d-005056a50502"));
                EntityReference requestid = new EntityReference("ntw_productivefamilyloanrequest", new Guid(ids[0][i]));
                EntityReference statusid = new EntityReference("ntw_requeststatus", new Guid("d80f2cc0-89cc-eb11-baeb-00155d003c17"));

                Entity loanRequest = service.Retrieve("ntw_productivefamilyloanrequest", requestid.Id, new ColumnSet("ntw_partnerid"));
               
                loanRequest.Attributes["ntw_partnerid"] = partnerid;
                loanRequest.Attributes["ntw_requeststatusid"] = statusid;
                
                service.Update(loanRequest);

                //var executeWorkflowRequest = new ExecuteWorkflowRequest() 
                //{
                //    WorkflowId = Guid.Parse(updateIbanWFId),
                //    EntityId = Guid.Parse(ids[0][i])
                //};
                //var executeWorkflowResponse = (ExecuteWorkflowResponse)service.Execute(executeWorkflowRequest);

                Console.WriteLine(Guid.Parse(ids[0][i]) + " # " + (i + 1));
            }
        }

        public static void TestSMSWithRMQueue(IOrganizationService service)
        {
            Entity rmqEntity = CRMHelper.RetrieveEntityById(service, "ntw_rabbitmqmessages", new Guid("a7f03ab6-df03-f011-b87b-00505692a571"), null, true);

            var entityName = rmqEntity.LogicalName;

            //var rabbitMqMessage = CRMHelper.RetrieveEntityById(service, "ntw_rabbitmqmessages",new ColumnSet("ntw_messagetype",);
            var messageType = rmqEntity.GetAttributeValue<OptionSetValue>("ntw_messagetype").Value;

            Console.WriteLine("Message Type " + messageType);


            //get request entity type
            var requestEntityId = rmqEntity.GetAttributeValue<EntityReference>("ntw_requestentity").Id;

            Console.WriteLine("request entity type " + requestEntityId);

            var requestEntity = CRMHelper.RetrieveEntityById(service, "ntw_requestentities", requestEntityId, null, true);

            var crmEntityCode = requestEntity.GetAttributeValue<string>("ntw_code");

            Console.WriteLine("crmEntityCode " + crmEntityCode);


            //get queue configuration
            var queueId = rmqEntity.GetAttributeValue<EntityReference>("ntw_queuename").Id;

            RMQueue queue = GetRmqConfigurations(service, queueId);

            Console.WriteLine("RMQueue " + queue.Name);



            #region Send SMS
            if (messageType == (int)RMQMessageType.SMS)
            {
                SMSByAppIDRequest smsMessage = new SMSByAppIDRequest();

                smsMessage.MessageId = rmqEntity.Id;
                smsMessage.Mobiles = rmqEntity.GetAttributeValue<string>("ntw_parameterscsv");
                string caseNumber = rmqEntity.GetAttributeValue<string>("nw_additionaldata");

                //get notification record data
                var notificationId = rmqEntity.GetAttributeValue<EntityReference>("ntw_requestnotification").Id;
                var notificationEntity = CRMHelper.RetrieveEntityById(service, "ntw_requestnotification", notificationId, null, true);

                //get template data
                var templateId = notificationEntity.GetAttributeValue<EntityReference>("ntw_notificationtemplate").Id;
                var notificationTemplate = CRMHelper.RetrieveEntityById(service, "ntw_requestnotificationtemplate", templateId, null, true);
    
                smsMessage.Title = notificationTemplate.GetAttributeValue<string>("ntw_name");
                smsMessage.MessageText = notificationTemplate.GetAttributeValue<string>("ntw_notificationbodyar");
                if (smsMessage.MessageText.Contains("ticketnumber"))
                {
                    smsMessage.MessageText = smsMessage.MessageText.Replace("ticketnumber", caseNumber);
                    Console.WriteLine("smsMessage.MessageText " + smsMessage.MessageText);
                }
                else
                {
                    smsMessage.MessageText = smsMessage.MessageText + Environment.NewLine + "رقم الطلب: " + caseNumber;
                }

                PushMessage(queue, smsMessage);
            }
            #endregion Send SMS

        }
        public static void PushMessage(RMQueue queue, object message)
        {
            try
            {
                var rabbitMQfactory = new ConnectionFactory()
                {
                    HostName = queue.URL,
                    Port = int.Parse(queue.Port),
                    UserName = queue.Username,
                    Password = queue.Password
                };
                //using (var connection = rabbitMQfactory.CreateConnectionAsync())
                //using (var channel = connection.CreateModel())
                //{
                //    channel.QueueDeclare(queue: queue.Name, durable: true, exclusive: false, autoDelete: false, arguments: null);

                //    JavaScriptSerializer js = new JavaScriptSerializer();

                //    string jsonMessage = js.Serialize(message);

                //    var body = Encoding.UTF8.GetBytes(jsonMessage);

                //    channel.BasicPublish(exchange: "", routingKey: queue.Name, basicProperties: null, body: body);
                //}
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static RMQueue GetRmqConfigurations(IOrganizationService service, Guid queueId)
        {
            var queueEntity = CRMHelper.RetrieveEntityById(service, "ntw_queue", queueId, null, true);
            return new RMQueue
            {
                Name = queueEntity.GetAttributeValue<string>("ntw_name"),
                URL = queueEntity.GetAttributeValue<string>("ntw_rabbitmqurl"),
                Port = queueEntity.GetAttributeValue<string>("ntw_port"),
                Username = queueEntity.GetAttributeValue<string>("ntw_username"),
                Password = queueEntity.GetAttributeValue<string>("ntw_password")

            };
        }

        public Response UpdateLoanReponseWaseet(IOrganizationService service)
        {
            LoanCreationResponse model = new LoanCreationResponse();
            model.loanRequestNo = "4119049381";
            model.disburcements = new List<Disburcement>();
            model.disburcements.Add(new Disburcement() { no = "1", amount = "29250.0", dueDate = "2025-03-02T13:40:53", status = "Paid", B2BBankRef = "20268653" });
            model.installments = new List<installment>();
            model.installments.Add(new installment() { no = "1",amount="1200.0",dueDate="2026-04-01T00:00:00"});
            model.installments.Add(new installment() { no = "2",amount="1200.0",dueDate="2026-05-01T00:00:00" });
            model.installments.Add(new installment() { no = "3",amount="1200.0",dueDate= "2026-06-01T00:00:00" });
            model.installments.Add(new installment() { no = "4",amount="1200.0",dueDate= "2026-07-01T00:00:00" });
            model.installments.Add(new installment() { no = "5",amount="1200.0",dueDate= "2026-08-01T00:00:00" });
            model.installments.Add(new installment() { no = "6",amount="1200.0",dueDate= "2026-09-01T00:00:00" });
            model.installments.Add(new installment() { no = "7",amount="1200.0",dueDate= "2026-10-01T00:00:00" });
            model.installments.Add(new installment() { no = "8",amount="1200.0",dueDate= "2026-11-01T00:00:00" });
            model.installments.Add(new installment() { no = "9",amount="1200.0",dueDate= "2026-12-01T00:00:00" });
            model.installments.Add(new installment() { no = "10",amount="1200.0",dueDate= "2027-01-01T00:00:00" });
            model.installments.Add(new installment() { no = "11",amount="1200.0",dueDate= "2027-02-01T00:00:00" });
            model.installments.Add(new installment() { no = "12",amount="1200.0",dueDate= "2027-03-01T00:00:00" });
            model.installments.Add(new installment() { no = "13",amount="1200.0",dueDate= "2027-04-01T00:00:00" });
            model.installments.Add(new installment() { no = "14",amount="1200.0",dueDate= "2027-05-01T00:00:00" });
            model.installments.Add(new installment() { no = "15",amount="1200.0",dueDate= "2027-06-01T00:00:00" });
            model.installments.Add(new installment() { no = "16",amount="1200.0",dueDate= "2027-07-01T00:00:00" });
            model.installments.Add(new installment() { no = "17",amount="1200.0",dueDate= "2027-08-01T00:00:00" });
            model.installments.Add(new installment() { no = "18",amount="1200.0",dueDate="2027-09-01T00:00:00"});
            model.installments.Add(new installment() { no = "19",amount="1200.0",dueDate="2027-10-01T00:00:00"});
            model.installments.Add(new installment() { no = "20",amount="1200.0",dueDate="2027-11-01T00:00:00"});
            model.installments.Add(new installment() { no = "21",amount="1200.0",dueDate="2027-12-01T00:00:00"});
            model.installments.Add(new installment() { no = "22",amount="1200.0",dueDate="2028-01-01T00:00:00"});
            model.installments.Add(new installment() { no = "23",amount="1200.0",dueDate="2028-02-01T00:00:00"});
            model.installments.Add(new installment() { no = "24",amount="1200.0",dueDate="2028-03-01T00:00:00"});
            model.installments.Add(new installment() { no = "25",amount="1200.0",dueDate="2028-04-01T00:00:00"});
      
            model.Status = "opened";
            model.errors = new List<LoanError>();
            model.errors.Add(new LoanError() { code = "", message = "" });
            try
            {
                if (!string.IsNullOrEmpty(model.loanRequestNo))
                {
                    JavaScriptSerializer js = new JavaScriptSerializer();
                    string jsonMessage = js.Serialize(model);
                

                    Entity LoanDisbursementRequest = CRMHelper.RetrieveEntity("ntw_createnewpaymentrequest", ConditionOperator.Equal, "sdb_waseetreferencenumber", model.loanRequestNo, new ColumnSet("sdb_waseetreferencenumber", "ntw_requeststatusid", "ntw_loan"), service);

                    if (!LoanDisbursementRequest.Attributes.Contains("ntw_loan")) { throw new Exception("Loan is Not Found in Disbursement Request"); }
                    EntityReference entityRefLoan = LoanDisbursementRequest.GetAttributeValue<EntityReference>("ntw_loan");

                    ColumnSet columnSetLoan = new ColumnSet("ntw_waseetloanreferencenumber", "ntw_name", "ntw_totalinstallments", "ntw_loanstatus", "ntw_installmentnumber", "ntw_loanamount", "ntw_loanispaid", "ntw_loanpaymentdate", "sdb_loandisbursedon");
                    var loanEntity = CRMHelper.RetrieveEntity("ntw_loan", ConditionOperator.Equal, "ntw_loanid", entityRefLoan.Id.ToString(), columnSetLoan, service);

                    if (loanEntity != null)
                    {
                        //Entity LoanDisbursementRequest = CRMHelper.RetrieveEntity("ntw_createnewpaymentrequest", ConditionOperator.Equal, "ntw_loan", loanEntity.Id.ToString(), new ColumnSet("ntw_requeststatusid"), service);
                        if (LoanDisbursementRequest != null)
                        {
                            try
                            {
                                if (!string.IsNullOrEmpty(model.Status))
                                {
                                    if (model.Status.ToLower() != "opened" && LoanDisbursementRequest != null)
                                    {
                                        UpdateDisbursementStatusRequest updateDisbursementStatusRequest = new UpdateDisbursementStatusRequest
                                        {
                                            LoanNumber = loanEntity.GetAttributeValue<string>("ntw_name"),
                                            DisbursementRequestGUID = LoanDisbursementRequest.Id,
                                            DisbursementStatusCode = "CNPR_REJECTED"
                                        };
                                        Console.WriteLine("Update Loan Disbursement Request to Reject: " + js.Serialize(updateDisbursementStatusRequest));
                                        var updateDisbursementStatus = UpdateDisbursementStatus(updateDisbursementStatusRequest, service);
                                        if (updateDisbursementStatus.IsSucceeded)
                                        {
                                            var createLogRequest = new CreateLogRequest();
                                            createLogRequest.Name = "تم إلغاء طلب الصرف بنجاح";
                                            createLogRequest.Request = jsonMessage;
                                            createLogRequest.Response = "0";
                                            createLogRequest.Status = 556310000;
                                            createLogRequest.CreateNewPaymentRequest = new EntityReference("ntw_createnewpaymentrequest", LoanDisbursementRequest.Id);
                                            CreateLog(service, createLogRequest);
                                        }
                                        else
                                            throw new Exception(js.Serialize(updateDisbursementStatus));
                                        return new Response()
                                        {
                                            status = StatusCode.SUCCESS
                                        };
                                    }
                                    else if (model.disburcements != null && model.disburcements.Count > 0)
                                    {
                                        if (model.Status.ToLower() == "opened" && model.disburcements?.FirstOrDefault()?.status.ToLower() == "unpaid".ToLower())
                                        {
                                            UpdateDisbursementStatusRequest updateDisbursementStatusRequest = new UpdateDisbursementStatusRequest
                                            {
                                                LoanNumber = loanEntity.GetAttributeValue<string>("ntw_name"),
                                                DisbursementRequestGUID = LoanDisbursementRequest.Id,
                                                DisbursementStatusCode = "CNPR_REJECTED"
                                            };
                                            Console.WriteLine("Unpaid - Update Loan Disbursement Request to Reject: " + js.Serialize(updateDisbursementStatusRequest));
                                            var updateDisbursementStatus = UpdateDisbursementStatus(updateDisbursementStatusRequest, service);
                                            //Create Loag Request
                                            if (updateDisbursementStatus.IsSucceeded == true)
                                            {
                                                var createLogRequest = new CreateLogRequest();
                                                createLogRequest.Name = "لم يتم انشاء طلب الصرف علي نظام وسيط";
                                                createLogRequest.Request = jsonMessage;
                                                createLogRequest.Response = "3";
                                                createLogRequest.Status = 556310001;
                                                createLogRequest.CreateNewPaymentRequest = new EntityReference("ntw_createnewpaymentrequest", LoanDisbursementRequest.Id);
                                                CreateLog(service, createLogRequest);
                                            }
                                            else
                                                throw new Exception(js.Serialize(updateDisbursementStatus));
                                            return new Response()
                                            {
                                                status = StatusCode.FAILED_NO_TRY
                                            };
                                        }
                                        else if (model.Status.ToLower() == "opened" && model.disburcements?.FirstOrDefault()?.status.ToLower() == "paid".ToLower())
                                        {
                                            #region Validate Loan Installments
                                            int installamentsCount = loanEntity.Attributes.Contains("ntw_totalinstallments") ? loanEntity.GetAttributeValue<int>("ntw_totalinstallments") : -1;
                                            // Get the installemnts count from Productive Family Loan Request if the value in Loan is 0 or -1.
                                            if (installamentsCount == 0 || installamentsCount == -1)
                                            {
                                                throw new Exception("The installemnts count(" + installamentsCount + ") from source is not valid.");
                                            }

                                            if (installamentsCount != model.installments.Count)
                                                throw new Exception("The request installemnts count(" + model.installments.Count + ") doesn't match with installemnt count(" + installamentsCount + ") from source.");
                                            #endregion

                                            #region Validate if Loan is already disbursed
                                            EntityReference loanStatusEntity = loanEntity.Attributes.Contains("ntw_loanstatus") ? loanEntity.GetAttributeValue<EntityReference>("ntw_loanstatus") : null;

                                            Entity notDisbursedLoanStatus = service.RetrieveMultiple(new QueryExpression("ntw_loanstatus")
                                            {
                                                ColumnSet = new ColumnSet(true),
                                                Criteria = new FilterExpression(LogicalOperator.And)
                                                {
                                                    Conditions = {
                                                    //new ConditionExpression("ntw_name", ConditionOperator.Equal, "لم يتم الصرف"),
                                                    new ConditionExpression("ntw_projectloancontractstatusid", ConditionOperator.Equal, 5),
                                                    new ConditionExpression("statecode", ConditionOperator.Equal, 0),
                                                }
                                                },
                                                Orders = { new OrderExpression("modifiedon", OrderType.Descending) },
                                                Distinct = true
                                            }).Entities.FirstOrDefault();

                                            bool loanispaid = loanEntity.Attributes.Contains("ntw_loanispaid") && loanEntity.GetAttributeValue<bool>("ntw_loanispaid");

                                            string loanpaymentdate = loanEntity.Attributes.Contains("ntw_loanpaymentdate") ? loanEntity.GetAttributeValue<string>("ntw_loanpaymentdate") : string.Empty;
                                            if (loanpaymentdate == string.Empty)
                                                loanpaymentdate = loanEntity.Attributes.Contains("sdb_loandisbursedon") ? loanEntity.GetAttributeValue<DateTime>("sdb_loandisbursedon").ToShortDateString() : string.Empty;

                                            EntityCollection existingInstallments = service.RetrieveMultiple(new QueryExpression("ntw_loaninstallmentschedule")
                                            {
                                                ColumnSet = new ColumnSet("ntw_loan"),
                                                Criteria = new FilterExpression(LogicalOperator.And)
                                                {
                                                    Conditions = {
                                                    new ConditionExpression("ntw_loan", ConditionOperator.Equal, loanEntity.Id),
                                                    new ConditionExpression("statecode", ConditionOperator.Equal, 0)
                                                }
                                                },
                                                Orders = { new OrderExpression("modifiedon", OrderType.Descending) },
                                                Distinct = true
                                            });

                                            if (loanStatusEntity.Id != notDisbursedLoanStatus.Id || loanispaid == true || loanpaymentdate != string.Empty || existingInstallments.Entities.Count > 0)
                                                throw new Exception("Loan is already disbursed.");
                                            #endregion

                                            #region validate loan amount with installment amounts
                                            decimal totalReceivedInstallmentAmount = model.installments.Aggregate(0m, (sum, installment) => sum + decimal.Parse(installment.amount));
                                            if (!loanEntity.Attributes.Contains("ntw_loanamount"))
                                                throw new Exception("Loan amount on the loan does not exits");
                                            else
                                            {
                                                if (loanEntity.GetAttributeValue<Money>("ntw_loanamount").Value != totalReceivedInstallmentAmount)
                                                    throw new Exception("Loan amount is not equal the recieved installments");
                                            }

                                            #endregion

                                            UpdateDisbursementStatusRequest updateDisbursementStatusRequest = new UpdateDisbursementStatusRequest
                                            {
                                                LoanNumber = loanEntity.GetAttributeValue<string>("ntw_name"),
                                                DisbursementRequestGUID = LoanDisbursementRequest.Id,
                                                DisbursementStatusCode = "CNPR_APPROVED",
                                                DisbursementDueDate = model.disburcements.FirstOrDefault().dueDate
                                            };

                                            loanEntity["ntw_waseetloanreferencenumber"] = model.loanRequestNo;
                                            service.Update(loanEntity);
                                            loanEntity = CRMHelper.RetrieveEntity("ntw_loan", ConditionOperator.Equal, "ntw_loanid", entityRefLoan.Id.ToString(), columnSetLoan, service);

                                            Console.WriteLine("Update Loan Disbursement Request to Approved: " + js.Serialize(updateDisbursementStatusRequest));
                                            var updateDisbursementStatus = UpdateDisbursementStatus(updateDisbursementStatusRequest, service);
                                            if (updateDisbursementStatus.IsSucceeded == true)
                                            {
                                                //Create Log Request
                                                var createLogRequest = new CreateLogRequest();
                                                createLogRequest.Name = "تم صرف القرض";
                                                createLogRequest.Request = jsonMessage;
                                                createLogRequest.Response = "1";
                                                createLogRequest.Status = 556310000;
                                                createLogRequest.CreateNewPaymentRequest = new EntityReference("ntw_createnewpaymentrequest", LoanDisbursementRequest.Id);
                                                CreateLog(service, createLogRequest);

                                                Console.WriteLine("Create Installment on crm return status with 1");
                                                if (model.installments != null && model.installments.Count > 0)
                                                {
                                                    List<Installment> installments = new List<Installment>();
                                                    foreach (var item in model.installments)
                                                    {
                                                        Installment installment = new Installment
                                                        {
                                                            Amount = decimal.Parse(item.amount),
                                                            DueDate = item.dueDate,
                                                            InstallmentNumber = int.Parse(item.no)
                                                        };
                                                        installments.Add(installment);

                                                    }
                                                    CreateLoanInstallmentScheduleRequest createLoanInstallmentScheduleRequest = new CreateLoanInstallmentScheduleRequest
                                                    {
                                                        LoanNumber = loanEntity.GetAttributeValue<string>("ntw_waseetloanreferencenumber"),
                                                        loanInstallments = installments
                                                    };
                                                    var Response = CreateLoanInstallmentsSchedule(createLoanInstallmentScheduleRequest, service);

                                                    if (Response.IsSucceeded == true)
                                                    {
                                                        //Create Loag Request
                                                        createLogRequest = new CreateLogRequest();
                                                        createLogRequest.Name = "تم انشاء اقساط القرش علي نظام الدينامكس";
                                                        createLogRequest.Request = jsonMessage;
                                                        createLogRequest.Response = "1";
                                                        createLogRequest.Status = 556310000;
                                                        createLogRequest.CreateNewPaymentRequest = new EntityReference("ntw_createnewpaymentrequest", LoanDisbursementRequest.Id);
                                                        CreateLog(service, createLogRequest);
                                                        //1 Succeeded
                                                        return new Response()
                                                        {
                                                            status = StatusCode.SUCCESS
                                                        };
                                                    }
                                                    else
                                                        throw new Exception(js.Serialize(Response));
                                                }
                                            }
                                            else
                                                throw new Exception(js.Serialize(updateDisbursementStatus));
                                        }

                                    }
                                }
                                else
                                    throw new Exception("Request Status is Empty or Null");
                            }
                            catch (Exception ex)
                            {
                                var createLogRequest = new CreateLogRequest
                                {
                                    Name = "Message from Custom Activity",
                                    Request = jsonMessage,
                                    Response = ex.Message,
                                    Status = 556310001,
                                    CreateNewPaymentRequest = new EntityReference("ntw_createnewpaymentrequest", LoanDisbursementRequest.Id)
                                };
                                CreateLog(service, createLogRequest);
                                throw new Exception(ex.Message);
                                return new Response()
                                {
                                    status = StatusCode.FAILED_NO_TRY
                                };
                            }

                        }
                        else
                            throw new Exception("LoanDisbursementRequest is Null");
                    }

                    else
                        throw new Exception("Loan is Null");
                }
                else
                    throw new Exception("Loan Request Number is Null or Empty");
                return new Response()
                {
                    status = StatusCode.FAILED_NO_TRY
                };
            }
            //3 failed no try
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException(ex.Message);
                return new Response()
                {
                    status = StatusCode.FAILED_NO_TRY
                };
            }
        }
        public static void CreateLog(IOrganizationService service, CreateLogRequest createLogRequest)
        {
            if (createLogRequest != null)
            {
                Entity newRecord = new Entity("ntw_pfpcreateloanlog");
                // Set attribute values for the new record
                newRecord["ntw_name"] = createLogRequest.Name;
                newRecord["ntw_response"] = createLogRequest.Response;
                newRecord["ntw_request"] = createLogRequest.Request;
                newRecord["ntw_createnewpaymentrequest"] = createLogRequest.CreateNewPaymentRequest;
                newRecord["statuscode"] = new OptionSetValue(createLogRequest.Status);
                // Create the record in CRM
                Guid createdRecordId = service.Create(newRecord);
            }
        }
        private Guid? GetDecisionID(string decision, Guid Requestid, IOrganizationService service)
        {

            string DecisionFetch = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
  <entity name='ntw_decision'>
    <attribute name='ntw_decisionid' />
    <attribute name='ntw_name' />
    <attribute name='createdon' />
    <order attribute='ntw_name' descending='false' />
    <filter type='and'>
      <condition attribute='ntw_requeststatusid' operator='eq' uitype='ntw_requeststatus' value='{" + Requestid + @"}' />
      <condition attribute='ntw_code' operator='eq' value='" + decision + @"' />
    </filter>
  </entity>agger
</fetch>";
            EntityCollection DecisionResult = service.RetrieveMultiple(new FetchExpression(DecisionFetch));
            if (DecisionResult != null && DecisionResult.Entities.Any())
            {
                return DecisionResult.Entities.First().Id;
            }
            else { return null; }

        }
        public Response<UpdateDisbursementStatusResponse> UpdateDisbursementStatus(UpdateDisbursementStatusRequest request, IOrganizationService service)
        {
            try
            {
                var fetchXml = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                                <fetch top=""1"">
                                  <entity name=""ntw_createnewpaymentrequest"">
                                    <filter>
                                      <condition attribute=""ntw_createnewpaymentrequestid"" operator=""eq"" value=""{request.DisbursementRequestGUID}"" uiname=""89"" uitype=""ntw_createnewpaymentrequest"" />
                                    </filter>
                                    <link-entity name=""ntw_loan"" from=""ntw_loanid"" to=""ntw_loan"" alias=""loan"">
                                      <filter>
                                        <condition attribute=""ntw_name"" operator=""eq"" value=""{request.LoanNumber}"" />
                                      </filter>
                                    </link-entity>
                                  </entity>
                                </fetch>";
                EntityCollection Createnewpaymentrequest = service.RetrieveMultiple(new FetchExpression(fetchXml));
                if (Createnewpaymentrequest != null && Createnewpaymentrequest.Entities.Any())
                {
                    Entity CreatenewpaymentrequestEntity = Createnewpaymentrequest.Entities.First();

                    if (request.DisbursementStatusCode.ToUpper() == "CNPR_REJECTED")
                    {
                        if (CreatenewpaymentrequestEntity.Contains("ntw_requeststatusid"))
                        {
                            Guid? DecisionID = GetDecisionID("CNPR_APO_Reject", (CreatenewpaymentrequestEntity.GetAttributeValue<EntityReference>("ntw_requeststatusid")).Id, service);
                            if (DecisionID != null && DecisionID.HasValue && DecisionID.Value != Guid.Empty)
                            {
                                Entity paymentrequestEntity = new Entity("ntw_createnewpaymentrequest");
                                paymentrequestEntity.Id = CreatenewpaymentrequestEntity.Id;
                                paymentrequestEntity["ntw_decisionid"] = new EntityReference("ntw_decision", DecisionID.Value);

                                service.Update(paymentrequestEntity);
                                return new Response<UpdateDisbursementStatusResponse>()
                                {
                                    Data = new UpdateDisbursementStatusResponse()
                                    {
                                        IsUpdated = true
                                    },
                                    IsSucceeded = true,
                                    MessageAr = "تم رفض الطلب "
                                };
                            }
                            else
                                throw new Exception("Decision not found");
                        }
                        else
                            throw new Exception("Request Status not found");
                    }
                    else if (request.DisbursementStatusCode.ToUpper() == "CNPR_RO")
                    {
                        string RequestStatusfetch = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                          <entity name='ntw_requeststatus'>
                                                            <attribute name='ntw_requeststatusid' />
                                                            <attribute name='ntw_name' />
                                                            <order attribute='ntw_name' descending='false' />
                                                            <filter type='and'>
                                                              <condition attribute='ntw_code' operator='eq' value='CNPR_RO' />
                                                            </filter>
                                                          </entity>
                                                        </fetch>";
                        EntityCollection RequestStatusResult = service.RetrieveMultiple(new FetchExpression(RequestStatusfetch));
                        if (RequestStatusResult != null && RequestStatusResult.Entities.Any())
                        {
                            Entity RequestStatus = RequestStatusResult.Entities.First();

                            string decisionfetch = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                            <entity name='ntw_decision'>
                                                            <attribute name='ntw_decisionid' />
                                                            <attribute name='ntw_name' />
                                                            <order attribute='ntw_name' descending='false' />
                                                            <filter type='and'>
                                                            <condition attribute='ntw_code' operator='eq' value='CNPR_Submit' />
                                                            </filter>
                                                            <link-entity name='ntw_routingrule' from='ntw_routingruleid' to='ntw_routingruleid' visible='false' link-type='outer' alias='routingrule'>
                                                            <attribute name='ntw_userid' />
                                                            <attribute name='ntw_typeset' />
                                                            <attribute name='ntw_teamid' />
                                                            </link-entity>
                                                            <link-entity name='ntw_requeststatus' from='ntw_requeststatusid' to='ntw_nextrequeststatusid' link-type='inner' alias='ac'>
                                                            <filter type='and'>
                                                            <condition attribute='ntw_code' operator='eq' value='CNPR_RO' />
                                                            </filter>
                                                            </link-entity>
                                                            </entity>
                                                            </fetch>";
                            EntityCollection decisionResult = service.RetrieveMultiple(new FetchExpression(decisionfetch));
                            if (decisionResult != null && decisionResult.Entities.Any())
                            {
                                Entity decisionEntity = decisionResult.Entities.First();
                                if (decisionEntity.Contains("routingrule.ntw_teamid"))
                                {
                                    AssignRequest assign = new AssignRequest
                                    {
                                        Assignee = new EntityReference("team", ((EntityReference)decisionEntity.GetAttributeValue<AliasedValue>("routingrule.ntw_teamid").Value).Id),
                                        Target = new EntityReference("ntw_createnewpaymentrequest", CreatenewpaymentrequestEntity.Id)
                                    };
                                    // Execute the Request
                                    service.Execute(assign);
                                }
                                else if (decisionEntity.Contains("routingrule.ntw_userid"))
                                {
                                    AssignRequest assign = new AssignRequest
                                    {
                                        Assignee = new EntityReference("systemuser", ((EntityReference)decisionEntity.GetAttributeValue<AliasedValue>("routingrule.ntw_userid").Value).Id),
                                        Target = new EntityReference("ntw_createnewpaymentrequest", CreatenewpaymentrequestEntity.Id)
                                    };
                                    // Execute the Request
                                    service.Execute(assign);
                                }
                            }
                            if (CreatenewpaymentrequestEntity.Contains("ntw_requeststatusid"))
                            {
                                Guid? DecisionID = GetDecisionID("CNPR_APO_NeedsInfo", (CreatenewpaymentrequestEntity.GetAttributeValue<EntityReference>("ntw_requeststatusid")).Id, service);
                                if (DecisionID != null && DecisionID.HasValue && DecisionID.Value != Guid.Empty)
                                {
                                    Entity paymentrequestEntity = new Entity("ntw_createnewpaymentrequest");
                                    paymentrequestEntity.Id = CreatenewpaymentrequestEntity.Id;
                                    paymentrequestEntity["ntw_decisionid"] = new EntityReference("ntw_decision", DecisionID.Value);

                                    service.Update(paymentrequestEntity);
                                    return new Response<UpdateDisbursementStatusResponse>()
                                    {
                                        Data = new UpdateDisbursementStatusResponse()
                                        {
                                            IsUpdated = true
                                        },
                                        IsSucceeded = true,
                                        MessageAr = "يتم استرجاع الطلب الي مسؤول العلاقة لاضافة بعض البيانات"
                                    };
                                }
                                else
                                    throw new Exception("Decision not found");
                            }
                            else
                                throw new Exception("Request Status not found");
                        }
                        else
                            throw new Exception("Request Statuses not found");
                        return new Response<UpdateDisbursementStatusResponse>()
                        {
                            Data = new UpdateDisbursementStatusResponse()
                            {
                                IsUpdated = true
                            },
                            IsSucceeded = true
                        };
                    }
                    else if (request.DisbursementStatusCode.ToUpper() == "CNPR_APPROVED")
                    {
                        if (CreatenewpaymentrequestEntity.Contains("ntw_requeststatusid"))
                        {
                            Guid? DecisionID = GetDecisionID("CNPR_APO_Approve", (CreatenewpaymentrequestEntity.GetAttributeValue<EntityReference>("ntw_requeststatusid")).Id, service);
                            if (DecisionID != null && DecisionID.HasValue && DecisionID.Value != Guid.Empty)
                            {
                                Entity paymentrequestEntity = new Entity("ntw_createnewpaymentrequest");
                                paymentrequestEntity.Id = CreatenewpaymentrequestEntity.Id;
                                paymentrequestEntity["ntw_decisionid"] = new EntityReference("ntw_decision", DecisionID.Value);
                                paymentrequestEntity["ntw_receiavableloandate"] = DateTime.ParseExact(request.DisbursementDueDate, "yyyy-MM-dd'T'HH:mm:ss", null);

                                service.Update(paymentrequestEntity);
                                return new Response<UpdateDisbursementStatusResponse>()
                                {
                                    Data = new UpdateDisbursementStatusResponse()
                                    {
                                        IsUpdated = true,

                                    },
                                    IsSucceeded = true,
                                    MessageAr = "تمت الموافقة علي الطلب "

                                };
                            }
                            else
                                throw new Exception("Decision not found");

                        }
                        else
                            throw new Exception("Request Status not found");
                    }
                    else
                    {
                        Console.WriteLine("Invalid Disbursement Status Code");
                        return new Response<UpdateDisbursementStatusResponse>()
                        {
                            Data = new UpdateDisbursementStatusResponse()
                            {
                                IsUpdated = false
                            },
                            IsSucceeded = false,
                            MessageAr = "Invalid status code",
                            MessageEn = "Invalid status code"
                        };
                    }
                }
                else
                {
                    Console.WriteLine("There is no Disbursement");
                    return new Response<UpdateDisbursementStatusResponse>()
                    {
                        Data = new UpdateDisbursementStatusResponse()
                        {
                            IsUpdated = false
                        },
                        IsSucceeded = false,
                        MessageAr = "لا يوجد طلب صرف",
                        MessageEn = "There is no Disbursement"
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw new InvalidPluginExecutionException(ex.Message);
                return new Response<UpdateDisbursementStatusResponse>()
                {
                    Data = new UpdateDisbursementStatusResponse()
                    {
                        IsUpdated = false
                    },
                    IsSucceeded = false,
                    MessageAr = ex.Message,
                    MessageEn = ex.Message
                };
            }
        }
        public Response<CreateLoanInstallmentScheduleResponse> CreateLoanInstallmentsSchedule(CreateLoanInstallmentScheduleRequest request, IOrganizationService service)
        {
            try
            {
                string LoanInfoFetch = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                          <entity name='ntw_loan'>
                                            <attribute name='ntw_partnerid' />
                                            <attribute name='ntw_familyid' />
                                            <order attribute='ntw_familyid' descending='false' />
                                            <filter type='and'>
                                              <condition attribute='ntw_waseetloanreferencenumber' operator='eq' value='" + request.LoanNumber + @"' />
                                            </filter>
                                          </entity>
                                        </fetch>";
                EntityCollection LoanInfo = service.RetrieveMultiple(new FetchExpression(LoanInfoFetch));
                if (LoanInfo != null && LoanInfo.Entities.Any())
                {
                    string Currencyfetch = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                              <entity name='transactioncurrency'>
                                                <attribute name='transactioncurrencyid' />
                                                <attribute name='isocurrencycode' />
                                                <order attribute='isocurrencycode' descending='false' />
                                                <filter type='and'>
                                                  <condition attribute='isocurrencycode' operator='eq' value='SAR' />
                                                </filter>
                                              </entity>
                                            </fetch>";
                    EntityCollection Currency = service.RetrieveMultiple(new FetchExpression(Currencyfetch));
                    Entity currencyEntity = null;
                    if (Currency != null && Currency.Entities.Any())
                        currencyEntity = Currency.Entities.FirstOrDefault();

                    OrganizationRequestCollection requests = new OrganizationRequestCollection();

                    Entity Loan = LoanInfo.Entities.First();
                    Console.WriteLine("Creating Loan Installment Schedule");
                    foreach (Installment loaninstallment in request.loanInstallments)
                    {
                        //حالة القسط غير مستحقه
                        Entity NotDueInstallmentStatus = CRMHelper.RetrieveEntity("ntw_migratedinstallmentstatus", ConditionOperator.Equal, "ntw_installmentstatusid", "6", new ColumnSet(true), service);
                        //-------------------------------
                        Entity loaninstallmentschedule = new Entity("ntw_loaninstallmentschedule");
                        loaninstallmentschedule["ntw_partnerid"] = new EntityReference("account", Loan.GetAttributeValue<EntityReference>("ntw_partnerid").Id);
                        loaninstallmentschedule["ntw_familyid"] = new EntityReference("account", Loan.GetAttributeValue<EntityReference>("ntw_familyid").Id);
                        loaninstallmentschedule["ntw_installmentno"] = loaninstallment.InstallmentNumber;
                        loaninstallmentschedule["ntw_amount"] = new Money(loaninstallment.Amount);
                        loaninstallmentschedule["ntw_paidamount"] = new Money(loaninstallment.PaidAmount);
                        loaninstallmentschedule["ntw_duedate"] = DateTime.ParseExact(loaninstallment.DueDate, "yyyy-MM-dd'T'HH:mm:ss", null);
                        //      loaninstallmentschedule["ntw_duedate"] = DateTime.ParseExact(loaninstallment.DueDate, "dd/MM/yyyy", null);
                        // loaninstallmentschedule["ntw_paymentdate"] = DateTime.ParseExact(loaninstallment.PaymentDate, "dd/MM/yyyy", null);
                        loaninstallmentschedule["ntw_saddadinvoice"] = loaninstallment.SaddadInvoice;
                        loaninstallmentschedule["ntw_loan"] = new EntityReference("ntw_loan", Loan.Id);
                        loaninstallmentschedule["ntw_installmentstatus"] = NotDueInstallmentStatus.ToEntityReference();
                        if (currencyEntity != null)
                            loaninstallmentschedule["transactioncurrencyid"] = currencyEntity.ToEntityReference();

                        //service.Create(loaninstallmentschedule);
                        requests.Add(new CreateRequest { Target = loaninstallmentschedule });
                    }

                    // Send the request
                    ExecuteTransactionResponse transactionResponse = CRMHelper.ExecuteTransactionCRM(service, requests);
                    if (transactionResponse != null && transactionResponse.GetType() == typeof(ExecuteTransactionResponse))
                    {
                        Console.WriteLine("Loan Installment Schedule created");
                        return new Response<CreateLoanInstallmentScheduleResponse>()
                        {
                            Data = new CreateLoanInstallmentScheduleResponse()
                            {
                                IsCreated = true
                            },
                            IsSucceeded = true,
                        };
                    }
                    else
                    {
                        throw new Exception("failed transaction of creating loan installment schedule");
                    }
                }
                else
                {
                    throw new Exception("Loan Record not found with waseet loan reference number : " + request.LoanNumber);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException(ex.Message);
                return new Response<CreateLoanInstallmentScheduleResponse>()
                {
                    Data = new CreateLoanInstallmentScheduleResponse()
                    { IsCreated = false },
                    IsSucceeded = false
                };
            }
        }
        
        public static void CheckAge(IOrganizationService service)
        {
            // start
            ColumnSet columnSet = new ColumnSet();
            columnSet.AddColumn("birthdate");
            columnSet.AddColumn("ntw_birthdayhijrydate");
            Entity contactProfileData = service.Retrieve("contact", new Guid("4a25366d-b372-f011-b880-00505692a571"), columnSet);

            if (contactProfileData == null)
            {
                Console.WriteLine("no profile");
            }
            //get ages
            if (contactProfileData.Contains("birthdate"))
            {
                int ageGregorian = GetGregorianAge(contactProfileData.GetAttributeValue<DateTime>("birthdate"));
                if (!(ageGregorian >= 18))
                {
                    Console.WriteLine("يجب ان لا يقل عن العميل عن 18");
                }
                else
                {
                    Console.WriteLine(ageGregorian);
                }
            }
            else if (contactProfileData.Contains("ntw_birthdayhijrydate"))
            {
                int ageHijri = GetHijriAge(contactProfileData.GetAttributeValue<string>("ntw_birthdayhijrydate"));
                if (!(ageHijri >= 18))
                {
                    Console.WriteLine("يجب ان لا يقل عن العميل عن 18");

                }
                else
                {
                    Console.WriteLine(ageHijri);
                }
            }
            else
            {
                Console.WriteLine("لا يوجد تاريخ ميلاد على ملف العميل");
            }

            //end
        }
        private static int GetGregorianAge(DateTime birthDate)
        {
            DateTime today = DateTime.Today;
            int age = today.Year - birthDate.Year;

            // Adjust age if the birthday hasn’t occurred yet this year
            if (birthDate.Date > today.AddYears(-age))
            {
                age--;
            }

            return age;
        }
        private static int GetHijriAge(string birthdateHijri)
        {
            DateTime convertedBirthdate = ConvertHijriToGregorian(birthdateHijri);
            int hijriAge = GetGregorianAge(convertedBirthdate);
            return hijriAge;
        }
        // Use Umm al-Qura calendar (official Saudi Hijri calendar)
        private static readonly UmAlQuraCalendar _umAlQuraCalendar = new UmAlQuraCalendar();

        /// <summary>
        /// Converts a Hijri date (in dd-MM-yyyy or yyyy-MM-dd) to Gregorian (Umm al-Qura-based).
        /// </summary>
        public static DateTime ConvertHijriToGregorian(string hijriDate)
        {
            if (string.IsNullOrWhiteSpace(hijriDate))
                return DateTime.Now;

            // Normalize delimiters to '-' for consistency
            hijriDate = hijriDate.Replace('/', '-').Replace('\\', '-').Trim();

            // Supported formats
            string[] supportedFormats =
            {
                "dd-MM-yyyy",
                "yyyy-MM-dd"
             };

            var culture = new CultureInfo("ar-SA")
            {
                DateTimeFormat = { Calendar = _umAlQuraCalendar }
            };

            // Try parsing using supported formats
            foreach (var format in supportedFormats)
            {
                if (DateTime.TryParseExact(hijriDate, format, culture,
                    DateTimeStyles.None, out DateTime hijriParsed))
                {
                    // Return as Gregorian DateTime
                    return hijriParsed;
                }
            }

            // If parsing fails, return null
            return DateTime.Now;
        }

        //migrate cases

        public static void MigrateCases(IOrganizationService service, string statusId, string reasonId)
        {
            var fetchXml = $@"<?xml version='1.0' encoding='utf-16'?>
                    <fetch >
                      <entity name='incident'>
                        <attribute name='title' />
                        <attribute name='ticketnumber' />
                        <attribute name='createdon' />
                        <attribute name='incidentid' />
                        <attribute name='caseorigincode' />
                        <attribute name='ntw_loannumber' />
                        <order attribute='title' descending='false' />
                        <filter type='and'>
                          <condition attribute='statecode' operator='eq' value='0' />
                          <condition attribute='sdb_casestatus' operator='null' />
                          <condition attribute='sdb_proccessinstance' operator='null' />
                          <condition attribute='sdb_generalproductclassification' operator='not-null' />
                          <condition attribute='sdb_subproduct' operator='not-null' />
                        </filter>
                      </entity>
                    </fetch>";
            int count =0;
            // Paging setup
            int pageNumber = 1;
            string pagingCookie = null;  // keep track for next page

            EntityCollection results;
            do
            {
                string pagedFetch = string.Format(fetchXml,
                    pagingCookie,
                    pageNumber,
                    50 // Page size
                );

                FetchExpression fetchQuery = new FetchExpression(pagedFetch);
                results = service.RetrieveMultiple(fetchQuery);

                if (results?.Entities?.Count > 0)
                {
                   Console.WriteLine("Count is " + results?.Entities?.Count);

                    foreach (var item in results?.Entities)
                    {
                        count++;
                        // item.Id = new Guid("6b6c4689-140b-f111-8c40-005056923aa9");
                       Console.WriteLine("Start ChangeCaseBpf for  " + item.Id.ToString());
                        ChangeCaseBpf(service, item.Id);
                       Console.WriteLine("start MapCaseFields for  " + item.Id.ToString());
                        MapCaseFields(service, item, statusId, reasonId);
                       Console.WriteLine("start AssignCaseToTeam for  " + item.Id.ToString());
                        AssignCaseToTeam(service, item.Id, new Guid("0171a755-563e-f111-8c47-005056923aa9"));
                        Console.WriteLine(count);
                    }
                }

                if (results.MoreRecords)
                {
                    pageNumber++;
                    pagingCookie = results.PagingCookie;
                }
                else
                {
                    break;
                }

            } while (true);

        }

        public static void ChangeCaseBpf(IOrganizationService service, Guid caseId)
        { // 1. Deactivate OLD BPF instance
            string oldBpfEntity = "new_bpf_7013d7c4826745e182fabb09ef464219";
            QueryExpression qOld = new QueryExpression(oldBpfEntity);
            qOld.ColumnSet = new ColumnSet("businessprocessflowinstanceid");
            qOld.Criteria.AddCondition("bpf_incidentid", ConditionOperator.Equal, caseId);

            var oldInstance = service.RetrieveMultiple(qOld).Entities.FirstOrDefault();
            if (oldInstance != null)
                service.Delete(oldBpfEntity, oldInstance.Id);

            // Your new BPF workflowid
            Guid newProcessId = new Guid("89C6F0B6-B991-4FA3-8235-5A1A152524A5");

            // 1. Switch the BPF using SetProcess (same as CRM UI)
            var req = new SetProcessRequest
            {
                Target = new EntityReference("incident", caseId),
                NewProcess = new EntityReference("workflow", newProcessId)
            };

            service.Execute(req);

            // 2. Retrieve the new BPF instance created by CRM
            // BPF entity: sdb_bpfcasecustomerservicebusinessproccessflow
            // Primary key: businessprocessflowinstanceid
            QueryExpression qe = new QueryExpression("sdb_bpfcasecustomerservicebusinessproccessflow");
            qe.ColumnSet = new ColumnSet("businessprocessflowinstanceid");
            qe.Criteria.AddCondition("bpf_incidentid", ConditionOperator.Equal, caseId);

            Entity newBpfInstance = service.RetrieveMultiple(qe).Entities.FirstOrDefault();

            if (newBpfInstance == null)
                throw new InvalidPluginExecutionException("New BPF instance was not created.");

            Guid bpfInstanceId = newBpfInstance.Id;

            // 3. Update your custom lookup field on Case
            Entity updateCase = new Entity("incident", caseId);
            updateCase["sdb_proccessinstance"] = new EntityReference(
                "sdb_bpfcasecustomerservicebusinessproccessflow",
                bpfInstanceId
            );

            service.Update(updateCase);
        }


        public static void AssignCaseToTeam(IOrganizationService service, Guid caseId, Guid teamId)
        {
            var assignRequest = new AssignRequest
            {
                Assignee = new EntityReference("team", teamId),
                Target = new EntityReference("incident", caseId)
            };

            service.Execute(assignRequest);
        }

        public static void MapCaseFields(IOrganizationService service, Entity Case, string caseStatusId, string reasonID)
        {
            Entity updatecase = new Entity(Case.LogicalName, Case.Id);
            updatecase["sdb_casestatus"] = new EntityReference("ntw_requeststatus", new Guid(caseStatusId));
            updatecase["sdb_ticketreason"] = new EntityReference("sdb_ticketreason", new Guid(reasonID));

            if (Case != null && !Case.Contains("ntw_loannumber"))
            {
                //Not finanical
                updatecase["sdb_tcketclassificationscope"] = new OptionSetValue(1);
            }
            else if (Case.Contains("ntw_loannumber"))
            {
                updatecase["sdb_tcketclassificationscope"] = new OptionSetValue(0);
            }
            service.Update(updatecase);
        }

        public static string GetStatusFromCode(string statusCode, IOrganizationService service)
        {
            string fetchXml = $@"
                            <fetch>
                              <entity name='ntw_requeststatus'>
                                <attribute name='ntw_requeststatusid'/>
                                    <filter type='and'>
                                  <condition attribute='ntw_code' operator='eq' value='{statusCode}' />
                                </filter>
                              </entity>
                            </fetch>";
            EntityCollection resultcol = service.RetrieveMultiple(new FetchExpression(fetchXml));

            if (resultcol?.Entities?.Count < 1)
                return null;

            return resultcol.Entities.FirstOrDefault().Id.ToString();
        }

        public static string GetTicketReasonByCode(string Code, IOrganizationService service)
        {
            string fetchXml = $@"
                            <fetch>
                              <entity name='sdb_ticketreason'>
                                <attribute name='sdb_ticketreasonid'/>
                                    <filter type='and'>
                                  <condition attribute='sdb_key' operator='eq' value='{Code}' />
                                </filter>
                              </entity>
                            </fetch>";
            EntityCollection resultcol = service.RetrieveMultiple(new FetchExpression(fetchXml));

            if (resultcol?.Entities?.Count < 1)
                return null;

            return resultcol.Entities.FirstOrDefault().Id.ToString();
        }

        public static void CopyTeamMembers(IOrganizationService service, Guid sourceTeamId, Guid targetTeamId)
        {
            //if (IsDefaultBusinessUnitTeam(service, sourceTeamId))
            //    throw new InvalidOperationException("Source team is the default Business Unit team.");

            //if (IsDefaultBusinessUnitTeam(service, targetTeamId))
            //    throw new InvalidOperationException("Target team is the default Business Unit team.");

            var users = GetTeamMembers(service, sourceTeamId);
            int count = 0;
            Console.WriteLine($"Found {users.Count} active users in source team.");

            foreach (var userId in users)
            {
                try
                {
                    count++;
                    var req = new AssociateRequest
                    {
                        Target = new EntityReference("team", targetTeamId),
                        RelatedEntities = new EntityReferenceCollection
                        {
                            new EntityReference("systemuser", userId)
                        },
                        Relationship = new Relationship("teammembership_association")
                    };

                    service.Execute(req);
                    Console.WriteLine($"Added user {userId}  to target team. {count}");

                }
                catch (FaultException<OrganizationServiceFault> ex)
                {
                    Console.WriteLine($"Failed to add user {userId}: {ex.Message}");
                }
            }
        }

        public static List<Guid> GetTeamMembers(IOrganizationService service, Guid teamId)

        {

            var fetch = $@"
<fetch>
<entity name='systemuser'>
<attribute name='systemuserid' />
<filter>
<condition attribute='isdisabled' operator='eq' value='0' />
</filter>
<link-entity name='teammembership' from='systemuserid' to='systemuserid' intersect='true'>
<filter>
<condition attribute='teamid' operator='eq' value='{teamId}' />
</filter>
</link-entity>
</entity>
</fetch>";

            var result = service.RetrieveMultiple(new FetchExpression(fetch));

            return result.Entities.Select(e => e.Id).ToList();

        }

        public static async Task<List<string>> GetAgreementActiveList(IOrganizationService service, Guid contactId)
        {
            var queryWatch = Stopwatch.StartNew();
            var list = new List<string>();

            // Set Condition Values
            var query_statuscode = 1;

            // Instantiate QueryExpression query
            var query = new QueryExpression("los_ucpagreementauthorization");
            query.ColumnSet = new ColumnSet("statecode");

            // Add conditions to query.Criteria
            query.Criteria.AddCondition("statecode", ConditionOperator.Equal, query_statuscode);

            // Add link-entity relation
            var relation = query.AddLink("los_customerrelatedentity", "los_customerrelationtypeid", "los_customerrelatedentityid");
            relation.EntityAlias = "relation";


            // Add conditions to relation.LinkCriteria
            relation.LinkCriteria.AddCondition("los_firstpartyname", ConditionOperator.Equal, contactId);

            // Add link-entity permission
            var permissionLink = query.AddLink("los_ucpauthorizationroles", "los_ucpauthorizationroleid", "los_ucpauthorizationrolesid");
            permissionLink.EntityAlias = "permission";

            // Add columns to permission.Columns
            permissionLink.Columns.AddColumns("los_ucpauthorizationrolesid", "los_name", "los_code");
            queryWatch.Stop();
            Console.WriteLine($"query watch took {queryWatch.ElapsedMilliseconds} MS");
            var crmWatch = Stopwatch.StartNew();
            var entities = service.RetrieveMultiple(query);
            crmWatch.Stop();
            Console.WriteLine($"crm watch took {crmWatch.ElapsedMilliseconds} MS");
            var loopingWatch = Stopwatch.StartNew();

            foreach (var item in entities.Entities)
            {
                var name = (string)item.GetAttributeValue<AliasedValue>("permission." + "los_name").Value;

                list.Add(name);

            }
            loopingWatch.Stop();
            Console.WriteLine($"loopingWatch took {loopingWatch.ElapsedMilliseconds} MS");
            return list;
        }

        public static void AddRepresenativeConfigurations(IOrganizationService service, string accountId)
        {
            //get list of representatives contacts and partners accounts
            string accountsFetch = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                  <entity name='account'>
                                    <attribute name='name' />
                                    <attribute name='telephone1' />
                                    <attribute name='ntw_contact' />
                                    <attribute name='ntw_nationalid' />
                                    <attribute name='ntw_city' />
                                    <attribute name='createdon' />
                                    <attribute name='ntw_contact' />
                                    <attribute name='accountid' />
                                    <order attribute='name' descending='false' />
                                    <filter type='and'>
                                      <condition attribute='ntw_accounttype' operator='eq' value='1' />
                                      <condition attribute='accountid' operator='eq' uitype='account' value='{"+accountId+@"}' />
                                      <condition attribute='statuscode' operator='eq' value='1' />
                                      <condition attribute='ntw_contact' operator='not-null' />
                                    </filter>
                                  </entity>
                                </fetch>";
            var accounts = service.RetrieveMultiple(new FetchExpression(accountsFetch));


            foreach (var account in accounts.Entities)
            {
                //create Customer Related Entity
                Entity customerRelatedEntity = new Entity("los_customerrelatedentity");

                //retrieve partner type Indv
                QueryExpression partnerTypeIndvQuery = new QueryExpression("ntw_partnertype");
                partnerTypeIndvQuery.Criteria.AddCondition("ntw_code", ConditionOperator.Equal, "001");
                partnerTypeIndvQuery.ColumnSet.AddColumn("ntw_partnertypeid");
                var partnerTypeIndv = service.RetrieveMultiple(partnerTypeIndvQuery);

                customerRelatedEntity["los_firstpartytype"] = new EntityReference("ntw_partnertype", partnerTypeIndv[0].Id);

                customerRelatedEntity["los_firstpartyname"] = account.Attributes["ntw_contact"];

                //retrieve partner type Corp
                QueryExpression partnerTypeCorpQuery = new QueryExpression("ntw_partnertype");
                partnerTypeCorpQuery.Criteria.AddCondition("ntw_code", ConditionOperator.Equal, "003");
                partnerTypeCorpQuery.ColumnSet.AddColumn("ntw_partnertypeid");
                var partnerTypeCorp = service.RetrieveMultiple(partnerTypeCorpQuery);

                customerRelatedEntity["los_secondpartytype"] = new EntityReference("ntw_partnertype", partnerTypeCorp[0].Id);

                customerRelatedEntity["los_secondpartyname"] = new EntityReference("account", account.Id);

                //retrieve type of relationship
                QueryExpression typeOfRelationshipQuery = new QueryExpression("los_typesofrelationship");
                typeOfRelationshipQuery.Criteria.AddCondition("los_code", ConditionOperator.Equal, "36");
                typeOfRelationshipQuery.ColumnSet.AddColumn("los_typesofrelationshipid");
                var typeOfRelationship = service.RetrieveMultiple(typeOfRelationshipQuery);

                customerRelatedEntity["los_relationtype"] = new EntityReference("los_typesofrelationship", typeOfRelationship[0].Id);

                Guid customerRelatedEntityId = service.Create(customerRelatedEntity);


                //create UCP Corporate Profile Agreement
                Entity corporateProfileAgreement = new Entity("los_ucpcorporateprofileagreement");

                corporateProfileAgreement["los_accountid"] = new EntityReference("account", account.Id);

                //retrieve UCP Agreement Type
                QueryExpression ucpagreementtypeQuery = new QueryExpression("los_ucpagreementtype");
                ucpagreementtypeQuery.Criteria.AddCondition("los_code", ConditionOperator.Equal, "88888");
                ucpagreementtypeQuery.ColumnSet.AddColumn("los_ucpagreementtypeid");
                var ucpagreementtype = service.RetrieveMultiple(ucpagreementtypeQuery);

                corporateProfileAgreement["los_ucpagreementtypeid"] = new EntityReference("los_ucpagreementtype", ucpagreementtype[0].Id);

                Guid corporateProfileAgreementId = service.Create(corporateProfileAgreement);

                //create UCP Agreement Authorization with new terms and condititons / UCP Type : Self
                Entity agreementAuthorization = new Entity("los_ucpagreementauthorization");

                agreementAuthorization["los_customerrelationtypeid"] = new EntityReference("los_customerrelatedentity", customerRelatedEntityId);

                //retrieve UCP Authorization Roles
                QueryExpression ucpauthorizationrolesQuery = new QueryExpression("los_ucpauthorizationroles");
                ucpauthorizationrolesQuery.Criteria.AddCondition("los_code", ConditionOperator.Equal, "9999");
                ucpauthorizationrolesQuery.ColumnSet.AddColumn("los_ucpauthorizationrolesid");
                var ucpauthorizationroles = service.RetrieveMultiple(ucpauthorizationrolesQuery);

                agreementAuthorization["los_ucpauthorizationroleid"] = new EntityReference("los_ucpauthorizationroles", ucpauthorizationroles[0].Id);

                agreementAuthorization["los_ucpcorporateprofileagreementid"] = new EntityReference("los_ucpcorporateprofileagreement", corporateProfileAgreementId);
                agreementAuthorization["sdb_termsandcondition"] = true;
                //test
                //retrieve sdb_agreementterm
                QueryExpression agreementtermQuery = new QueryExpression("sdb_agreementterm");
                agreementtermQuery.Criteria.AddCondition("sdb_name", ConditionOperator.Equal, "V0001");
                agreementtermQuery.ColumnSet.AddColumn("sdb_agreementtermid");
                var agreementterm = service.RetrieveMultiple(agreementtermQuery);
                agreementAuthorization["sdb_articleoftermsandcondition"] = new EntityReference("sdb_agreementterm", agreementterm[0].Id);


                Guid agreementAuthorizationId = service.Create(agreementAuthorization);
                Console.WriteLine(agreementAuthorizationId.ToString());
            }
        }
        public static void EnableDisableUsers(IOrganizationService service, bool enable)
        {
            //get emails
            var usersEmails = GetIdsFromExcel("Ids.xlsx");
            string fetchEmailsForUsers = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                  <entity name='systemuser'>
                                                    <attribute name='fullname' />
                                                    <attribute name='title' />
                                                    <attribute name='address1_telephone1' />
                                                    <attribute name='businessunitid' />
                                                    <attribute name='siteid' />
                                                    <attribute name='positionid' />
                                                    <attribute name='domainname' />
                                                    <attribute name='systemuserid' />
                                                    <order attribute='fullname' descending='false' />
                                                    <filter type='and'>
                                                        <filter type='and'>
                                                                <condition attribute='isdisabled' operator='eq' value='1' />
                                                                    <filter type='or'>";
            foreach (var email in usersEmails[0])
            {
                fetchEmailsForUsers += @"<condition attribute='internalemailaddress' operator='eq' value='" + email + @"' />";

            }
            fetchEmailsForUsers += @"</filter>
                                    </filter>
                                    </filter>
                                    </entity>
                                </fetch>";
            var Users = service.RetrieveMultiple(new FetchExpression(fetchEmailsForUsers));

            if (enable == true)
            {
                //Enable Users
                foreach (var user in Users.Entities)
                {
                    Console.WriteLine(user.GetAttributeValue<string>("domainname"));
                    var requestToDisableUser = new SetStateRequest()
                    {
                        EntityMoniker = new EntityReference("systemuser", user.Id),
                        State = new OptionSetValue(0),
                        Status = new OptionSetValue(-1)
                    };
                    service.Execute(requestToDisableUser);
                }
            }
            else
            {
                //Disable Users
                foreach (var user in Users.Entities)
                {
                    Console.WriteLine(user.GetAttributeValue<string>("domainname"));
                    var requestToDisableUser = new SetStateRequest()
                    {
                        EntityMoniker = new EntityReference("systemuser", user.Id),
                        State = new OptionSetValue(1),
                        Status = new OptionSetValue(-1)
                    };
                    service.Execute(requestToDisableUser);
                }
            }
                
            
            

        }

        public static void AddTicketReasons(IOrganizationService service, Guid[] ticketClassificationIds)
        {
            //loop on classifications
            foreach (var ticketClassificationId in ticketClassificationIds)
            {
                //get sub categories
                QueryExpression subCategoriesQuery = new QueryExpression("sdb_ticketclassification");
                //subCategoriesQuery.Criteria.AddCondition("sdb_parentcalssification", ConditionOperator.Equal, ticketClassificationId);
                //subCategoriesQuery.Criteria.AddCondition("sdb_ticketclassificationid", ConditionOperator.NotEqual, new Guid("e73a25cc-585a-f011-8c2f-005056923aa9"));//siola
                //subCategoriesQuery.Criteria.AddCondition("sdb_ticketclassificationid", ConditionOperator.NotEqual, new Guid("d53a25cc-585a-f011-8c2f-005056923aa9"));//tmaioz
                //subCategoriesQuery.Criteria.AddCondition("sdb_ticketclassificationid", ConditionOperator.NotEqual, new Guid("e43a25cc-585a-f011-8c2f-005056923aa9"));//tmaioz
                //subCategoriesQuery.Criteria.AddCondition("sdb_ticketclassificationid", ConditionOperator.NotEqual, new Guid("d83a25cc-585a-f011-8c2f-005056923aa9"));//rowad
                subCategoriesQuery.Criteria.AddCondition("sdb_ticketclassificationid", ConditionOperator.Equal, new Guid("d7c0b5b1-6591-f111-b8a5-00505692a571"));

                var subCategories = service.RetrieveMultiple(subCategoriesQuery);
                var category = subCategories.Entities[0];
                //create bulk reasons on sub categories
                //foreach (var category in subCategories.Entities)
                //{
                string[] reasons = {
                "تعويضات التأمين",
                "إصدار وثيقة التأمين",
                "إصلاح المركبة",
                "المخالفات المرورية",
                "النسخة الإحتياطية للمفتاح",
                "تجديد رخصة السير",
                "تسليم السيارة",
                "مخالصة ونقل الملكية",
                "تغيير لوحة المركبة",
                "تفويض السيارات (دولي /داخلي)"
                };
                foreach (var reason in reasons)
                {
                    Entity createdTicketReason = new Entity("sdb_ticketreason");
                    createdTicketReason.Attributes["sdb_name"] = reason;
                    createdTicketReason.Attributes["sdb_englishname"] = reason;
                    createdTicketReason.Attributes["sdb_exceptionalclassification"] = false;
                    createdTicketReason.Attributes["sdb_isexternal"] = true;
                    createdTicketReason.Attributes["sdb_isinternal"] = true;
                    IList<OptionSetValue> ticketTypes = new List<OptionSetValue>() { new OptionSetValue(0), new OptionSetValue(1), new OptionSetValue(2), new OptionSetValue(3) };
                    createdTicketReason.Attributes["sdb_tickettypes"] = new OptionSetValueCollection(ticketTypes);
                    createdTicketReason.Attributes["sdb_allowreopen"] = true;
                    createdTicketReason.Attributes["sdb_priority"] = new OptionSetValue(2);
                    createdTicketReason.Attributes["sdb_assignto"] = new OptionSetValue(0);
                    createdTicketReason.Attributes["sdb_team"] = new EntityReference("team", new Guid("0171a755-563e-f111-8c47-005056923aa9"));
                    createdTicketReason.Attributes["sdb_nextstage"] = new EntityReference("processstage", new Guid("8de10b9d-4c60-4366-8d0f-8627f3543a60"));
                    createdTicketReason.Attributes["sdb_nextrequeststatus"] = new EntityReference("ntw_requeststatus", new Guid("c80fe48c-d0f5-f011-b8b3-005056a53d44"));
                    createdTicketReason.Attributes["sdb_ticketclassification"] = new EntityReference("sdb_ticketclassification", category.Id);
                    var newReason = service.Create(createdTicketReason);
                    Console.WriteLine("new created reason " + newReason.ToString() + "for category : " + category.Id);
                    Entity addtFieldConfig = new Entity("sdb_additionalfieldconfiguration");
                    addtFieldConfig.Attributes["sdb_requestentity"] = new EntityReference("ntw_requestentities", new Guid("5988d761-54f5-f011-b8b3-005056a53d44"));
                    addtFieldConfig.Attributes["sdb_relatedcasereason"] = new EntityReference("sdb_ticketreason", newReason);
                    addtFieldConfig.Attributes["sdb_name"] = "مرفقات إضافية";
                    addtFieldConfig.Attributes["sdb_nameen"] = "Optional Attachments";
                    addtFieldConfig.Attributes["sdb_fielddatatype"] = new OptionSetValue(13);
                    IList<OptionSetValue> filetypes = new List<OptionSetValue>() { new OptionSetValue(1), new OptionSetValue(2), new OptionSetValue(3), new OptionSetValue(11), new OptionSetValue(12), new OptionSetValue(13) };
                    addtFieldConfig.Attributes["sdb_allowedfileextensions"] = new OptionSetValueCollection(filetypes);
                    service.Create(addtFieldConfig);
                    Console.WriteLine(" reason updated " + newReason.ToString());
                }
                   

                //}
            }
        }

        public static void UpdateTicketReasons(IOrganizationService service)
        {
           
            //get reasons
            QueryExpression reasonQuery = new QueryExpression("sdb_ticketreason");
            reasonQuery.ColumnSet.AddColumn("createdon");
            reasonQuery.Criteria.AddCondition("createdon", ConditionOperator.LastXDays,1);
            var reasons = service.RetrieveMultiple(reasonQuery);

            //update bulk reasons
            foreach (var reason in reasons.Entities)
            {
                IList<OptionSetValue> ticketTypes = new List<OptionSetValue>() { new OptionSetValue(2) };
                reason.Attributes["sdb_tickettypes"] = new OptionSetValueCollection(ticketTypes);
                    
                service.Update(reason);
                Console.WriteLine(" reason updated " + reason.ToString());
            }
        }
        public static void AddAdditionalFieldConfiguration(IOrganizationService service)
        {
            //get all reasons
            //get reasons
            QueryExpression reasonQuery = new QueryExpression("sdb_ticketreason");
            reasonQuery.ColumnSet.AddColumn("createdon");
            reasonQuery.Criteria.AddCondition("createdon", ConditionOperator.LastXDays, 1);
            var reasons = service.RetrieveMultiple(reasonQuery);

            //create field config for each reason
         
            foreach (var reason in reasons.Entities)
            {
                Entity addtFieldConfig = new Entity("sdb_additionalfieldconfiguration");
                addtFieldConfig.Attributes["sdb_requestentity"] = new EntityReference("ntw_requestentities", new Guid("5988d761-54f5-f011-b8b3-005056a53d44"));
                addtFieldConfig.Attributes["sdb_relatedcasereason"] = new EntityReference("sdb_ticketreason", reason.Id);
                addtFieldConfig.Attributes["sdb_name"] = "مرفقات إضافية";
                addtFieldConfig.Attributes["sdb_nameen"] = "Optional Attachments";
                addtFieldConfig.Attributes["sdb_fielddatatype"] = new OptionSetValue(13);
                IList<OptionSetValue> filetypes = new List<OptionSetValue>() { new OptionSetValue(1), new OptionSetValue(2), new OptionSetValue(3), new OptionSetValue(11), new OptionSetValue(12), new OptionSetValue(13)};
                addtFieldConfig.Attributes["sdb_allowedfileextensions"] = new OptionSetValueCollection(filetypes);
                service.Create(addtFieldConfig);
                Console.WriteLine(" reason updated " + reason.ToString());
            }
        }
        public static void AddSubProductsAndAssociateReasons(IOrganizationService service)
        {
            //get data from excel
            var productsInfo = GetIdsFromExcel("Ids.xlsx");
            var subProductNames = productsInfo[0];
            var parentProductIds = productsInfo[1];
            var subProductCodes = productsInfo[3];
            var ticketReasons = productsInfo[6];
            var requiredAttachments = productsInfo[7];
            var reasonVisiblility = productsInfo[9];
         
            var distinctProductNames = subProductNames.Distinct();
        
            foreach (var subProduct in distinctProductNames)
            {
                int indexOfSubProduct = productsInfo[0].IndexOf(subProduct);
                Console.WriteLine(subProduct + " " +indexOfSubProduct);
                Console.WriteLine( "Parent Id " + parentProductIds[indexOfSubProduct]);
                Console.WriteLine("sub Product Code " + subProductCodes[indexOfSubProduct]);

                //create subproduct
                Entity newSubProduct = new Entity("sdb_ticketclassification");
                newSubProduct.Attributes["sdb_name"] = subProduct;
                newSubProduct.Attributes["sdb_englishname"] = subProduct;
                newSubProduct.Attributes["sdb_waseetcode"] = subProductCodes[indexOfSubProduct];
                newSubProduct.Attributes["sdb_scope"] = new OptionSetValue(0);
                newSubProduct.Attributes["sdb_assignto"] = new OptionSetValue(0);
                newSubProduct.Attributes["sdb_team"] = new EntityReference("team", new Guid("d67790b7-3983-ec11-b817-00505692b4e1"));
                IList<OptionSetValue> channels = new List<OptionSetValue>() { new OptionSetValue(0), new OptionSetValue(1), new OptionSetValue(2), new OptionSetValue(3) };
                newSubProduct.Attributes["sdb_visibleinui"] = new OptionSetValueCollection(channels);
                IList<OptionSetValue> ticketTypes = new List<OptionSetValue>() { new OptionSetValue(0), new OptionSetValue(1), new OptionSetValue(2), new OptionSetValue(3) };
                newSubProduct.Attributes["sdb_tickettype"] = new OptionSetValueCollection(ticketTypes);
                newSubProduct.Attributes["sdb_parentcalssification"] = new EntityReference("sdb_ticketclassification", new Guid(parentProductIds[indexOfSubProduct]));
                
                Guid newSubProdId = service.Create(newSubProduct);

                //create reasons for specific subProduct code associated to the new subProductCodeId 
                int count = 1;
                for (int i = 0; i < ticketReasons.Count; i++)
                {
                    if (subProductCodes[i] == subProductCodes[indexOfSubProduct])
                    {
                        Console.WriteLine("subProductCodes " + count);
                        count++;
                        //create ticket reason
                        Entity createdTicketReason = new Entity("sdb_ticketreason");
                        createdTicketReason.Attributes["sdb_name"] = ticketReasons[i];
                        createdTicketReason.Attributes["sdb_englishname"] = ticketReasons[i];
                        createdTicketReason.Attributes["sdb_exceptionalclassification"] = false;
                        //check notes for visiblity
                        if (reasonVisiblility[i] != "")
                            createdTicketReason.Attributes["sdb_isexternal"] = false;
                        else
                            createdTicketReason.Attributes["sdb_isexternal"] = true;
                        createdTicketReason.Attributes["sdb_isinternal"] = true;
                        IList<OptionSetValue> types = new List<OptionSetValue>() { new OptionSetValue(0), new OptionSetValue(1), new OptionSetValue(2), new OptionSetValue(3) };
                        createdTicketReason.Attributes["sdb_tickettypes"] = new OptionSetValueCollection(ticketTypes);
                        createdTicketReason.Attributes["sdb_allowreopen"] = true;
                        createdTicketReason.Attributes["sdb_priority"] = new OptionSetValue(2);
                        createdTicketReason.Attributes["sdb_assignto"] = new OptionSetValue(0);
                        createdTicketReason.Attributes["sdb_team"] = new EntityReference("team", new Guid("0171a755-563e-f111-8c47-005056923aa9"));
                        createdTicketReason.Attributes["sdb_nextstage"] = new EntityReference("processstage", new Guid("8de10b9d-4c60-4366-8d0f-8627f3543a60"));
                        createdTicketReason.Attributes["sdb_nextrequeststatus"] = new EntityReference("ntw_requeststatus", new Guid("c80fe48c-d0f5-f011-b8b3-005056a53d44"));
                        createdTicketReason.Attributes["sdb_ticketclassification"] = new EntityReference("sdb_ticketclassification", newSubProdId);
                        var newReason = service.Create(createdTicketReason);
                        Console.WriteLine("new created reason " + newReason.ToString() + " for category : " + newSubProdId);
                        //add attachements
                        Entity addtFieldConfig = new Entity("sdb_additionalfieldconfiguration");
                        addtFieldConfig.Attributes["sdb_requestentity"] = new EntityReference("ntw_requestentities", new Guid("5988d761-54f5-f011-b8b3-005056a53d44"));
                        addtFieldConfig.Attributes["sdb_relatedcasereason"] = new EntityReference("sdb_ticketreason", newReason);
                        addtFieldConfig.Attributes["sdb_name"] = "مرفقات إضافية";
                        addtFieldConfig.Attributes["sdb_nameen"] = "Optional Attachments";
                        addtFieldConfig.Attributes["sdb_fielddatatype"] = new OptionSetValue(13);
                        IList<OptionSetValue> filetypes = new List<OptionSetValue>() { new OptionSetValue(1), new OptionSetValue(2), new OptionSetValue(3), new OptionSetValue(11), new OptionSetValue(12), new OptionSetValue(13) };
                        addtFieldConfig.Attributes["sdb_allowedfileextensions"] = new OptionSetValueCollection(filetypes);
                        Guid newaddtFieldConfig = service.Create(addtFieldConfig);
                        if (requiredAttachments[i] != "")
                        {   
                            Entity requiredFieldConfig = new Entity("sdb_additionalfieldconfiguration");
                            requiredFieldConfig.Attributes["sdb_requestentity"] = new EntityReference("ntw_requestentities", new Guid("5988d761-54f5-f011-b8b3-005056a53d44"));
                            requiredFieldConfig.Attributes["sdb_relatedcasereason"] = new EntityReference("sdb_ticketreason", newReason);
                            requiredFieldConfig.Attributes["sdb_name"] = requiredAttachments[i];
                            requiredFieldConfig.Attributes["sdb_nameen"] = requiredAttachments[i];
                            requiredFieldConfig.Attributes["sdb_fielddatatype"] = new OptionSetValue(13);
                            IList<OptionSetValue> reqFileTypes = new List<OptionSetValue>() { new OptionSetValue(1), new OptionSetValue(2), new OptionSetValue(3), new OptionSetValue(11), new OptionSetValue(12), new OptionSetValue(13) };
                            requiredFieldConfig.Attributes["sdb_allowedfileextensions"] = new OptionSetValueCollection(reqFileTypes);
                            Guid newrequiredFieldConfig =  service.Create(requiredFieldConfig);
                        }
                        Console.WriteLine(" reason updated " + newReason.ToString());
                    }

                }

            }

        }
        public static void UpdateProcessesOwner(IOrganizationService service)
        {
            string fetchProcesses = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                      <entity name='workflow'>
                                        <attribute name='workflowid' />
                                        <attribute name='name' />
                                        <attribute name='category' />
                                        <attribute name='primaryentity' />
                                        <attribute name='statecode' />
                                        <attribute name='createdon' />
                                        <attribute name='modifiedon' />
                                        <attribute name='type' />
                                        <attribute name='ownerid' />
                                        <attribute name='statecode' />
                                        <order attribute='name' descending='false' />
                                        <filter type='and'>s
                                          <condition attribute='type' operator='eq' value='1' />
                                          <condition attribute='ownerid' operator='not-in'>
                                            <value uiname='CRM System' uitype='systemuser'>{2F088080-4A80-EC11-B814-00505692B4E1}</value>
                                            <value uiname='SRV-CRM DevOps' uitype='systemuser'>{1430C7DA-3260-F011-B87F-00505692A571}</value>
                                          </condition>
                                          <condition attribute='rendererobjecttypecode' operator='null' />
                                          <filter type='or'>
                                            <condition attribute='category' operator='eq' value='0' />
                                            <filter type='and'>
                                              <condition attribute='category' operator='eq' value='1' />
                                              <condition attribute='languagecode' operator='eq-userlanguage' />
                                            </filter>
                                            <condition attribute='category' operator='eq' value='3' />
                                            <condition attribute='category' operator='eq' value='4' />
                                          </filter>
                                        </filter>
                                      </entity>
                                    </fetch>"; 
            var processes = service.RetrieveMultiple(new FetchExpression(fetchProcesses));
            foreach (var process in processes.Entities)
            {
                //if workflow active
                var status = (OptionSetValue)process.Attributes["statecode"];
                var statusvalue = new OptionSetValue(1);
                if (status.Value == statusvalue.Value)
                {
                    //process.Attributes["statecode"] = new OptionSetValue(0);
                    //process.Attributes["statuscode"] = new OptionSetValue(1);
                    //service.Update(process);
                    service.Execute(new SetStateRequest
                    {
                        EntityMoniker = new EntityReference("workflow", process.Id),
                        State = new OptionSetValue(0), //Status
                        Status = new OptionSetValue(1) //Status reason
                    });

                    process.Attributes["ownerid"] = new EntityReference("systemuser", new Guid("2f088080-4a80-ec11-b814-00505692b4e1"));
                    service.Update(process);
                    service.Execute(new SetStateRequest
                    {
                        EntityMoniker = new EntityReference("workflow", process.Id),
                        State = new OptionSetValue(1), //Status
                        Status = new OptionSetValue(2) //Status reason
                    });
                }
                else
                {
                    process.Attributes["ownerid"] = new EntityReference("systemuser", new Guid("2f088080-4a80-ec11-b814-00505692b4e1"));
                    service.Update(process);
                }
                
                Console.WriteLine(process.Id);
            }

        }

        //license renew
        public static void RenewLicenseWithActiveLoan(IOrganizationService _service)
        {
            Console.WriteLine("Inside RenewLicenseWithActiveLoan()");
            try
            {//-------------------------------------------------------------------------------------
                EntityCollection expiredLicenseCol = new EntityCollection();
                var query_ntw_expirydate = DateTime.Now.Date;
                var query_statuscode_1 = 1; // Active
                var query_statuscode_2 = 961110000; // License Expired
                var LoanEntity_ntw_loanstatus_ntw_projectloancontractstatusid_1 = 1;  //Loan Status Active
                var LoanEntity_ntw_loanstatus_ntw_projectloancontractstatusid_2 = 5; //Loan Status Not disbursed
                var LoanTypeEntity_ntw_code_1 = 1; // Loan Type
                var LoanTypeEntity_ntw_code_2 = 2; // Group Loan Type


                var query = new QueryExpression("ntw_license")
                {

                    PageInfo = new PagingInfo
                    {
                        PageNumber = 1,
                        Count = 250
                    }
                };
                query.ColumnSet.AddColumns("createdon", "ntw_name", "ntw_licenseid");

                query.Criteria.AddCondition("ntw_expirydate", ConditionOperator.LessEqual, query_ntw_expirydate);
                query.Criteria.AddCondition("ntw_expirydate", ConditionOperator.Last7Days);
                query.Criteria.AddCondition("statuscode", ConditionOperator.In, query_statuscode_1, query_statuscode_2);
                query.Criteria.AddCondition("ntw_familyname", ConditionOperator.NotNull);
                var contactEntity = query.AddLink("contact", "ntw_requester", "contactid");
                contactEntity.EntityAlias = "contactEntity";

                var LoanEntity = contactEntity.AddLink("ntw_loan", "contactid", "ntw_contactid");
                LoanEntity.EntityAlias = "LoanEntity";

                var LoanEntity_ntw_loanstatus = LoanEntity.AddLink("ntw_loanstatus", "ntw_loanstatus", "ntw_loanstatusid");

                LoanEntity_ntw_loanstatus.LinkCriteria.AddCondition("ntw_projectloancontractstatusid", ConditionOperator.In, LoanEntity_ntw_loanstatus_ntw_projectloancontractstatusid_1, LoanEntity_ntw_loanstatus_ntw_projectloancontractstatusid_2);
                var LoanTypeEntity = LoanEntity.AddLink("ntw_productmanagement", "ntw_loantype", "ntw_productmanagementid");
                LoanTypeEntity.EntityAlias = "LoanTypeEntity";

                LoanTypeEntity.LinkCriteria.AddCondition("ntw_code", ConditionOperator.In, LoanTypeEntity_ntw_code_1, LoanTypeEntity_ntw_code_2);



                while (true)
                {
                    expiredLicenseCol = _service.RetrieveMultiple(query);


                    if (expiredLicenseCol.Entities.Count == 0 || expiredLicenseCol.Entities == null)
                    {
                        Console.WriteLine("Expired License with Auto renew Not Found");
                        return;
                    }
                    else
                    {
                        Console.WriteLine("Expired License with Auto renew Found = " + expiredLicenseCol.Entities.Count);
                        Guid RenewalWorkflowId = new Guid(ConfigurationManager.AppSettings["License_Renewal_Workflow"]);
                        RunWorkflowOnLicenses(_service, expiredLicenseCol, RenewalWorkflowId, "Automatic Renew Licenses");
                    }

                    if (!expiredLicenseCol.MoreRecords)
                        break;

                    query.PageInfo.PageNumber++;
                    query.PageInfo.PagingCookie = expiredLicenseCol.PagingCookie;
                }
            }
            catch (Exception ex)
            {
                 Console.WriteLine($"\n {ex.Message}");
            }
        }

        public static void SendNotificationLicenseWillbeExpiredNextMonth(IOrganizationService service)
        {
            Console.WriteLine("Inside SendNotificationLicenseWillbeExpiredNextMonth()");
            try
            {//-------------------------------------------------------------------------------------
                EntityCollection licenseCol = new EntityCollection();
                var query_ntw_expirydate = DateTime.Now.AddDays(1).Date;
                var query_statuscode = 1;


                var query = new QueryExpression("ntw_license")
                {

                    PageInfo = new PagingInfo
                    {
                        PageNumber = 1,
                        Count = 250
                    }
                };
                query.ColumnSet.AddColumns("createdon", "ntw_name", "ntw_licenseid", "ntw_expirydate", "ntw_requester");
                query.Criteria.AddCondition("ntw_expirydate", ConditionOperator.On, query_ntw_expirydate);
                query.Criteria.AddCondition("statuscode", ConditionOperator.In, query_statuscode);
                query.Criteria.AddCondition("ntw_familyname", ConditionOperator.NotNull);


                while (true)
                {
                    licenseCol = service.RetrieveMultiple(query);


                    if (licenseCol?.Entities?.Count == 0 || licenseCol.Entities == null)
                    {
                        Console.WriteLine("Cannot Found License Expired Next Month");
                        return;
                    }
                    else
                    {
                        Console.WriteLine("Expired License Found = " + licenseCol.Entities.Count);
                        EntityCollection licenseHasNoLoanCol = new EntityCollection();

                        foreach (var licenseEntity in licenseCol?.Entities)
                        {
                            if (licenseEntity.Contains("ntw_requester"))
                            {
                                if (!CheckHasActiveLoan(service, licenseEntity.GetAttributeValue<EntityReference>("ntw_requester")) &&
                                   !CheckHasActiveGroupLoan(service, licenseEntity.GetAttributeValue<EntityReference>("ntw_requester")))
                                {
                                    licenseHasNoLoanCol.Entities.Add(licenseEntity);
                                }

                            }

                        }

                        if (licenseHasNoLoanCol?.Entities?.Count > 0)
                        {
                            Guid RenewalWorkflowId = new Guid(ConfigurationManager.AppSettings["License_Notification30Days_Workflow"]);
                            RunWorkflowOnLicenses(service, licenseHasNoLoanCol, RenewalWorkflowId, "SendNotification For Customers");
                        }

                    }

                    if (!licenseCol.MoreRecords)
                        break;

                    query.PageInfo.PageNumber++;
                    query.PageInfo.PagingCookie = licenseCol.PagingCookie;
                }
            }
            catch (Exception ex)
            {
                 Console.WriteLine($"\n {ex.Message}");
            }
        }

        public static void RunWorkflowOnLicenses(IOrganizationService service, EntityCollection elCol, Guid workFlowId, string ActionName)
        {
            Console.WriteLine("Inside RunWorkflowOnLicenses() for " + ActionName);

            // Create an ExecuteMultipleRequest object.
            ExecuteMultipleRequest requestWithResults = new ExecuteMultipleRequest()
            {
                // Assign settings that define execution behavior: continue on error, return responses. 
                Settings = new ExecuteMultipleSettings()
                {
                    ContinueOnError = true,
                    ReturnResponses = true
                },
                // Create an empty organization request collection.
                Requests = new OrganizationRequestCollection()
            };

            Console.WriteLine("Adding These License For" + ActionName);
            // Add a CreateRequest for each entity to the request collection.
            foreach (var rec in elCol.Entities)
            {
                ExecuteWorkflowRequest renewalLicenseRequest = new ExecuteWorkflowRequest()
                {
                    WorkflowId = workFlowId,
                    EntityId = rec.Id
                };
                Console.WriteLine("License Id = " + rec.Id);
                requestWithResults.Requests.Add(renewalLicenseRequest);
            }

            // Execute all the requests in the request collection using a single web method call.
            ExecuteMultipleResponse responseWithResults =
                (ExecuteMultipleResponse)service.Execute(requestWithResults);

            // Display the results returned in the responses.
            foreach (var responseItem in responseWithResults.Responses)
            {
                // A valid response.
                if (responseItem.Response != null)
                    Console.WriteLine("At = " + requestWithResults.Requests[responseItem.RequestIndex].RequestId + " Response = " + responseItem.Response.ResponseName);

                // An error has occurred.
                else if (responseItem.Fault != null)
                     Console.WriteLine($"At = " + requestWithResults.Requests[responseItem.RequestIndex] + " Response = " +
                        responseItem.RequestIndex + responseItem.Fault.InnerFault);
            }
            Console.WriteLine("end Run Workflow for " + ActionName);
        }


        private static bool CheckHasActiveLoan(IOrganizationService service, EntityReference contactRef)
        {
            try
            {
                string fetchXML = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                  <entity name='ntw_loan' >
                                    <attribute name='ntw_loanid'/>
                                    <attribute name='ntw_name'/>
                                    <filter type='and'>
                                      <condition attribute='ntw_contactid' operator='eq' uitype='contact' value='" + contactRef.Id + @"' />
                                    </filter>
                                      <link-entity name='ntw_loanstatus' from='ntw_loanstatusid' to='ntw_loanstatus' link-type='inner' alias='ad'>
                                      <filter type='or'>
                                      <condition attribute='ntw_projectloancontractstatusid' operator='eq' value='1' />
                                      <condition attribute='ntw_projectloancontractstatusid' operator='eq' value='5' />
                                      </filter>
                                      </link-entity>
                                    <link-entity name='ntw_productmanagement' from='ntw_productmanagementid' to='ntw_loantype' link-type='inner' alias='LC'>
                                          <attribute name='ntw_code' />
                                          <filter type='and'>
                                              <condition attribute='ntw_code' operator='eq' value='1' />
                                          </filter>
                                    </link-entity>
                                  </entity>
                              </fetch>";


                var Collection = service.RetrieveMultiple(new FetchExpression(fetchXML));
                if (Collection?.Entities?.Count > 0)
                {
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new Exception("CRM exception : " + ex.ToString());
            }
        }
        private static bool CheckHasActiveGroupLoan(IOrganizationService service, EntityReference contactRef)
        {
            try
            {
                string fetchXML =
                         @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                <entity name='ntw_loan'>
                                 <attribute name='ntw_loanid'/>
                                    <attribute name='ntw_name'/>
                                  <order attribute='ntw_name' descending='true' />
                                  <filter type='or'>
                                      <condition attribute='ntw_contactid' operator='eq' uitype='contact' value='" + contactRef.Id + @"' />
                                      <condition attribute='ntw_groupboss' operator='eq' uitype='contact' value='" + contactRef.Id + @"' />
                                      <condition attribute='ntw_treasurykeeper' operator='eq' uitype='contact' value='" + contactRef.Id + @"' />
                                      <condition attribute='ntw_member1' operator='eq' uitype='contact' value='" + contactRef.Id + @"' />
                                      <condition attribute='ntw_member2' operator='eq'  uitype='contact' value='" + contactRef.Id + @"' />
                                      <condition attribute='ntw_member3' operator='eq'  uitype='contact' value='" + contactRef.Id + @"' />
                                   </filter>
                                  <link-entity name='ntw_loanstatus' from='ntw_loanstatusid' to='ntw_loanstatus' link-type='inner' alias='ad'>
                                      <filter type='or'>
                                      <condition attribute='ntw_projectloancontractstatusid' operator='eq' value='1' />
                                      <condition attribute='ntw_projectloancontractstatusid' operator='eq' value='5' />
                                      </filter>
                                      </link-entity>
                                  <link-entity name='ntw_productmanagement' from='ntw_productmanagementid' to='ntw_loantype' link-type='inner' alias='LC'>
                                    <attribute name='ntw_code' />
                                    <filter type='and'>
                                      <condition attribute='ntw_code' operator='eq' value='2' />
                                    </filter>
                                  </link-entity>                                      
                                </entity>
                              </fetch>";
                var collection = service.RetrieveMultiple(new FetchExpression(fetchXML));

                if (collection?.Entities?.Count > 0)
                {
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {

                throw new Exception("CRM exception : " + ex.ToString());
            }

        }
        private static string _intAPPKey = "";
        private static string _intAPPSecert = "";
        public static void LoanDisbursedRequest_AfterRelationShipOfficerApproval_CreateWaseetLoan(IOrganizationService service)
        {
            
            //Getting OrganizationService from Context
            //var workflowContext = executionContext.GetExtension<IWorkflowContext>();
            //var serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            //var service = serviceFactory.CreateOrganizationService(workflowContext.UserId);
            var targetEntity = service.Retrieve("ntw_createnewpaymentrequest", new Guid("47ef77e3-e999-49ee-81d1-3ac2e257da7c"), new ColumnSet(true));
            //ITracingService tracingService = executionContext.GetExtension<ITracingService>();
            try
            {
                // Get App Secret Key and App Code From SDb Configuration

                // @ TODO : make this in one query

                _intAPPKey = "SDB-CRM-LOS-Loans-User";
                _intAPPSecert = "76A9207C-502B-4B7B-A7DC-3C2BEF8A6E5F";

                //---------------------------------------------
                //get request Status -----------------
                var requestStatus = targetEntity.Contains("ntw_requeststatusid") ? targetEntity.GetAttributeValue<EntityReference>("ntw_requeststatusid") : new EntityReference();
                Console.WriteLine($"Request Status : '{requestStatus.Name}'");
                //--------------------
                // @TODO: to get from input of workflow (request status, nationalid)
                if (requestStatus != null)
                {
                    var requestStatusdata = service.Retrieve("ntw_requeststatus", requestStatus.Id, new ColumnSet("ntw_code"));
                    if (requestStatusdata.Contains("ntw_code"))
                    {
                        var requestStatusCode = requestStatusdata.GetAttributeValue<string>("ntw_code");
                        Console.WriteLine($"Request Status Code : '{requestStatusCode}'");
                        if (requestStatusCode == "CNPR_APO")
                        {
                            var APIUrl = "";
                            var nationalId = targetEntity.Contains("ntw_nationalid") ? targetEntity.GetAttributeValue<string>("ntw_nationalid") : "";
                            Console.WriteLine($"Contact NationId : '{nationalId}'");
                            #region Get Client Profile From CRM by national ID
                            QueryExpression query = new QueryExpression("contact");
                            query.ColumnSet = new ColumnSet("ntw_waseetreferencenumber", "firstname", "middlename", "ntw_grandfathernamear", "lastname", "ntw_birthdayhijrydate", "ntw_idissuedate", "ntw_idexpirydate", "mobilephone", "gendercode", "ntw_iban");
                            query.Criteria.AddCondition("ntw_nationalidtext", ConditionOperator.Equal, nationalId);
                            query.AddOrder("createdon", OrderType.Descending);
                            var RetrieveClientProfileFromCRM = service.RetrieveMultiple(query);
                            // @TODO : throw exception if profile is duplicated or it not found

                            #endregion
                            if (nationalId != null && nationalId != "")
                            {
                                //Check if Prodile Created on waseet or not
                                APIUrl = "https://gateway.sdb.gov.sa/gateway/CoreBanking/wasset/Profile/individual/getby-nationalId";
                                var Result = GetContactByNationaIdFromWaseet(service, targetEntity, null, nationalId, APIUrl);

                                //clientInfo = Result.Result.Data;
                                if (Result != null && Result.IsCompleted) // @TODO : Return if the result is not completed or success
                                {
                                    if (RetrieveClientProfileFromCRM.Entities != null && RetrieveClientProfileFromCRM.Entities.Count != 0)
                                    {
                                        var loan = targetEntity.Contains("ntw_loan") ? targetEntity.GetAttributeValue<EntityReference>("ntw_loan") : new EntityReference();
                                        if (loan != null)
                                        {
                                            // Fet Loan Data From CRM------------------------
                                            var LoanInfoFromCRM = service.Retrieve("ntw_loan", loan.Id, new ColumnSet(true)); // @TODO : Include only the required columns
                                            // ---------------------------------------
                                            var ClienProfile = RetrieveClientProfileFromCRM.Entities[0]; // @TODO : It has to be first or default
                                            if (Result.Result.indvNo != null)
                                            {
                                                ClienProfile["ntw_waseetreferencenumber"] = Result.Result.indvNo;
                                                service.Update(ClienProfile);
                                                // Create Loan ---------------------------
                                                APIUrl = RestAPIHandler.GetWaseetAPIhFromSDBConfiguration(service, "createloanurl_waseet"); // @TODO : To be inculed at the top for reteieving all the wasset related configurations
                                                // @TODO : The creation request can be common method if the GetContactByNationaIdFromWaseet is included to CreateIndividualProfileRequest
                                                var createLoanRequest = PrepareCreateLoanRequest(service, APIUrl, targetEntity, nationalId, LoanInfoFromCRM, Result.Result.indvNo, ClienProfile);
                                                var responsecreateloan = CreateLoan(service, targetEntity, null, createLoanRequest, APIUrl);
                                                //------------------------------------------
                                                if (responsecreateloan.Result.loanRequestNo != null) // @TODO : else should be there to return if the result is not success
                                                {

                                                    LoanInfoFromCRM["ntw_waseetloanreferencenumber"] = responsecreateloan.Result.loanRequestNo;
                                                    service.Update(LoanInfoFromCRM);
                                                }

                                                //--------------------------------------

                                            }
                                            else
                                            {

                                                APIUrl = RestAPIHandler.GetWaseetAPIhFromSDBConfiguration(service, "createclientprofileurl_waseet");// @TODO : To be inculed at the top for reteieving all the wasset related configurations
                                                var CreateIndividualProfileRequest = new CreateIndividualProfileRequest(); // @TODO : Simplify the object intiiation
                                                CreateIndividualProfileRequest.birthDate = ClienProfile.Contains("ntw_birthdayhijrydate") ? ClienProfile.GetAttributeValue<string>("ntw_birthdayhijrydate") : "";
                                                CreateIndividualProfileRequest.firstName = ClienProfile.Contains("firstname") ? ClienProfile.GetAttributeValue<string>("firstname") : ""; ;
                                                CreateIndividualProfileRequest.fatherName = ClienProfile.Contains("middlename") ? ClienProfile.GetAttributeValue<string>("middlename") : ""; ;
                                                CreateIndividualProfileRequest.gFatherName = ClienProfile.Contains("ntw_grandfathernamear") ? ClienProfile.GetAttributeValue<string>("ntw_grandfathernamear") : ""; ;
                                                CreateIndividualProfileRequest.famillyName = ClienProfile.Contains("lastname") ? ClienProfile.GetAttributeValue<string>("lastname") : ""; ;
                                                CreateIndividualProfileRequest.gender = ClienProfile.Contains("gendercode") ? ClienProfile.GetAttributeValue<OptionSetValue>("gendercode").Value : 0; ;
                                                CreateIndividualProfileRequest.nationalId = nationalId;
                                                CreateIndividualProfileRequest.idExpiry = ClienProfile.Contains("ntw_idexpirydate") ? ClienProfile.GetAttributeValue<string>("ntw_idexpirydate") : ""; ;
                                                CreateIndividualProfileRequest.mobile = ClienProfile.Contains("mobilephone") ? ClienProfile.GetAttributeValue<string>("mobilephone") : ""; ;
                                                CreateIndividualProfileRequest.idIssueDateHijri = ClienProfile.Contains("ntw_idissuedate") ? ClienProfile.GetAttributeValue<string>("ntw_idissuedate") : ""; ;


                                                // convert ID Expiry Date to 
                                                var IdExpiryDateAfterTransfer = ConvertDateFormat(CreateIndividualProfileRequest.idExpiry);
                                                CreateIndividualProfileRequest.idExpiry = IdExpiryDateAfterTransfer; // @TODO : Simplify the assignment
                                                // ID Issue Date
                                                var IdissuedateAfterTransfer = ConvertDateFormat(CreateIndividualProfileRequest.idIssueDateHijri);
                                                CreateIndividualProfileRequest.idIssueDateHijri = IdissuedateAfterTransfer;  // @TODO :  Simplify the assignment
                                                // // @TODO : This can be include or added to the GetContactByNationaIdFromWaseet function 
                                                var response = CreateIndividualProfile(service, targetEntity, null, CreateIndividualProfileRequest, APIUrl);
                                                if (response.Result.indvNo != null)  // @TODO : Include else if there is any error or the reponse is not success
                                                {
                                                    // Update Client Profile on CRM By waseet Refenece Number ----------------
                                                    ClienProfile["ntw_waseetreferencenumber"] = response.Result.indvNo;
                                                    service.Update(ClienProfile);
                                                    // -----------------------------                                               
                                                    // ---------------------------------------
                                                    //Call Prepare loan request
                                                    APIUrl = RestAPIHandler.GetWaseetAPIhFromSDBConfiguration(service, "createloanurl_waseet");
                                                    var createLoanRequest = PrepareCreateLoanRequest(service, APIUrl, targetEntity, nationalId, LoanInfoFromCRM, response.Result.indvNo, ClienProfile);
                                                    Console.WriteLine("Create Loan Request" + createLoanRequest);
                                                    var responsecreateloan = CreateLoan(service, targetEntity, null, createLoanRequest, APIUrl);
                                                    if (responsecreateloan.Result.loanRequestNo != null)
                                                    {
                                                        LoanInfoFromCRM["ntw_waseetloanreferencenumber"] = responsecreateloan.Result.loanRequestNo;
                                                        service.Update(LoanInfoFromCRM);
                                                    }

                                                }


                                            }
                                        }
                                    }
                                }

                            }
                        }
                    }
                }

            }
            catch (Exception e)
            {
                throw e;
            }
        }
        public static async Task<ClienInfo> GetContactByNationaIdFromWaseet(IOrganizationService service, Entity targetEntity, ITracingService tracingService, string nationalId, string intUrl)
        {

            JavaScriptSerializer js = new JavaScriptSerializer();

            StringBuilder url = new StringBuilder();
            var waseetContact = new ClienInfo();
            var CreateLogRequest = new CreateLogRequest();
            CreateLogRequest.Name = "Get Contact From Waseet";
            CreateLogRequest.CreateNewPaymentRequest = new EntityReference(targetEntity.LogicalName, targetEntity.Id);
            url.Append(intUrl);

            var response = await new RestAPIHandler().RunAsync<ClienInfo>(url, MethodTypes.post, nationalId, CreateLogRequest, service, tracingService, _intAPPKey, _intAPPSecert);
            if (response.data != null)
            {
                // Console.WriteLine("Response.Data" + response.data.ToString());
                //waseetContact = JsonConvert.DeserializeObject<ClienInfo>(response.data.ToString());
                // waseetContact = js.Deserialize<ClienInfo>(response.data.ToString());
                waseetContact = response.data;

                Console.WriteLine("waseetContact" + js.Serialize(waseetContact));
            }


            return waseetContact;
        }
        public static async Task<CreateIndividualProfileResponse> CreateIndividualProfile(IOrganizationService service, Entity targetEntity, ITracingService tracingService, CreateIndividualProfileRequest request, string intUrl)
        {
            StringBuilder url = new StringBuilder();
            JavaScriptSerializer js = new JavaScriptSerializer();
            var CreateLogRequest = new CreateLogRequest();
            CreateLogRequest.Name = " ????? ??? ?????? ??? ???? ????";
            CreateLogRequest.CreateNewPaymentRequest = new EntityReference(targetEntity.LogicalName, targetEntity.Id);
            var CreateIndividualProfileResponse = new CreateIndividualProfileResponse();
            request.countryId = "1001";
            request.dataSource = 2;
            request.mobileStatus = false;
            request.idTypeCode = "111";
            url.Append(intUrl);
            var response = await new RestAPIHandler().RunAsync<CreateIndividualProfileResponse>(url, MethodTypes.post, request, CreateLogRequest, service, tracingService, _intAPPKey, _intAPPSecert);
            if (response.data != null)
            {

                CreateIndividualProfileResponse = response.data;
            }

            else
                CreateIndividualProfileResponse.error = response.error;
            return CreateIndividualProfileResponse;
        }

        public static async Task<CreateLoanResponse> CreateLoan(IOrganizationService service, Entity targetEntity, ITracingService tracingService, CreateLoanRequest request, string intUrl)
        {
            StringBuilder url = new StringBuilder();
            JavaScriptSerializer js = new JavaScriptSerializer();
            var CreateLoanResponse = new CreateLoanResponse();
            var CreateLogRequest = new CreateLogRequest();
            CreateLogRequest.Name = " ????? ????? ??? ???? ????";
            CreateLogRequest.CreateNewPaymentRequest = new EntityReference(targetEntity.LogicalName, targetEntity.Id);
            url.Append(intUrl);
            var response = await new RestAPIHandler().RunAsync<CreateLoanResponse>(url, MethodTypes.post, request, CreateLogRequest, service, tracingService, _intAPPKey, _intAPPSecert);
            if (response.data != null)
                CreateLoanResponse = response.data;

            else
                CreateLoanResponse.error = response.error;
            return CreateLoanResponse;
        }


        public static CreateLoanRequest PrepareCreateLoanRequest(IOrganizationService service, string APIUrl, Entity targetEntity, string nationalId, Entity LoanInfoFromCRM, string requesterIndvNo, Entity ClienProfile)
        {
            //create loan
            APIUrl = RestAPIHandler.GetWaseetAPIhFromSDBConfiguration(service, "createloanurl_waseet");
            CreateLoanRequest createLoanRequest = new CreateLoanRequest();
            //Request
            Request request = new Request();
            //Fixed Data -----------------------------
            request.requestDate = DateTime.UtcNow.ToString();
            request.branchCode = "10101";
            request.productCode = "pfpf";
            request.waseetLoanRequestNo = "string";
            request.zoodSubscribtion = false;
            //---------------------------------------
            request.requestAmount = targetEntity.Contains("ntw_requiredamount") ? targetEntity.GetAttributeValue<int>("ntw_requiredamount") : 0;
            request.clientLoanReference = LoanInfoFromCRM.GetAttributeValue<string>("ntw_name");
            request.requesterNationalId = nationalId;
            request.requesterIndvNo = requesterIndvNo;
            request.requesterIBAN = ClienProfile.Contains("ntw_iban") ? ClienProfile.GetAttributeValue<string>("ntw_iban") : "";

            //Primary Contact object
            PrimaryContract primaryContract = new PrimaryContract();
            // Fixed Data on Primary Contact object---------------------------
            primaryContract.committeeMettingNo = "PFP Auto Approval";
            primaryContract.committeeMettingDate = "1445-01-01";
            primaryContract.term = "monopay";
            primaryContract.installmentFrequency = "m";
            //----------------------------------------------------------------
            //Ger Loan Grace Period ---------------
            // if Loan Grace Perios is 60 funding period value is 62 if 30 is 31
            var LoanGracPeriod = LoanInfoFromCRM.GetAttributeValue<EntityReference>("ntw_graceperiods");
            if (LoanGracPeriod != null)
            {
                var garcePeriodData = service.Retrieve("ntw_lkpgraceperiod", LoanGracPeriod.Id, new ColumnSet("ntw_name"));
                if (LoanGracPeriod != null)
                {
                    var GracePeriodValue = garcePeriodData.GetAttributeValue<string>("ntw_name");
                    if (GracePeriodValue == "60")
                    {
                        primaryContract.fundingPeriodValue = 62;
                        primaryContract.gracePeriodValue = 60;
                    }
                    else if (GracePeriodValue == "30")
                    {
                        primaryContract.fundingPeriodValue = 31;
                        primaryContract.gracePeriodValue = 30;
                    }
                }
            }
            // -----------------------------------

            //create loan request
            createLoanRequest.primaryContract = primaryContract;
            createLoanRequest.request = request;
            return createLoanRequest;
        }
        public static string ConvertDateFormat(string date)
        {
            string dateAfterTranser = "";
            if (date != null)
            {
                string[] parts = date.Split('/');
                int hijriYear = int.Parse(parts[0]);
                int hijriMonth = int.Parse(parts[1]);
                int hijriDay = int.Parse(parts[2]);
                string formattedDate = $"{hijriDay:D2}/{hijriMonth:D2}/{hijriYear}";
                dateAfterTranser = formattedDate;
            }
            return dateAfterTranser;
        }
        public static void AssociateRoleToUser(IOrganizationService service)
        {
            var fetchUsers = $@" <fetch>
                            <entity name='systemuser'>
                            <attribute name='systemuserid' />
                            <attribute name='businessunitid' />
                            <filter type='and'>
                                <condition attribute='isdisabled' operator='eq' value='0' />
                                <filter type='or'>";

            //get data from excel
            var emails = GetIdsFromExcel("Ids.xlsx");
            foreach (var email in emails[0])
            {
                fetchUsers += $@"<condition attribute='internalemailaddress' operator='eq' value='{email}' />";
            }

            fetchUsers += $@"</filter>
                            </filter>
                            </entity>
                            </fetch>";

            var resultUsers = service.RetrieveMultiple(new FetchExpression(fetchUsers));
            var count = 0;
            foreach (var user in resultUsers.Entities)
            {
                count++;
                var BUId = ((EntityReference)user.Attributes["businessunitid"]).Id;

                //fetch security role to associate
                var fetchRole = $@"<fetch>
                                  <entity name = 'role' >
                                    <attribute name = 'name' />
                                    <attribute name = 'businessunitid' />
                                    <attribute name = 'roleid' />
                                    <order attribute = 'name' descending = 'false' />
                                    <filter type = 'and' >
                                      <condition attribute = 'businessunitid' operator= 'eq' value = '{BUId}' />
                                      <condition attribute = 'name' operator= 'like' value = '%360%' />
                                    </filter >
                                  </entity >
                                </fetch >";
                var role = service.RetrieveMultiple(new FetchExpression(fetchRole));
                try
                {
                    service.Associate("systemuser", user.Id, new Relationship("systemuserroles_association"), new EntityReferenceCollection() { new EntityReference("role", role.Entities[0].Id) });
                }
                catch (Exception)
                {

                    Console.WriteLine(user.Id);
                }
                
                Console.WriteLine(count);
            }
        }
        public static void AssociateRoleToTeam(IOrganizationService service)
        {
            var fetchUsers = $@" <fetch>
                            <entity name='team'>
                            <attribute name='teamid' />
                            <attribute name='businessunitid' />
                            <filter type='and'>
                                <condition attribute='teamtype' operator='eq' value='0' />
                            </filter>
                            </entity>
                            </fetch>";

            var resultTeams = service.RetrieveMultiple(new FetchExpression(fetchUsers));
            var count = 0;
            foreach (var team in resultTeams.Entities)
            {
                count++;
                var BUId = ((EntityReference)team.Attributes["businessunitid"]).Id;

                //fetch security role to associate
                var fetchRole = $@"<fetch>
                                  <entity name = 'role' >
                                    <attribute name = 'name' />
                                    <attribute name = 'businessunitid' />
                                    <attribute name = 'roleid' />
                                    <order attribute = 'name' descending = 'false' />
                                    <filter type = 'and' >
                                      <condition attribute = 'businessunitid' operator= 'eq' value = '{BUId}' />
                                      <condition attribute = 'name' operator= 'like' value = '%Manage Innovation%' />
                                    </filter >
                                  </entity >
                                </fetch >";
                var role = service.RetrieveMultiple(new FetchExpression(fetchRole));
                try
                {
                    service.Associate("team", team.Id, new Relationship("teamroles_association"), new EntityReferenceCollection() { new EntityReference("role", role.Entities[0].Id) });
                }
                catch (Exception)
                {

                    Console.WriteLine(team.Id);
                }

                Console.WriteLine(count);
            }
        }
        public class CreateIndividualProfileRequest
        {
            // creat profile on waseet
            public string mobile { get; set; }
            public string birthDate { get; set; }
            public string firstName { get; set; }
            public string fatherName { get; set; }
            public string gFatherName { get; set; }
            public string famillyName { get; set; }
            public int gender { get; set; }
            public string countryId { get; set; }
            public string nationalId { get; set; }
            public string idExpiry { get; set; }
            public string idTypeCode { get; set; }
            public int dataSource { get; set; }
            public bool mobileStatus { get; set; }
            public string idIssueDateHijri { get; set; }
        }
        public class Request
        {

            //  this is the request which is enet to wasseet

            public string requestDate { get; set; }
            public int requestAmount { get; set; }
            public string branchCode { get; set; }
            public string productCode { get; set; }
            public string clientLoanReference { get; set; }
            public string requesterNationalId { get; set; }
            public string requesterIndvNo { get; set; }
            public string requesterIBAN { get; set; }
            public string waseetLoanRequestNo { get; set; }
            public bool zoodSubscribtion { get; set; }

        }
        public class PrimaryContract
        {
            public string committeeMettingNo { get; set; }
            public string committeeMettingDate { get; set; }
            public string term { get; set; }
            public int fundingPeriodValue { get; set; }
            public string installmentFrequency { get; set; }
            public int gracePeriodValue { get; set; }

        }
        public class CreateLoanRequest
        {
            // on waseet
            public Request request { get; set; }
            public PrimaryContract primaryContract { get; set; }
        }
        public class CreateLoanResponse
        {
            // the comming response from waseet
            public string loanRequestNo { get; set; }
            public Error error { get; set; }
        }
        public class CreateIndividualProfileResponse
        {
            // resopnse which comes back after calling create profile request on waseet
            public string indvNo { get; set; }
            public string mobileNumber { get; set; }
            public string birthDate { get; set; }
            public string firstName { get; set; }
            public string fatherName { get; set; }
            public string gFatherName { get; set; }
            public string famillyName { get; set; }
            public string gender { get; set; }
            public string countryId { get; set; }
            public string nationalId { get; set; }
            public string idExpiry { get; set; }
            public string idTypeCode { get; set; }
            public string dataSource { get; set; }
            public bool mobileStatus { get; set; }
            public string idIssueDate { get; set; }
            public Error error { get; set; }
        }
        public class ClienInfo
        {
            // client profile which is comming back from waseet when checking profile existance
            public string mobile { get; set; }
            public string birthDate { get; set; }
            public string fullName { get; set; }
            public string firstName { get; set; }
            public string fatherName { get; set; }
            public string gFatherName { get; set; }
            public string famillyName { get; set; }
            public string gender { get; set; }
            public string countryId { get; set; }
            public string idNo { get; set; }
            public string idExpiry { get; set; }
            public string idIssueDateHijri { get; set; }
            public string idTypeCode { get; set; }
            public string indvNo { get; set; }
            public string dataSource { get; set; }
            public bool mobileStatus { get; set; }
            public string profileStatus { get; set; }

        }

        public class Response
        {
            public StatusCode status { get; set; }
            public string statusMessgae { get; set; }
        }
        public enum StatusCode
        {
            SUCCESS = 1,
            FAILED_TRY = 2,
            FAILED_NO_TRY = 3
        }
        public class Response<T>
        {
            public bool IsSucceeded { set; get; } = true;
            public string MessageAr { set; get; } = "";
            public string MessageEn { set; get; } = "";
            public string RequestNumber { get; set; } = "";
            public T Data { set; get; }
        }
        public class CreateLoanInstallmentScheduleRequest
        {
            public string LoanNumber { get; set; }
            public List<Installment> loanInstallments { get; set; }
        }
        public class ConfigurationValue
        {
            public int IntVal { get; set; }
            public string StringVal { get; set; }
            public DateTime DateVal { get; set; }
        }
        public class UpdateDisbursementStatusResponse
        {
            public bool IsUpdated { get; set; }
        }
        public class CreateLoanInstallmentScheduleResponse
        {
            public bool IsCreated { get; set; }
        }
        public class LoanCreationResponse
        {
            public string loanRequestNo { get; set; }
            public List<Disburcement> disburcements { get; set; }
            public List<installment> installments { get; set; }

            public string Status { get; set; } // 0,1
            public List<LoanError> errors { get; set; }
        }
        public class Installment
        {
            public decimal Amount { get; set; }
            public int InstallmentNumber { get; set; }
            public decimal PaidAmount { get; set; }
            public string DueDate { get; set; }
            public string SaddadInvoice { get; set; }
            // public string PaymentDate{get;set;}
        }
        public class UpdateDisbursementStatusRequest
        {
            [Required]
            public string LoanNumber { get; set; }
            [Required]
            public Guid DisbursementRequestGUID { get; set; }
            [Required]
            public string DisbursementStatusCode { get; set; }
            [Required]
            public string DisbursementDueDate { get; set; }
        }
        public class Disburcement
        {
            public string no { get; set; }
            public string amount { get; set; }
            public string dueDate { get; set; }
            public string status { get; set; }
            public string B2BBankRef { get; set; }
        }
        public class LoanError
        {
            public string code { get; set; }
            public string message { get; set; }
        }
        public class installment
        {
            public string no { get; set; }
            public string amount { get; set; }
            public string dueDate { get; set; }
        }
        
        public class CrmCondition
        {
            public string Key { get; set; }
            public string Value { get; set; }
            public ConditionOperator ConditionOperator { get; set; }
        }
        public class RMQueue
        {
            public string Name { get; set; }
            public string URL { get; set; }
            public string Port { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
        }
        public enum RMQMessageType
        {
            Portal_Notification = 1,
            Yakeen = 2,
            MCI = 3,
            Simah = 4,
            SMS = 5,
            Waseet = 6,
            GOSI = 7,
            MOSA = 8,
            UploadDocumentToSP = 9,
            Wasel = 10,
            ErpTransaction = 11,
            WaseetBatch = 12,
            CSReport = 13,
            HDRS = 15
        }
        public class SMSByAppIDRequest
        {
            public string Mobiles { get; set; }
            public string Title { get; set; }
            public string MessageText { get; set; }
            public Guid MessageId { get; set; }
        }

    }
}