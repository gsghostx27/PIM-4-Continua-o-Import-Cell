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
    // Consulta versões de armazenamento, preços e controle de estoque.
    public class ArmazenamentoController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ArmazenamentoController(AppDbContext context)
        {
            _context = context;
        }

        // GET - Listar armazenamentos (público)
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var armazenamentos = await _context.Armazenamentos
                .AsNoTracking()
                .Select(a => new
                {
                    a.ArmazenamentoId,
                    a.ProdutoId,
                    a.Tamanho,
                    a.Preco,
                    a.Estoque
                })
                .ToListAsync();

            return Ok(armazenamentos);
        }

        // GET - Consultar por ID (público)
        [AllowAnonymous]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetPorId(int id)
        {
            var armazenamento = await _context.Armazenamentos
                .AsNoTracking()
                .Where(a => a.ArmazenamentoId == id)
                .Select(a => new
                {
                    a.ArmazenamentoId,
                    a.ProdutoId,
                    a.Tamanho,
                    a.Preco,
                    a.Estoque
                })
                .FirstOrDefaultAsync();

            if (armazenamento == null)
                return NotFound("Armazenamento não encontrado.");

            return Ok(armazenamento);
        }

        // GET - Armazenamentos de um smartphone (público)
        [AllowAnonymous]
        [HttpGet("smartphone/{produtoId:int}")]
        public async Task<IActionResult> GetPorSmartphone(int produtoId)
        {
            var armazenamentos = await _context.Armazenamentos
                .AsNoTracking()
                .Where(a => a.ProdutoId == produtoId)
                .Select(a => new
                {
                    a.ArmazenamentoId,
                    a.ProdutoId,
                    a.Tamanho,
                    a.Preco,
                    a.Estoque
                })
                .ToListAsync();

            return Ok(armazenamentos);
        }

        // POST - Cadastrar armazenamento (somente Admin)
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] ArmazenamentoDto dados)
        {
            if (dados == null || string.IsNullOrWhiteSpace(dados.Tamanho))
                return BadRequest("O tamanho do armazenamento é obrigatório.");

            if (dados.Preco < 0)
                return BadRequest("O preço não pode ser negativo.");

            if (dados.Estoque < 0)
                return BadRequest("O estoque não pode ser negativo.");

            var smartphoneAtivo = await _context.Smartphones
                .AnyAsync(s => s.ProdutoId == dados.ProdutoId && s.Ativo);

            if (!smartphoneAtivo)
                return BadRequest("O smartphone informado não existe ou está inativo.");

            var tamanho = dados.Tamanho.Trim();
            var duplicado = await _context.Armazenamentos.AnyAsync(a =>
                a.ProdutoId == dados.ProdutoId && a.Tamanho == tamanho);

            if (duplicado)
                return Conflict("Esse armazenamento já está cadastrado para este smartphone.");

            var armazenamento = new Armazenamento
            {
                ProdutoId = dados.ProdutoId,
                Tamanho = tamanho,
                Preco = dados.Preco,
                Estoque = dados.Estoque
            };

            _context.Armazenamentos.Add(armazenamento);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPorId),
                new { id = armazenamento.ArmazenamentoId },
                new
                {
                    armazenamento.ArmazenamentoId,
                    armazenamento.ProdutoId,
                    armazenamento.Tamanho,
                    armazenamento.Preco,
                    armazenamento.Estoque
                });
        }

        // PUT - Alterar somente tamanho e preço (somente Admin)
        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Atualizar(int id, [FromBody] AtualizarArmazenamentoDto dados)
        {
            if (dados == null || string.IsNullOrWhiteSpace(dados.Tamanho))
                return BadRequest("O tamanho do armazenamento é obrigatório.");

            if (dados.Preco < 0)
                return BadRequest("O preço não pode ser negativo.");

            var armazenamento = await _context.Armazenamentos
                .FirstOrDefaultAsync(a => a.ArmazenamentoId == id);

            if (armazenamento == null)
                return NotFound("Armazenamento não encontrado.");

            var tamanho = dados.Tamanho.Trim();
            var duplicado = await _context.Armazenamentos.AnyAsync(a =>
                a.ArmazenamentoId != id &&
                a.ProdutoId == armazenamento.ProdutoId &&
                a.Tamanho == tamanho);

            if (duplicado)
                return Conflict("Esse armazenamento já está cadastrado para este smartphone.");

            // ProdutoId e Estoque não são alterados por este endpoint.
            armazenamento.Tamanho = tamanho;
            armazenamento.Preco = dados.Preco;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensagem = "Armazenamento atualizado com sucesso!",
                armazenamento.ArmazenamentoId,
                armazenamento.ProdutoId,
                armazenamento.Tamanho,
                armazenamento.Preco,
                armazenamento.Estoque
            });
        }

        // PATCH - Ajustar estoque por diferença (somente Admin)
        // Exemplo: { "quantidade": 5 } adiciona 5 unidades;
        //          { "quantidade": -2 } retira 2 unidades.
        [Authorize(Roles = "Admin")]
        [HttpPatch("{id:int}/estoque")]
        public async Task<IActionResult> AjustarEstoque(
            int id, [FromBody] AjustarEstoqueDto dados)
        {
            if (dados == null || dados.Quantidade == 0)
                return BadRequest("Informe uma quantidade diferente de zero.");

            // UPDATE atômico evita sobrescrever pedidos concorrentes.
            var alterados = await _context.Armazenamentos
                .Where(a => a.ArmazenamentoId == id &&
                    (long)a.Estoque + dados.Quantidade >= 0 &&
                    (long)a.Estoque + dados.Quantidade <= int.MaxValue)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(a => a.Estoque, a => a.Estoque + dados.Quantidade));

            if (alterados == 0)
            {
                var existe = await _context.Armazenamentos
                    .AnyAsync(a => a.ArmazenamentoId == id);

                if (!existe)
                    return NotFound("Armazenamento não encontrado.");

                return Conflict("Ajuste inválido: o estoque não pode ficar negativo ou exceder o limite permitido.");
            }

            var estoqueAtual = await _context.Armazenamentos
                .AsNoTracking()
                .Where(a => a.ArmazenamentoId == id)
                .Select(a => a.Estoque)
                .FirstAsync();

            return Ok(new
            {
                mensagem = "Estoque ajustado com sucesso!",
                armazenamentoId = id,
                quantidadeAjustada = dados.Quantidade,
                estoqueAtual
            });
        }

        // DELETE - Bloqueado para preservar referências históricas
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public IActionResult Excluir(int id)
        {
            return StatusCode(403,
                "Exclusão de armazenamentos desabilitada para proteger os pedidos.");
        }
    }

    // DTO usado apenas no cadastro
    public class ArmazenamentoDto
    {
        public int ProdutoId { get; set; }
        public string Tamanho { get; set; } = string.Empty;
        public decimal Preco { get; set; }
        public int Estoque { get; set; }
    }

    // DTO usado apenas na atualização de tamanho e preço
    public class AtualizarArmazenamentoDto
    {
        public string Tamanho { get; set; } = string.Empty;
        public decimal Preco { get; set; }
    }

    // DTO usado apenas no ajuste de estoque
    public class AjustarEstoqueDto
    {
        public int Quantidade { get; set; }
    }
}
