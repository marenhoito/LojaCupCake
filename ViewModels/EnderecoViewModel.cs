
using System.ComponentModel.DataAnnotations;

namespace LojaCupCake.ViewModels
{
    public class EnderecoViewModel
    {
        public long EnderecoId { get; set; }

        [StringLength(50)]
        [Display(Name = "Apelido do endereço")]
        public string? Apelido { get; set; }

        [Required(ErrorMessage = "Informe o destinatário.")]
        [StringLength(120)]
        [Display(Name = "Destinatário")]
        public string Destinatario { get; set; } = string.Empty;

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

        [Required(ErrorMessage = "Informe a UF.")]
        [StringLength(2, MinimumLength = 2,
            ErrorMessage = "Informe uma UF com duas letras.")]
        public string Uf { get; set; } = string.Empty;

        [StringLength(255)]
        [Display(Name = "Ponto de referência")]
        public string? PontoReferencia { get; set; }

        [Display(Name = "Definir como endereço principal")]
        public bool Principal { get; set; }
    }
}
