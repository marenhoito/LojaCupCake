
using System.ComponentModel.DataAnnotations;

namespace LojaCupCake.Models
{
    public enum TipoRecebimento
    {
        Entrega,
        Retirada
    }

    public enum StatusPedido
    {
        AguardandoAprovacao,
        Aprovado,
        Rejeitado,
        EmPreparacao,
        Pronto,
        SaiuParaEntrega,
        Concluido,
        Cancelado
    }

    public class Pedido
    {
        public long PedidoId { get; set; }

        [Required]
        [StringLength(20)]
        public string CodigoPedido { get; set; } = string.Empty;

        public long ClienteId { get; set; }

        public long FilialId { get; set; }

        public DateTime DataPedido { get; set; } = DateTime.UtcNow;

        public TipoRecebimento TipoRecebimento { get; set; }

        public StatusPedido StatusPedido { get; set; }
            = StatusPedido.AguardandoAprovacao;

        public decimal Subtotal { get; set; }

        public decimal Desconto { get; set; } = 0;

        public decimal TaxaEntrega { get; set; } = 0;

        public decimal Total { get; set; }

        [StringLength(20)]
        public string? CodigoRetirada { get; set; }

        // Entrega: o pedido só é concluído após a confirmação das duas partes.
        public bool EntregaConfirmadaPelaLoja { get; set; } = false;

        public bool EntregaConfirmadaPeloCliente { get; set; } = false;

        [StringLength(500)]
        public string? Observacoes { get; set; }

        // Relacionamentos
        public Cliente Cliente { get; set; } = null!;

        public Filial Filial { get; set; } = null!;

        public EnderecoEntregaPedido? EnderecoEntrega { get; set; }

        public ICollection<ItemPedido> Itens { get; set; }
            = new List<ItemPedido>();

        public ICollection<Pagamento> Pagamentos { get; set; }
            = new List<Pagamento>();

        public ICollection<HistoricoStatusPedido> Historico { get; set; }
            = new List<HistoricoStatusPedido>();

        public ICollection<ChatMensagem> MensagensChat { get; set; }
            = new List<ChatMensagem>();
    }
}
