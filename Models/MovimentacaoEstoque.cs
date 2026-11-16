
using System.ComponentModel.DataAnnotations;

namespace LojaCupCake.Models
{
    public enum TipoMovimentacaoEstoque
    {
        Entrada,
        Saida,
        AjusteEntrada,
        AjusteSaida
    }

    public class MovimentacaoEstoque
    {
        public long MovimentacaoEstoqueId { get; set; }

        public long FilialId { get; set; }

        public long ProdutoId { get; set; }

        public TipoMovimentacaoEstoque Tipo { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantidade { get; set; }

        public int SaldoAnterior { get; set; }

        public int SaldoPosterior { get; set; }

        [StringLength(255)]
        public string? Motivo { get; set; }

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        // Usuário responsável pela movimentação
        public string? UsuarioId { get; set; }

        public ApplicationUser? Responsavel { get; set; }

        // Relacionamento com Estoque
        public Estoque Estoque { get; set; } = null!;
    }
}
