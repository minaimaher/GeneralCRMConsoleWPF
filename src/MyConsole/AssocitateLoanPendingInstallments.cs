using System;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk.Messages;
using System.Linq;

namespace SDBCRM.CustomActivities
{
    public class AssocitateLoanPendingInstallments : CodeActivity
    {
        [Input("Loan")]
        [ReferenceTarget("ntw_loan")]
        public InArgument<EntityReference> Loan { get; set; }

        [Input("Received Amount")]
        public InArgument<Money> ReceivedAmount { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            // Getting OrganizationService from Context
            var workflowContext = context.GetExtension<IWorkflowContext>();
            var serviceFactory = context.GetExtension<IOrganizationServiceFactory>();
            var service = serviceFactory.CreateOrganizationService(workflowContext.UserId);

            var settlmentAmount = ReceivedAmount.Get<Money>(context).Value;
            //


            DeleteLoanPendingInstallments(service, workflowContext.PrimaryEntityId);

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
                                      <condition attribute='ntw_loan' operator='eq' uitype='ntw_loan' value='" + Loan.Get(context).Id.ToString() + @"' />
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

            //get number of installments
            var loanAmountCheck = InstallmentsCollection.Entities[0].Attributes.Contains("ntw_amount") ?
                                        InstallmentsCollection.Entities[0].GetAttributeValue<Money>("ntw_amount").Value : 0;
            List<Entity> installmentDataCollection = new List<Entity> { };

            if (settlmentAmount <= loanAmountCheck)
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

                if (!IsExisted(service, workflowContext.PrimaryEntityId, installment.Id))
                {
                    Entity loanSettelmentInstallment = new Entity("ntw_loansettlementinstallments");

                    loanSettelmentInstallment["ntw_loansettlement"] = new EntityReference("ntw_productivefamilyloansettlement", workflowContext.PrimaryEntityId);
                    loanSettelmentInstallment["ntw_loaninstallment"] = new EntityReference("ntw_loaninstallmentschedule", installment.Id);

                    service.Create(loanSettelmentInstallment);
                }
            }


            #region old code
            //string fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
            //                      <entity name='ntw_loaninstallmentschedule'>
            //                        <attribute name='ntw_loaninstallmentscheduleid' />
            //                        <attribute name='createdon' />
            //                        <attribute name='ntw_installmentno' />
            //                        <attribute name='ntw_remainingamount' />
            //                        <attribute name='ntw_paidamount' />
            //                        <attribute name='ntw_amount' />
            //                        <order attribute='ntw_installmentno' descending='false' />
            //                        <filter type='and'>
            //                          <condition attribute='ntw_loan' operator='eq' uitype='ntw_loan' value='" + Loan.Get(context).Id + @"' />
            //                          <condition attribute='statuscode' operator='not-in'>
            //                            <value>961110000</value>
            //                            <value>2</value>
            //                          </condition>
            //                        </filter>
            //                      </entity>
            //                    </fetch>";

            //var receivedAmount = ReceivedAmount.Get(context).Value;
            //decimal tempAmount = 0;

            //EntityCollection InstallmentsCollection = service.RetrieveMultiple(new FetchExpression(fetchXml));

            //foreach (var installment in InstallmentsCollection.Entities)
            //{
            //    var amount = installment.Attributes.Contains("ntw_amount") ?
            //                            installment.GetAttributeValue<Money>("ntw_amount").Value : 0;
            //    var paidAmount = installment.Attributes.Contains("ntw_paidamount") ?
            //                            installment.GetAttributeValue<Money>("ntw_paidamount").Value : 0;
            //    var remainingAmount = installment.Attributes.Contains("ntw_remainingamount") ?
            //                            installment.GetAttributeValue<Money>("ntw_remainingamount").Value : 0;

            //    if (remainingAmount == 0)
            //    {
            //        remainingAmount = amount;
            //    }

            //    tempAmount += remainingAmount;

            //    if (tempAmount <= receivedAmount)
            //    {
            //        //create many to many relation record on Loan Settelment Installments

            //        if (!IsExisted(service, workflowContext.PrimaryEntityId, installment.Id))
            //        {
            //            Entity loanSettelmentInstallment = new Entity("ntw_loansettlementinstallments");

            //            loanSettelmentInstallment["ntw_loansettlement"] = new EntityReference("ntw_productivefamilyloansettlement", workflowContext.PrimaryEntityId);
            //            loanSettelmentInstallment["ntw_loaninstallment"] = new EntityReference("ntw_loaninstallmentschedule", installment.Id);

            //            service.Create(loanSettelmentInstallment);
            //        }
            //    }
            //    else
            //    {
            //        break;
            //    }
            //}
            #endregion
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

        private static void DeleteLoanPendingInstallments(IOrganizationService service, Guid loanSettlementId)
        {
            QueryExpression query = new QueryExpression("ntw_loansettlementinstallments");
            query.ColumnSet = new ColumnSet(true);
            query.Criteria.AddCondition("ntw_loansettlement", ConditionOperator.Equal, loanSettlementId);
            EntityCollection pendingInstallmentsCollection = service.RetrieveMultiple(query);
            if (pendingInstallmentsCollection.Entities.Any())
            {
                foreach (var item in pendingInstallmentsCollection.Entities)
                {
                    service.Delete("ntw_loansettlementinstallments", item.Id);
                }
            }
        }

    }
}