using PaymentGateway.Api.Domain;
using PaymentGateway.Api.Models.Requests;

namespace PaymentGateway.Api.Application;

public interface IAcquiringBankClient
{
    Task<PaymentStatus> ProcessPaymentAsync(PostPaymentRequest request, CancellationToken cancellationToken = default);
}
