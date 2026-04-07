using LegacyRenewalApp.Billing;
using LegacyRenewalApp.Models;
using System;

namespace LegacyRenewalApp.Email
{
    public class RenewalEmailService : IRenewalEmailService
    {
        private readonly IBillingGateway billingGateway;

        public RenewalEmailService(IBillingGateway billingGateway)
        {
            this.billingGateway = billingGateway ?? throw new ArgumentNullException(nameof(billingGateway));
        }

        public void SendRenewalInvoice(Customer customer, string normalizedPlanCode, RenewalInvoice invoice)
        {
            if (string.IsNullOrWhiteSpace(customer.Email))
            {
                return;
            }

            string subject = "Subscription renewal invoice";
            string body =
                $"Hello {customer.FullName}, your renewal for plan {normalizedPlanCode} " +
                $"has been prepared. Final amount: {invoice.FinalAmount:F2}.";

            this.billingGateway.SendEmail(customer.Email, subject, body);
        }
    }
}