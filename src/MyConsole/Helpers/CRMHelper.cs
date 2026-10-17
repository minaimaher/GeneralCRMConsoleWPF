using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Description;
using System.Text;
using System.Threading.Tasks;
using static MyConsole.Applications;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Messages;
using System.ServiceModel;

namespace MyConsole.Helpers
{
    public class CRMHelper
    {
        public static IOrganizationService ConnectToCRM()
        {
            try
            {
                ClientCredentials clientCredentials = new ClientCredentials();
                clientCredentials.UserName.UserName = "-";
                clientCredentials.UserName.Password = "-";
                Uri url = new Uri("https://nppf2.sdb.gov.sa/SDBCRM/XRMServices/2011/Organization.svc");


                OrganizationServiceProxy proxy = new OrganizationServiceProxy(url, null, clientCredentials, null);
                proxy.EnableProxyTypes();

                return (IOrganizationService)proxy;

            }
            catch (Exception)
            {
                throw;
            }
        }

        public static Entity RetrieveEntityById(IOrganizationService service, string strEntityLogicalName, Guid entityId
                                                    , ColumnSet columnSet, bool retrieveAllColumns)
        {
            if (retrieveAllColumns)
                return service.Retrieve(strEntityLogicalName, entityId, new ColumnSet(true)); //it will retrieve the all attrributes
            return service.Retrieve(strEntityLogicalName, entityId, columnSet); //it will retrieve the selected attrributes
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

            var entityCollection = CRMHelper.GetEntityData(service, "ntw_sdbconfiguratin", conditions, true, null);
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

        public static EntityCollection GetEntityData(IOrganizationService service, string entityName, Dictionary<string, string> conditionAttributes,
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
                Condition.Operator = ConditionOperator.Equal;
                Condition.Values.Add(attribute.Value);
                MyQuery.Criteria.Conditions.Add(Condition);
            }
            return service.RetrieveMultiple(MyQuery);
        }
        public static string GetConfigurationData(IOrganizationService service, string value, string fieldName = "")
        {
            try
            {
                QueryExpression query = new QueryExpression("ntw_sdbconfiguratin");
                query.Criteria.AddCondition("ntw_configurationcode", ConditionOperator.Equal, value);
                query.ColumnSet.AddColumn("ntw_numbervalue");
                if (!string.IsNullOrEmpty(fieldName))
                {
                    query.ColumnSet.AddColumn(fieldName);

                }
                var result = service.RetrieveMultiple(query);
                //"ntw_textvalue"
                if (result.Entities.Any())
                {
                    if (string.IsNullOrEmpty(fieldName))
                    {
                        return result.Entities[0].Attributes["ntw_numbervalue"].ToString();

                    }
                    else
                    {
                        return result.Entities[0].Attributes[fieldName].ToString();
                    }
                }
                return null;
            }
            catch (Exception e)
            {

                throw e;
            }
        }
        public static void ChangeEntityStatus(IOrganizationService service, string entityName, Guid entityId, int state, int status)
        {
            SetStateRequest setStateRequest = new SetStateRequest
            {
                EntityMoniker = new EntityReference(entityName, entityId),
                State = new OptionSetValue(state),
                Status = new OptionSetValue(status),
            };

            service.Execute(setStateRequest);
        }

        public static Entity RetrieveInstallmentStatusEntity(string EntityName, ConditionOperator? conditionOperator, string ComparedFiled, int ComparedValue, ColumnSet columns, IOrganizationService service)
        {
            QueryExpression query = new QueryExpression(EntityName);
            if (columns != null)
                query.ColumnSet = columns;
            else
                query.ColumnSet = new ColumnSet(true);
            if (conditionOperator.HasValue)
            {
                query.Criteria.AddCondition(ComparedFiled, conditionOperator.Value, ComparedValue);
                query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);
            }
            OrderExpression order = new OrderExpression("modifiedon", OrderType.Descending);
            query.Orders.Add(order);

            EntityCollection results = service.RetrieveMultiple(query);

            return results.Entities.FirstOrDefault();
        }

        public static Entity RetrieveEntity(string EntityName, ConditionOperator? conditionOperator, string ComparedFiled, string ComparedValue, ColumnSet columns, IOrganizationService service)
        {
            QueryExpression query = new QueryExpression(EntityName);
            if (columns != null)
                query.ColumnSet = columns;
            else
                query.ColumnSet = new ColumnSet(true);
            if (conditionOperator.HasValue && !string.IsNullOrEmpty(ComparedFiled) && !string.IsNullOrWhiteSpace(ComparedFiled) && !string.IsNullOrEmpty(ComparedValue) && !string.IsNullOrWhiteSpace(ComparedValue))
            {
                query.Criteria.AddCondition(ComparedFiled, conditionOperator.Value, ComparedValue);
                query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);
            }
            OrderExpression order = new OrderExpression("modifiedon", OrderType.Descending);
            query.Orders.Add(order);

