
using LojaCupCake.Models;

namespace LojaCupCake.ViewModels
{
    public class PedidoResumoViewModel
    {
        public long PedidoId { get; set; }

        public string CodigoPedido { get; set; } = string.Empty;

        public string NomeFilial { get; set; } = string.Empty;

        public DateTime DataPedido { get; set; }

        public StatusPedido StatusPedido { get; set; }

        public TipoRecebimento TipoRecebimento { get; set; }

        public decimal Total { get; set; }

        public StatusPagamento? StatusPagamento { get; set; }

        public FormaPagamento? FormaPagamento { get; set; }

        public string NomeCliente { get; set; } = string.Empty;
    }
}
