using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaffleIt.API.Data;
using RaffleIt.API.Models.DTOs;

namespace RaffleIt.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TicketsController : ControllerBase
{
    private readonly AppDbContext _db;

    public TicketsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TicketDetailDto>> GetTicket(Guid id)
    {
        var ticket = await _db.Tickets
            .Include(t => t.Raffle)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket == null) return NotFound();

        var raffleDto = new RaffleDto(
            ticket.Raffle.Id, ticket.Raffle.Name, ticket.Raffle.Description,
            ticket.Raffle.Organisation, ticket.Raffle.Prizes,
            ticket.Raffle.NumberOfTickets, ticket.Raffle.TicketPrice,
            ticket.Raffle.Currency, ticket.Raffle.CreatedAt, 0);

        return Ok(new TicketDetailDto(
            ticket.Id, ticket.TicketNumber, ticket.IsClaimed,
            ticket.ClaimedName, ticket.ClaimedSurname, ticket.ClaimedMobile,
            ticket.ClaimedEmail, ticket.PurchasedFrom, ticket.ClaimedAt,
            raffleDto));
    }

    [HttpPost("{id:guid}/claim")]
    public async Task<ActionResult<TicketDetailDto>> ClaimTicket(Guid id, ClaimTicketRequest req)
    {
        var ticket = await _db.Tickets
            .Include(t => t.Raffle)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket == null) return NotFound();

        ticket.IsClaimed = true;
        ticket.ClaimedName = req.Name.Trim();
        ticket.ClaimedSurname = req.Surname.Trim();
        ticket.ClaimedMobile = req.Mobile.Trim();
        ticket.ClaimedEmail = req.Email.Trim().ToLower();
        ticket.PurchasedFrom = req.PurchasedFrom.Trim();
        ticket.ClaimedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        var raffleDto = new RaffleDto(
            ticket.Raffle.Id, ticket.Raffle.Name, ticket.Raffle.Description,
            ticket.Raffle.Organisation, ticket.Raffle.Prizes,
            ticket.Raffle.NumberOfTickets, ticket.Raffle.TicketPrice,
            ticket.Raffle.Currency, ticket.Raffle.CreatedAt, 0);

        return Ok(new TicketDetailDto(
            ticket.Id, ticket.TicketNumber, ticket.IsClaimed,
            ticket.ClaimedName, ticket.ClaimedSurname, ticket.ClaimedMobile,
            ticket.ClaimedEmail, ticket.PurchasedFrom, ticket.ClaimedAt,
            raffleDto));
    }
}
