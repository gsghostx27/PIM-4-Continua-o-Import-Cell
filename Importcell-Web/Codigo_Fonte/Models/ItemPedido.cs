
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ImportCell.Models
{
    // Representa um item e seus valores registrados no pedido.
    public class ItemPedido
    {
        [Key]
        public int ItemPedidoId { get; set; }

        [Required]
        public int PedidoId { get; set; }

        [Required]
        public int ArmazenamentoId { get; set; }

        // Cor escolhida no pedido
        public int? CorId { get; set; }

        public int Quantidade { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal PrecoUnitario { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Subtotal { get; set; }

        // Relacionamento com Pedido
        public Pedido Pedido { get; set; } = null!;

        // Relacionamento com Armazenamento
        public Armazenamento Armazenamento { get; set; } = null!;

        // Relacionamento com Cor
        [ForeignKey(nameof(CorId))]
        public Cor? Cor { get; set; }
    }
}
