
namespace LojaCupCake.ViewModels
{
    public class GestaoPedidosIndexViewModel
    {
        public long? FilialId { get; set; }

        public List<FilialGestaoPedidosViewModel> Filiais { get; set; }
            = new();

        public FilialGestaoPedidosViewModel? FilialSelecionada { get; set; }

        public List<PedidoResumoViewModel> Pedidos { get; set; }
            = new();
    }

    public class FilialGestaoPedidosViewModel
    {
        public long FilialId { get; set; }

        public string Codigo { get; set; } = string.Empty;

        public string Nome { get; set; } = string.Empty;

        public string? Telefone { get; set; }

        public string? Email { get; set; }

        public string Logradouro { get; set; } = string.Empty;

        public string Numero { get; set; } = string.Empty;

        public string? Complemento { get; set; }

        public string Bairro { get; set; } = string.Empty;

        public string Cidade { get; set; } = string.Empty;

        public string Uf { get; set; } = string.Empty;

        public int TotalPedidos { get; set; }

        public int PedidosPendentes { get; set; }

        public int PedidosEmAndamento { get; set; }

        public int PedidosConcluidos { get; set; }

        public bool Ativa { get; set; }
    }
}
