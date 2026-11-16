
using LojaCupCake.Data;
using LojaCupCake.Models;
using LojaCupCake.Services;
using LojaCupCake.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Data;

namespace LojaCupCake.Controllers
{
    [Authorize(Roles = "Administrador,Funcionario")]
    public class EstoqueController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AutorizacaoFuncionarioService _autorizacao;

        public EstoqueController(
            ApplicationDbContext context,
            AutorizacaoFuncionarioService autorizacao)
        {
            _context = context;
            _autorizacao = autorizacao;
        }

        // CONSULTA DE ESTOQUE
        [HttpGet]
        public async Task<IActionResult> Index(long? filialId)
        {
            bool administrador = User.IsInRole("Administrador");

            IQueryable<Filial> consultaFiliais = _context.Filiais
                .Where(f => f.Ativo);

            // Funcionários visualizam somente suas filiais ativas.
            if (!administrador)
            {
                var usuarioId = User.FindFirstValue(
                    ClaimTypes.NameIdentifier
                );

                if (string.IsNullOrWhiteSpace(usuarioId))
                    return Forbid();

                var funcionarioAtivo = await _context.Funcionarios
                    .AnyAsync(f =>
                        f.UsuarioId == usuarioId &&
                        f.Ativo
                    );

                if (!funcionarioAtivo)
                    return Forbid();

                consultaFiliais = consultaFiliais
                    .Where(f => f.Funcionarios.Any(ff =>
                        ff.Funcionario.UsuarioId == usuarioId &&
                        ff.Funcionario.Ativo &&
                        ff.Ativo
                    ));
            }

            var filiais = await consultaFiliais
                .AsNoTracking()
                .OrderBy(f => f.Nome)
                .Select(f => new SelectListItem
                {
                    Value = f.FilialId.ToString(),
                    Text = f.Nome
                })
                .ToListAsync();

            var model = new EstoqueIndexViewModel
            {
                Filiais = filiais
            };

            // Nenhuma filial selecionada: apresentar apenas o seletor.
            if (!filialId.HasValue)
                return View(model);

            // Confirmar que a filial está entre as permitidas.
            var filialSelecionada = filiais.FirstOrDefault(f =>
                f.Value == filialId.Value.ToString()
            );

            if (filialSelecionada == null)
                return Forbid();

            // Validar também pelo serviço centralizado.
            if (!await _autorizacao.PodeAcessarFilialAsync(
                User, filialId.Value))
            {
                return Forbid();
            }

            model.FilialId = filialId.Value;
            model.NomeFilial = filialSelecionada.Text;

            foreach (var filial in model.Filiais)
            {
                filial.Selected =
                    filial.Value == filialId.Value.ToString();
            }

            // Consultar produtos ativos.
            var produtos = await _context.Produtos
                .Where(p => p.Ativo)
                .AsNoTracking()
                .OrderBy(p => p.Nome)
                .Select(p => new
                {
                    p.ProdutoId,
                    p.Codigo,
                    p.Nome,
                    NomeCategoria = p.Categoria.Nome
                })
                .ToListAsync();

            // Consultar o estoque existente na filial.
            var estoques = await _context.Estoques
                .Where(e => e.FilialId == filialId.Value)
                .AsNoTracking()
                .ToDictionaryAsync(e => e.ProdutoId);

            // Combinar produtos e estoque.
            model.Produtos = produtos.Select(p =>
            {
                estoques.TryGetValue(
                    p.ProdutoId,
                    out var estoque
                );

                return new EstoqueProdutoViewModel
                {
                    ProdutoId = p.ProdutoId,
                    Codigo = p.Codigo,
                    NomeProduto = p.Nome,
                    NomeCategoria = p.NomeCategoria,

                    QuantidadeAtual =
                        estoque?.QuantidadeAtual ?? 0,

                    QuantidadeMinima =
                        estoque?.QuantidadeMinima ?? 0,

                    PossuiEstoqueCadastrado =
                        estoque != null
                };
            }).ToList();

            return View(model);
        }
        // REGISTRAR MOVIMENTAÇÃO
        [Authorize(Roles = "Administrador")]
        [HttpGet]
        public async Task<IActionResult> Movimentar(
            long filialId,
            long produtoId)
        {
            var model = new MovimentacaoEstoqueViewModel
            {
                FilialId = filialId,
                ProdutoId = produtoId,
                Tipo = TipoMovimentacaoEstoque.Entrada
            };

            if (!await PrepararMovimentacao(model))
                return NotFound();

            return View(model);
        }

