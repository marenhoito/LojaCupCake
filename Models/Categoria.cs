
using System.ComponentModel.DataAnnotations;

namespace LojaCupCake.Models
{
    public class Categoria
    {
        public long CategoriaId { get; set; }

        [Required]
        [StringLength(80)]
        public string Nome { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Descricao { get; set; }

        public bool Ativa { get; set; } = true;

        public DateTime DataCadastro { get; set; } = DateTime.UtcNow;

        // Relacionamento com produtos
        public ICollection<Produto> Produtos { get; set; }
            = new List<Produto>();
    }
}
