using Microsoft.AspNetCore.SignalR;

namespace ServiceTokenApi.Hubs
{
    /// <summary>
    /// Persistent WebSocket connection held open while the investor's QR code is displayed.
    /// The company-side scanner calls POST /api/ServiceToken/GetService, which resolves the
    /// investor's connectionId from the QR payload and pushes "ServiceResult" back here.
    /// </summary>
    public class RedemptionHub : Hub
    {
        // No server-side methods needed — the hub is purely used for server → client push.
        // The connectionId is read by the controller via IHubContext<RedemptionHub>.
    }
}
