
using System.ComponentModel.DataAnnotations;

namespace LojaCupCake.Models
{
    public class ItemPedido
    {
        public long ItemPedidoId { get; set; }

        public long PedidoId { get; set; }

        public long ProdutoId { get; set; }

        [Required]
        [StringLength(120)]
        public string NomeProdutoSnapshot { get; set; }
            = string.Empty;

        public decimal PrecoUnitario { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantidade { get; set; }

        public decimal DescontoItem { get; set; } = 0;

        public decimal TotalItem { get; set; }

        // Relacionamentos
        public Pedido Pedido { get; set; } = null!;

        public Produto Produto { get; set; } = null!;
    }
}
