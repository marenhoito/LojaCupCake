
using System.ComponentModel.DataAnnotations;

namespace LojaCupCake.ViewModels
{
    public class FilialViewModel
    {
        public long FilialId { get; set; }

        [Required(ErrorMessage = "Informe o código da filial.")]
        [StringLength(20)]
        [Display(Name = "Código")]
        public string Codigo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe o nome da filial.")]
        [StringLength(120)]
        [Display(Name = "Nome da filial")]
        public string Nome { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Telefone { get; set; }

        [EmailAddress(ErrorMessage = "E-mail inválido.")]
        [StringLength(180)]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Informe o CEP.")]
        [StringLength(9)]
        public string Cep { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe o logradouro.")]
        [StringLength(150)]
        public string Logradouro { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe o número.")]
        [StringLength(20)]
        public string Numero { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Complemento { get; set; }

        [Required(ErrorMessage = "Informe o bairro.")]
        [StringLength(100)]
        public string Bairro { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe a cidade.")]
        [StringLength(100)]
        public string Cidade { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe o estado.")]
        [StringLength(2, MinimumLength = 2)]
        public string Uf { get; set; } = string.Empty;
    }
}
