using SupportDesk.Api.Domain.Enums;

namespace SupportDesk.Api.Domain.Entities;

public class Agent
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Department Department { get; set; }
    public bool Active { get; set; } = true;

    public ICollection<Ticket> AssignedTickets { get; set; } = new List<Ticket>();
}
