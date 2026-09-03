using Microsoft.AspNetCore.Mvc;
using SupportDesk.Api.Dtos.Comments;
using SupportDesk.Api.Dtos.Common;
using SupportDesk.Api.Dtos.Tickets;
using SupportDesk.Api.Services;

namespace SupportDesk.Api.Controllers;

[ApiController]
[Route("api/tickets")]
public class TicketsController : ControllerBase
{
    private readonly ITicketService _ticketService;

    public TicketsController(ITicketService ticketService)
    {
        _ticketService = ticketService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<TicketListItemDto>>> GetTickets(
        [FromQuery] TicketQueryParameters query, CancellationToken cancellationToken)
    {
        return Ok(await _ticketService.GetTicketsAsync(query, cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TicketDetailDto>> GetTicket(int id, CancellationToken cancellationToken)
    {
        return Ok(await _ticketService.GetTicketByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<TicketDetailDto>> CreateTicket(
        [FromBody] CreateTicketRequest request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketService.CreateTicketAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetTicket), new { id = ticket.Id }, ticket);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TicketDetailDto>> UpdateTicket(
        int id, [FromBody] UpdateTicketRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _ticketService.UpdateTicketAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteTicket(int id, CancellationToken cancellationToken)
    {
        await _ticketService.DeleteTicketAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Dedicated status endpoint (rather than a generic PUT) so every status change - including
    /// the transition rules and the resulting ResolvedDate/ClosedDate side effects - goes through
    /// one code path. See README "Status transitions" for the rationale.
    /// </summary>
    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<TicketDetailDto>> ChangeStatus(
        int id, [FromBody] ChangeStatusRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _ticketService.ChangeStatusAsync(id, request, cancellationToken));
    }

    /// <summary>Dedicated assignment endpoint. Pass a null AgentId to unassign.</summary>
    [HttpPatch("{id:int}/assignment")]
    public async Task<ActionResult<TicketDetailDto>> AssignAgent(
        int id, [FromBody] AssignAgentRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _ticketService.AssignAgentAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:int}/comments")]
    public async Task<ActionResult<CommentDto>> AddComment(
        int id, [FromBody] CreateCommentRequest request, CancellationToken cancellationToken)
    {
        var comment = await _ticketService.AddCommentAsync(id, request, cancellationToken);
        return CreatedAtAction(nameof(GetTicket), new { id }, comment);
    }
}
