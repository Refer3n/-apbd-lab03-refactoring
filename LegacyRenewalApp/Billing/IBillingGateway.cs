using LegacyRenewalApp.Models;

namespace LegacyRenewalApp.Billing
{
    public interface IBillingGateway
    {
        void SaveInvoice(RenewalInvoice invoice);

        void SendEmail(string email, string subject, string body);
    }
}