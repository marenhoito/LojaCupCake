
using System.ComponentModel.DataAnnotations;

namespace LojaCupCake.Models
{
    public class Estoque
    {
        public long FilialId { get; set; }

        public long ProdutoId { get; set; }

        [Range(0, int.MaxValue)]
        public int QuantidadeAtual { get; set; } = 0;

        [Range(0, int.MaxValue)]
        public int QuantidadeMinima { get; set; } = 0;

        public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;

        // Relacionamentos
        public Filial Filial { get; set; } = null!;

        public Produto Produto { get; set; } = null!;

        public ICollection<MovimentacaoEstoque> Movimentacoes { get; set; }
            = new List<MovimentacaoEstoque>();
    }
}
