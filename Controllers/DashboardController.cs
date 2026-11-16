using LojaCupCake.Data;
using LojaCupCake.Models;
using LojaCupCake.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LojaCupCake.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            DateTime? dataInicial,
            DateTime? dataFinal,
            long[]? filialIds,
            long? filialId,
            bool filiaisSelecionadas = false)
        {
            var fuso = ObterFusoBrasilia();

            var hoje = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                fuso
            ).Date;

            var inicio = (dataInicial ?? hoje.AddDays(-29)).Date;
            var fim = (dataFinal ?? hoje).Date;

            if (inicio > fim)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "A data inicial não pode ser posterior à data final."
                );

                inicio = hoje.AddDays(-29);
                fim = hoje;
            }

            if ((fim - inicio).TotalDays > 365)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "O período máximo permitido é de 366 dias."
                );

                inicio = fim.AddDays(-29);
            }

            // O Dashboard administrativo deve listar todas as filiais,
            // inclusive aquelas que foram desativadas.
            var filiais = await _context.Filiais
                .AsNoTracking()
                .OrderBy(f => f.Nome)
                .ToListAsync();

            var idsDisponiveis = filiais
                .Select(f => f.FilialId)
                .ToHashSet();

            // Compatibilidade com links antigos que ainda enviam filialId.
            var idsRecebidos = (filialIds ?? Array.Empty<long>()).ToList();
            if (idsRecebidos.Count == 0 && filialId.HasValue)
            {
                idsRecebidos.Add(filialId.Value);
            }

            // Na primeira abertura, todas ficam selecionadas.
            // Após enviar o formulário, respeitamos exatamente as caixas marcadas.
            var idsSelecionados = filiaisSelecionadas || filialId.HasValue
                ? idsRecebidos
                    .Where(idsDisponiveis.Contains)
                    .Distinct()
                    .ToList()
                : filiais
                    .Select(f => f.FilialId)
                    .ToList();

            var model = new DashboardAdminViewModel
            {
                FilialId = idsSelecionados.Count == 1
                    ? idsSelecionados[0]
                    : null,
                FilialIds = idsSelecionados,
                DataInicial = inicio,
                DataFinal = fim,
                Filiais = filiais.Select(f => new SelectListItem
                {
                    Value = f.FilialId.ToString(),
                    Text = $"{f.Nome} — {(f.Ativo ? "Ativa" : "Desativada")}",
                    Selected = idsSelecionados.Contains(f.FilialId)
                }).ToList()
            };

            // Converte os limites do período de Brasília para UTC.
            DateTime inicioUtc = TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(inicio, DateTimeKind.Unspecified),
                fuso
            );

            DateTime fimUtc = TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(
                    fim.AddDays(1),
                    DateTimeKind.Unspecified
                ),
                fuso
            );

            // A filtragem é feita pela lista de filiais escolhidas.
            // Se nenhuma caixa estiver marcada, as consultas retornam zero registros.
            var pedidos = _context.Pedidos
                .Where(p =>
                    p.DataPedido >= inicioUtc &&
                    p.DataPedido < fimUtc &&
                    idsSelecionados.Contains(p.FilialId)
                );

            model.TotalPedidos = await pedidos.CountAsync();

            model.PedidosPendentes = await pedidos
                .CountAsync(p =>
                    p.StatusPedido == StatusPedido.AguardandoAprovacao
                );

            var pedidosConcluidos = pedidos.Where(p =>
                p.StatusPedido == StatusPedido.Concluido &&
                p.Pagamentos.Any(pg =>
                    pg.StatusPagamento == StatusPagamento.Aprovado
                )
            );

            model.PedidosConcluidos =
                await pedidosConcluidos.CountAsync();

            model.Faturamento =
                await pedidosConcluidos
                    .Select(p => (decimal?)p.Total)
                    .SumAsync() ?? 0m;

            model.TicketMedio =
                model.PedidosConcluidos > 0
                    ? model.Faturamento / model.PedidosConcluidos
                    : 0m;

            // Inclui estoque de filiais ativas e desativadas selecionadas.
            var consultaEstoque = _context.Estoques
                .Where(e =>
                    e.Produto.Ativo &&
                    e.QuantidadeAtual <= e.QuantidadeMinima &&
                    idsSelecionados.Contains(e.FilialId)
                );

            model.ProdutosEstoqueBaixo =
                await consultaEstoque.CountAsync();

            model.EstoquesBaixos = await consultaEstoque
                .OrderBy(e => e.QuantidadeAtual)
                .Select(e => new EstoqueAlertaViewModel
                {
                    NomeFilial = e.Filial.Nome,
                    FilialAtiva = e.Filial.Ativo,
                    NomeProduto = e.Produto.Nome,
                    QuantidadeAtual = e.QuantidadeAtual,
                    QuantidadeMinima = e.QuantidadeMinima
                })
                .Take(20)
                .AsNoTracking()
                .ToListAsync();

            // Vendas concluídas e pagas, agrupadas por dia.
            var vendas = await pedidosConcluidos
                .Select(p => new
                {
                    p.DataPedido,
                    p.Total
                })
                .ToListAsync();

            model.VendasPorDia = vendas
                .Select(v => new
                {
                    Data = TimeZoneInfo.ConvertTimeFromUtc(
                        DateTime.SpecifyKind(
                            v.DataPedido,
                            DateTimeKind.Utc
                        ),
                        fuso
                    ).Date,
                    v.Total
                })
                .GroupBy(v => v.Data)
                .Select(g => new VendaDiaViewModel
                {
                    Data = g.Key,
                    QuantidadePedidos = g.Count(),
                    TotalVendido = g.Sum(v => v.Total)
                })
                .OrderBy(v => v.Data)
                .ToList();

            // Produtos mais vendidos nas filiais selecionadas,
            // considerando apenas pedidos concluídos e pagos.
            var itensVendidos = await _context.ItensPedido
                .Where(i =>
                    i.Pedido.DataPedido >= inicioUtc &&
                    i.Pedido.DataPedido < fimUtc &&
                    i.Pedido.StatusPedido == StatusPedido.Concluido &&
                    i.Pedido.Pagamentos.Any(pg =>
                        pg.StatusPagamento == StatusPagamento.Aprovado
                    ) &&
                    idsSelecionados.Contains(i.Pedido.FilialId)
                )
                .Select(i => new
                {
                    i.NomeProdutoSnapshot,
                    i.Quantidade,
                    i.TotalItem
                })
                .ToListAsync();

            model.ProdutosMaisVendidos = itensVendidos
                .GroupBy(i => i.NomeProdutoSnapshot)
                .Select(g => new ProdutoVendidoViewModel
                {
                    NomeProduto = g.Key,
                    QuantidadeVendida = g.Sum(i => i.Quantidade),
                    TotalVendido = g.Sum(i => i.TotalItem)
                })
                .OrderByDescending(p => p.QuantidadeVendida)
                .Take(10)
                .ToList();

            return View(model);
        }

        private static TimeZoneInfo ObterFusoBrasilia()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(
                    "America/Sao_Paulo"
                );
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById(
                    "E. South America Standard Time"
                );
            }
        }
    }
}