
using System.ComponentModel.DataAnnotations;

namespace LojaCupCake.Models
{
    public class AvaliacaoAtendimento
    {
        public long AvaliacaoAtendimentoId { get; set; }

        public long PedidoId { get; set; }

        [Range(1, 5)]
        public int Nota { get; set; }

        [StringLength(500)]
        public string? Comentario { get; set; }

        public DateTime DataAvaliacao { get; set; } = DateTime.UtcNow;

        public Pedido Pedido { get; set; } = null!;
    }
}
