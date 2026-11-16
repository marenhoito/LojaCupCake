using System.ComponentModel.DataAnnotations;

namespace LojaCupCake.Models
{
    public class ChatMensagem
    {
        public long ChatMensagemId { get; set; }

        public long PedidoId { get; set; }

        [Required]
        public string RemetenteUsuarioId { get; set; } = string.Empty;

        // Nome salvo como fotografia do momento do envio.
        [Required]
        [StringLength(120)]
        public string RemetenteNome { get; set; } = string.Empty;

        [Required]
        [StringLength(2000, MinimumLength = 1)]
        public string Conteudo { get; set; } = string.Empty;

        public DateTime EnviadaEm { get; set; } = DateTime.UtcNow;

        // Leituras independentes: a conversa pode ser lida por cada lado em momentos diferentes.
        public DateTime? LidaPeloClienteEm { get; set; }

        public DateTime? LidaPelaLojaEm { get; set; }

        public Pedido Pedido { get; set; } = null!;

        public ApplicationUser Remetente { get; set; } = null!;
    }
}