        [Authorize(Roles = "Administrador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Movimentar(
            MovimentacaoEstoqueViewModel model)
        {
            if (!Enum.IsDefined(
                typeof(TipoMovimentacaoEstoque),
                model.Tipo))
            {
                ModelState.AddModelError(
                    nameof(model.Tipo),
                    "Selecione um tipo de movimentação válido."
                );
            }

            if (!await PrepararMovimentacao(model))
                return NotFound();

            if (!ModelState.IsValid)
                return View(model);

            await using var transacao =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable
                );

            try
            {
                var estoque = await _context.Estoques
                    .FirstOrDefaultAsync(e =>
                        e.FilialId == model.FilialId &&
                        e.ProdutoId == model.ProdutoId
                    );

                // Se ainda não existe estoque, começar com saldo zero.
                if (estoque == null)
                {
                    if (model.Tipo == TipoMovimentacaoEstoque.Saida ||
                        model.Tipo == TipoMovimentacaoEstoque.AjusteSaida)
                    {
                        await transacao.RollbackAsync();

                        ModelState.AddModelError(
                            string.Empty,
                            "Não existe estoque cadastrado para este produto. " +
                            "Registre uma entrada primeiro."
                        );

                        return View(model);
                    }

                    estoque = new Estoque
                    {
                        FilialId = model.FilialId,
                        ProdutoId = model.ProdutoId,
                        QuantidadeAtual = 0,
                        QuantidadeMinima = 0,
                        AtualizadoEm = DateTime.UtcNow
                    };

                    _context.Estoques.Add(estoque);
                }

                int saldoAnterior = estoque.QuantidadeAtual;
                int saldoPosterior;

                switch (model.Tipo)
                {
                    case TipoMovimentacaoEstoque.Entrada:
                    case TipoMovimentacaoEstoque.AjusteEntrada:

                        saldoPosterior = checked(
                            saldoAnterior + model.Quantidade
                        );

                        break;

                    case TipoMovimentacaoEstoque.Saida:
                    case TipoMovimentacaoEstoque.AjusteSaida:

                        if (model.Quantidade > saldoAnterior)
                        {
                            await transacao.RollbackAsync();

                            ModelState.AddModelError(
                                nameof(model.Quantidade),
                                $"Estoque insuficiente. Saldo disponível: {saldoAnterior}."
                            );

                            return View(model);
                        }

                        saldoPosterior =
                            saldoAnterior - model.Quantidade;

                        break;

                    default:
                        await transacao.RollbackAsync();
                        return BadRequest();
                }

                // Atualizar saldo.
                estoque.QuantidadeAtual = saldoPosterior;
                estoque.AtualizadoEm = DateTime.UtcNow;

                // Registrar histórico.
                var movimentacao = new MovimentacaoEstoque
                {
                    FilialId = model.FilialId,
                    ProdutoId = model.ProdutoId,
                    Tipo = model.Tipo,
                    Quantidade = model.Quantidade,
                    SaldoAnterior = saldoAnterior,
                    SaldoPosterior = saldoPosterior,
                    Motivo = string.IsNullOrWhiteSpace(model.Motivo)
                        ? null
                        : model.Motivo.Trim(),
                    CriadoEm = DateTime.UtcNow,
                    Estoque = estoque,
                        UsuarioId = User.FindFirstValue(
                            System.Security.Claims.ClaimTypes.NameIdentifier
                            ),




                };

                _context.MovimentacoesEstoque.Add(movimentacao);

                await _context.SaveChangesAsync();

                await transacao.CommitAsync();

                TempData["Sucesso"] =
                    "Movimentação registrada com sucesso!";

                return RedirectToAction(
                    nameof(Index),
                    new { filialId = model.FilialId }
                );
            }
            catch (OverflowException)
            {
                await transacao.RollbackAsync();

                ModelState.AddModelError(
                    string.Empty,
                    "A quantidade resultante ultrapassa o limite permitido."
                );

                return View(model);
            }
            catch (DbUpdateException)
            {
                await transacao.RollbackAsync();

                ModelState.AddModelError(
                    string.Empty,
                    "Não foi possível registrar a movimentação."
                );

                return View(model);
            }
        }

        // CARREGAR DADOS DO FORMULÁRIO
        private async Task<bool> PrepararMovimentacao(
            MovimentacaoEstoqueViewModel model)
        {
            var filial = await _context.Filiais
                .AsNoTracking()
                .FirstOrDefaultAsync(f =>
                    f.FilialId == model.FilialId &&
                    f.Ativo
                );

            var produto = await _context.Produtos
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.ProdutoId == model.ProdutoId &&
                    p.Ativo
                );

