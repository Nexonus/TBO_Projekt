using System.Diagnostics;
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

    [HttpGet("users")]
    public IActionResult Get(string email)
    {
        var users = _apdDbContext.Users
            .FromSqlRaw($"SELECT * FROM \"AspNetUsers\" WHERE \"Email\" = '{email}'")
            .ToList();
        
        return Ok(users);
    }
    
    [HttpGet("ping")]
    public IActionResult PingHost(string hostname)
    {
        try
        {
            bool isWindows = OperatingSystem.IsWindows();
    
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = isWindows ? "cmd.exe" : "/bin/bash",
                    Arguments = isWindows
                        ? $"/c ping {hostname}"
                        : $"-c \"ping -c 4 {hostname}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
    
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();
    
            return Content(string.IsNullOrWhiteSpace(output) ? error : output, "text/plain");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}