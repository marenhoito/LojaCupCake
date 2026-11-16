
using System.ComponentModel.DataAnnotations;

namespace LojaCupCake.Models
{
    public class Cliente
    {
        public long ClienteId { get; set; }

        [Required]
        public string UsuarioId { get; set; } = string.Empty;

        [Required]
        [StringLength(120)]
        public string NomeCompleto { get; set; } = string.Empty;

        [StringLength(14)]
        public string? Cpf { get; set; }

        public DateTime DataCadastro { get; set; } = DateTime.UtcNow;

        public bool Ativo { get; set; } = true;

        // Relacionamento com a conta Identity
        public ApplicationUser Usuario { get; set; } = null!;

        public ICollection<Endereco> Enderecos { get; set; }
         = new List<Endereco>();

        public ICollection<Pedido> Pedidos { get; set; }
            = new List<Pedido>();
    }
}
