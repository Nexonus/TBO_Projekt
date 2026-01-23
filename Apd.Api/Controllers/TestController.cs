using Adp.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Apd.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class TestController : ControllerBase
{
    private readonly ApdDbContext _apdDbContext;

    public TestController(ApdDbContext apdDbContext)
    {
        _apdDbContext = apdDbContext;
    }

    [HttpGet]
    public IActionResult Get(string username)
    {
        var users = _apdDbContext.Users
            .FromSqlRaw($"SELECT * FROM Users WHERE Username = '{username}'")
            .ToList();
        
        return Ok(users);
    }
}