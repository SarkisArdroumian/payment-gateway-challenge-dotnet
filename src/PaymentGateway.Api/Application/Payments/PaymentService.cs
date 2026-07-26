using Microsoft.Extensions.Logging;
using PaymentGateway.Api.Application.Abstractions;
using PaymentGateway.Api.Application.Payments.Commands;
using PaymentGateway.Api.Application.Payments.Queries;
using PaymentGateway.Api.Domain;

namespace PaymentGateway.Api.Application.Payments;

public class PaymentService : IPaymentService
{
    private readonly IAcquiringBankClient _acquiringBankClient;
    private readonly IPaymentsRepository _paymentsRepository;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IAcquiringBankClient acquiringBankClient,
        IPaymentsRepository paymentsRepository,
        ILogger<PaymentService> logger)
    {
        _acquiringBankClient = acquiringBankClient;
        _paymentsRepository = paymentsRepository;
        _logger = logger;
    }

    public Task<Payment?> GetPaymentAsync(GetPaymentQuery query, CancellationToken cancellationToken = default)
    {
        var payment = _paymentsRepository.Get(query.Id);

        if (payment is null)
        {
            _logger.LogWarning("Payment {PaymentId} was not found in the repository.", query.Id);
        }
        else
        {
            _logger.LogInformation(
                "Payment {PaymentId} was loaded from the repository with status {Status}.",
                payment.Id,
                payment.Status);
        }

        return Task.FromResult(payment);
    }

    public async Task<Payment> ProcessPaymentAsync(ProcessPaymentCommand command, CancellationToken cancellationToken = default)
    {
        var normalizedCurrency = command.Currency.Trim().ToUpperInvariant();
        var lastFour = command.CardNumber[^4..];

        _logger.LogInformation(
            "Starting payment processing for amount {Amount} {Currency} with card ending {LastFour}.",
            command.Amount,
            normalizedCurrency,
            lastFour);

        var status = await _acquiringBankClient.ProcessPaymentAsync(command, cancellationToken);

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Status = status,
            LastFour = lastFour,
            ExpiryMonth = command.ExpiryMonth,
            ExpiryYear = command.ExpiryYear,
            Currency = normalizedCurrency,
            Amount = command.Amount
        };

        _paymentsRepository.Add(payment);

        _logger.LogInformation(
            "Payment {PaymentId} stored with status {Status} for card ending {LastFour}.",
            payment.Id,
            payment.Status,
            payment.LastFour);

        return payment;
    }
}
