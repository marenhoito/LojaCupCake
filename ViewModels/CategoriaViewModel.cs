
using System.ComponentModel.DataAnnotations;

namespace LojaCupCake.ViewModels
{
    public class CategoriaViewModel
    {
        public long CategoriaId { get; set; }

        [Required(ErrorMessage = "Informe o nome da categoria.")]
        [StringLength(80, ErrorMessage = "O nome deve ter no máximo 80 caracteres.")]
        [Display(Name = "Nome da categoria")]
        public string Nome { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "A descrição deve ter no máximo 255 caracteres.")]
        [Display(Name = "Descrição")]
        public string? Descricao { get; set; }

        public bool Ativa { get; set; } = true;

        public int QuantidadeProdutos { get; set; }
    }
}
