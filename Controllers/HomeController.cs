using LojaCupCake.Data;
using LojaCupCake.Models;
using LojaCupCake.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LojaCupCake.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Primeiro agrega somente pelo identificador. Isso evita agrupar colunas
            // textuais (como a descrição do produto) no MySQL.
            var quantidadesVendidas = await _context.ItensPedido
                .AsNoTracking()
                .Where(item =>
                    item.Pedido.StatusPedido == StatusPedido.Concluido &&
                    item.Produto.Ativo &&
                    item.Produto.Categoria.Ativa)
                .GroupBy(item => item.ProdutoId)
                .Select(grupo => new
                {
                    ProdutoId = grupo.Key,
                    QuantidadeVendida = grupo.Sum(item => item.Quantidade)
                })
                .OrderByDescending(item => item.QuantidadeVendida)
                .Take(4)
                .ToListAsync();

            var idsProdutos = quantidadesVendidas
                .Select(item => item.ProdutoId)
                .ToList();

            var detalhesProdutos = idsProdutos.Count == 0
                ? new List<ProdutoDestaqueHomeViewModel>()
                : await _context.Produtos
                    .AsNoTracking()
                    .Where(produto => idsProdutos.Contains(produto.ProdutoId))
                    .Select(produto => new ProdutoDestaqueHomeViewModel
                    {
                        ProdutoId = produto.ProdutoId,
                        Nome = produto.Nome,
                        Descricao = produto.Descricao,
                        ImagemUrl = produto.ImagemUrl,
                        Preco = produto.PrecoAtual
                    })
                    .ToListAsync();

            var porProdutoId = detalhesProdutos.ToDictionary(
                produto => produto.ProdutoId);

            var produtosMaisVendidos = quantidadesVendidas
                .Where(item => porProdutoId.ContainsKey(item.ProdutoId))
                .Select(item =>
                {
                    var produto = porProdutoId[item.ProdutoId];
                    produto.QuantidadeVendida = item.QuantidadeVendida;
                    return produto;
                })
                .ToList();

            // Se ainda não houver vendas concluídas, exibe produtos ativos do catálogo
            // para que a página inicial não fique vazia durante a fase de implantação.
            bool possuiVendasConcluidas = produtosMaisVendidos.Any();

            if (!possuiVendasConcluidas)
            {
                produtosMaisVendidos = await _context.Produtos
                    .AsNoTracking()
                    .Where(produto => produto.Ativo && produto.Categoria.Ativa)
                    .OrderBy(produto => produto.Nome)
                    .Select(produto => new ProdutoDestaqueHomeViewModel
                    {
                        ProdutoId = produto.ProdutoId,
                        Nome = produto.Nome,
                        Descricao = produto.Descricao,
                        ImagemUrl = produto.ImagemUrl,
                        Preco = produto.PrecoAtual,
                        QuantidadeVendida = 0
                    })
                    .Take(4)
                    .ToListAsync();
            }

            // Filiais ativas em destaque, ordenadas pela quantidade de pedidos concluídos.
            var lojasDestaque = await _context.Filiais
                .AsNoTracking()
                .Where(filial => filial.Ativo)
                .Select(filial => new LojaDestaqueHomeViewModel
                {
                    FilialId = filial.FilialId,
                    Nome = filial.Nome,
                    Cidade = filial.Cidade,
                    Uf = filial.Uf,
                    Bairro = filial.Bairro,
                    Logradouro = filial.Logradouro,
                    Numero = filial.Numero,
                    Telefone = filial.Telefone,
                    PedidosConcluidos = filial.Pedidos.Count(pedido =>
                        pedido.StatusPedido == StatusPedido.Concluido)
                })
                .OrderByDescending(filial => filial.PedidosConcluidos)
                .ThenBy(filial => filial.Nome)
                .Take(3)
                .ToListAsync();

            var model = new HomeIndexViewModel
            {
                ProdutosMaisVendidos = produtosMaisVendidos,
                LojasDestaque = lojasDestaque,
                PossuiVendasConcluidas = possuiVendasConcluidas
            };

            return View(model);
        }

        public IActionResult Privacy() => View();

        public IActionResult Error() => View();
    }
}
