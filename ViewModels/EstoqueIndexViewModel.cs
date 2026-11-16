
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LojaCupCake.ViewModels
{
    public class EstoqueIndexViewModel
    {
        public long? FilialId { get; set; }

        public string NomeFilial { get; set; } = string.Empty;

        public List<SelectListItem> Filiais { get; set; }
            = new();

        public List<EstoqueProdutoViewModel> Produtos { get; set; }
            = new();
    }

    public class EstoqueProdutoViewModel
    {
        public long ProdutoId { get; set; }

        public string Codigo { get; set; } = string.Empty;

        public string NomeProduto { get; set; } = string.Empty;

        public string NomeCategoria { get; set; } = string.Empty;

        public int QuantidadeAtual { get; set; }

        public int QuantidadeMinima { get; set; }

        public bool PossuiEstoqueCadastrado { get; set; }
    }
}
