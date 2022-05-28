using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using VerbIt.Backend.Models;
using VerbIt.Backend.Services;
using VerbIt.DataModels;

namespace VerbIt.Backend.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IVerbitAuthService _verbitAuthService;
    private readonly JwtSettings _jwtSettings;

    public AuthController(IVerbitAuthService verbitAuthService, IOptions<JwtSettings> jwtSettings)
    {
        _verbitAuthService = verbitAuthService;
        _jwtSettings = jwtSettings.Value;
    }

    [HttpPost]
    [Route("login")]
    public async Task<ActionResult<string>> Login(LoginRequest request)
    {
        AuthenticatedUser? result = await _verbitAuthService.Login(
            request.Username,
            request.Password
        );
        if (result == null)
        {
            return BadRequest();
        }

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)),
            SecurityAlgorithms.HmacSha256
        );

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, result.Id.ToString("N")),
            new Claim(JwtRegisteredClaimNames.Name, result.Name),
            new Claim(ClaimTypes.Role, result.Role),
        };

        var tokenOptions = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            claims: claims,
            expires: DateTimeOffset.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes).UtcDateTime,
            signingCredentials: credentials
        );

        var token = new JwtSecurityTokenHandler().WriteToken(tokenOptions);

        return token;
    }

    [HttpPost]
    [Authorize]
    [Route("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return Ok();
    }
}
