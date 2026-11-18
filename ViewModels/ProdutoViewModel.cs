using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LojaCupCake.ViewModels
{
    public class ProdutoViewModel
    {
        public long ProdutoId { get; set; }

        [Required(ErrorMessage = "Selecione uma categoria.")]
        [Display(Name = "Categoria")]
        public long CategoriaId { get; set; }

        [Required(ErrorMessage = "Informe o código do produto.")]
        [StringLength(30)]
        [Display(Name = "Código")]
        public string Codigo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe o nome do produto.")]
        [StringLength(120)]
        [Display(Name = "Nome do produto")]
        public string Nome { get; set; } = string.Empty;

        [Display(Name = "Descrição")]
        public string? Descricao { get; set; }

        [Required]
        [Range(
            typeof(decimal),
            "0.01",
            "99999999",
            ParseLimitsInInvariantCulture = true,
            ConvertValueInInvariantCulture = true,
            ErrorMessage = "Informe um preço maior que zero."
        )]
        [Display(Name = "Preço")]
        public decimal PrecoAtual { get; set; }

        [StringLength(500)]
        [Display(Name = "URL externa da imagem (opcional)")]
        public string? ImagemUrl { get; set; }

        [Display(Name = "Enviar imagem do cupcake")]
        public IFormFile? ArquivoImagem { get; set; }

        [Display(Name = "Remover imagem atual")]
        public bool RemoverImagem { get; set; }

        [Display(Name = "Produto ativo")]
        public bool Ativo { get; set; } = true;

        public string? NomeCategoria { get; set; }

        public List<SelectListItem> Categorias { get; set; } = new();

        public double? MediaAvaliacao { get; set; }

        public int QuantidadeAvaliacoes { get; set; }
    }
}
