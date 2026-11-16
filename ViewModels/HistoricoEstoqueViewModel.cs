
using LojaCupCake.Models;

namespace LojaCupCake.ViewModels
{
    public class HistoricoEstoqueViewModel
    {
        public long FilialId { get; set; }

        public long ProdutoId { get; set; }

        public string NomeFilial { get; set; } = string.Empty;

        public string NomeProduto { get; set; } = string.Empty;

        public string CodigoProduto { get; set; } = string.Empty;

        public List<MovimentacaoEstoqueItemViewModel> Movimentacoes
        { get; set; } = new();
    }

    public class MovimentacaoEstoqueItemViewModel
    {
        public TipoMovimentacaoEstoque Tipo { get; set; }

        public int Quantidade { get; set; }

        public int SaldoAnterior { get; set; }

        public int SaldoPosterior { get; set; }

        public string? Motivo { get; set; }

        public DateTime CriadoEm { get; set; }


        public string NomeResponsavel { get; set; }
         = "Não identificado";
    }
}
