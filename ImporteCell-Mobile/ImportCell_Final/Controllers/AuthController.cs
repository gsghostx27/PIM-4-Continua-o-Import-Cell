
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using ImportCell.Data;
using ImportCell.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace ImportCell.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // Realiza login e emite tokens JWT para acesso aos endpoints protegidos.
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly PasswordHasher<Usuario> _hasher = new();

        public AuthController(
            AppDbContext context,
            IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // POST - Login com e-mail e senha
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromBody] LoginDto dados)
        {
            if (dados == null ||
                string.IsNullOrWhiteSpace(dados.Email) ||
                string.IsNullOrWhiteSpace(dados.Senha))
            {
                return BadRequest(
                    "Informe o e-mail e a senha."
                );
            }

            var email = dados.Email.Trim().ToLowerInvariant();

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == email);

            if (usuario == null)
            {
                return Unauthorized(
                    "E-mail ou senha inválidos."
                );
            }

            var resultado = _hasher.VerifyHashedPassword(
                usuario,
                usuario.Senha,
                dados.Senha
            );

            if (resultado == PasswordVerificationResult.Failed)
            {
                return Unauthorized(
                    "E-mail ou senha inválidos."
                );
            }

            var chave = _configuration["Jwt:Key"];

            if (string.IsNullOrWhiteSpace(chave))
            {
                return StatusCode(
                    500,
                    "Chave JWT não configurada."
                );
            }

            // Permissão definida pelo banco de dados
            var perfil = usuario.IsAdmin ? "Admin" : "Cliente";

            var claims = new[]
            {
                new Claim(
                    JwtRegisteredClaimNames.Sub,
                    usuario.UsuarioId.ToString()
                ),
                new Claim(
                    JwtRegisteredClaimNames.Email,
                    usuario.Email
                ),
                new Claim(
                    ClaimTypes.NameIdentifier,
                    usuario.UsuarioId.ToString()
                ),
                new Claim(
                    ClaimTypes.Name,
                    usuario.Nome
                ),
                new Claim(
                    ClaimTypes.Role,
                    perfil
                ),
                new Claim(
                    JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid().ToString()
                )
            };

            var chaveSeguranca = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(chave)
            );

            var credenciais = new SigningCredentials(
                chaveSeguranca,
                SecurityAlgorithms.HmacSha256
            );

            var expiracao = DateTime.UtcNow.AddHours(1);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: expiracao,
                signingCredentials: credenciais
            );

            var tokenGerado = new JwtSecurityTokenHandler()
                .WriteToken(token);

            return Ok(new
            {
                mensagem = "Login realizado com sucesso!",
                usuarioId = usuario.UsuarioId,
                nome = usuario.Nome,
                perfil = perfil,
                token = tokenGerado,
                expiraEm = expiracao
            });
        }

        // GET - Consultar perfil autenticado
        [Authorize]
        [HttpGet("perfil")]
        public IActionResult Perfil()
        {
            return Ok(new
            {
                mensagem = "Acesso autorizado!",
                usuarioId = User.FindFirst(
                    ClaimTypes.NameIdentifier
                )?.Value,
                nome = User.Identity?.Name,
                perfil = User.FindFirst(
                    ClaimTypes.Role
                )?.Value
            });
        }
    }

    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
    }
}
