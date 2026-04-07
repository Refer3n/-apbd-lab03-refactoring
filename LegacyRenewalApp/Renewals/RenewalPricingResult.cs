namespace LegacyRenewalApp.Renewals
{
    public class RenewalPricingResult
    {
        public decimal BaseAmount { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal SupportFee { get; set; }

        public decimal PaymentFee { get; set; }

        public decimal TaxAmount { get; set; }

        public decimal FinalAmount { get; set; }

        public string Notes { get; set; } = string.Empty;
    }
}