using Microsoft.AspNetCore.Mvc.Rendering;

namespace LojaCupCake.ViewModels
{
    public class CatalogoViewModel
    {
        public long? FilialId { get; set; }
        public long? CategoriaId { get; set; }
        public string Ordenacao { get; set; } = "pedidos-desc";
        public bool CarrinhoPossuiItens { get; set; }

        public string NomeFilialSelecionada { get; set; } = string.Empty;
        public bool FilialAberta { get; set; }
        public string StatusFilialSelecionada { get; set; } = string.Empty;

        public List<FilialCatalogoViewModel> Filiais { get; set; } = new();
        public List<SelectListItem> Categorias { get; set; } = new();
        public List<ProdutoCatalogoViewModel> Produtos { get; set; } = new();

        public List<CarrinhoLateralItemViewModel> ItensCarrinho { get; set; } = new();
        public string NomeFilialCarrinho { get; set; } = string.Empty;
        public int QuantidadeItensCarrinho { get; set; }
        public decimal TotalCarrinho { get; set; }
        public bool CarrinhoFilialDisponivel { get; set; }
        public bool CarrinhoLojaAberta { get; set; }
        public string StatusFuncionamentoCarrinho { get; set; } = string.Empty;
        public bool CarrinhoTodosItensDisponiveis { get; set; }
        public bool CarrinhoPodeFinalizar { get; set; }
    }

    public class FilialCatalogoViewModel
    {
        public long FilialId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string EnderecoResumido { get; set; } = string.Empty;
        public bool AbertaAgora { get; set; }
        public string StatusFuncionamento { get; set; } = string.Empty;
        public string ModoFuncionamentoDescricao { get; set; } = string.Empty;
        public string HorariosHoje { get; set; } = string.Empty;
        public List<string> HorariosSemana { get; set; } = new();
        public bool Selecionada { get; set; }
        public bool SelecaoBloqueada { get; set; }
    }

    public class ProdutoCatalogoViewModel
    {
        public long ProdutoId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public string? ImagemUrl { get; set; }
        public decimal Preco { get; set; }
        public string NomeCategoria { get; set; } = string.Empty;
        public int? EstoqueDisponivel { get; set; }
        public int QuantidadePedidos { get; set; }
    }

    public class CarrinhoLateralItemViewModel
    {
        public long ProdutoId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? ImagemUrl { get; set; }
        public decimal PrecoUnitario { get; set; }
        public int Quantidade { get; set; }
        public decimal TotalItem { get; set; }
        public int EstoqueDisponivel { get; set; }
        public bool Disponivel { get; set; }
    }
}
