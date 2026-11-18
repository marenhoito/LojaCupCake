using System.ComponentModel.DataAnnotations;

namespace LojaCupCake.Models
{
    public class Produto
    {
        public long ProdutoId { get; set; }

        public long CategoriaId { get; set; }

        [Required]
        [StringLength(30)]
        public string Codigo { get; set; } = string.Empty;

        [Required]
        [StringLength(120)]
        public string Nome { get; set; } = string.Empty;

        public string? Descricao { get; set; }

        [Range(
            typeof(decimal),
            "0.01",
            "99999999",
            ParseLimitsInInvariantCulture = true,
            ConvertValueInInvariantCulture = true
        )]
        public decimal PrecoAtual { get; set; }

        [StringLength(500)]
        public string? ImagemUrl { get; set; }

        public bool Ativo { get; set; } = true;

        public DateTime DataCadastro { get; set; } = DateTime.UtcNow;

        public Categoria Categoria { get; set; } = null!;

        public ICollection<Estoque> Estoques { get; set; }
            = new List<Estoque>();

        public ICollection<ItemPedido> ItensPedido { get; set; }
            = new List<ItemPedido>();


        public ICollection<AvaliacaoProduto> Avaliacoes { get; set; }
    = new List<AvaliacaoProduto>();


    }
}
