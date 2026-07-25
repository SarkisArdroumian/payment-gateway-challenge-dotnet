using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;

namespace PaymentGateway.Api.Services;

public interface IPaymentService
{
    Task<Payment?> GetPaymentAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Payment> ProcessPaymentAsync(PostPaymentRequest request, CancellationToken cancellationToken = default);
}
