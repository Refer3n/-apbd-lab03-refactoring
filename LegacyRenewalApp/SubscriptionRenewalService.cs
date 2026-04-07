using LegacyRenewalApp.Billing;
using LegacyRenewalApp.Email;
using LegacyRenewalApp.Models;
using LegacyRenewalApp.Repositories;
using LegacyRenewalApp.Renewals;
using System;

namespace LegacyRenewalApp
{
    public class SubscriptionRenewalService
    {
        private readonly CustomerRepository customerRepository;
        private readonly SubscriptionPlanRepository planRepository;
        private readonly RenewalPricingCalculator pricingCalculator;
        private readonly RenewalInvoiceFactory invoiceFactory;
        private readonly IRenewalEmailService renewalEmailService;
        private readonly IBillingGateway billingGateway;

        public SubscriptionRenewalService()
            : this(
                new CustomerRepository(),
                new SubscriptionPlanRepository(),
                new RenewalPricingCalculator(),
                new RenewalInvoiceFactory(),
                new LegacyBillingGatewayAdapter())
        {
        }

        private SubscriptionRenewalService(
            CustomerRepository customerRepository,
            SubscriptionPlanRepository planRepository,
            RenewalPricingCalculator pricingCalculator,
            RenewalInvoiceFactory invoiceFactory,
            IBillingGateway billingGateway)
            : this(
                customerRepository,
                planRepository,
                pricingCalculator,
                invoiceFactory,
                new RenewalEmailService(billingGateway),
                billingGateway)
        {
        }

        public SubscriptionRenewalService(
            CustomerRepository customerRepository,
            SubscriptionPlanRepository planRepository,
            RenewalPricingCalculator pricingCalculator,
            RenewalInvoiceFactory invoiceFactory,
            IRenewalEmailService renewalEmailService,
            IBillingGateway billingGateway)
        {
            this.customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
            this.planRepository = planRepository ?? throw new ArgumentNullException(nameof(planRepository));
            this.pricingCalculator = pricingCalculator ?? throw new ArgumentNullException(nameof(pricingCalculator));
            this.invoiceFactory = invoiceFactory ?? throw new ArgumentNullException(nameof(invoiceFactory));
            this.renewalEmailService = renewalEmailService ?? throw new ArgumentNullException(nameof(renewalEmailService));
            this.billingGateway = billingGateway ?? throw new ArgumentNullException(nameof(billingGateway));
        }

        public RenewalInvoice CreateRenewalInvoice(
            int customerId,
            string planCode,
            int seatCount,
            string paymentMethod,
            bool includePremiumSupport,
            bool useLoyaltyPoints)
        {
            ValidateInputs(customerId, planCode, seatCount, paymentMethod);

            string normalizedPlanCode = NormalizePlanCode(planCode);
            string normalizedPaymentMethod = NormalizePaymentMethod(paymentMethod);

            Customer customer = this.customerRepository.GetById(customerId);
            SubscriptionPlan plan = this.planRepository.GetByCode(normalizedPlanCode);

            EnsureCustomerCanRenew(customer);

            RenewalPricingResult pricingResult = this.pricingCalculator.Calculate(
                customer,
                plan,
                seatCount,
                normalizedPlanCode,
                normalizedPaymentMethod,
                includePremiumSupport,
                useLoyaltyPoints);

            RenewalInvoice invoice = this.invoiceFactory.Create(
                customer,
                seatCount,
                normalizedPlanCode,
                normalizedPaymentMethod,
                pricingResult);

            this.billingGateway.SaveInvoice(invoice);
            this.renewalEmailService.SendRenewalInvoice(customer, normalizedPlanCode, invoice);

            return invoice;
        }

        private static void ValidateInputs(
            int customerId,
            string planCode,
            int seatCount,
            string paymentMethod)
        {
            if (customerId <= 0)
            {
                throw new ArgumentException("Customer id must be positive");
            }

            if (string.IsNullOrWhiteSpace(planCode))
            {
                throw new ArgumentException("Plan code is required");
            }

            if (seatCount <= 0)
            {
                throw new ArgumentException("Seat count must be positive");
            }

            if (string.IsNullOrWhiteSpace(paymentMethod))
            {
                throw new ArgumentException("Payment method is required");
            }
        }

        private static string NormalizePlanCode(string planCode)
        {
            return planCode.Trim().ToUpperInvariant();
        }

        private static string NormalizePaymentMethod(string paymentMethod)
        {
            return paymentMethod.Trim().ToUpperInvariant();
        }

        private static void EnsureCustomerCanRenew(Customer customer)
        {
            if (!customer.IsActive)
            {
                throw new InvalidOperationException("Inactive customers cannot renew subscriptions");
            }
        }
    }
}