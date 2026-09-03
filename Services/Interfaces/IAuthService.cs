using ArquiteturaBaseApi.DTOs;

namespace ArquiteturaBaseApi.Services.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto?> AuthenticateAsync(string username, string password);
}