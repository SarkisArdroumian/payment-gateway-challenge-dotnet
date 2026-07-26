using PaymentGateway.Api.Application.Payments.Commands;
using PaymentGateway.Api.Application.Payments;

namespace PaymentGateway.Api.Application.Abstractions;

public interface IAcquiringBankClient
{
    Task<AcquiringBankPaymentResult> ProcessPaymentAsync(ProcessPaymentCommand command, CancellationToken cancellationToken = default);
}
