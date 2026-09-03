using SupportDesk.Api.Dtos.Agents;

namespace SupportDesk.Api.Services;

public interface IAgentService
{
    Task<IReadOnlyList<AgentDto>> GetAgentsAsync(string? search, bool? activeOnly, CancellationToken cancellationToken);
    Task<AgentDto> GetAgentByIdAsync(int id, CancellationToken cancellationToken);
}
