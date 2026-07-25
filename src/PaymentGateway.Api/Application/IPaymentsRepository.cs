using PaymentGateway.Api.Domain;

namespace PaymentGateway.Api.Application;

public interface IPaymentsRepository
{
    void Add(Payment payment);
    Payment? Get(Guid id);
}
