using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SmartHome.Shared {
    [Authorize]
    public class DeviceHub : Hub {

    }
}
