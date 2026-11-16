using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ImportCell.Models
{
    // Representa um aparelho cadastrado no catálogo.
    public class Smartphone
    {
        public string? ImagemBase { get; set; }
        
        [Key]
        public int ProdutoId { get; set; }

        [Required]
        [MaxLength(150)]
        public string Nome { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Marca { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Descricao { get; set; } = string.Empty;

        public decimal PrecoBase { get; set; }

        public DateTime DataCadastro { get; set; } = DateTime.Now;

        public bool Ativo { get; set; } = true;

        // Relacionamento 1:N com Cor
        public ICollection<Cor> Cores { get; set; }
            = new List<Cor>();

        // Relacionamento 1:N com Armazenamento
        public ICollection<Armazenamento> Armazenamentos { get; set; }
            = new List<Armazenamento>();
    }
}