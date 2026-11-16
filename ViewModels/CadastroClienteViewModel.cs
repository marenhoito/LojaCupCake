
using System.ComponentModel.DataAnnotations;

namespace LojaCupCake.ViewModels
{
    public class CadastroClienteViewModel
    {
        [Required(ErrorMessage = "Informe seu nome.")]
        [StringLength(120)]
        [Display(Name = "Nome completo")]
        public string NomeCompleto { get; set; } = string.Empty;

        [StringLength(14)]
        [Display(Name = "CPF")]
        public string? Cpf { get; set; }

        [Required(ErrorMessage = "Informe seu e-mail.")]
        [EmailAddress(ErrorMessage = "E-mail inválido.")]
        [StringLength(180)]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Telefone inválido.")]
        [StringLength(20)]
        public string? Telefone { get; set; }

        [Required(ErrorMessage = "Informe uma senha.")]
        [DataType(DataType.Password)]
        [MinLength(8, ErrorMessage = "A senha deve ter pelo menos 8 caracteres.")]
        public string Senha { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirme sua senha.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Senha), ErrorMessage = "As senhas não conferem.")]
        [Display(Name = "Confirmar senha")]
        public string ConfirmarSenha { get; set; } = string.Empty;
    }
}
