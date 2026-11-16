using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ImportCell.Data;
using ImportCell.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ImportCell.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    // Registra, consulta e cancela pedidos, preservando itens e controlando o estoque.
    public class PedidoController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PedidoController(AppDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // IDENTIFICAR USUARIO AUTENTICADO
        // ==========================================

        private int? ObterUsuarioAutenticado()
        {
            var identificador = User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );

            if (int.TryParse(identificador, out int id))
                return id;

            return null;
        }

         // ==========================================
        // GET - LISTAR PEDIDOS DO USUARIO
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var usuarioId = ObterUsuarioAutenticado();

            if (usuarioId == null)
                return Unauthorized();

            var pedidos = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.UsuarioId == usuarioId.Value)
                .Select(p => new
                {
                    p.PedidoId,
                    p.UsuarioId,
                    p.DataPedido,
                    p.EnderecoEntrega,
                    p.CidadeEntrega,
                    p.CepEntrega,
                    p.MetodoPagamento,
                    p.Subtotal,
                    p.Frete,
                    p.ValorTotal,
                    p.Status,

                    Itens = p.Itens.Select(i => new
                    {
                        i.ItemPedidoId,
                        i.ArmazenamentoId,
                        i.CorId,
                        NomeCor = i.Cor != null
                            ? i.Cor.NomeCor
                            : null,
                        i.Quantidade,
                        i.PrecoUnitario,
                        i.Subtotal
                    })
                })
                .ToListAsync();

            return Ok(pedidos);
        }

          // ==========================================
        // GET - CONSULTAR PEDIDO POR ID
        // ==========================================

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetPorId(int id)
        {
            var usuarioId = ObterUsuarioAutenticado();

            if (usuarioId == null)
                return Unauthorized();

            var pedido = await _context.Pedidos
                .AsNoTracking()
                .Where(p =>
                    p.PedidoId == id &&
                    p.UsuarioId == usuarioId.Value)
                .Select(p => new
                {
                    p.PedidoId,
                    p.UsuarioId,
                    p.DataPedido,
                    p.EnderecoEntrega,
                    p.CidadeEntrega,
                    p.CepEntrega,
                    p.MetodoPagamento,
                    p.Subtotal,
                    p.Frete,
                    p.ValorTotal,
                    p.Status,

                    Itens = p.Itens.Select(i => new
                    {
                        i.ItemPedidoId,
                        i.ArmazenamentoId,
                        i.CorId,
                        NomeCor = i.Cor != null
                            ? i.Cor.NomeCor
                            : null,
                        i.Quantidade,
                        i.PrecoUnitario,
                        i.Subtotal
                    })
                })
                .FirstOrDefaultAsync();

            if (pedido == null)
                return NotFound("Pedido não encontrado.");

            return Ok(pedido);
        }

        // ==========================================
        // POST - CRIAR PEDIDO
        // ==========================================

        [HttpPost]
        public async Task<IActionResult> Criar(
            [FromBody] CriarPedidoDto dados)
        {
            var usuarioId = ObterUsuarioAutenticado();

            if (usuarioId == null)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(dados.EnderecoEntrega) ||
                string.IsNullOrWhiteSpace(dados.CidadeEntrega) ||
                string.IsNullOrWhiteSpace(dados.CepEntrega) ||
                string.IsNullOrWhiteSpace(dados.MetodoPagamento) ||
                dados.Itens == null ||
                dados.Itens.Count == 0)
            {
                return BadRequest("Dados do pedido inválidos.");
            }

            // Validar itens
            if (dados.Itens.Any(i =>
                i.ArmazenamentoId <= 0 ||
                i.CorId <= 0 ||
                i.Quantidade <= 0))
            {
                return BadRequest(
                    "Informe armazenamento, cor e quantidade válidos."
                );
            }

            // Impedir itens duplicados
            if (dados.Itens
                .GroupBy(i => new
                {
                    i.ArmazenamentoId,
                    i.CorId
                })
                .Any(g => g.Count() > 1))
            {
                return BadRequest(
                    "Não repita o mesmo armazenamento e cor no pedido."
                );
            }

            var usuarioExiste = await _context.Usuarios
                .AnyAsync(u => u.UsuarioId == usuarioId.Value);

            if (!usuarioExiste)
                return Unauthorized();

            // ======================================
            // INICIAR TRANSACAO
            // ======================================

            await using var transacao =
                await _context.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable
                );

            var armazenamentoIds = dados.Itens
                .Select(i => i.ArmazenamentoId)
                .Distinct()
                .ToList();

            var corIds = dados.Itens
                .Select(i => i.CorId)
                .Distinct()
                .ToList();

            // Buscar armazenamentos
            var armazenamentos = await _context.Armazenamentos
                .Where(a => armazenamentoIds.Contains(
                    a.ArmazenamentoId))
                .ToDictionaryAsync(a => a.ArmazenamentoId);

            // Buscar cores
            var cores = await _context.Cores
                .Where(c => corIds.Contains(c.CorId))
                .ToDictionaryAsync(c => c.CorId);

            var produtoIds = armazenamentos.Values
                .Select(a => a.ProdutoId)
                .Distinct()
                .ToList();

            // Verificar smartphones ativos
            var produtosAtivos = await _context.Smartphones
                .Where(s =>
                    produtoIds.Contains(s.ProdutoId) &&
                    s.Ativo)
                .Select(s => s.ProdutoId)
                .ToListAsync();

            var produtosAtivosSet = produtosAtivos.ToHashSet();

             // ======================================
            // CRIAR PEDIDO
            // ======================================

            var pedido = new Pedido
            {
                UsuarioId = usuarioId.Value,
                DataPedido = DateTime.UtcNow,
                EnderecoEntrega = dados.EnderecoEntrega.Trim(),
                CidadeEntrega = dados.CidadeEntrega.Trim(),
                CepEntrega = dados.CepEntrega.Trim(),
                MetodoPagamento = dados.MetodoPagamento.Trim(),
                Status = "Pendente",
                Frete = 15.00m
            };

            decimal subtotal = 0;

            foreach (var itemDto in dados.Itens)
            {
                // Verificar armazenamento
                if (!armazenamentos.TryGetValue(
                    itemDto.ArmazenamentoId,
                    out var armazenamento))
                {
                    return BadRequest(
                        "Um dos armazenamentos não existe."
                    );
                }

                // Verificar cor
                if (!cores.TryGetValue(
                    itemDto.CorId,
                    out var cor))
                {
                    return BadRequest(
                        "Uma das cores informadas não existe."
                    );
                }

                // Verificar se a cor pertence ao smartphone
                if (cor.ProdutoId != armazenamento.ProdutoId)
                {
                    return BadRequest(
                        "A cor selecionada não pertence ao smartphone escolhido."
                    );
                }

                // Verificar se o smartphone está ativo
                if (!produtosAtivosSet.Contains(
                    armazenamento.ProdutoId))
                {
                    return BadRequest(
                        "O smartphone selecionado está inativo ou indisponível."
                    );
                }

                // Verificar estoque
                if (armazenamento.Estoque < itemDto.Quantidade)
                {
                    return BadRequest(
                        $"Estoque insuficiente para {armazenamento.Tamanho}."
                    );
                }

                // Calcular valores
                decimal precoUnitario = armazenamento.Preco;

                decimal subtotalItem =
                    precoUnitario * itemDto.Quantidade;

                // Atualizar estoque
                armazenamento.Estoque -= itemDto.Quantidade;

                subtotal += subtotalItem;

                // Adicionar item ao pedido
                pedido.Itens.Add(new ItemPedido
                {
                    ArmazenamentoId = armazenamento.ArmazenamentoId,
                    CorId = cor.CorId,
                    Quantidade = itemDto.Quantidade,
                    PrecoUnitario = precoUnitario,
                    Subtotal = subtotalItem
                });
            }

            // Calcular valores finais
            pedido.Subtotal = subtotal;
            pedido.ValorTotal = subtotal + pedido.Frete;

            _context.Pedidos.Add(pedido);

            await _context.SaveChangesAsync();

            await transacao.CommitAsync();

            return StatusCode(201, new
            {
                mensagem = "Pedido criado com sucesso!",
                pedido.PedidoId,
                pedido.UsuarioId,
                pedido.Subtotal,
                pedido.Frete,
                pedido.ValorTotal,
                pedido.Status,
                itens = pedido.Itens.Select(i => new
                {
                    i.ArmazenamentoId,
                    i.CorId,
                    NomeCor = cores[i.CorId!.Value].NomeCor,
                    i.Quantidade,
                    i.PrecoUnitario,
                    i.Subtotal
                })
            });
        }

        // ==========================================
        // PUT - ATUALIZAR STATUS (SOMENTE ADMIN)
        // ==========================================

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}/status")]
        public async Task<IActionResult> AtualizarStatus(
            int id,
            [FromBody] AtualizarStatusPedidoDto dados)
        {
            if (dados == null || string.IsNullOrWhiteSpace(dados.Status))
                return BadRequest("Informe um status válido.");

            var novoStatus = dados.Status.Trim();
            var statusPermitidos = new[] { "Pendente", "Pago", "Enviado", "Entregue" };

            if (!statusPermitidos.Contains(novoStatus, StringComparer.OrdinalIgnoreCase))
                return BadRequest("Status permitido: Pendente, Pago, Enviado ou Entregue.");

            novoStatus = statusPermitidos.First(s =>
                s.Equals(novoStatus, StringComparison.OrdinalIgnoreCase));

            await using var transacao =
                await _context.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable);

            var pedido = await _context.Pedidos
                .FirstOrDefaultAsync(p => p.PedidoId == id);

            if (pedido == null)
                return NotFound("Pedido não encontrado.");

            if (pedido.Status == novoStatus)
                return Conflict("O pedido já está com esse status.");

            bool transicaoPermitida =
                (pedido.Status == "Pendente" && novoStatus == "Pago") ||
                (pedido.Status == "Pago" && novoStatus == "Enviado") ||
                (pedido.Status == "Enviado" && novoStatus == "Entregue");

            if (!transicaoPermitida)
                return Conflict(
                    $"Não é permitido alterar o status de '{pedido.Status}' para '{novoStatus}'.");

            pedido.Status = novoStatus;
            await _context.SaveChangesAsync();
            await transacao.CommitAsync();

            return Ok(new
            {
                mensagem = "Status do pedido atualizado com sucesso!",
                pedido.PedidoId,
                pedido.UsuarioId,
                pedido.Status
            });
        }

        // ==========================================
        // DELETE - CANCELAR PEDIDO PENDENTE
        // ==========================================

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Excluir(int id)
        {
            var usuarioId = ObterUsuarioAutenticado();

            if (usuarioId == null)
                return Unauthorized();

            await using var transacao =
                await _context.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable
                );

            var pedido = await _context.Pedidos
                .Include(p => p.Itens)
                .FirstOrDefaultAsync(p =>
                    p.PedidoId == id &&
                    p.UsuarioId == usuarioId.Value);

            if (pedido == null)
                return NotFound("Pedido não encontrado.");

            if (pedido.Status != "Pendente")
            {
                return Conflict(
                    "Somente pedidos pendentes podem ser cancelados."
                );
            }

            // Restaurar estoque
            foreach (var item in pedido.Itens)
            {
                var armazenamento = await _context.Armazenamentos
                    .FindAsync(item.ArmazenamentoId);

                if (armazenamento != null)
                {
                    armazenamento.Estoque += item.Quantidade;
                }
            }

           pedido.Status = "Cancelado";

            await _context.SaveChangesAsync();

            await transacao.CommitAsync();

            return Ok(new
            {
                mensagem = "Pedido cancelado e estoque restaurado!",
                pedidoId = id
            });
        }
    }

    // ==========================================
    // DTO - CRIAR PEDIDO
    // ==========================================

    public class CriarPedidoDto
    {
        // O ID do usuário é obtido pelo token JWT.
        // Esta propriedade é mantida por compatibilidade.
        public int UsuarioId { get; set; }

        public string EnderecoEntrega { get; set; } = "";

        public string CidadeEntrega { get; set; } = "";

        public string CepEntrega { get; set; } = "";

        public string MetodoPagamento { get; set; } = "";

        public List<CriarItemPedidoDto> Itens { get; set; } = new();
    }

    // ==========================================
    // DTO - ITEM DO PEDIDO
    // ==========================================

    public class CriarItemPedidoDto
    {
        public int ArmazenamentoId { get; set; }

        public int CorId { get; set; }

        public int Quantidade { get; set; }
    }

    // ==========================================
    // DTO - ATUALIZAR STATUS
    // ==========================================

    public class AtualizarStatusPedidoDto
    {
        public string Status { get; set; } = "";
    }
}
