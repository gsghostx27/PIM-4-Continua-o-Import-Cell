
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
    // Disponibiliza o catálogo e as operações administrativas sobre smartphones.
    public class SmartphoneController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SmartphoneController(AppDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // GET - LISTAR TODOS OS SMARTPHONES
        // Público
        // ==========================================

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var smartphones = await _context.Smartphones
                .AsNoTracking()
                .Select(s => new
                {
                    s.ProdutoId,
                    s.Nome,
                    s.Marca,
                    s.Descricao,
                    s.PrecoBase,
                    s.ImagemBase,
                    s.DataCadastro,
                    s.Ativo
                })
                .ToListAsync();

            return Ok(smartphones);
        }

        // ==========================================
        // GET - CONSULTAR SMARTPHONE POR ID
        // Público
        // ==========================================

        [AllowAnonymous]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetPorId(int id)
        {
            var smartphone = await _context.Smartphones
                .AsNoTracking()
                .Where(s => s.ProdutoId == id)
                .Select(s => new
                {
                    s.ProdutoId,
                    s.Nome,
                    s.Marca,
                    s.Descricao,
                    s.PrecoBase,
                    s.ImagemBase,
                    s.DataCadastro,
                    s.Ativo
                })
                .FirstOrDefaultAsync();

            if (smartphone == null)
            {
                return NotFound(
                    "Smartphone não encontrado."
                );
            }

            return Ok(smartphone);
        }

        // ==========================================
        // POST - CADASTRAR SMARTPHONE
        // Somente administrador
        // ==========================================

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Criar(
            [FromBody] CriarSmartphoneDto dados)
        {
            if (dados == null)
            {
                return BadRequest(
                    "Informe os dados do smartphone."
                );
            }

            if (string.IsNullOrWhiteSpace(dados.Nome) ||
                string.IsNullOrWhiteSpace(dados.Marca))
            {
                return BadRequest(
                    "Nome e marca são obrigatórios."
                );
            }

            if (dados.PrecoBase <= 0)
            {
                return BadRequest(
                    "O preço deve ser maior que zero."
                );
            }

            var smartphone = new Smartphone
            {
                Nome = dados.Nome.Trim(),
                Marca = dados.Marca.Trim(),
                Descricao = dados.Descricao?.Trim()
                    ?? string.Empty,
                PrecoBase = dados.PrecoBase,
                ImagemBase = dados.ImagemBase?.Trim(),
                DataCadastro = DateTime.UtcNow,
                Ativo = true
            };

            _context.Smartphones.Add(smartphone);

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetPorId),
                new { id = smartphone.ProdutoId },
                new
                {
                    mensagem = "Smartphone cadastrado com sucesso!",
                    smartphone.ProdutoId,
                    smartphone.Nome,
                    smartphone.Marca,
                    smartphone.Descricao,
                    smartphone.PrecoBase,
                    smartphone.ImagemBase,
                    smartphone.DataCadastro,
                    smartphone.Ativo
                }
            );
        }

        // ==========================================
        // PUT - ATUALIZAR SMARTPHONE
        // Somente administrador
        // ==========================================

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Atualizar(
            int id,
            [FromBody] CriarSmartphoneDto dados)
        {
            if (dados == null)
            {
                return BadRequest(
                    "Informe os dados do smartphone."
                );
            }

            var smartphone = await _context.Smartphones
                .FirstOrDefaultAsync(s => s.ProdutoId == id);

            if (smartphone == null)
            {
                return NotFound(
                    "Smartphone não encontrado."
                );
            }

            if (string.IsNullOrWhiteSpace(dados.Nome) ||
                string.IsNullOrWhiteSpace(dados.Marca))
            {
                return BadRequest(
                    "Nome e marca são obrigatórios."
                );
            }

            if (dados.PrecoBase <= 0)
            {
                return BadRequest(
                    "O preço deve ser maior que zero."
                );
            }

            smartphone.Nome = dados.Nome.Trim();
            smartphone.Marca = dados.Marca.Trim();
            smartphone.Descricao = dados.Descricao?.Trim()
                ?? string.Empty;
            smartphone.PrecoBase = dados.PrecoBase;
            smartphone.ImagemBase = dados.ImagemBase?.Trim();

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensagem = "Smartphone atualizado com sucesso!",
                smartphone.ProdutoId,
                smartphone.Nome,
                smartphone.Marca,
                smartphone.Descricao,
                smartphone.PrecoBase,
                smartphone.ImagemBase,
                smartphone.Ativo
            });
        }

        // ==========================================
        // DELETE - DESATIVAR SMARTPHONE
        // Exclusão lógica
        // Somente administrador
        // ==========================================

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Excluir(int id)
        {
            var smartphone = await _context.Smartphones
                .FirstOrDefaultAsync(s => s.ProdutoId == id);

            if (smartphone == null)
            {
                return NotFound(
                    "Smartphone não encontrado."
                );
            }

            if (!smartphone.Ativo)
            {
                return BadRequest(
                    "Este smartphone já está desativado."
                );
            }

            // Preserva os registros e pedidos históricos.
            smartphone.Ativo = false;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensagem = "Smartphone desativado com sucesso!",
                smartphone.ProdutoId,
                smartphone.Nome,
                smartphone.Ativo
            });
        }
    }

    // ==========================================
    // DTO - DADOS RECEBIDOS PELO POST E PUT
    // ==========================================

    public class CriarSmartphoneDto
    {
        public string Nome { get; set; } = string.Empty;

        public string Marca { get; set; } = string.Empty;

        public string Descricao { get; set; } = string.Empty;

        public decimal PrecoBase { get; set; }

        public string? ImagemBase { get; set; }
    }
}
