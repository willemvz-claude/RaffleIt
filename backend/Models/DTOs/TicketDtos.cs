namespace RaffleIt.API.Models.DTOs;

public record TicketDto(
    Guid Id,
    int TicketNumber,
    bool IsClaimed,
    string? ClaimedName,
    string? ClaimedSurname,
    string? ClaimedMobile,
    string? ClaimedEmail,
    string? PurchasedFrom,
    DateTime? ClaimedAt
);

public record TicketDetailDto(
    Guid Id,
    int TicketNumber,
    bool IsClaimed,
    string? ClaimedName,
    string? ClaimedSurname,
    string? ClaimedMobile,
    string? ClaimedEmail,
    string? PurchasedFrom,
    DateTime? ClaimedAt,
    RaffleDto Raffle
);

public record ClaimTicketRequest(
    string Name,
    string Surname,
    string Mobile,
    string Email,
    string PurchasedFrom
);
