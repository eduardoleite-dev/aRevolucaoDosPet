using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ArquiteturaBaseApi.Services.Interfaces;

namespace ArquiteturaBaseApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HelloWorldController : ControllerBase
{
    private readonly IMongoTestService _mongoTestService;

    public HelloWorldController(IMongoTestService mongoTestService)
    {
        _mongoTestService = mongoTestService;
    }

    [HttpGet("publico")]
    public IActionResult GetPublic()
    {
        return Ok(new { message = "Hello World" });
    }

    [Authorize]
    [HttpGet("privado")]
    public IActionResult GetPrivate()
    {
        var dbStatus = _mongoTestService.TestConnection();
        return Ok(new { 
            message = "Hello World Autenticado!",
            mongoStatus = dbStatus
        });
    }
}