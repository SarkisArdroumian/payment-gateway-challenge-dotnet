using PaymentGateway.Api.Application.Abstractions;
using PaymentGateway.Api.Application.Payments.Commands;
using PaymentGateway.Api.Application.Payments.Queries;
using PaymentGateway.Api.Domain;

namespace PaymentGateway.Api.Application.Payments;

public class PaymentService : IPaymentService
{
    private readonly IAcquiringBankClient _acquiringBankClient;
    private readonly IPaymentsRepository _paymentsRepository;

    public PaymentService(IAcquiringBankClient acquiringBankClient, IPaymentsRepository paymentsRepository)
    {
        _acquiringBankClient = acquiringBankClient;
        _paymentsRepository = paymentsRepository;
    }

    public Task<Payment?> GetPaymentAsync(GetPaymentQuery query, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_paymentsRepository.Get(query.Id));
    }

    public async Task<Payment> ProcessPaymentAsync(ProcessPaymentCommand command, CancellationToken cancellationToken = default)
    {
        var status = await _acquiringBankClient.ProcessPaymentAsync(command, cancellationToken);

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Status = status,
            LastFour = command.CardNumber[^4..],
            ExpiryMonth = command.ExpiryMonth,
            ExpiryYear = command.ExpiryYear,
            Currency = command.Currency.Trim().ToUpperInvariant(),
            Amount = command.Amount
        };

        _paymentsRepository.Add(payment);

        return payment;
    }
}
