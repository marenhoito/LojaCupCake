
using System.ComponentModel.DataAnnotations;

namespace LojaCupCake.Models
{
    public enum ModoFuncionamentoFilial : byte
    {
        Automatico = 0,
        ForcarAberta = 1,
        ForcarFechada = 2
    }

    public class Filial
    {
        public long FilialId { get; set; }

        [Required]
        [StringLength(20)]
        public string Codigo { get; set; } = string.Empty;

        [Required]
        [StringLength(120)]
        public string Nome { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Telefone { get; set; }

        [StringLength(180)]
        public string? Email { get; set; }

        [Required]
        [StringLength(9)]
        public string Cep { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string Logradouro { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Numero { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Complemento { get; set; }

        [Required]
        [StringLength(100)]
        public string Bairro { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Cidade { get; set; } = string.Empty;

        [Required]
        [StringLength(2)]
        public string Uf { get; set; } = string.Empty;

        public DateTime DataCadastro { get; set; } = DateTime.UtcNow;

        public bool Ativo { get; set; } = true;

        // Automatico segue FuncionamentoFilial; os demais modos permitem intervenção autorizada.
        public ModoFuncionamentoFilial ModoFuncionamento { get; set; }
            = ModoFuncionamentoFilial.Automatico;

        // Funcionários vinculados à filial
        public ICollection<FuncionarioFilial> Funcionarios { get; set; }
          = new List<FuncionarioFilial>();


        public ICollection<Estoque> Estoques { get; set; }
          = new List<Estoque>();

        public ICollection<Pedido> Pedidos { get; set; }
          = new List<Pedido>();

        public ICollection<FuncionamentoFilial> Funcionamentos { get; set; }
          = new List<FuncionamentoFilial>();
    }
}
