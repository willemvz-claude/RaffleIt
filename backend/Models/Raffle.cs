namespace RaffleIt.API.Models;

public class Raffle
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Organisation { get; set; }
    public string? Prizes { get; set; }
    public int NumberOfTickets { get; set; }
    public decimal TicketPrice { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime CreatedAt { get; set; }
    public User User { get; set; } = null!;
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
