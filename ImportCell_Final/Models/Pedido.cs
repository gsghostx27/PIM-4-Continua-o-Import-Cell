using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ImportCell.Models
{
    // Representa um pedido com entrega, pagamento demonstrativo e status.
    public class Pedido
    {
        [Key]
        public int PedidoId { get; set; }

        [Required]
        public int UsuarioId { get; set; }

        public DateTime DataPedido { get; set; } = DateTime.Now;

        [Required]
        [MaxLength(200)]
        public string EnderecoEntrega { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string CidadeEntrega { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string CepEntrega { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        public string MetodoPagamento { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        public decimal Subtotal { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Frete { get; set; } = 15.00m;

        [Column(TypeName = "decimal(10,2)")]
        public decimal ValorTotal { get; set; }

        [MaxLength(30)]
        public string Status { get; set; } = "Pendente";

        // Chave estrangeira
        public Usuario Usuario { get; set; } = null!;

        // Relacionamento 1:N com ItemPedido
        public ICollection<ItemPedido> Itens { get; set; }
            = new List<ItemPedido>();
    }
}