using LegacyRenewalApp.Models;
using System;

namespace LegacyRenewalApp.Renewals
{
    public class RenewalInvoiceFactory
    {
        public RenewalInvoice Create(
            Customer customer,
            int seatCount,
            string normalizedPlanCode,
            string normalizedPaymentMethod,
            RenewalPricingResult pricingResult)
        {
            DateTime generatedAt = DateTime.UtcNow;

            return new RenewalInvoice
            {
                InvoiceNumber = $"INV-{generatedAt:yyyyMMdd}-{customer.Id}-{normalizedPlanCode}",
                CustomerName = customer.FullName,
                PlanCode = normalizedPlanCode,
                PaymentMethod = normalizedPaymentMethod,
                SeatCount = seatCount,
                BaseAmount = RoundAmount(pricingResult.BaseAmount),
                DiscountAmount = RoundAmount(pricingResult.DiscountAmount),
                SupportFee = RoundAmount(pricingResult.SupportFee),
                PaymentFee = RoundAmount(pricingResult.PaymentFee),
                TaxAmount = RoundAmount(pricingResult.TaxAmount),
                FinalAmount = RoundAmount(pricingResult.FinalAmount),
                Notes = pricingResult.Notes,
                GeneratedAt = generatedAt
            };
        }

        private static decimal RoundAmount(decimal amount)
        {
            return Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        }
    }
}