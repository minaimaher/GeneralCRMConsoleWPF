using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.ServiceModel.Description;

namespace CrmFunctionRunner.Engine
{
    public sealed class CrmEnvironment
    {
        public string Name { get; set; }
        public string ServiceUrl { get; set; }
        public bool IsProduction { get; set; }
        public override string ToString() => Name;
    }

    /// <summary>The signed-in connection for this session. Credentials are kept in memory only.</summary>
    public sealed class Connection
    {
        public CrmEnvironment Environment { get; private set; }
        public string UserName { get; private set; }
        Func<IOrganizationService> _factory;

        /// <summary>
        /// Environments from CrmFunctionRunner.exe.config:
        ///   &lt;add key="Environment:Development" value="https://server/ORG/XRMServices/2011/Organization.svc" /&gt;
        ///   &lt;add key="Environment:Production" value="https://server/ORG/XRMServices/2011/Organization.svc|production" /&gt;
        /// </summary>
        public static List<CrmEnvironment> LoadEnvironments()
        {
            var list = new List<CrmEnvironment>();
            foreach (string key in ConfigurationManager.AppSettings.AllKeys)
            {
                if (!key.StartsWith("Environment:", StringComparison.OrdinalIgnoreCase)) continue;
                string value = ConfigurationManager.AppSettings[key] ?? "";
                var parts = value.Split('|');
                if (string.IsNullOrWhiteSpace(parts[0])) continue;
                list.Add(new CrmEnvironment
                {
                    Name = key.Substring("Environment:".Length).Trim(),
                    ServiceUrl = parts[0].Trim(),
                    IsProduction = parts.Skip(1).Any(p => p.Trim().Equals("production", StringComparison.OrdinalIgnoreCase))
                });
            }
            return list;
        }

        /// <summary>Connects and checks the credentials with WhoAmI. Pass a null user name to use the Windows account.</summary>
        public static Connection Open(CrmEnvironment env, string userName, string password)
        {
            Func<IOrganizationService> factory = () =>
            {
                var credentials = new ClientCredentials();
                if (string.IsNullOrEmpty(userName))
                    credentials.Windows.ClientCredential = CredentialCache.DefaultNetworkCredentials;
                else
                {
                    credentials.UserName.UserName = userName;
                    credentials.UserName.Password = password;
                }
                var proxy = new OrganizationServiceProxy(new Uri(env.ServiceUrl), null, credentials, null);
                proxy.EnableProxyTypes();
                proxy.Timeout = new TimeSpan(0, 20, 0);
                return proxy;
            };

            var service = factory();
            var who = (WhoAmIResponse)service.Execute(new WhoAmIRequest());
            string fullName;
            try { fullName = service.Retrieve("systemuser", who.UserId, new ColumnSet("fullname")).GetAttributeValue<string>("fullname"); }
            catch { fullName = userName ?? System.Environment.UserDomainName + "\\" + System.Environment.UserName; }

            return new Connection { Environment = env, UserName = fullName, _factory = factory };
        }

        /// <summary>A fresh CRM connection (the console code reconnects during long runs).</summary>
        public IOrganizationService Create() => _factory();
    }
}
