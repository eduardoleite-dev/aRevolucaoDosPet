using Microsoft.AspNetCore.Mvc;
using aRevolucaoDosPet.DTOs;
using aRevolucaoDosPet.Services.Interfaces;

namespace aRevolucaoDosPet.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

   [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var authResult = await _authService.AuthenticateAsync(request.Username, request.Password);
        
        if (authResult == null)
            return Unauthorized(new { message = "Credenciais inválidas" });

        return Ok(authResult);
    }
}