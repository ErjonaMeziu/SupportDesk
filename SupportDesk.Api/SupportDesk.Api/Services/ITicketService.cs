using SupportDesk.Api.Dtos.Comments;
using SupportDesk.Api.Dtos.Common;
using SupportDesk.Api.Dtos.Tickets;

namespace SupportDesk.Api.Services;

public interface ITicketService
{
    Task<PagedResult<TicketListItemDto>> GetTicketsAsync(TicketQueryParameters query, CancellationToken cancellationToken);
    Task<TicketDetailDto> GetTicketByIdAsync(int id, CancellationToken cancellationToken);
    Task<TicketDetailDto> CreateTicketAsync(CreateTicketRequest request, CancellationToken cancellationToken);
    Task<TicketDetailDto> UpdateTicketAsync(int id, UpdateTicketRequest request, CancellationToken cancellationToken);
    Task DeleteTicketAsync(int id, CancellationToken cancellationToken);
    Task<TicketDetailDto> ChangeStatusAsync(int id, ChangeStatusRequest request, CancellationToken cancellationToken);
    Task<TicketDetailDto> AssignAgentAsync(int id, AssignAgentRequest request, CancellationToken cancellationToken);
    Task<CommentDto> AddCommentAsync(int id, CreateCommentRequest request, CancellationToken cancellationToken);
}
