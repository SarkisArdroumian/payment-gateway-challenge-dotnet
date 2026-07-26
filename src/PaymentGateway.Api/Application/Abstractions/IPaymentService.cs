using PaymentGateway.Api.Application.Payments.Commands;
using PaymentGateway.Api.Application.Payments.Queries;
using PaymentGateway.Api.Domain;

namespace PaymentGateway.Api.Application.Abstractions;

public interface IPaymentService
{
    Task<Payment?> GetPaymentAsync(GetPaymentQuery query, CancellationToken cancellationToken = default);
    Task<Payment> ProcessPaymentAsync(ProcessPaymentCommand command, CancellationToken cancellationToken = default);
}
