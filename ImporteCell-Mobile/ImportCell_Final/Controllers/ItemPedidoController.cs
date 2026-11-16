
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
    // Consulta os itens vinculados aos pedidos conforme as permissões da API.
    public class ItemPedidoController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ItemPedidoController(AppDbContext context)
        {
            _context = context;
        }

        // Identificar o usuário pelo token JWT
        private int? ObterUsuarioAutenticado()
        {
            var identificador = User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );

            if (int.TryParse(identificador, out int id))
                return id;

            return null;
        }

        // GET - Listar itens dos próprios pedidos
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var usuarioId = ObterUsuarioAutenticado();

            if (usuarioId == null)
                return Unauthorized();

            var itens = await _context.Set<ItemPedido>()
                .AsNoTracking()
                .Where(i => _context.Pedidos.Any(p =>
                    p.PedidoId == i.PedidoId &&
                    p.UsuarioId == usuarioId.Value))
                .Select(i => new
                {
                    i.ItemPedidoId,
                    i.PedidoId,
                    i.ArmazenamentoId,
                    i.Quantidade,
                    i.PrecoUnitario,
                    i.Subtotal
                })
                .ToListAsync();

            return Ok(itens);
        }

        // GET - Consultar um item pelo ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPorId(int id)
        {
            var usuarioId = ObterUsuarioAutenticado();

            if (usuarioId == null)
                return Unauthorized();

            var item = await _context.Set<ItemPedido>()
                .AsNoTracking()
                .Where(i =>
                    i.ItemPedidoId == id &&
                    _context.Pedidos.Any(p =>
                        p.PedidoId == i.PedidoId &&
                        p.UsuarioId == usuarioId.Value))
                .Select(i => new
                {
                    i.ItemPedidoId,
                    i.PedidoId,
                    i.ArmazenamentoId,
                    i.Quantidade,
                    i.PrecoUnitario,
                    i.Subtotal
                })
                .FirstOrDefaultAsync();

            if (item == null)
                return NotFound("Item do pedido não encontrado.");

            return Ok(item);
        }

        // GET - Consultar itens de um pedido próprio
        [HttpGet("pedido/{pedidoId}")]
        public async Task<IActionResult> GetPorPedido(int pedidoId)
        {
            var usuarioId = ObterUsuarioAutenticado();

            if (usuarioId == null)
                return Unauthorized();

            var pedidoPertenceAoUsuario = await _context.Pedidos
                .AsNoTracking()
                .AnyAsync(p =>
                    p.PedidoId == pedidoId &&
                    p.UsuarioId == usuarioId.Value);

            if (!pedidoPertenceAoUsuario)
                return NotFound("Pedido não encontrado.");

            var itens = await _context.Set<ItemPedido>()
                .AsNoTracking()
                .Where(i => i.PedidoId == pedidoId)
                .Select(i => new
                {
                    i.ItemPedidoId,
                    i.PedidoId,
                    i.ArmazenamentoId,
                    i.Quantidade,
                    i.PrecoUnitario,
                    i.Subtotal
                })
                .ToListAsync();

            return Ok(itens);
        }
    }
}
