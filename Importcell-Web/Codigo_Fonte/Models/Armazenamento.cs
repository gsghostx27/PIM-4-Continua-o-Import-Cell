
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ImportCell.Models
{
    // Representa uma opção de capacidade, preço e estoque.
    public class Armazenamento
    {
        [Key]
        public int ArmazenamentoId { get; set; }

        [Required]
        public int ProdutoId { get; set; }

        [Required]
        [MaxLength(30)]
        public string Tamanho { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        public decimal Preco { get; set; }

        public int Estoque { get; set; }

        // Relacionamento com Smartphone
        [ForeignKey(nameof(ProdutoId))]
        public Smartphone Smartphone { get; set; } = null!;

        // Relacionamento com ItemPedido
        public ICollection<ItemPedido> ItensPedido { get; set; }
            = new List<ItemPedido>();
    }
}