using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreManagement.Api.DTOs.Transactions;
using StoreManagement.Api.Services.Sales;

namespace StoreManagement.Api.Controllers;

[ApiController]
[Route("api/sales")]
[Authorize]
public class SalesController : ControllerBase
{
    private readonly SalesService _salesService;

    public SalesController(SalesService salesService)
    {
        _salesService = salesService;
    }

    [HttpPost]
    public async Task<ActionResult<SaleResponse>> CreateSale(
        CreateSaleRequest request)
    {
        var result = await _salesService.CreateSaleAsync(request);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(result.Sale);
    }

    [HttpGet]
    public async Task<ActionResult<TransactionHistoryResponse>> GetSales(
    [FromQuery] TransactionHistoryRequest request)
    {
        var result = await _salesService.GetSalesAsync(request);

        return Ok(result);
    }

    [HttpGet("{transactionId:guid}")]
    public async Task<ActionResult<SaleResponse>> GetSale(
    Guid transactionId)
    {
        var result = await _salesService.GetSaleAsync(transactionId);

        if (result == null)
        {
            return NotFound(new
            {
                message = "Sale transaction not found."
            });
        }

        return Ok(result);
    }

    [HttpPost("{transactionId:guid}/void")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<IActionResult> VoidSale(
    Guid transactionId,
    VoidSaleRequest request)
    {
        var result = await _salesService.VoidSaleAsync(
            transactionId,
            request.Reason);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(new
        {
            message = "Sale voided successfully."
        });
    }
    [HttpPost("return")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<SaleResponse>> CreateReturn(
    CreateReturnRequest request)
    {
        var result = await _salesService.CreateReturnAsync(request);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(result.Return);
    }
}