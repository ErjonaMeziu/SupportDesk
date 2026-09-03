using Microsoft.AspNetCore.Mvc;
using SupportDesk.Api.Dtos.Agents;
using SupportDesk.Api.Services;

namespace SupportDesk.Api.Controllers;

[ApiController]
[Route("api/agents")]
public class AgentsController : ControllerBase
{
    private readonly IAgentService _agentService;

    public AgentsController(IAgentService agentService)
    {
        _agentService = agentService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AgentDto>>> GetAgents(
        [FromQuery] string? search, [FromQuery] bool? activeOnly, CancellationToken cancellationToken)
    {
        return Ok(await _agentService.GetAgentsAsync(search, activeOnly, cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AgentDto>> GetAgent(int id, CancellationToken cancellationToken)
    {
        return Ok(await _agentService.GetAgentByIdAsync(id, cancellationToken));
    }
}
