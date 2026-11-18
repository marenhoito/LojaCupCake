
using System.ComponentModel.DataAnnotations;

namespace LojaCupCake.Models
{
    public class AvaliacaoProduto
    {
        public long AvaliacaoProdutoId { get; set; }

        public long PedidoId { get; set; }

        public long ProdutoId { get; set; }

        [Range(1, 5)]
        public int Nota { get; set; }

        [StringLength(500)]
        public string? Comentario { get; set; }

        public DateTime DataAvaliacao { get; set; } = DateTime.UtcNow;

        public Pedido Pedido { get; set; } = null!;

        public Produto Produto { get; set; } = null!;
    }
}
