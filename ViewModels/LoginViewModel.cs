
using System.ComponentModel.DataAnnotations;

namespace LojaCupCake.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Informe seu e-mail.")]
        [EmailAddress(ErrorMessage = "E-mail inválido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe sua senha.")]
        [DataType(DataType.Password)]
        public string Senha { get; set; } = string.Empty;

        [Display(Name = "Manter conectado")]
        public bool LembrarDeMim { get; set; }
    }
}
