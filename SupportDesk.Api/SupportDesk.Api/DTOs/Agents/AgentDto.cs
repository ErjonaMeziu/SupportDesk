using SupportDesk.Api.Domain.Enums;

namespace SupportDesk.Api.Dtos.Agents;

public record AgentDto(int Id, string FullName, string Email, Department Department, bool Active);

public record AgentSummaryDto(int Id, string FullName, bool Active);
