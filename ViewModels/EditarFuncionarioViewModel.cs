
using System.ComponentModel.DataAnnotations;

namespace LojaCupCake.ViewModels
{
    public class EditarFuncionarioViewModel
    {
        public long FuncionarioId { get; set; }

        [Required(ErrorMessage = "Informe o nome completo.")]
        [StringLength(120)]
        [Display(Name = "Nome completo")]
        public string NomeCompleto { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe a matrícula.")]
        [StringLength(30)]
        public string Matricula { get; set; } = string.Empty;

        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public bool Ativo { get; set; } = true;

        public List<FuncionarioFilialViewModel> Filiais { get; set; }
            = new();
    }
}
