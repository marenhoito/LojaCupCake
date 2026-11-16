
using System.ComponentModel.DataAnnotations;

namespace LojaCupCake.Models
{
    public class HistoricoStatusPedido
    {
        public long HistoricoId { get; set; }

        public long PedidoId { get; set; }

        public StatusPedido? StatusAnterior { get; set; }

        public StatusPedido StatusNovo { get; set; }

        [StringLength(500)]
        public string? Observacao { get; set; }

        // Null quando a alteração for automática
        public string? UsuarioId { get; set; }

        public DateTime DataAlteracao { get; set; }
            = DateTime.UtcNow;

        // Relacionamentos
        public Pedido Pedido { get; set; } = null!;

        public ApplicationUser? Responsavel { get; set; }
    }
}
