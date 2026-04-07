using LegacyRenewalApp.Models;

namespace LegacyRenewalApp.Email
{
    public interface IRenewalEmailService
    {
        void SendRenewalInvoice(Customer customer, string normalizedPlanCode, RenewalInvoice invoice);
    }
}