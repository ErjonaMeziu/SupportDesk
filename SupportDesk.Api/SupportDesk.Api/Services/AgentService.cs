using Microsoft.EntityFrameworkCore;
using SupportDesk.Api.Common.Exceptions;
using SupportDesk.Api.Data;
using SupportDesk.Api.Dtos.Agents;

namespace SupportDesk.Api.Services;

public class AgentService : IAgentService
{
    private readonly SupportDeskDbContext _db;

    public AgentService(SupportDeskDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AgentDto>> GetAgentsAsync(string? search, bool? activeOnly, CancellationToken cancellationToken)
    {
        var query = _db.Agents.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(a => a.FullName.Contains(term) || a.Email.Contains(term));
        }

        if (activeOnly is true)
        {
            query = query.Where(a => a.Active);
        }

        return await query
            .OrderBy(a => a.FullName)
            .Select(a => new AgentDto(a.Id, a.FullName, a.Email, a.Department, a.Active))
            .ToListAsync(cancellationToken);
    }

    public async Task<AgentDto> GetAgentByIdAsync(int id, CancellationToken cancellationToken)
    {
        var agent = await _db.Agents.FindAsync(new object?[] { id }, cancellationToken)
            ?? throw new NotFoundException("AGENT_NOT_FOUND", $"Agent {id} was not found.");

        return new AgentDto(agent.Id, agent.FullName, agent.Email, agent.Department, agent.Active);
    }
}
