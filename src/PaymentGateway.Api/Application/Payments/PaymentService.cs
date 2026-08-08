using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;

using Microsoft.Extensions.Logging;
using PaymentGateway.Api.Application.Abstractions;
using PaymentGateway.Api.Application.Payments.Commands;
using PaymentGateway.Api.Application.Payments.Queries;
using PaymentGateway.Api.Domain;

namespace PaymentGateway.Api.Application.Payments;

public class PaymentService : IPaymentService
{
    private readonly IAcquiringBankClient _acquiringBankClient;
    private readonly IIdempotencyRepository _idempotencyRepository;
    private readonly IPaymentsRepository _paymentsRepository;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IAcquiringBankClient acquiringBankClient,
        IIdempotencyRepository idempotencyRepository,
        IPaymentsRepository paymentsRepository,
        ILogger<PaymentService> logger)
    {
        _acquiringBankClient = acquiringBankClient;
        _idempotencyRepository = idempotencyRepository;
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
        var requestFingerprint = ComputeRequestFingerprint(command, normalizedCurrency);

        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["TraceId"] = Activity.Current?.TraceId.ToString(),
            ["IdempotencyKey"] = command.IdempotencyKey,
            ["CardLastFour"] = lastFour,
            ["Amount"] = command.Amount,
            ["Currency"] = normalizedCurrency
        });

        _logger.LogInformation(
            "Starting payment processing for amount {Amount} {Currency} with card ending {LastFour}.",
            command.Amount,
            normalizedCurrency,
            lastFour);

        if (!string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            var existingRecord = _idempotencyRepository.Get(command.IdempotencyKey);

            if (existingRecord is not null)
            {
                if (!string.Equals(existingRecord.RequestFingerprint, requestFingerprint, StringComparison.Ordinal))
                {
                    _logger.LogWarning(
                        "Idempotency key {IdempotencyKey} was reused with a different request fingerprint.",
                        command.IdempotencyKey);

                    throw new IdempotencyConflictException(command.IdempotencyKey);
                }

                var existingPayment = _paymentsRepository.Get(existingRecord.PaymentId)
                    ?? throw new InvalidOperationException($"A payment mapped to idempotency key '{command.IdempotencyKey}' could not be found.");

                _logger.LogInformation(
                    "Returning existing payment {PaymentId} for idempotency key {IdempotencyKey}.",
                    existingPayment.Id,
                    command.IdempotencyKey);

                return existingPayment;
            }
        }

        var bankPaymentResult = await _acquiringBankClient.ProcessPaymentAsync(command, cancellationToken);

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Status = bankPaymentResult.Status,
            AuthorizationCode = bankPaymentResult.AuthorizationCode,
            LastFour = lastFour,
            ExpiryMonth = command.ExpiryMonth,
            ExpiryYear = command.ExpiryYear,
            Currency = normalizedCurrency,
            Amount = command.Amount
        };

        _paymentsRepository.Add(payment);

        if (!string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            var idempotencyRecord = new IdempotencyRecord
            {
                IdempotencyKey = command.IdempotencyKey,
                RequestFingerprint = requestFingerprint,
                PaymentId = payment.Id
            };

            if (!_idempotencyRepository.TryAdd(idempotencyRecord))
            {
                var existingRecord = _idempotencyRepository.Get(command.IdempotencyKey)
                    ?? throw new InvalidOperationException($"An idempotency record for key '{command.IdempotencyKey}' was expected but not found.");

                if (!string.Equals(existingRecord.RequestFingerprint, requestFingerprint, StringComparison.Ordinal))
                {
                    throw new IdempotencyConflictException(command.IdempotencyKey);
                }

                var existingPayment = _paymentsRepository.Get(existingRecord.PaymentId)
                    ?? throw new InvalidOperationException($"A payment mapped to idempotency key '{command.IdempotencyKey}' could not be found.");

                _logger.LogInformation(
                    "Returning existing payment {PaymentId} after detecting a duplicate idempotency key {IdempotencyKey} during persistence.",
                    existingPayment.Id,
                    command.IdempotencyKey);

                return existingPayment;
            }
        }

        _logger.LogInformation(
            "Payment {PaymentId} stored with status {Status} for card ending {LastFour}. Authorization code present: {HasAuthorizationCode}.",
            payment.Id,
            payment.Status,
            payment.LastFour,
            !string.IsNullOrWhiteSpace(payment.AuthorizationCode));

        return payment;
    }

    private static string ComputeRequestFingerprint(ProcessPaymentCommand command, string normalizedCurrency)
    {
        var rawFingerprint = string.Join('|',
            command.CardNumber,
            command.ExpiryMonth,
            command.ExpiryYear,
            normalizedCurrency,
            command.Amount,
            command.Cvv);

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawFingerprint));
        return Convert.ToHexString(hashBytes);
    }
}
