
using System;
using System.Linq;
using System.Threading.Tasks;
using ImportCell.Data;
using ImportCell.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ImportCell.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // Consulta e administra as cores associadas aos smartphones.
    public class CorController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CorController(AppDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // GET - LISTAR TODAS AS CORES
        // Público
        // ==========================================

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var cores = await _context.Cores
                .AsNoTracking()
                .Select(c => new
                {
                    c.CorId,
                    c.ProdutoId,
                    c.NomeCor,
                    c.CodigoHex,
                    c.ImagemCor
                })
                .ToListAsync();

            return Ok(cores);
        }

        // ==========================================
        // GET - CONSULTAR COR POR ID
        // Público
        // ==========================================

        [AllowAnonymous]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetPorId(int id)
        {
            var cor = await _context.Cores
                .AsNoTracking()
                .Where(c => c.CorId == id)
                .Select(c => new
                {
                    c.CorId,
                    c.ProdutoId,
                    c.NomeCor,
                    c.CodigoHex,
                    c.ImagemCor
                })
                .FirstOrDefaultAsync();

            if (cor == null)
            {
                return NotFound("Cor não encontrada.");
            }

            return Ok(cor);
        }

        // ==========================================
        // GET - CORES DE UM SMARTPHONE
        // Público
        // ==========================================

        [AllowAnonymous]
        [HttpGet("smartphone/{produtoId:int}")]
        public async Task<IActionResult> GetPorSmartphone(
            int produtoId)
        {
            var cores = await _context.Cores
                .AsNoTracking()
                .Where(c => c.ProdutoId == produtoId)
                .Select(c => new
                {
                    c.CorId,
                    c.ProdutoId,
                    c.NomeCor,
                    c.CodigoHex,
                    c.ImagemCor
                })
                .ToListAsync();

            return Ok(cores);
        }

        // ==========================================
        // POST - CADASTRAR COR
        // Somente administrador
        // ==========================================

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Criar(
            [FromBody] CorDto dados)
        {
            if (string.IsNullOrWhiteSpace(dados.NomeCor))
            {
                return BadRequest(
                    "O nome da cor é obrigatório."
                );
            }

            var nomeCor = dados.NomeCor.Trim();

            if (!CodigoHexValido(dados.CodigoHex))
            {
                return BadRequest(
                    "Código hexadecimal inválido. Use #RRGGBB."
                );
            }

            var smartphoneExiste = await _context.Smartphones
                .AnyAsync(s =>
                    s.ProdutoId == dados.ProdutoId &&
                    s.Ativo);

            if (!smartphoneExiste)
            {
                return BadRequest(
                    "O smartphone não existe ou está inativo."
                );
            }

            var corExistente = await _context.Cores
                .AnyAsync(c =>
                    c.ProdutoId == dados.ProdutoId &&
                    c.NomeCor == nomeCor);

            if (corExistente)
            {
                return Conflict(
                    "Essa cor já está cadastrada para este smartphone."
                );
            }

            var cor = new Cor
            {
                ProdutoId = dados.ProdutoId,
                NomeCor = nomeCor,
                CodigoHex = dados.CodigoHex?.Trim(),
                ImagemCor = dados.ImagemCor?.Trim()
            };

            _context.Cores.Add(cor);

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetPorId),
                new { id = cor.CorId },
                new
                {
                    cor.CorId,
                    cor.ProdutoId,
                    cor.NomeCor,
                    cor.CodigoHex,
                    cor.ImagemCor
                }
            );
        }

        // ==========================================
        // PUT - ATUALIZAR COR
        // Somente administrador
        // ==========================================

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Atualizar(
            int id,
            [FromBody] CorDto dados)
        {
            var cor = await _context.Cores
                .FirstOrDefaultAsync(c => c.CorId == id);

            if (cor == null)
            {
                return NotFound("Cor não encontrada.");
            }

            // Impedir transferência para outro smartphone.
            if (dados.ProdutoId != cor.ProdutoId)
            {
                return Conflict(
                    "Não é permitido transferir uma cor para outro smartphone."
                );
            }

            if (string.IsNullOrWhiteSpace(dados.NomeCor))
            {
                return BadRequest(
                    "O nome da cor é obrigatório."
                );
            }

            if (!CodigoHexValido(dados.CodigoHex))
            {
                return BadRequest(
                    "Código hexadecimal inválido. Use #RRGGBB."
                );
            }

            var nomeCor = dados.NomeCor.Trim();

            var corDuplicada = await _context.Cores
                .AnyAsync(c =>
                    c.CorId != id &&
                    c.ProdutoId == cor.ProdutoId &&
                    c.NomeCor == nomeCor);

            if (corDuplicada)
            {
                return Conflict(
                    "Essa cor já está cadastrada para este smartphone."
                );
            }

            cor.NomeCor = nomeCor;
            cor.CodigoHex = dados.CodigoHex?.Trim();
            cor.ImagemCor = dados.ImagemCor?.Trim();

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensagem = "Cor atualizada com sucesso!",
                cor.CorId,
                cor.ProdutoId,
                cor.NomeCor,
                cor.CodigoHex,
                cor.ImagemCor
            });
        }

        // ==========================================
        // DELETE - EXCLUIR COR
        // Bloqueado para preservar pedidos
        // ==========================================

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public IActionResult Excluir(int id)
        {
            return StatusCode(
                403,
                "Exclusão de cores desabilitada para proteger os pedidos."
            );
        }

        // ==========================================
        // VALIDAR CÓDIGO HEXADECIMAL
        // ==========================================

        private static bool CodigoHexValido(string? codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                return true;

            return System.Text.RegularExpressions.Regex.IsMatch(
                codigo.Trim(),
                @"^#[0-9a-fA-F]{6}$"
            );
        }
    }

    // ==========================================
    // DTO - DADOS RECEBIDOS PELO POST E PUT
    // ==========================================

    public class CorDto
    {
        public int ProdutoId { get; set; }

        public string NomeCor { get; set; } = string.Empty;

        public string? CodigoHex { get; set; }

        public string? ImagemCor { get; set; }
    }
}
