using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using aRevolucaoDosPet.DTOs;
using aRevolucaoDosPet.Enums;
using aRevolucaoDosPet.Models;
using aRevolucaoDosPet.Services.Interfaces;

namespace aRevolucaoDosPet.Services;

public class AuthService : IAuthService
{
    private readonly IConfiguration _configuration;
    private readonly IMongoCollection<User> _usersCollection;

    public AuthService(IConfiguration configuration, IMongoDatabase database)
    {
        _configuration = configuration;
        _usersCollection = database.GetCollection<User>("Users");
    }

    public async Task<LoginResponseDto?> AuthenticateAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        var login = username.Trim();
        var normalizedEmail = login.ToLowerInvariant();
        var user = await _usersCollection.Find(u =>
            (u.Email == normalizedEmail || u.Name == login) && u.Status == UserStatus.ACTIVE)
            .FirstOrDefaultAsync();

        if (user is null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var update = Builders<User>.Update
            .Set(u => u.LastLoginAt, now)
            .Set(u => u.UpdatedAt, now);
        await _usersCollection.UpdateOneAsync(u => u.Id == user.Id, update);

        return GenerateJwtToken(user.Name, user.Id);
    }

    private LoginResponseDto GenerateJwtToken(string username, string userId)
    {
        var secretKey = _configuration["JwtSettings:SecretKey"] 
                        ?? _configuration["JwtSettings__SecretKey"];

        if (string.IsNullOrWhiteSpace(secretKey))
        {
            throw new InvalidOperationException("A chave secreta JWT não foi configurada em JwtSettings:SecretKey ou JwtSettings__SecretKey.");
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(secretKey);
        
        var expiresAt = DateTime.UtcNow.AddHours(2);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] 
            { 
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.NameIdentifier, userId)
            }),
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        return new LoginResponseDto(tokenString, expiresAt, username, userId);
    }
}
