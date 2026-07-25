using PaymentGateway.Api.Domain;
using PaymentGateway.Api.Models.Requests;

namespace PaymentGateway.Api.Application;

public class PaymentService : IPaymentService
{
    private readonly IAcquiringBankClient _acquiringBankClient;
    private readonly IPaymentsRepository _paymentsRepository;

    public PaymentService(IAcquiringBankClient acquiringBankClient, IPaymentsRepository paymentsRepository)
    {
        _acquiringBankClient = acquiringBankClient;
        _paymentsRepository = paymentsRepository;
    }

    public Task<Payment?> GetPaymentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_paymentsRepository.Get(id));
    }

    public async Task<Payment> ProcessPaymentAsync(PostPaymentRequest request, CancellationToken cancellationToken = default)
    {
        var status = await _acquiringBankClient.ProcessPaymentAsync(request, cancellationToken);

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Status = status,
            LastFour = request.CardNumber[^4..],
            ExpiryMonth = request.ExpiryMonth,
            ExpiryYear = request.ExpiryYear,
            Currency = request.Currency.Trim().ToUpperInvariant(),
            Amount = request.Amount
        };

        _paymentsRepository.Add(payment);

        return payment;
    }
}
