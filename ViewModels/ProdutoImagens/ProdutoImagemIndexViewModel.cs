namespace LojaCupCake.ViewModels.ProdutoImagens
{
    public class ProdutoImagemIndexViewModel
    {
        public List<ProdutoImagemItemViewModel> Produtos { get; set; } = new();
    }

    public class ProdutoImagemItemViewModel
    {
        public long ProdutoId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public string? ImagemUrl { get; set; }
        public bool Ativo { get; set; }
    }
}
