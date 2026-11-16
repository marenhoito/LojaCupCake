
using System.ComponentModel.DataAnnotations;
using LojaCupCake.Models;

namespace LojaCupCake.ViewModels
{
    public class MovimentacaoEstoqueViewModel
    {
        public long FilialId { get; set; }

        public long ProdutoId { get; set; }

        public string NomeFilial { get; set; } = string.Empty;

        public string NomeProduto { get; set; } = string.Empty;

        [Required(ErrorMessage = "Selecione o tipo de movimentação.")]
        [EnumDataType(typeof(TipoMovimentacaoEstoque))]
        [Display(Name = "Tipo de movimentação")]
        public TipoMovimentacaoEstoque Tipo { get; set; }

        [Required(ErrorMessage = "Informe a quantidade.")]
        [Range(1, int.MaxValue,
            ErrorMessage = "A quantidade deve ser maior que zero.")]
        public int Quantidade { get; set; }

        [StringLength(255,
            ErrorMessage = "O motivo deve ter no máximo 255 caracteres.")]
        public string? Motivo { get; set; }
    }
}
