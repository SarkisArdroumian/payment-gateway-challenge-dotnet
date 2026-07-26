using PaymentGateway.Api.Application.Payments.Commands;
using PaymentGateway.Api.Domain;

namespace PaymentGateway.Api.Application.Abstractions;

public interface IAcquiringBankClient
{
    Task<PaymentStatus> ProcessPaymentAsync(ProcessPaymentCommand command, CancellationToken cancellationToken = default);
}
