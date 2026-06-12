namespace RaffleIt.API.Models;

public class Ticket
{
    public Guid Id { get; set; }
    public Guid RaffleId { get; set; }
    public int TicketNumber { get; set; }
    public bool IsClaimed { get; set; }
    public string? ClaimedName { get; set; }
    public string? ClaimedSurname { get; set; }
    public string? ClaimedMobile { get; set; }
    public string? ClaimedEmail { get; set; }
    public string? PurchasedFrom { get; set; }
    public DateTime? ClaimedAt { get; set; }
    public Raffle Raffle { get; set; } = null!;
}
