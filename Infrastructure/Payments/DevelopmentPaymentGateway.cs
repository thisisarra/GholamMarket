using Application.Store;

namespace Infrastructure.Payments;

public sealed class DevelopmentPaymentGateway : IPaymentGateway
{
    public string Name => "Development";

    public Task<PaymentVerification> VerifyAsync(
        PaymentVerificationRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
        {
            return Task.FromResult(new PaymentVerification(
                false,
                request.GatewayReference ?? $"DEV-{request.PaymentId}",
                "مبلغ پرداخت نامعتبر است."));
        }

        var reference = string.IsNullOrWhiteSpace(request.GatewayReference)
            ? $"DEV-{request.OrderNumber}-{request.PaymentId}"
            : request.GatewayReference.Trim();

        if (!request.ApprovedBySimulator)
        {
            return Task.FromResult(new PaymentVerification(
                false,
                reference,
                "پرداخت در درگاه آزمایشی لغو شد."));
        }

        return Task.FromResult(new PaymentVerification(true, reference, null));
    }
}
