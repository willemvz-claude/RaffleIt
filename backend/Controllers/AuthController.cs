using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaffleIt.API.Data;
using RaffleIt.API.Models;
using RaffleIt.API.Models.DTOs;
using RaffleIt.API.Services;

namespace RaffleIt.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly JwtService _jwt;

    public AuthController(AppDbContext db, JwtService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest req)
    {
        if (await _db.Users.AnyAsync(u => u.Email == req.Email.ToLower()))
            return BadRequest(new { message = "Email already registered" });

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = req.Email.ToLower().Trim(),
            FullName = req.FullName.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var token = _jwt.GenerateToken(user);
        return Ok(new AuthResponse(token, new UserDto(user.Id, user.Email, user.FullName)));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email.ToLower());
        if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password" });

        var token = _jwt.GenerateToken(user);
        return Ok(new AuthResponse(token, new UserDto(user.Id, user.Email, user.FullName)));
    }
}
