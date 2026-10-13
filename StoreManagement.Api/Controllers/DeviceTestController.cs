
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace StoreManagement.Api.Controllers;

[ApiController]
[Route("api/device-test")]
[Authorize(Policy = "ActiveDevice")]
public class DeviceTestController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        var deviceId = User.FindFirst("device_id")?.Value;
        var deviceCode = User.FindFirst("device_code")?.Value;

        return Ok(new
        {
            message = "Device authentication successful.",
            deviceId,
            deviceCode,
            authenticated = User.Identity?.IsAuthenticated ?? false
        });
    }
}
