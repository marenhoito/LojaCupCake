using Microsoft.AspNetCore.Mvc.Rendering;

namespace LojaCupCake.ViewModels
{
    public class DashboardAdminViewModel
    {
        // Mantida para compatibilidade com recursos antigos que usam filial única.
        public long? FilialId { get; set; }

        // Filiais atualmente marcadas no filtro do Dashboard.
        public List<long> FilialIds { get; set; } = new();

        public DateTime DataInicial { get; set; }

        public DateTime DataFinal { get; set; }

        public List<SelectListItem> Filiais { get; set; } = new();

        public int TotalPedidos { get; set; }

        public int PedidosPendentes { get; set; }

        public int PedidosConcluidos { get; set; }

        public int ProdutosEstoqueBaixo { get; set; }

        public decimal Faturamento { get; set; }

        public decimal TicketMedio { get; set; }

        public List<EstoqueAlertaViewModel> EstoquesBaixos { get; set; } = new();

        public List<VendaDiaViewModel> VendasPorDia { get; set; } = new();

        public List<ProdutoVendidoViewModel> ProdutosMaisVendidos { get; set; } = new();
    }

    public class EstoqueAlertaViewModel
    {
        public string NomeFilial { get; set; } = string.Empty;

        // Permite exibir se a filial do alerta está ativa ou desativada.
        public bool FilialAtiva { get; set; }

        public string NomeProduto { get; set; } = string.Empty;

        public int QuantidadeAtual { get; set; }

        public int QuantidadeMinima { get; set; }
    }

    public class VendaDiaViewModel
    {
        public DateTime Data { get; set; }

        public int QuantidadePedidos { get; set; }

        public decimal TotalVendido { get; set; }
    }

    public class ProdutoVendidoViewModel
    {
        public string NomeProduto { get; set; } = string.Empty;

        public int QuantidadeVendida { get; set; }

        public decimal TotalVendido { get; set; }
    }
}
