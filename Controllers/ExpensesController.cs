using System.Security.Claims;
using aRevolucaoDosPet.DTOs;
using aRevolucaoDosPet.Enums;
using aRevolucaoDosPet.Services;
using aRevolucaoDosPet.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace aRevolucaoDosPet.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _expenseService;
    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private bool IsAdministrator => User.IsInRole(AccessLevel.ADMINISTRADOR.ToString());

    public ExpensesController(IExpenseService expenseService) => _expenseService = expenseService;

    [HttpGet]
    [ProducesResponseType<PagedResult<ExpenseResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Search([FromQuery] ExpenseSearchFilter filter, CancellationToken cancellationToken)
    {
        try { return Ok(await _expenseService.SearchAsync(filter, CurrentUserId, IsAdministrator, cancellationToken)); }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
        catch (RecordPetNotFoundException) { return NotFound(new { message = "Pet não encontrado." }); }
        catch (RecordPermissionException) { return Forbid(); }
    }

    [HttpGet("{id}")]
    [ProducesResponseType<ExpenseResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        try
        {
            var item = await _expenseService.GetByIdAsync(id, CurrentUserId, IsAdministrator, cancellationToken);
            return item is null ? NotFound() : Ok(item);
        }
        catch (RecordPermissionException) { return Forbid(); }
    }

    [HttpPost]
    [ProducesResponseType<ExpenseResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateExpenseRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var item = await _expenseService.CreateAsync(request, CurrentUserId, IsAdministrator, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, item);
        }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
        catch (RecordPetNotFoundException) { return NotFound(new { message = "Pet não encontrado." }); }
        catch (RecordPermissionException) { return Forbid(); }
        catch (ExpensePayerNotFoundException) { return NotFound(new { message = "Pagador não encontrado ou inativo." }); }
    }

    [HttpPut("{id}")]
    [ProducesResponseType<ExpenseResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] ExpenseWriteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var item = await _expenseService.UpdateAsync(id, request, CurrentUserId, IsAdministrator, cancellationToken);
            return item is null ? NotFound() : Ok(item);
        }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
        catch (RecordPermissionException) { return Forbid(); }
        catch (ExpensePayerNotFoundException) { return NotFound(new { message = "Pagador não encontrado ou inativo." }); }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        try { return await _expenseService.DeleteAsync(id, CurrentUserId, IsAdministrator, cancellationToken) ? NoContent() : NotFound(); }
        catch (RecordPermissionException) { return Forbid(); }
    }
}
