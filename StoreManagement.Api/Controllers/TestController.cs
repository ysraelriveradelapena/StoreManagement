using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StoreManagement.Api.Controllers;

[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    [Authorize]
    [HttpGet("authenticated")]
    public IActionResult Authenticated()
    {
        return Ok(new
        {
            message = "You are authenticated."
        });
    }

    [Authorize(Roles = "SystemAdmin")]
    [HttpGet("admin")]
    public IActionResult Admin()
    {
        return Ok(new
        {
            message = "You are a System Administrator."
        });
    }

    [Authorize(Roles = "StoreBA")]
    [HttpGet("store-ba")]
    public IActionResult StoreBA()
    {
        return Ok(new
        {
            message = "You are a Store BA."
        });
    }
}