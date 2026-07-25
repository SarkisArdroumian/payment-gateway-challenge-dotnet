using PaymentGateway.Api.Domain;
using PaymentGateway.Api.Models.Requests;

namespace PaymentGateway.Api.Application;

public interface IPaymentService
{
    Task<Payment?> GetPaymentAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Payment> ProcessPaymentAsync(PostPaymentRequest request, CancellationToken cancellationToken = default);
}
