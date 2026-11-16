
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ImportCell.Models
{
    // Representa uma conta de usuário do sistema.
    public class Usuario
    {
        [Key]
        public int UsuarioId { get; set; }

        [Required]
        [MaxLength(150)]
        public string Nome { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Senha { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Telefone { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Endereco { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Cidade { get; set; } = string.Empty;

        [MaxLength(10)]
        public string Cep { get; set; } = string.Empty;

        // Identifica se o usuário é administrador
        public bool IsAdmin { get; set; } = false;

        // Relacionamento 1:N com Pedido
        public ICollection<Pedido> Pedidos { get; set; }
            = new List<Pedido>();
    }
}
