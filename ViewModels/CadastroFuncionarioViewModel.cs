
using System.ComponentModel.DataAnnotations;

namespace LojaCupCake.ViewModels
{
    public class CadastroFuncionarioViewModel
    {
        [Required(ErrorMessage = "Informe o nome completo.")]
        [StringLength(120)]
        [Display(Name = "Nome completo")]
        public string NomeCompleto { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe a matrícula.")]
        [StringLength(30)]
        public string Matricula { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe o e-mail.")]
        [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe a senha.")]
        [DataType(DataType.Password)]
        [MinLength(8, ErrorMessage = "A senha deve ter pelo menos 8 caracteres.")]
        public string Senha { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirme a senha.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Senha), ErrorMessage = "As senhas não coincidem.")]
        [Display(Name = "Confirmar senha")]
        public string ConfirmarSenha { get; set; } = string.Empty;

        public List<FuncionarioFilialViewModel> Filiais { get; set; }
            = new();
    }
}
