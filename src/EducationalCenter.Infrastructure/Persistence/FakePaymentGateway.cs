using EducationalCenter.Application.Features.OnlinePayments;

namespace EducationalCenter.Infrastructure.Persistence;

/// <summary>Stand-in for development and tests: no provider page. Replace with the real gateway (for example Sham Cash).</summary>
internal sealed class FakePaymentGateway : IPaymentGateway
{
    public string Name => "Fake";

    public Task<GatewayCheckout> CreateCheckoutAsync(PaymentCheckoutRequest request, CancellationToken ct = default) =>
        Task.FromResult(new GatewayCheckout(null, "FAKE-" + request.Reference));
}