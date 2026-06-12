using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaffleIt.API.Data;
using RaffleIt.API.Models;
using RaffleIt.API.Models.DTOs;
using RaffleIt.API.Services;

namespace RaffleIt.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RafflesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly PdfService _pdf;

    public RafflesController(AppDbContext db, PdfService pdf)
    {
        _db = db;
        _pdf = pdf;
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<List<RaffleDto>>> GetRaffles()
    {
        var raffles = await _db.Raffles
            .Where(r => r.UserId == CurrentUserId)
            .Include(r => r.Tickets)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return Ok(raffles.Select(r => new RaffleDto(
            r.Id, r.Name, r.Description, r.Organisation, r.Prizes,
            r.NumberOfTickets, r.TicketPrice, r.Currency, r.CreatedAt,
            r.Tickets.Count(t => t.IsClaimed))));
    }

    [HttpPost]
    public async Task<ActionResult<RaffleDto>> CreateRaffle(CreateRaffleRequest req)
    {
        var raffle = new Raffle
        {
            Id = Guid.NewGuid(),
            UserId = CurrentUserId,
            Name = req.Name.Trim(),
            Description = req.Description?.Trim(),
            Organisation = req.Organisation?.Trim(),
            Prizes = req.Prizes?.Trim(),
            NumberOfTickets = req.NumberOfTickets,
            TicketPrice = req.TicketPrice,
            Currency = req.Currency.ToUpper(),
            CreatedAt = DateTime.UtcNow
        };

        _db.Raffles.Add(raffle);

        var tickets = Enumerable.Range(1, req.NumberOfTickets).Select(n => new Ticket
        {
            Id = Guid.NewGuid(),
            RaffleId = raffle.Id,
            TicketNumber = n,
            IsClaimed = false
        }).ToList();

        _db.Tickets.AddRange(tickets);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetRaffle), new { id = raffle.Id },
            new RaffleDto(raffle.Id, raffle.Name, raffle.Description, raffle.Organisation,
                raffle.Prizes, raffle.NumberOfTickets, raffle.TicketPrice, raffle.Currency,
                raffle.CreatedAt, 0));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RaffleDetailDto>> GetRaffle(Guid id)
    {
        var raffle = await _db.Raffles
            .Include(r => r.Tickets)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == CurrentUserId);

        if (raffle == null) return NotFound();

        var ticketDtos = raffle.Tickets.OrderBy(t => t.TicketNumber)
            .Select(t => new TicketDto(t.Id, t.TicketNumber, t.IsClaimed,
                t.ClaimedName, t.ClaimedSurname, t.ClaimedMobile,
                t.ClaimedEmail, t.PurchasedFrom, t.ClaimedAt))
            .ToList();

        return Ok(new RaffleDetailDto(
            raffle.Id, raffle.Name, raffle.Description, raffle.Organisation,
            raffle.Prizes, raffle.NumberOfTickets, raffle.TicketPrice, raffle.Currency,
            raffle.CreatedAt, raffle.Tickets.Count(t => t.IsClaimed), ticketDtos));
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> DownloadPdf(Guid id)
    {
        var raffle = await _db.Raffles
            .Include(r => r.Tickets)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == CurrentUserId);

        if (raffle == null) return NotFound();

        var tickets = raffle.Tickets.OrderBy(t => t.TicketNumber).ToList();
        var pdfBytes = _pdf.GenerateRafflePdf(raffle, tickets);

        return File(pdfBytes, "application/pdf",
            $"{raffle.Name.Replace(" ", "_")}_tickets.pdf");
    }
}
