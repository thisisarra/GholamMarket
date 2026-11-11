namespace Application.Store;

public interface IPaymentGateway
{
    string Name { get; }

    Task<PaymentVerification> VerifyAsync(
        PaymentVerificationRequest request,
        CancellationToken cancellationToken);
}

public sealed record PaymentVerificationRequest(
    int PaymentId,
    string OrderNumber,
    decimal Amount,
    bool ApprovedBySimulator,
    string? GatewayReference);

public sealed record PaymentVerification(
    bool Succeeded,
    string Reference,
    string? FailureReason);
