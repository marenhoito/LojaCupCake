
using System.ComponentModel.DataAnnotations;

namespace LojaCupCake.Models
{
    public enum FormaPagamento
    {
        Pix,
        Cartao,
        Dinheiro
    }

    public enum StatusPagamento
    {
        Pendente,
        Aprovado,
        Recusado,
        Cancelado,
        Estornado
    }

    public class Pagamento
    {
        public long PagamentoId { get; set; }

        public long PedidoId { get; set; }

        public FormaPagamento FormaPagamento { get; set; }

        public StatusPagamento StatusPagamento { get; set; }
            = StatusPagamento.Pendente;

        public decimal Valor { get; set; }

        [StringLength(80)]
        public string? CodigoTransacao { get; set; }

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        public DateTime? AtualizadoEm { get; set; }

        // Relacionamento
        public Pedido Pedido { get; set; } = null!;
    }
}
