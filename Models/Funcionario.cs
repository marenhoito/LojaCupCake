
using System.ComponentModel.DataAnnotations;

namespace LojaCupCake.Models
{
    public class Funcionario
    {
        public long FuncionarioId { get; set; }

        [Required]
        public string UsuarioId { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string Matricula { get; set; } = string.Empty;

        [Required]
        [StringLength(120)]
        public string NomeCompleto { get; set; } = string.Empty;

        public DateTime DataCadastro { get; set; } = DateTime.UtcNow;

        public bool Ativo { get; set; } = true;

        // Relacionamento com a conta Identity
        public ApplicationUser Usuario { get; set; } = null!;


        public ICollection<FuncionarioFilial> Filiais { get; set; }
           = new List<FuncionarioFilial>();
    }
}
