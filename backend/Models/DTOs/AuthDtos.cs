namespace RaffleIt.API.Models.DTOs;

public record RegisterRequest(string Email, string Password, string FullName);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Token, UserDto User);
public record UserDto(Guid Id, string Email, string FullName);
