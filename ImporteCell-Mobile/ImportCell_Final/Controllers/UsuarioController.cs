
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using ImportCell.Data;
using ImportCell.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ImportCell.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    // Gerencia cadastro e dados de usuários, respeitando as regras de autorização.
    public class UsuarioController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly PasswordHasher<Usuario> _hasher = new();

        public UsuarioController(AppDbContext context)
        {
            _context = context;
        }

        // Identifica o usuário pelo token JWT
        private int? ObterUsuarioAutenticado()
        {
            var identificador = User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );

            if (int.TryParse(identificador, out int id))
                return id;

            return null;
        }

        // GET - Listagem desativada para usuários comuns
        [HttpGet]
        public IActionResult Get()
        {
            return StatusCode(
                403,
                "A listagem de usuários é restrita."
            );
        }

        // GET - Consultar os próprios dados
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPorId(int id)
        {
            var usuarioId = ObterUsuarioAutenticado();

            if (usuarioId == null)
                return Unauthorized();

            if (usuarioId != id)
                return Forbid();

            var usuario = await _context.Usuarios
                .AsNoTracking()
                .Where(u => u.UsuarioId == id)
                .Select(u => new
                {
                    u.UsuarioId,
                    u.Nome,
                    u.Email,
                    u.Telefone,
                    u.Endereco,
                    u.Cidade,
                    u.Cep
                })
                .FirstOrDefaultAsync();

            if (usuario == null)
                return NotFound("Usuário não encontrado.");

            return Ok(usuario);
        }

        // POST - Cadastro público
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Criar(
            [FromBody] CriarUsuarioDto dados)
        {
            var erro = ValidarDados(
                dados.Nome,
                dados.Email,
                dados.Telefone,
                dados.Cep
            );

            if (erro != null)
                return BadRequest(erro);

            if (string.IsNullOrWhiteSpace(dados.Senha) ||
                dados.Senha.Length < 8)
            {
                return BadRequest(
                    "A senha deve ter pelo menos 8 caracteres."
                );
            }

            var email = dados.Email.Trim().ToLowerInvariant();

            var emailExiste = await _context.Usuarios
                .AnyAsync(u => u.Email == email);

            if (emailExiste)
                return Conflict("Este e-mail já está cadastrado.");

            var usuario = new Usuario
            {
                Nome = dados.Nome.Trim(),
                Email = email,
                Telefone = LimparNumeros(dados.Telefone),
                Endereco = dados.Endereco?.Trim() ?? "",
                Cidade = dados.Cidade?.Trim() ?? "",
                Cep = LimparNumeros(dados.Cep)
            };

            usuario.Senha = _hasher.HashPassword(
                usuario,
                dados.Senha
            );

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            return StatusCode(201, new
            {
                mensagem = "Usuário cadastrado com sucesso!",
                usuario.UsuarioId,
                usuario.Nome,
                usuario.Email
            });
        }

        // PUT - Atualizar apenas os próprios dados
        [HttpPut("{id}")]
        public async Task<IActionResult> Atualizar(
            int id,
            [FromBody] AtualizarUsuarioDto dados)
        {
            var usuarioId = ObterUsuarioAutenticado();

            if (usuarioId == null)
                return Unauthorized();

            if (usuarioId != id)
                return Forbid();

            var usuario = await _context.Usuarios.FindAsync(id);

            if (usuario == null)
                return NotFound("Usuário não encontrado.");

            var erro = ValidarDados(
                dados.Nome,
                dados.Email,
                dados.Telefone,
                dados.Cep
            );

            if (erro != null)
                return BadRequest(erro);

            var email = dados.Email.Trim().ToLowerInvariant();

            var emailExiste = await _context.Usuarios
                .AnyAsync(u => u.Email == email &&
                               u.UsuarioId != id);

            if (emailExiste)
                return Conflict("Este e-mail já está cadastrado.");

            usuario.Nome = dados.Nome.Trim();
            usuario.Email = email;
            usuario.Telefone = LimparNumeros(dados.Telefone);
            usuario.Endereco = dados.Endereco?.Trim() ?? "";
            usuario.Cidade = dados.Cidade?.Trim() ?? "";
            usuario.Cep = LimparNumeros(dados.Cep);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensagem = "Usuário atualizado com sucesso!",
                usuario.UsuarioId,
                usuario.Nome,
                usuario.Email
            });
        }

        // DELETE - Excluir apenas a própria conta
        [HttpDelete("{id}")]
        public async Task<IActionResult> Excluir(int id)
        {
            var usuarioId = ObterUsuarioAutenticado();

            if (usuarioId == null)
                return Unauthorized();

            if (usuarioId != id)
                return Forbid();

            var usuario = await _context.Usuarios.FindAsync(id);

            if (usuario == null)
                return NotFound("Usuário não encontrado.");

            var possuiPedidos = await _context.Pedidos
                .AnyAsync(p => p.UsuarioId == id);

            if (possuiPedidos)
            {
                return Conflict(
                    "Não é possível excluir um usuário com pedidos vinculados."
                );
            }

            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensagem = "Usuário excluído com sucesso!",
                usuarioId = id
            });
        }

        // Validações de dados
        private static string? ValidarDados(
            string nome,
            string email,
            string? telefone,
            string? cep)
        {
            if (string.IsNullOrWhiteSpace(nome))
                return "O nome é obrigatório.";

            if (string.IsNullOrWhiteSpace(email) ||
                !new EmailAddressAttribute().IsValid(email))
                return "Informe um e-mail válido.";

            if (!string.IsNullOrWhiteSpace(telefone))
            {
                var numeros = LimparNumeros(telefone);

                if (!Regex.IsMatch(numeros, @"^\d{10,11}$"))
                    return "O telefone deve ter 10 ou 11 dígitos.";
            }

            if (!string.IsNullOrWhiteSpace(cep))
            {
                var numeros = LimparNumeros(cep);

                if (!Regex.IsMatch(numeros, @"^\d{8}$"))
                    return "O CEP deve ter exatamente 8 dígitos.";
            }

            return null;
        }

        private static string LimparNumeros(string? valor)
        {
            return Regex.Replace(valor ?? "", @"\D", "");
        }
    }

    public class CriarUsuarioDto
    {
        [Required, MaxLength(150)]
        public string Nome { get; set; } = "";

        [Required, EmailAddress, MaxLength(150)]
        public string Email { get; set; } = "";

        [Required, MinLength(8)]
        public string Senha { get; set; } = "";

        [MaxLength(20)]
        public string? Telefone { get; set; }

        [MaxLength(200)]
        public string? Endereco { get; set; }

        [MaxLength(100)]
        public string? Cidade { get; set; }

        [MaxLength(10)]
        public string? Cep { get; set; }
    }

    public class AtualizarUsuarioDto
    {
        [Required, MaxLength(150)]
        public string Nome { get; set; } = "";

        [Required, EmailAddress, MaxLength(150)]
        public string Email { get; set; } = "";

        [MaxLength(20)]
        public string? Telefone { get; set; }

        [MaxLength(200)]
        public string? Endereco { get; set; }

        [MaxLength(100)]
        public string? Cidade { get; set; }

        [MaxLength(10)]
        public string? Cep { get; set; }
    }
}