            EntityCollection results = service.RetrieveMultiple(query);

            return results.Entities.FirstOrDefault();
        }

        

      
        public static EntityCollection GetSDBConfigurationEntityData(IOrganizationService service, string entityName, List<CrmCondition> conditionAttributes,
                                                bool retrieveAllColumns, params string[] columns)
        {
            QueryExpression MyQuery = new QueryExpression();

            MyQuery.EntityName = entityName;
            if (retrieveAllColumns)
                MyQuery.ColumnSet = new ColumnSet(true);
            else
                MyQuery.ColumnSet = new ColumnSet(columns);

            MyQuery.Criteria = new FilterExpression();
            MyQuery.Criteria.FilterOperator = LogicalOperator.Or;
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
        public static EntityCollection GetEntityData(IOrganizationService service, string entityName, List<CrmCondition> conditionAttributes, LogicalOperator logicalOperator,
                                                        bool retrieveAllColumns, params string[] columns)
        {
            QueryExpression MyQuery = new QueryExpression();

            MyQuery.EntityName = entityName;
            if (retrieveAllColumns)
                MyQuery.ColumnSet = new ColumnSet(true);
            else
                MyQuery.ColumnSet = new ColumnSet(columns);

            MyQuery.Criteria = new FilterExpression();
            MyQuery.Criteria.FilterOperator = logicalOperator;
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

        

        public static EntityCollection GetLinkEntitiesData(IOrganizationService service, string entity1Name, string entity2Name,
                                                            string entity1LinkId, string entity2LinkId, ColumnSet columnSet1,
                                                            Dictionary<string, string> conditionAttributes,
                                                            Dictionary<string, string> entity1ConditionAttributes, string entity2AliasName, params string[] columnSet2)
        {
            QueryExpression query = new QueryExpression(entity1Name);
            if (columnSet1 != null)
                query.ColumnSet = columnSet1;
            else
                query.ColumnSet = new ColumnSet(true);

            LinkEntity linkEntity1 = new LinkEntity(entity1Name, entity2Name, entity1LinkId, entity2LinkId, JoinOperator.Inner);
            query.LinkEntities.Add(linkEntity1);
            if (columnSet2 != null && columnSet2.Count() > 0)
            {
                query.LinkEntities[0].Columns.AddColumns(columnSet2);
                query.LinkEntities[0].EntityAlias = entity2AliasName;
            }

            if (conditionAttributes != null && conditionAttributes.Count > 0)
            {
                foreach (var attribute in conditionAttributes)
                {
                    ConditionExpression Condition = new ConditionExpression();
                    Condition.AttributeName = attribute.Key;
                    Condition.Operator = ConditionOperator.Equal;
                    Condition.Values.Add(attribute.Value);
                    query.Criteria.Conditions.Add(Condition);
                }
            }

            if (entity1ConditionAttributes != null && entity1ConditionAttributes.Count > 0)
            {
                linkEntity1.LinkCriteria = new FilterExpression();

                foreach (var attribute in entity1ConditionAttributes)
                {
                    ConditionExpression Condition = new ConditionExpression();
                    Condition.AttributeName = attribute.Key;
                    Condition.Operator = ConditionOperator.Equal;
                    Condition.Values.Add(attribute.Value);
                    linkEntity1.LinkCriteria.AddCondition(Condition);
                }
            }

            return service.RetrieveMultiple(query);
        }

        public static EntityCollection GetManyToManyEntityData(IOrganizationService service, string entity1Name, ColumnSet columnSet1, string entity2Name, string intersectEntityName, string intersectEntityAliasName,
                                                            Dictionary<string, string> entity1ConditionAttributes,
                                                            Dictionary<string, string> entity2ConditionAttributes, params string[] columnSetIntersectEntity)
        {
            QueryExpression query = new QueryExpression(entity1Name);
            if (columnSet1 != null)
                query.ColumnSet = columnSet1;
            else
                query.ColumnSet = new ColumnSet(true);

            LinkEntity linkEntity1 = new LinkEntity(entity1Name, intersectEntityName, entity1Name + "id", entity1Name + "id", JoinOperator.Inner);
            LinkEntity linkEntity2 = new LinkEntity(intersectEntityName, entity2Name, entity2Name + "id", entity2Name + "id", JoinOperator.Inner);

            linkEntity1.LinkEntities.Add(linkEntity2);
            query.LinkEntities.Add(linkEntity1);
            if (columnSetIntersectEntity != null && columnSetIntersectEntity.Count() > 0)
            {
                query.LinkEntities[0].Columns.AddColumns(columnSetIntersectEntity);
                query.LinkEntities[0].EntityAlias = intersectEntityAliasName;
            }

            if (entity1ConditionAttributes != null && entity1ConditionAttributes.Count > 0)
            {
                linkEntity1.LinkCriteria = new FilterExpression();

                foreach (var attribute in entity1ConditionAttributes)
                {
                    ConditionExpression Condition = new ConditionExpression();
                    Condition.AttributeName = attribute.Key;
                    Condition.Operator = ConditionOperator.Equal;
                    Condition.Values.Add(attribute.Value);
                    linkEntity1.LinkCriteria.AddCondition(Condition);
                }
            }

            if (entity2ConditionAttributes != null && entity2ConditionAttributes.Count > 0)
            {
                linkEntity2.LinkCriteria = new FilterExpression();

                foreach (var attribute in entity2ConditionAttributes)
                {
                    ConditionExpression Condition = new ConditionExpression();
                    Condition.AttributeName = attribute.Key;
                    Condition.Operator = ConditionOperator.Equal;
                    Condition.Values.Add(attribute.Value);
                    linkEntity2.LinkCriteria.AddCondition(Condition);
                }
            }
            return service.RetrieveMultiple(query);
        }
        
        public static bool IsDivisible(int number, int n)
        {
            if (n <= 0)
            {
                n = 1;
            }
            return (number % n) == 0;
        }

        public static ExecuteTransactionResponse ExecuteTransactionCRM(IOrganizationService service, OrganizationRequestCollection requests)
        {
            ExecuteTransactionResponse response;
            try
            {
                ExecuteTransactionRequest executeTransactionRequest = new ExecuteTransactionRequest()
                {
                    Requests = requests,
                    ReturnResponses = true,
                };
                // Send the request
                response = (ExecuteTransactionResponse)service.Execute(executeTransactionRequest);
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                int index = ((ExecuteTransactionFault)ex.Detail).FaultedRequestIndex + 1;
                string message = ex.Detail.Message;

                throw ex;
            }
            return response;
        }

        public static Entity GetDecisionByCode(IOrganizationService service, string code)
        {
            string fetchDecision = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                        <entity name='ntw_decision'>
                                        <attribute name='ntw_decisionid' />
                                        <attribute name='ntw_name' />
                                        <attribute name='createdon' />
                                        <attribute name='ntw_requeststatusid' />
                                        <order attribute='ntw_name' descending='false' />
                                        <filter type='and'>
                                            <condition attribute='ntw_code' operator='eq' value='{code}' />
                                        </filter>
                                        </entity>
                                    </fetch>";

            EntityCollection resEntCol = service.RetrieveMultiple(new FetchExpression(fetchDecision));
            return (resEntCol == null || resEntCol.Entities.Count <= 0) ? null : resEntCol.Entities[0];
        }

        public static Entity GetRequestStatusByCode(IOrganizationService service, string code)
        {
            string fetchDecision = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                      <entity name='ntw_requeststatus'>
                                        <attribute name='ntw_requeststatusid' />
                                        <attribute name='ntw_name' />
                                        <attribute name='createdon' />
                                        <attribute name='statecode' />
                                        <attribute name='ntw_requesttype' />
                                        <attribute name='ntw_requestsource' />
                                        <attribute name='ntw_relatedentity' />
                                        <attribute name='ntw_relatedentityid' />
                                        <attribute name='ntw_isfinal' />
                                        <attribute name='ntw_internalstatusnamear' />
                                        <attribute name='ntw_externalstatusnamear' />
                                        <attribute name='ntw_externalstatusname' />
                                        <attribute name='ntw_code' />
                                        <filter type='and'>
                                          <condition attribute='ntw_code' operator='eq' value='{code}' />
                                        </filter>
                                      </entity>
                                    </fetch>";

            EntityCollection resEntCol = service.RetrieveMultiple(new FetchExpression(fetchDecision));
            return (resEntCol == null || resEntCol.Entities.Count <= 0) ? null : resEntCol.Entities[0];
        }
        public static string GetSDBConfiguration(IOrganizationService service, string code)
        {
            var URLValue = "";
            QueryExpression query = new QueryExpression("ntw_sdbconfiguratin");
            query.ColumnSet = new ColumnSet("ntw_textvalue");
            query.Criteria.AddCondition("ntw_configurationcode", ConditionOperator.Equal, code);
            var queryResult = service.RetrieveMultiple(query);
            if (queryResult.Entities != null && queryResult.Entities.Count != 0)
            {
                var sdbConfigurationRecord = queryResult.Entities[0];
                URLValue = sdbConfigurationRecord.GetAttributeValue<string>("ntw_textvalue");
            }
            return URLValue;
        }
    }
    public class ConfigurationValue
    {
        public int IntVal { get; set; }
        public string StringVal { get; set; }
        public DateTime DateVal { get; set; }
    }
    public class CrmCondition
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public ConditionOperator ConditionOperator { get; set; }
    }
}
