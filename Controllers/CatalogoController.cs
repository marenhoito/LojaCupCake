using LojaCupCake.Data;
using LojaCupCake.Models;
using LojaCupCake.Services;
using LojaCupCake.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LojaCupCake.Controllers
{
    [AllowAnonymous]
    public class CatalogoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly CarrinhoService _carrinho;

        public CatalogoController(
            ApplicationDbContext context,
            CarrinhoService carrinho)
        {
            _context = context;
            _carrinho = carrinho;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            long? filialId,
            long? categoriaId,
            string? ordenacao)
        {
            var carrinho = _carrinho.Obter();

            // O carrinho fica vinculado à filial onde os itens foram adicionados.
            if (carrinho.Itens.Any() && carrinho.FilialId.HasValue)
                filialId = carrinho.FilialId;

            var filiais = await _context.Filiais
                .Where(f => f.Ativo)
                .Include(f => f.Funcionamentos)
                .AsNoTracking()
                .OrderBy(f => f.Nome)
                .ToListAsync();

            if (filialId.HasValue &&
                !filiais.Any(f => f.FilialId == filialId.Value))
            {
                // Se a filial do carrinho foi desativada, ela não pode ser usada
                // para novas compras, mas os itens continuam visíveis para remoção.
                filialId = null;
            }

            var categorias = await _context.Categorias
                .Where(c => c.Ativa)
                .AsNoTracking()
                .OrderBy(c => c.Nome)
                .ToListAsync();

            if (categoriaId.HasValue &&
                !categorias.Any(c => c.CategoriaId == categoriaId.Value))
            {
                categoriaId = null;
            }

            var ordenacaoValida = new[]
            {
                "pedidos-desc", "pedidos-asc", "preco-asc", "preco-desc"
            };

            if (string.IsNullOrWhiteSpace(ordenacao) ||
                !ordenacaoValida.Contains(ordenacao))
            {
                ordenacao = "pedidos-desc";
            }

            var consultaProdutos = _context.Produtos
                .Where(p => p.Ativo && p.Categoria.Ativa)
                .AsNoTracking();

            if (categoriaId.HasValue)
            {
                consultaProdutos = consultaProdutos.Where(p =>
                    p.CategoriaId == categoriaId.Value);
            }

            var produtosBase = await consultaProdutos
                .Select(p => new
                {
                    p.ProdutoId,
                    p.Codigo,
                    p.Nome,
                    p.Descricao,
                    p.ImagemUrl,
                    p.PrecoAtual,
                    NomeCategoria = p.Categoria.Nome
                })
                .ToListAsync();

            // Popularidade: quantidade de pedidos que contiveram cada produto.
            // Pedidos cancelados/rejeitados não entram na contagem.
            var popularidade = await _context.ItensPedido
                .Where(i =>
                    i.Pedido.StatusPedido != StatusPedido.Cancelado &&
                    i.Pedido.StatusPedido != StatusPedido.Rejeitado)
                .GroupBy(i => i.ProdutoId)
                .Select(g => new
                {
                    ProdutoId = g.Key,
                    QuantidadePedidos = g.Select(i => i.PedidoId).Distinct().Count()
                })
                .ToDictionaryAsync(x => x.ProdutoId, x => x.QuantidadePedidos);

            var estoques = new Dictionary<long, int>();

            if (filialId.HasValue)
            {
                estoques = await _context.Estoques
                    .Where(e => e.FilialId == filialId.Value)
                    .AsNoTracking()
                    .ToDictionaryAsync(e => e.ProdutoId, e => e.QuantidadeAtual);
            }

            var quantidadesNoCarrinho = carrinho.Itens
                .GroupBy(i => (long)i.ProdutoId)
                .ToDictionary(g => g.Key, g => g.Sum(i => (int)i.Quantidade));

            var produtos = produtosBase.Select(p =>
            {
                int saldo = estoques.TryGetValue(p.ProdutoId, out var estoqueAtual)
                    ? estoqueAtual
                    : 0;
                int quantidadeJaNoCarrinho = quantidadesNoCarrinho.TryGetValue(
                    p.ProdutoId, out var quantidadeCarrinho)
                    ? quantidadeCarrinho
                    : 0;

                return new ProdutoCatalogoViewModel
                {
                    ProdutoId = p.ProdutoId,
                    Codigo = p.Codigo,
                    Nome = p.Nome,
                    Descricao = p.Descricao,
                    ImagemUrl = p.ImagemUrl,
                    Preco = p.PrecoAtual,
                    NomeCategoria = p.NomeCategoria,
                    EstoqueDisponivel = filialId.HasValue
                        ? Math.Max(0, saldo - quantidadeJaNoCarrinho)
                        : null,
                    QuantidadePedidos = popularidade.TryGetValue(p.ProdutoId, out var qtd)
                        ? qtd
                        : 0
                };
            }).ToList();

            produtos = ordenacao switch
            {
                "preco-asc" => produtos.OrderBy(p => p.Preco).ThenBy(p => p.Nome).ToList(),
                "preco-desc" => produtos.OrderByDescending(p => p.Preco).ThenBy(p => p.Nome).ToList(),
                "pedidos-asc" => produtos.OrderBy(p => p.QuantidadePedidos).ThenBy(p => p.Nome).ToList(),
                _ => produtos.OrderByDescending(p => p.QuantidadePedidos).ThenBy(p => p.Nome).ToList()
            };

            var horarioBrasil = FuncionamentoLojaHelper.ObterHorarioLocal();
            var filiaisViewModel = filiais.Select(f =>
                MontarFilialViewModel(f, horarioBrasil, carrinho.Itens.Any(), filialId))
                .ToList();
            var filialSelecionadaViewModel = filiaisViewModel
                .FirstOrDefault(f => f.FilialId == filialId);

            var model = new CatalogoViewModel
            {
                FilialId = filialId,
                CategoriaId = categoriaId,
                Ordenacao = ordenacao,
                CarrinhoPossuiItens = carrinho.Itens.Any(),
                NomeFilialSelecionada = filiais.FirstOrDefault(f => f.FilialId == filialId)?.Nome ?? string.Empty,
                FilialAberta = filialSelecionadaViewModel?.AbertaAgora == true,
                StatusFilialSelecionada = filialSelecionadaViewModel?.StatusFuncionamento ?? string.Empty,
                Filiais = filiaisViewModel,
                Categorias = categorias.Select(c => new SelectListItem
                {
                    Value = c.CategoriaId.ToString(),
                    Text = c.Nome,
                    Selected = c.CategoriaId == categoriaId
                }).ToList(),
                Produtos = produtos
            };

            var itensCarrinho = carrinho.Itens
                .Select(i => (ProdutoId: (long)i.ProdutoId, Quantidade: (int)i.Quantidade))
                .ToList();

            await PrepararCarrinhoLateralAsync(model, carrinho.FilialId, itensCarrinho);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Adicionar(
            long produtoId,
            long filialId,
            int quantidade = 1,
            long? categoriaId = null,
            string? ordenacao = null)
        {
            var carrinho = _carrinho.Obter();

            if (quantidade < 1)
            {
                TempData["Erro"] = "Informe uma quantidade válida.";
                return VoltarAoCatalogo(carrinho.FilialId ?? filialId, categoriaId, ordenacao);
            }

            var filial = await _context.Filiais
                .Include(f => f.Funcionamentos)
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.FilialId == filialId && f.Ativo);

            if (filial == null)
            {
                TempData["Erro"] = "Esta filial não está disponível para novas compras.";
                return VoltarAoCatalogo(carrinho.FilialId ?? filialId, categoriaId, ordenacao);
            }

            if (!FuncionamentoLojaHelper.EstaAberta(filial))
            {
                TempData["Erro"] = "Esta loja está fechada no momento e não está recebendo novos pedidos.";
                return VoltarAoCatalogo(filialId, categoriaId, ordenacao);
            }

            if (carrinho.Itens.Any() && carrinho.FilialId != filialId)
            {
                TempData["Erro"] =
                    "Seu carrinho pertence a outra filial. Remova os itens ou limpe o carrinho antes de trocar de unidade.";
                return VoltarAoCatalogo(carrinho.FilialId, categoriaId, ordenacao);
            }

            var produto = await _context.Produtos
                .Include(p => p.Categoria)
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.ProdutoId == produtoId &&
                    p.Ativo &&
                    p.Categoria.Ativa);

            if (produto == null)
            {
                TempData["Erro"] = "Produto indisponível.";
                return VoltarAoCatalogo(carrinho.FilialId ?? filialId, categoriaId, ordenacao);
            }

            var estoque = await _context.Estoques
                .AsNoTracking()
                .FirstOrDefaultAsync(e =>
                    e.FilialId == filialId &&
                    e.ProdutoId == produtoId);

            int disponivel = estoque?.QuantidadeAtual ?? 0;
            var itemExistente = carrinho.Itens.FirstOrDefault(i => i.ProdutoId == produtoId);
            int quantidadeNoCarrinho = itemExistente?.Quantidade ?? 0;

            if (quantidade > disponivel - quantidadeNoCarrinho)
            {
                TempData["Erro"] = $"Estoque insuficiente. Disponível: {disponivel}.";
                return VoltarAoCatalogo(carrinho.FilialId ?? filialId, categoriaId, ordenacao);
            }

            if (itemExistente == null)
            {
                carrinho.Itens.Add(new ItemCarrinhoSessao
                {
                    ProdutoId = produtoId,
                    Quantidade = quantidade
                });
            }
            else
            {
                itemExistente.Quantidade += quantidade;
            }

            carrinho.FilialId = filialId;
            _carrinho.Salvar(carrinho);

            TempData["Sucesso"] = "Produto adicionado ao carrinho.";
            return RedirectToAction(nameof(Index), new { filialId, categoriaId, ordenacao });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoverItem(
            long produtoId,
            long? filialId,
            long? categoriaId,
            string? ordenacao)
        {
            var carrinho = _carrinho.Obter();
            int removidos = carrinho.Itens.RemoveAll(i => i.ProdutoId == produtoId);
            _carrinho.Salvar(carrinho);

            TempData[removidos > 0 ? "Sucesso" : "Erro"] = removidos > 0
                ? "Item removido do carrinho."
                : "O item não foi encontrado no carrinho.";

            return RedirectToAction(nameof(Index), new { filialId, categoriaId, ordenacao });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult LimparCarrinho(
            long? filialId,
            long? categoriaId,
            string? ordenacao)
        {
            _carrinho.Limpar();
            TempData["Sucesso"] = "Carrinho esvaziado com sucesso.";
            return RedirectToAction(nameof(Index), new { filialId, categoriaId, ordenacao });
        }

        private IActionResult VoltarAoCatalogo(
            long? filialId,
            long? categoriaId,
            string? ordenacao)
        {
            return RedirectToAction(nameof(Index), new { filialId, categoriaId, ordenacao });
        }

        private async Task PrepararCarrinhoLateralAsync(
            CatalogoViewModel model,
            long? filialCarrinhoId,
            List<(long ProdutoId, int Quantidade)> itensSessao)
        {
            model.QuantidadeItensCarrinho = itensSessao.Sum(i => i.Quantidade);

            if (itensSessao.Count == 0)
            {
                model.CarrinhoTodosItensDisponiveis = true;
                model.CarrinhoPodeFinalizar = false;
                return;
            }

            var filialCarrinho = filialCarrinhoId.HasValue
                ? await _context.Filiais
                    .Include(f => f.Funcionamentos)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(f => f.FilialId == filialCarrinhoId.Value)
                : null;

            model.NomeFilialCarrinho = filialCarrinho?.Nome ?? "Filial não identificada";
            model.CarrinhoFilialDisponivel = filialCarrinho?.Ativo == true;
            var estadoCarrinho = filialCarrinho == null
                ? null
                : FuncionamentoLojaHelper.Avaliar(filialCarrinho);
            model.CarrinhoLojaAberta = estadoCarrinho?.Aberta == true;
            model.StatusFuncionamentoCarrinho = estadoCarrinho?.Status ?? "Filial indisponível";

            var quantidades = itensSessao
                .GroupBy(i => i.ProdutoId)
                .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantidade));

            var idsProdutos = quantidades.Keys.ToList();
            var produtos = await _context.Produtos
                .Include(p => p.Categoria)
                .Where(p => idsProdutos.Contains(p.ProdutoId))
                .AsNoTracking()
                .ToDictionaryAsync(p => p.ProdutoId);

            var estoques = new Dictionary<long, int>();
            if (filialCarrinhoId.HasValue && model.CarrinhoFilialDisponivel)
            {
                estoques = await _context.Estoques
                    .Where(e => e.FilialId == filialCarrinhoId.Value && idsProdutos.Contains(e.ProdutoId))
                    .AsNoTracking()
                    .ToDictionaryAsync(e => e.ProdutoId, e => e.QuantidadeAtual);
            }

            model.CarrinhoTodosItensDisponiveis = model.CarrinhoFilialDisponivel;

            foreach (var item in quantidades)
            {
                if (!produtos.TryGetValue(item.Key, out var produto))
                {
                    model.ItensCarrinho.Add(new CarrinhoLateralItemViewModel
                    {
                        ProdutoId = item.Key,
                        Nome = "Produto não encontrado",
                        Quantidade = item.Value,
                        Disponivel = false
                    });
                    model.CarrinhoTodosItensDisponiveis = false;
                    continue;
                }

                int saldo = estoques.TryGetValue(item.Key, out var quantidadeEstoque)
                    ? quantidadeEstoque
                    : 0;
                bool disponivel = model.CarrinhoFilialDisponivel &&
                    produto.Ativo && produto.Categoria.Ativa && saldo >= item.Value;
                decimal total = produto.PrecoAtual * item.Value;

                model.ItensCarrinho.Add(new CarrinhoLateralItemViewModel
                {
                    ProdutoId = produto.ProdutoId,
                    Nome = produto.Nome,
                    ImagemUrl = produto.ImagemUrl,
                    PrecoUnitario = produto.PrecoAtual,
                    Quantidade = item.Value,
                    TotalItem = total,
                    EstoqueDisponivel = saldo,
                    Disponivel = disponivel
                });

                model.TotalCarrinho += total;
                if (!disponivel)
                    model.CarrinhoTodosItensDisponiveis = false;
            }

            model.CarrinhoPodeFinalizar = model.CarrinhoFilialDisponivel &&
                model.CarrinhoLojaAberta &&
                model.CarrinhoTodosItensDisponiveis && model.ItensCarrinho.Any();
        }

        private static FilialCatalogoViewModel MontarFilialViewModel(
            Filial filial,
            DateTime agora,
            bool carrinhoPossuiItens,
            long? filialSelecionada)
        {
            int numeroDia = (int)agora.DayOfWeek;
            if (numeroDia == 0)
                numeroDia = 7;

            var horariosHoje = filial.Funcionamentos
                .Where(h => (int)h.DiaSemana == numeroDia)
                .OrderBy(h => h.HoraAbertura)
                .ToList();

            bool possuiHorarioCadastrado = filial.Funcionamentos.Any();
            var estado = FuncionamentoLojaHelper.Avaliar(filial, agora);
            bool abertaAgora = estado.Aberta;

            string horariosHojeTexto = horariosHoje.Count > 0
                ? string.Join(" • ", horariosHoje.Select(h =>
                    $"{h.HoraAbertura:hh\\:mm}–{h.HoraFechamento:hh\\:mm}"))
                : (possuiHorarioCadastrado ? "Fechada hoje" : "Horário não cadastrado");

            var nomesDias = new Dictionary<DiaSemana, string>
            {
                [DiaSemana.Segunda] = "Segunda-feira",
                [DiaSemana.Terca] = "Terça-feira",
                [DiaSemana.Quarta] = "Quarta-feira",
                [DiaSemana.Quinta] = "Quinta-feira",
                [DiaSemana.Sexta] = "Sexta-feira",
                [DiaSemana.Sabado] = "Sábado",
                [DiaSemana.Domingo] = "Domingo"
            };

            var semana = Enum.GetValues<DiaSemana>()
                .OrderBy(d => (int)d)
                .Select(dia =>
                {
                    var horariosDoDia = filial.Funcionamentos
                        .Where(h => h.DiaSemana == dia)
                        .OrderBy(h => h.HoraAbertura)
                        .ToList();

                    string horarioTexto = horariosDoDia.Count == 0
                        ? "Fechado"
                        : string.Join(" / ", horariosDoDia.Select(h =>
                            $"{h.HoraAbertura:hh\\:mm}–{h.HoraFechamento:hh\\:mm}"));

                    return $"{nomesDias[dia]}: {horarioTexto}";
                })
                .ToList();

            return new FilialCatalogoViewModel
            {
                FilialId = filial.FilialId,
                Nome = filial.Nome,
                EnderecoResumido = $"{filial.Logradouro}, {filial.Numero} — {filial.Bairro}, {filial.Cidade}/{filial.Uf}",
                AbertaAgora = abertaAgora,
                StatusFuncionamento = estado.Status,
                ModoFuncionamentoDescricao = estado.Modo,
                HorariosHoje = horariosHojeTexto,
                HorariosSemana = semana,
                Selecionada = filial.FilialId == filialSelecionada,
                SelecaoBloqueada = carrinhoPossuiItens && filial.FilialId != filialSelecionada
            };
        }


    }
}
