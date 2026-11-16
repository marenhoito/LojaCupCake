namespace LojaCupCake.ViewModels
{
    public class HomeIndexViewModel
    {
        public List<ProdutoDestaqueHomeViewModel> ProdutosMaisVendidos { get; set; }
            = new();

        public List<LojaDestaqueHomeViewModel> LojasDestaque { get; set; }
            = new();

        public bool PossuiVendasConcluidas { get; set; }
    }

    public class ProdutoDestaqueHomeViewModel
    {
        public long ProdutoId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public string? ImagemUrl { get; set; }
        public decimal Preco { get; set; }
        public int QuantidadeVendida { get; set; }
    }

    public class LojaDestaqueHomeViewModel
    {
        public long FilialId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Cidade { get; set; } = string.Empty;
        public string Uf { get; set; } = string.Empty;
        public string Bairro { get; set; } = string.Empty;
        public string Logradouro { get; set; } = string.Empty;
        public string Numero { get; set; } = string.Empty;
        public string? Telefone { get; set; }
        public int PedidosConcluidos { get; set; }
    }
}
