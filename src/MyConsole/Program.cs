using ExcelDataReader.Log;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Rest;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using MyConsole.Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.ServiceModel.Configuration;
using System.Text;
using System.Threading.Tasks;
using System.Web.Services.Description;
using System.Windows;
using System.Xml.Linq;

namespace MyConsole
{ 

    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Start Connection");
            IOrganizationService service = CRMConnector.ConnectToCRM();
            Console.WriteLine("Connected");
            //Applications.UpdateLoanPartner(service);
            //Applications.UpdateContactWithIndvNo(service);
            //Applications.UpdateAccountsWithActivities();
            //Applications.FixContacts(service);
            //Applications.SettleLoans(service);
            //Applications.CancelEmails(service);
            //Applications.UpdateIds(service);
            //Applications.RenewLicense(service);
            //Applications.ReadJsonFile(service);
            //Applications.StopDuplicationCase(service);
            //Applications.UpdateFundingWalletSettlement(service);
            //Applications.SLAMatixForCases(service);               
            //Applications.DeleteBPFRecords(service);
            //Applications.UpdateLoanRequests(service);
            //Applications.TestSMSWithRMQueue(service);
            //Applications.Calculator();

            //Applications.UpdateContactWithDOB(service);
            //Applications.CloseJanaLoans(service);
            //Applications app = new Applications();
            //var response = app.UpdateLoanReponseWaseet(service);
            //Console.WriteLine(response);

            //migrate cases
            //string statusId = Applications.GetStatusFromCode("CSC_CSNew", service);
            //string reasonId = Applications.GetTicketReasonByCode("Migrate", service);

            //if (string.IsNullOrEmpty(statusId) || string.IsNullOrEmpty(reasonId))
            //{
            //   Console.WriteLine("cannot get Reason ID or Status ID ");
            //    throw new Exception("cannot get Reason ID or Status ID ");
            //}
            //Applications.MigrateCases(service, statusId, reasonId);

            ////2p
            //Guid sourceTeamId = new Guid("d67790b7-3983-ec11-b817-00505692b4e1");
            //Guid targetTeamId = new Guid("0171a755-563e-f111-8c47-005056923aa9");

            //Applications.CopyTeamMembers(service, sourceTeamId, targetTeamId);
            //customer care
            //Guid contactEmarketId = new Guid("32722d09-40b2-f011-b88b-00505692a571");
            //var response = Applications.GetAgreementActiveList(service, contactEmarketId);
            //Console.WriteLine(response.ToString());

            //sourceTeamId = new Guid("DBACD7F1-5781-EC11-B817-00505692B4E1");
            //targetTeamId = new Guid("1e3974c7-4f3e-f111-8c47-005056923aa9");

            //Applications.CopyTeamMembers(service, sourceTeamId, targetTeamId);

            //Applications.AddRepresenativeConfigurations(service, "3DF594FE-BBE7-EC11-B826-00505692A571");
            //Applications.EnableDisableUsers(service,true);
            //Applications.AddSubProductsAndAssociateReasons(service);
            //Applications.UpdateProcessesOwner(service);
            //Applications.AddTicketReasons(service, new Guid[] { new Guid("1a3b25cc-585a-f011-8c2f-005056923aa9") });
            //Applications.AddAdditionalFieldConfiguration(service);
            //Applications.UpdateTicketReasons(service);

            //renew license
            //try
            //{

            //   Console.WriteLine("License Renewal - START");
            //    if (service != null)
            //    {
            //        // To get Expired License with Active (Loan and Group Loan)
            //        Applications.RenewLicenseWithActiveLoan(service);

            //        //Send Notifications for License will be expired Next Month
            //        Applications.SendNotificationLicenseWillbeExpiredNextMonth(service);

            //    }


            //    Console.WriteLine("License Renewal - END");
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex.Message);
            //}
            //finally
            //{
            //    Console.WriteLine("close");
            //}
            //Applications.LoanDisbursedRequest_AfterRelationShipOfficerApproval_CreateWaseetLoan(service);
            //Applications.AssociateRoleToUser(service);

            //update classification

            //create subproduct

            //Entity newSubProduct = new Entity("sdb_ticketclassification",new Guid("44bfa2af-6a91-f111-b8a5-00505692a571"));
            //newSubProduct.Attributes["sdb_englishname"] = "نقل";
            //newSubProduct.Attributes["sdb_scope"] = new OptionSetValue(0);
            //newSubProduct.Attributes["sdb_assignto"] = new OptionSetValue(0);
            //newSubProduct.Attributes["sdb_team"] = new EntityReference("team", new Guid("d67790b7-3983-ec11-b817-00505692b4e1"));
            //IList<OptionSetValue> channels = new List<OptionSetValue>() { new OptionSetValue(0), new OptionSetValue(1), new OptionSetValue(2), new OptionSetValue(3) };
            //newSubProduct.Attributes["sdb_visibleinui"] = new OptionSetValueCollection(channels);
            //IList<OptionSetValue> ticketTypes = new List<OptionSetValue>() { new OptionSetValue(0), new OptionSetValue(1), new OptionSetValue(2), new OptionSetValue(3) };
            //newSubProduct.Attributes["sdb_tickettype"] = new OptionSetValueCollection(ticketTypes);
            //service.Update(newSubProduct);

            Applications.AssociateRoleToTeam(service);


            Console.WriteLine("App Closed");
            Console.ReadLine();
        }

        
    }
}