            if (filial == null || produto == null)
                return false;

            model.NomeFilial = filial.Nome;
            model.NomeProduto = produto.Nome;

            return true;
        }


        [HttpGet]
        public async Task<IActionResult> Historico(
            long filialId,
            long produtoId)
        {
            // Verificar se o usuário pode acessar a filial.
            bool podeAcessar =
                await _autorizacao.PodeAcessarFilialAsync(
                    User,
                    filialId
                );

            if (!podeAcessar)
                return Forbid();

            var filial = await _context.Filiais
                .AsNoTracking()
                .FirstOrDefaultAsync(f =>
                    f.FilialId == filialId &&
                    f.Ativo
                );

            var produto = await _context.Produtos
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.ProdutoId == produtoId
                );

            if (filial == null || produto == null)
                return NotFound();

            var movimentacoes = await _context.MovimentacoesEstoque
                .Include(m => m.Responsavel)
                .Where(m =>
                    m.FilialId == filialId &&
                    m.ProdutoId == produtoId
                )
                .OrderByDescending(m => m.CriadoEm)
                .AsNoTracking()
                .ToListAsync();

            var model = new HistoricoEstoqueViewModel
            {
                FilialId = filialId,
                ProdutoId = produtoId,
                NomeFilial = filial.Nome,
                NomeProduto = produto.Nome,
                CodigoProduto = produto.Codigo,

                Movimentacoes = movimentacoes.Select(m =>
                    new MovimentacaoEstoqueItemViewModel
                    {
                        Tipo = m.Tipo,
                        Quantidade = m.Quantidade,
                        SaldoAnterior = m.SaldoAnterior,
                        SaldoPosterior = m.SaldoPosterior,
                        Motivo = m.Motivo,
                        CriadoEm = m.CriadoEm,

                        NomeResponsavel = m.Responsavel?.Email
                            ?? "Não identificado"
                    }).ToList()
            };

            return View(model);
        }


        [Authorize(Roles = "Administrador")]
        [HttpGet]
        public async Task<IActionResult> ConfigurarMinimo(
            long filialId,
            long produtoId)
        {
            var filial = await _context.Filiais
                .AsNoTracking()
                .FirstOrDefaultAsync(f =>
                    f.FilialId == filialId &&
                    f.Ativo
                );

            var produto = await _context.Produtos
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.ProdutoId == produtoId &&
                    p.Ativo
                );

            if (filial == null || produto == null)
                return NotFound();

            var estoque = await _context.Estoques
                .AsNoTracking()
                .FirstOrDefaultAsync(e =>
                    e.FilialId == filialId &&
                    e.ProdutoId == produtoId
                );

            var model = new EstoqueMinimoViewModel
            {
                FilialId = filialId,
                ProdutoId = produtoId,
                NomeFilial = filial.Nome,
                NomeProduto = produto.Nome,
                QuantidadeAtual = estoque?.QuantidadeAtual ?? 0,
                QuantidadeMinima = estoque?.QuantidadeMinima ?? 0
            };

            return View(model);
        }

        [Authorize(Roles = "Administrador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfigurarMinimo(
            EstoqueMinimoViewModel model)
        {
            var filial = await _context.Filiais
                .FirstOrDefaultAsync(f =>
                    f.FilialId == model.FilialId &&
                    f.Ativo
                );

            var produto = await _context.Produtos
                .FirstOrDefaultAsync(p =>
                    p.ProdutoId == model.ProdutoId &&
                    p.Ativo
                );

            if (filial == null || produto == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                model.NomeFilial = filial.Nome;
                model.NomeProduto = produto.Nome;

                return View(model);
            }

            await using var transacao =
                await _context.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable
                );

            var estoque = await _context.Estoques
                .FirstOrDefaultAsync(e =>
                    e.FilialId == model.FilialId &&
                    e.ProdutoId == model.ProdutoId
                );

            if (estoque == null)
            {
                estoque = new Estoque
                {
                    FilialId = model.FilialId,
                    ProdutoId = model.ProdutoId,
                    QuantidadeAtual = 0,
                    QuantidadeMinima = model.QuantidadeMinima,
                    AtualizadoEm = DateTime.UtcNow
                };

                _context.Estoques.Add(estoque);
            }
            else
            {
                estoque.QuantidadeMinima = model.QuantidadeMinima;
                estoque.AtualizadoEm = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            await transacao.CommitAsync();

            TempData["Sucesso"] =
                "Estoque mínimo configurado com sucesso!";

            return RedirectToAction(
                nameof(Index),
                new { filialId = model.FilialId }
            );
        }




    }
}
