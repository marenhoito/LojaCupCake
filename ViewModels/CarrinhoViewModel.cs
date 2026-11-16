
namespace LojaCupCake.ViewModels
{
    public class CarrinhoViewModel
    {
        public string NomeFilial { get; set; } = string.Empty;

        public bool FilialDisponivel { get; set; }

        public bool TodosItensDisponiveis { get; set; } = true;

        public List<CarrinhoItemViewModel> Itens { get; set; }
            = new();

        public decimal Subtotal { get; set; }
    }

    public class CarrinhoItemViewModel
    {
        public long ProdutoId { get; set; }

        public string Codigo { get; set; } = string.Empty;

        public string NomeProduto { get; set; } = string.Empty;

        public string? ImagemUrl { get; set; }

        public decimal PrecoUnitario { get; set; }

        public int Quantidade { get; set; }

        public int EstoqueDisponivel { get; set; }

        public bool ProdutoDisponivel { get; set; }

        public decimal TotalItem { get; set; }
    }
}
