using LegacyRenewalApp.Models;
using LegacyRenewalApp.Renewals;
using System;
using System.Collections.Generic;
using System.Text;

namespace LegacyRenewalApp.Renewals
{
    public class RenewalPricingCalculator
    {
        private const decimal DefaultTaxRate = 0.20m;
        private const decimal MinimumDiscountedSubtotal = 300m;
        private const decimal MinimumInvoiceAmount = 500m;
        private const int MaxLoyaltyPointsToUse = 200;

        private static readonly Dictionary<string, decimal> SegmentDiscountRates = new()
        {
            ["Silver"] = 0.05m,
            ["Gold"] = 0.10m,
            ["Platinum"] = 0.15m
        };

        private static readonly Dictionary<string, decimal> SupportFeesByPlanCode = new()
        {
            ["START"] = 250m,
            ["PRO"] = 400m,
            ["ENTERPRISE"] = 700m
        };

        private static readonly Dictionary<string, decimal> PaymentFeeRates = new()
        {
            ["CARD"] = 0.02m,
            ["BANK_TRANSFER"] = 0.01m,
            ["PAYPAL"] = 0.035m,
            ["INVOICE"] = 0m
        };

        private static readonly Dictionary<string, decimal> TaxRatesByCountry = new()
        {
            ["Poland"] = 0.23m,
            ["Germany"] = 0.19m,
            ["Czech Republic"] = 0.21m,
            ["Norway"] = 0.25m
        };

        private static readonly (int MinimumSeatCount, decimal Rate, string Note)[] SeatDiscountRules =
        {
            (50, 0.12m, "large team discount; "),
            (20, 0.08m, "medium team discount; "),
            (10, 0.04m, "small team discount; ")
        };

        private static readonly (int MinimumYears, decimal Rate, string Note)[] LoyaltyDiscountRules =
        {
            (5, 0.07m, "long-term loyalty discount; "),
            (2, 0.03m, "basic loyalty discount; ")
        };

        public RenewalPricingResult Calculate(
            Customer customer,
            SubscriptionPlan plan,
            int seatCount,
            string normalizedPlanCode,
            string normalizedPaymentMethod,
            bool includePremiumSupport,
            bool useLoyaltyPoints)
        {
            decimal baseAmount = CalculateBaseAmount(plan, seatCount);
            decimal discountAmount = 0m;
            StringBuilder notesBuilder = new StringBuilder();

            discountAmount += CalculateSegmentDiscount(customer, plan, baseAmount, notesBuilder);
            discountAmount += CalculateYearsWithCompanyDiscount(customer, baseAmount, notesBuilder);
            discountAmount += CalculateSeatCountDiscount(seatCount, baseAmount, notesBuilder);
            discountAmount += CalculateLoyaltyPointsDiscount(customer, useLoyaltyPoints, notesBuilder);

            decimal subtotalAfterDiscount = baseAmount - discountAmount;
            if (subtotalAfterDiscount < MinimumDiscountedSubtotal)
            {
                subtotalAfterDiscount = MinimumDiscountedSubtotal;
                notesBuilder.Append("minimum discounted subtotal applied; ");
            }

            decimal supportFee = CalculateSupportFee(includePremiumSupport, normalizedPlanCode, notesBuilder);
            decimal paymentFee = CalculatePaymentFee(subtotalAfterDiscount, supportFee, normalizedPaymentMethod, notesBuilder);

            decimal taxRate = GetTaxRate(customer.Country);
            decimal taxBase = subtotalAfterDiscount + supportFee + paymentFee;
            decimal taxAmount = taxBase * taxRate;
            decimal finalAmount = taxBase + taxAmount;

            if (finalAmount < MinimumInvoiceAmount)
            {
                finalAmount = MinimumInvoiceAmount;
                notesBuilder.Append("minimum invoice amount applied; ");
            }

            return new RenewalPricingResult
            {
                BaseAmount = baseAmount,
                DiscountAmount = discountAmount,
                SupportFee = supportFee,
                PaymentFee = paymentFee,
                TaxAmount = taxAmount,
                FinalAmount = finalAmount,
                Notes = notesBuilder.ToString().Trim()
            };
        }

