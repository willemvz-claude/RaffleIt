namespace RaffleIt.API.Models.DTOs;

public record CreateRaffleRequest(
    string Name,
    string? Description,
    string? Organisation,
    string? Prizes,
    int NumberOfTickets,
    decimal TicketPrice,
    string Currency
);

public record RaffleDto(
    Guid Id,
    string Name,
    string? Description,
    string? Organisation,
    string? Prizes,
    int NumberOfTickets,
    decimal TicketPrice,
    string Currency,
    DateTime CreatedAt,
    int ClaimedTickets
);

public record RaffleDetailDto(
    Guid Id,
    string Name,
    string? Description,
    string? Organisation,
    string? Prizes,
    int NumberOfTickets,
    decimal TicketPrice,
    string Currency,
    DateTime CreatedAt,
    int ClaimedTickets,
    List<TicketDto> Tickets
);
