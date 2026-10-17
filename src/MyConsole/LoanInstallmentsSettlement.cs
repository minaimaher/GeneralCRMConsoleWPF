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
    public class LoanInstallmentsSettlement : CodeActivity
    {
        [Input("Loan Settelment")]
        [ReferenceTarget("ntw_productivefamilyloansettlement")]
        public InArgument<EntityReference> LoanSettelment { get; set; }

        protected override void Execute(CodeActivityContext executionContext)
        {
            var context = executionContext.GetExtension<IWorkflowContext>();
            var serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            var service = serviceFactory.CreateOrganizationService(context.UserId);
            ITracingService tracingService = executionContext.GetExtension<ITracingService>();

            var loanSettlment = CrmHelper.RetrieveEntityById(service, "ntw_productivefamilyloansettlement", LoanSettelment.Get(executionContext).Id, new ColumnSet("ntw_paymentmethod", "ntw_paidamount", "ntw_settlementdate"), false);

            var paymentMethod = loanSettlment.GetAttributeValue<OptionSetValue>("ntw_paymentmethod").Value;
            var settlmentAmount = loanSettlment.GetAttributeValue<Money>("ntw_paidamount").Value;
            var settlementDate = loanSettlment.GetAttributeValue<DateTime>("ntw_settlementdate");
            var remainingTemp = settlmentAmount;

            var fetch = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                          <entity name='ntw_loansettlementinstallments'>
                            <attribute name='ntw_loansettlementinstallmentsid' />
                            <attribute name='ntw_loaninstallment' />
                            <order attribute='ntw_loaninstallment' descending='false' />
                            <filter type='and'>
                              <condition attribute='ntw_loansettlement' operator='eq' uitype='ntw_productivefamilyloansettlement' value='" + LoanSettelment.Get(executionContext).Id.ToString() + @"' />
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
                    installmentPaymentHistory["ntw_paymentmethod"] = new OptionSetValue(paymentMethod);
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
                    installmentPaymentHistory["ntw_paymentmethod"] = new OptionSetValue(paymentMethod);
                    installmentPaymentHistory["ntw_paymentdate"] = settlementDate;
                    service.Create(installmentPaymentHistory);

                }

            }

            #region old code
            //var loanSettlment = CrmHelper.RetrieveEntityById(service, "ntw_productivefamilyloansettlement", LoanSettelment.Get(executionContext).Id, new ColumnSet("ntw_paymentmethod"), false);

            //var paymentMethod = loanSettlment.GetAttributeValue<OptionSetValue>("ntw_paymentmethod").Value;


            //var fetch = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
            //              <entity name='ntw_loansettlementinstallments'>
            //                <attribute name='ntw_loansettlementinstallmentsid' />
            //                <attribute name='ntw_loaninstallment' />
            //                <order attribute='ntw_loaninstallment' descending='false' />
            //                <filter type='and'>
            //                  <condition attribute='ntw_loansettlement' operator='eq' uitype='ntw_productivefamilyloansettlement' value='"+ LoanSettelment.Get(executionContext).Id + @"' />
            //                </filter>
            //                <link-entity name='ntw_loaninstallmentschedule' from='ntw_loaninstallmentscheduleid' to='ntw_loaninstallment' visible='false' link-type='outer' alias='Inst'>
            //                  <attribute name='ntw_remainingamount' />
            //                  <attribute name='ntw_paidamount' />
            //                  <attribute name='ntw_amount' />
            //                </link-entity>
            //              </entity>
            //            </fetch>";

            //var InstallmentCollection = service.RetrieveMultiple(new FetchExpression(fetch));

            //foreach (var installment in InstallmentCollection.Entities)
            //{
            //    var installmentId = installment.GetAttributeValue<EntityReference>("ntw_loaninstallment").Id;
            //    var amount = installment.Attributes.Contains("Inst.ntw_amount") ?
            //                            ((Money)installment.GetAttributeValue<AliasedValue>("Inst.ntw_amount").Value).Value : 0;
            //    var remainingAmount = installment.Attributes.Contains("Inst.ntw_remainingamount") ?
            //                            ((Money)installment.GetAttributeValue<AliasedValue>("Inst.ntw_remainingamount").Value).Value : 0;
            //    if (remainingAmount == 0)
            //    {
            //        remainingAmount = amount;
            //    }

            //    Entity installmentPaymentHistory = new Entity("ntw_installmentpayment");
            //    installmentPaymentHistory["ntw_loaninstallment"] = new EntityReference("ntw_loaninstallmentschedule", installmentId);
            //    installmentPaymentHistory["ntw_paidamount"] = new Money(remainingAmount);
            //    installmentPaymentHistory["ntw_paymentmethod"] = new OptionSetValue(paymentMethod);

            //    service.Create(installmentPaymentHistory);
            //}

            #endregion 
        }
    }
}
