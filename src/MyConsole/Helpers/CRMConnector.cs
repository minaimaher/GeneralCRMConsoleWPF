using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.ServiceModel.Description;
using System.Text;
using System.Threading.Tasks;

namespace MyConsole.Helpers
{
    internal class CRMConnector
    {
        public static IOrganizationService ConnectToCRM()
        {
            string serviceUrl = ConfigurationManager.AppSettings["ServiceUrl"];
            string envUrl = ConfigurationManager.AppSettings["EnvUrl"].ToString();
            string userName = ConfigurationManager.AppSettings["UserName"].ToString();
            string password = ConfigurationManager.AppSettings["Password"].ToString();
            string appId = ConfigurationManager.AppSettings["AppId"].ToString();
            string redirectUri = ConfigurationManager.AppSettings["RedirectUri"].ToString();
            
            ClientCredentials clientCredentials = new ClientCredentials();
            clientCredentials.UserName.UserName = userName;
            clientCredentials.UserName.Password = password;
            // For Dynamics 365 Customer Engagement V9.X, set Security Protocol as TLS12
            //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            try
            {
                //string connection = $@"Url = {envUrl};AuthType = OAuth;UserName = {userName};Password = {password};AppId = {appId};
                //                    RedirectUri = {redirectUri};LoginPrompt=Auto;RequireNewInstance = True";
                //IOrganizationService organizationService = new CrmServiceClient(connection);
                OrganizationServiceProxy organizationService = new OrganizationServiceProxy(new Uri(serviceUrl),
                 null, clientCredentials, null);
                organizationService.EnableProxyTypes();
                organizationService.Timeout = new TimeSpan(0, 20, 0);
                return organizationService;
            }
            catch (Exception e)
            {
                Console.WriteLine("Can't connect because of: "+e.ToString());
                throw e;
            }
            
        }
    }
}