        private static decimal CalculateBaseAmount(SubscriptionPlan plan, int seatCount)
        {
            return (plan.MonthlyPricePerSeat * seatCount * 12m) + plan.SetupFee;
        }

        private static decimal CalculateSegmentDiscount(
            Customer customer,
            SubscriptionPlan plan,
            decimal baseAmount,
            StringBuilder notesBuilder)
        {
            if (customer.Segment == "Education" && plan.IsEducationEligible)
            {
                notesBuilder.Append("education discount; ");
                return baseAmount * 0.20m;
            }

            if (SegmentDiscountRates.TryGetValue(customer.Segment, out decimal rate))
            {
                notesBuilder.Append($"{customer.Segment.ToLowerInvariant()} discount; ");
                return baseAmount * rate;
            }

            return 0m;
        }

        private static decimal CalculateYearsWithCompanyDiscount(Customer customer, decimal baseAmount, StringBuilder notesBuilder)
        {
            foreach (var rule in LoyaltyDiscountRules)
            {
                if (customer.YearsWithCompany >= rule.MinimumYears)
                {
                    notesBuilder.Append(rule.Note);
                    return baseAmount * rule.Rate;
                }
            }

            return 0m;
        }

        private static decimal CalculateSeatCountDiscount(int seatCount, decimal baseAmount, StringBuilder notesBuilder)
        {
            foreach (var rule in SeatDiscountRules)
            {
                if (seatCount >= rule.MinimumSeatCount)
                {
                    notesBuilder.Append(rule.Note);
                    return baseAmount * rule.Rate;
                }
            }

            return 0m;
        }

        private static decimal CalculateLoyaltyPointsDiscount(
            Customer customer,
            bool useLoyaltyPoints,
            StringBuilder notesBuilder)
        {
            if (!useLoyaltyPoints || customer.LoyaltyPoints <= 0)
            {
                return 0m;
            }

            int pointsToUse = Math.Min(customer.LoyaltyPoints, MaxLoyaltyPointsToUse);
            notesBuilder.Append($"loyalty points used: {pointsToUse}; ");
            return pointsToUse;
        }

        private static decimal CalculateSupportFee(
            bool includePremiumSupport,
            string normalizedPlanCode,
            StringBuilder notesBuilder)
        {
            if (!includePremiumSupport)
            {
                return 0m;
            }

            decimal supportFee = 0m;
            if (SupportFeesByPlanCode.TryGetValue(normalizedPlanCode, out decimal configuredSupportFee))
            {
                supportFee = configuredSupportFee;
            }

            notesBuilder.Append("premium support included; ");
            return supportFee;
        }

        private static decimal CalculatePaymentFee(
            decimal subtotalAfterDiscount,
            decimal supportFee,
            string normalizedPaymentMethod,
            StringBuilder notesBuilder)
        {
            if (!PaymentFeeRates.TryGetValue(normalizedPaymentMethod, out decimal feeRate))
            {
                throw new ArgumentException("Unsupported payment method");
            }

            decimal paymentBase = subtotalAfterDiscount + supportFee;
            decimal paymentFee = paymentBase * feeRate;

            switch (normalizedPaymentMethod)
            {
                case "CARD":
                    notesBuilder.Append("card payment fee; ");
                    break;
                case "BANK_TRANSFER":
                    notesBuilder.Append("bank transfer fee; ");
                    break;
                case "PAYPAL":
                    notesBuilder.Append("paypal fee; ");
                    break;
                case "INVOICE":
                    notesBuilder.Append("invoice payment; ");
                    break;
            }

            return paymentFee;
        }

        private static decimal GetTaxRate(string country)
        {
            if (TaxRatesByCountry.TryGetValue(country, out decimal taxRate))
            {
                return taxRate;
            }

            return DefaultTaxRate;
        }
    }
}
