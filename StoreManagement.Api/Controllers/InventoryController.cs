using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreManagement.Api.DTOs.Inventory;
using StoreManagement.Api.Services.Inventory;

namespace StoreManagement.Api.Controllers;

[ApiController]
[Route("api/inventory")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly InventoryService _inventoryService;

    public InventoryController(InventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpGet("stock")]
    public async Task<ActionResult<InventoryStockResponse>> GetStock(
        [FromQuery] int storeId,
        [FromQuery] int productVariantId)
    {
        var result = await _inventoryService.GetStockAsync(
            storeId,
            productVariantId);

        if (result == null)
            return NotFound(new
            {
                message = "Product variant not found."
            });

        return Ok(result);
    }

    [HttpPost("receive")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<IActionResult> Receive(
    ReceiveInventoryRequest request)
    {
        if (request.Quantity <= 0)
            return BadRequest("Quantity must be greater than zero.");

        var id = await _inventoryService.ReceiveAsync(
            request.StoreId,
            request.ProductVariantId,
            request.Quantity,
            request.LocationCode);

        if (id == null)
            return BadRequest(
                "Store does not exist or product variant does not exist or is inactive.");

        return Ok(new
        {
            id,
            message = "Inventory received successfully."
        });
    }

    [HttpPost("adjust")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<IActionResult> Adjust(
    AdjustInventoryRequest request)
    {
        var result = await _inventoryService.AdjustAsync(
            request.StoreId,
            request.ProductVariantId,
            request.Quantity,
            request.LocationCode);

        if (!result.Success)
            return BadRequest(result.Error);

        return Ok(new
        {
            id = result.Id,
            message = "Inventory adjusted successfully."
        });
    }

    [HttpPost("transfer")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<IActionResult> Transfer(
    TransferInventoryRequest request)
    {
        var result = await _inventoryService.TransferAsync(
            request.FromStoreId,
            request.ToStoreId,
            request.ProductVariantId,
            request.Quantity,
            request.FromLocationCode,
            request.ToLocationCode);

        if (!result.Success)
            return BadRequest(result.Error);

        return Ok(new
        {
            message = "Inventory transferred successfully."
        });
    }

    [HttpGet("history")]
    public async Task<ActionResult<InventoryHistoryResponse>> GetHistory(
    [FromQuery] InventoryHistoryRequest request)
    {
        var result = await _inventoryService.GetHistoryAsync(request);

        return Ok(result);
    }
}