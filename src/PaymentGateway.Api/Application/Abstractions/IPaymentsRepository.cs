using PaymentGateway.Api.Domain;

namespace PaymentGateway.Api.Application.Abstractions;

public interface IPaymentsRepository
{
    void Add(Payment payment);
    Payment? Get(Guid id);
}
