using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GestaoViaturasAPI.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
namespace GestaoViaturasAPI.Controllers;

[ApiController, Route("api/[controller]")]
public class AuthController(AppDbContext context, IPasswordHasher<Usuario> passwordHasher,
    IConfiguration configuration) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDTO login, CancellationToken ct)
    {
        var nome = login.NomeUtilizador.Trim().ToUpperInvariant();
        var usuario = await context.Usuarios.SingleOrDefaultAsync(u => u.NomeUtilizador == nome, ct);
        if (usuario is null) return Problem(statusCode: 401, title: "Usuário ou senha inválidos.");
        var result = passwordHasher.VerifyHashedPassword(usuario, usuario.PalavraPasseHash, login.PalavraPasse);
        if (result == PasswordVerificationResult.Failed)
            return Problem(statusCode: 401, title: "Usuário ou senha inválidos.");
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            usuario.PalavraPasseHash = passwordHasher.HashPassword(usuario, login.PalavraPasse);
            await context.SaveChangesAsync(ct);
        }
        var expires = DateTime.UtcNow.AddHours(2);
        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"], audience: configuration["Jwt:Audience"],
            claims: new[] { new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.NomeUtilizador), new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) },
            expires: expires,
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!)),
                SecurityAlgorithms.HmacSha256));
        return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc = expires });
    }
}
