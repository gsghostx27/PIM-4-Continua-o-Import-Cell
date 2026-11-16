
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ImportCell.Models
{
    // Representa uma opção de cor de um smartphone.
    public class Cor
    {
        [Key]
        public int CorId { get; set; }

        [Required]
        public int ProdutoId { get; set; }

        [Required]
        [MaxLength(50)]
        public string NomeCor { get; set; } = string.Empty;

        [MaxLength(7)]
        public string? CodigoHex { get; set; }

        [MaxLength(500)]
        public string? ImagemCor { get; set; }

        // Relacionamento com Smartphone
        [ForeignKey(nameof(ProdutoId))]
        public Smartphone Smartphone { get; set; } = null!;

        // Relacionamento com ItemPedido
        public ICollection<ItemPedido> ItensPedido { get; set; }
            = new List<ItemPedido>();
    }
}
