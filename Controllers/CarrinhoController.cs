
using LojaCupCake.Data;
using LojaCupCake.Services;
using LojaCupCake.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LojaCupCake.Controllers
{
    [AllowAnonymous]
    public class CarrinhoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly CarrinhoService _carrinho;

        public CarrinhoController(
            ApplicationDbContext context,
            CarrinhoService carrinho)
        {
            _context = context;
            _carrinho = carrinho;
        }

        // VISUALIZAR CARRINHO
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var carrinho = _carrinho.Obter();

            var model = new CarrinhoViewModel();

            if (!carrinho.Itens.Any() ||
                !carrinho.FilialId.HasValue)
            {
                return View(model);
            }

            var filial = await _context.Filiais
                .Include(f => f.Funcionamentos)
                .AsNoTracking()
                .FirstOrDefaultAsync(f =>
                    f.FilialId == carrinho.FilialId.Value &&
                    f.Ativo);

            model.NomeFilial =
                filial?.Nome ?? "Filial indisponível";

            model.FilialDisponivel = filial != null;
            bool filialAberta = filial != null && FuncionamentoLojaHelper.EstaAberta(filial);
            ViewData["FilialFechada"] = filial != null && !filialAberta;
            ViewData["StatusFuncionamentoFilial"] = filial == null
                ? "Filial indisponível"
                : FuncionamentoLojaHelper.Avaliar(filial).Status;

            var idsProdutos = carrinho.Itens
                .Select(i => i.ProdutoId)
                .Distinct()
                .ToList();

            var produtos = await _context.Produtos
                .Include(p => p.Categoria)
                .Where(p => idsProdutos.Contains(p.ProdutoId))
                .AsNoTracking()
                .ToDictionaryAsync(p => p.ProdutoId);

            var estoques = new Dictionary<long, int>();

            if (filial != null)
            {
                estoques = await _context.Estoques
                    .Where(e => e.FilialId == filial.FilialId)
                    .AsNoTracking()
                    .ToDictionaryAsync(
                        e => e.ProdutoId,
                        e => e.QuantidadeAtual
                    );
            }

            foreach (var item in carrinho.Itens)
            {
                produtos.TryGetValue(item.ProdutoId, out var produto);

                if (produto == null)
                {
                    model.TodosItensDisponiveis = false;
                    continue;
                }

                estoques.TryGetValue(
                    item.ProdutoId,
                    out var estoqueDisponivel
                );

                bool disponivel =
                    filial != null &&
                    produto.Ativo &&
                    produto.Categoria.Ativa &&
                    estoqueDisponivel >= item.Quantidade;

                decimal totalItem =
                    produto.PrecoAtual * item.Quantidade;

                model.Itens.Add(new CarrinhoItemViewModel
                {
                    ProdutoId = produto.ProdutoId,
                    Codigo = produto.Codigo,
                    NomeProduto = produto.Nome,
                    ImagemUrl = produto.ImagemUrl,
                    PrecoUnitario = produto.PrecoAtual,
                    Quantidade = item.Quantidade,
                    EstoqueDisponivel = estoqueDisponivel,
                    ProdutoDisponivel = disponivel,
                    TotalItem = totalItem
                });

                model.Subtotal += totalItem;

                if (!disponivel)
                    model.TodosItensDisponiveis = false;
            }

            if (filial != null && !filialAberta)
                model.TodosItensDisponiveis = false;

            return View(model);
        }

        // ATUALIZAR QUANTIDADE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AtualizarQuantidade(
            long produtoId,
            int quantidade)
        {
            var carrinho = _carrinho.Obter();

            var item = carrinho.Itens.FirstOrDefault(i =>
                i.ProdutoId == produtoId);

            if (item == null)
                return RedirectToAction(nameof(Index));

            if (quantidade < 1)
            {
                TempData["Erro"] =
                    "A quantidade deve ser maior que zero.";

                return RedirectToAction(nameof(Index));
            }

            var filialId = carrinho.FilialId;

            if (!filialId.HasValue)
                return RedirectToAction(nameof(Index));

            var filial = await _context.Filiais
                .Include(f => f.Funcionamentos)
                .AsNoTracking()
                .FirstOrDefaultAsync(f =>
                    f.FilialId == filialId.Value &&
                    f.Ativo
                );

            if (filial == null)
            {
                TempData["Erro"] = "A filial do carrinho não está mais disponível.";
                return RedirectToAction(nameof(Index));
            }

            if (!FuncionamentoLojaHelper.EstaAberta(filial))
            {
                TempData["Erro"] = "A loja está fechada. Não é possível alterar quantidades agora, mas você ainda pode remover itens.";
                return RedirectToAction(nameof(Index));
            }


            var produto = await _context.Produtos
                .Include(p => p.Categoria)
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.ProdutoId == produtoId &&
                    p.Ativo &&
                    p.Categoria.Ativa);

            var estoque = await _context.Estoques
                .AsNoTracking()
                .FirstOrDefaultAsync(e =>
                    e.FilialId == filialId.Value &&
                    e.ProdutoId == produtoId);

            int disponivel = estoque?.QuantidadeAtual ?? 0;

            if (produto == null || quantidade > disponivel)
            {
                TempData["Erro"] =
                    $"Quantidade indisponível. Estoque atual: {disponivel}.";

                return RedirectToAction(nameof(Index));
            }

            item.Quantidade = quantidade;

            _carrinho.Salvar(carrinho);

            TempData["Sucesso"] =
                "Quantidade atualizada com sucesso!";

            return RedirectToAction(nameof(Index));
        }

        // REMOVER ITEM
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remover(long produtoId)
        {
            var carrinho = _carrinho.Obter();

            carrinho.Itens.RemoveAll(i =>
                i.ProdutoId == produtoId);

            _carrinho.Salvar(carrinho);

            TempData["Sucesso"] =
                "Produto removido do carrinho.";

            return RedirectToAction(nameof(Index));
        }

        // LIMPAR CARRINHO
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Limpar()
        {
            _carrinho.Limpar();

            TempData["Sucesso"] =
                "Carrinho esvaziado com sucesso.";

            return RedirectToAction(nameof(Index));
        }
    }
}
