using System.Collections.Concurrent;

using PaymentGateway.Api.Application.Abstractions;
using PaymentGateway.Api.Domain;

namespace PaymentGateway.Api.Infrastructure.Persistence;

public class PaymentsRepository : IPaymentsRepository
{
    private readonly ConcurrentDictionary<Guid, Payment> _payments = new();

    public void Add(Payment payment)
    {
        if (!_payments.TryAdd(payment.Id, payment))
        {
            throw new DuplicatePaymentIdException(payment.Id);
        }
    }

    public Payment? Get(Guid id)
    {
        return _payments.GetValueOrDefault(id);
    }
}
