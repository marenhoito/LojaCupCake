
using System.ComponentModel.DataAnnotations;

namespace LojaCupCake.ViewModels
{
    public class EstoqueMinimoViewModel
    {
        public long FilialId { get; set; }

        public long ProdutoId { get; set; }

        public string NomeFilial { get; set; } = string.Empty;

        public string NomeProduto { get; set; } = string.Empty;

        public int QuantidadeAtual { get; set; }

        [Range(0, int.MaxValue,
            ErrorMessage = "A quantidade mínima não pode ser negativa.")]
        [Display(Name = "Quantidade mínima")]
        public int QuantidadeMinima { get; set; }
    }
}
