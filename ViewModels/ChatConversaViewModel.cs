namespace LojaCupCake.ViewModels
{
    public class ChatConversaViewModel
    {
        public long PedidoId { get; set; }
        public string CodigoPedido { get; set; } = string.Empty;
        public string NomeFilial { get; set; } = string.Empty;
        public string NomeCliente { get; set; } = string.Empty;
        public string UltimaMensagem { get; set; } = string.Empty;
        public DateTime? DataUltimaMensagem { get; set; }
        public int MensagensNaoLidas { get; set; }
    }
}
