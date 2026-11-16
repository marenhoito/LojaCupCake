
using LojaCupCake.Data;
using LojaCupCake.Models;
using LojaCupCake.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace LojaCupCake.Controllers
{
    [Authorize(Roles = "Cliente")]
    public class PedidosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PedidosController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // MEUS PEDIDOS
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var cliente = await ObterClienteAsync();

            if (cliente == null)
                return Forbid();

            var pedidos = await _context.Pedidos
                .Include(p => p.Filial)
                .Include(p => p.Pagamentos)
                .Where(p => p.ClienteId == cliente.ClienteId)
                .AsNoTracking()
                .OrderByDescending(p => p.DataPedido)
                .ToListAsync();

            var model = pedidos.Select(p =>
            {
                var pagamento = p.Pagamentos
                    .OrderByDescending(pg => pg.CriadoEm)
                    .ThenByDescending(pg => pg.PagamentoId)
                    .FirstOrDefault();

                return new PedidoResumoViewModel
                {
                    PedidoId = p.PedidoId,
                    CodigoPedido = p.CodigoPedido,
                    NomeFilial = p.Filial.Nome,
                    DataPedido = p.DataPedido,
                    StatusPedido = p.StatusPedido,
                    TipoRecebimento = p.TipoRecebimento,
                    Total = p.Total,
                    StatusPagamento = pagamento?.StatusPagamento,
                    FormaPagamento = pagamento?.FormaPagamento
                };
            }).ToList();

            return View(model);
        }

        // DETALHES DO PEDIDO
        [HttpGet]
        public async Task<IActionResult> Detalhes(long id)
        {
            var cliente = await ObterClienteAsync();

            if (cliente == null)
                return Forbid();

            var pedido = await _context.Pedidos
                .Include(p => p.Filial)
                .Include(p => p.Itens)
                .Include(p => p.Pagamentos)
                .Include(p => p.EnderecoEntrega)
                .Include(p => p.Historico)
                    .ThenInclude(h => h.Responsavel)
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.PedidoId == id &&
                    p.ClienteId == cliente.ClienteId
                );

            if (pedido == null)
                return NotFound();

            return View(pedido);
        }

        // SIMULAÇÃO DE PAGAMENTO
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SimularPagamento(
            long pedidoId,
            bool aprovado)
        {
            var cliente = await ObterClienteAsync();

            if (cliente == null)
                return Forbid();

            // Serializa a simulação com cancelamentos e alterações feitas
            // pela equipe, evitando aprovar um pagamento após o pedido
            // já ter sido cancelado ou rejeitado.
            await using var transacao =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable
                );

            try
            {
                var pedido = await _context.Pedidos
                    .Include(p => p.Pagamentos)
                    .FirstOrDefaultAsync(p =>
                        p.PedidoId == pedidoId &&
                        p.ClienteId == cliente.ClienteId
                    );

                if (pedido == null)
                {
                    await transacao.RollbackAsync();
                    return NotFound();
                }

                if (pedido.StatusPedido != StatusPedido.AguardandoAprovacao)
                {
                    await transacao.RollbackAsync();
                    TempData["Erro"] =
                        "O pagamento não pode mais ser alterado porque o pedido não está aguardando aprovação.";

                    return RedirectToAction(
                        nameof(Detalhes),
                        new { id = pedidoId }
                    );
                }

                var pagamento = pedido.Pagamentos
                    .OrderByDescending(p => p.CriadoEm)
                    .ThenByDescending(p => p.PagamentoId)
                    .FirstOrDefault();

                if (pagamento == null ||
                    pagamento.FormaPagamento == FormaPagamento.Dinheiro)
                {
                    await transacao.RollbackAsync();
                    return BadRequest(
                        "Esta modalidade não permite simulação online."
                    );
                }

                // O valor vem do pedido calculado no servidor. Esta
                // conferência protege contra inconsistência nos registros.
                if (pagamento.Valor != pedido.Total)
                {
                    await transacao.RollbackAsync();
                    TempData["Erro"] =
                        "O valor do pagamento diverge do total do pedido. Entre em contato com a loja.";

                    return RedirectToAction(
                        nameof(Detalhes),
                        new { id = pedidoId }
                    );
                }

                // Permite nova tentativa apenas quando o pagamento anterior
                // foi recusado; pagamentos aprovados/cancelados/estornados
                // não podem ser processados novamente.
                if (pagamento.StatusPagamento != StatusPagamento.Pendente &&
                    pagamento.StatusPagamento != StatusPagamento.Recusado)
                {
                    await transacao.RollbackAsync();
                    TempData["Erro"] =
                        "Este pagamento já foi processado.";

                    return RedirectToAction(
                        nameof(Detalhes),
                        new { id = pedidoId }
                    );
                }

                pagamento.StatusPagamento = aprovado
                    ? StatusPagamento.Aprovado
                    : StatusPagamento.Recusado;

                pagamento.CodigoTransacao =
                    "SIM-" + Guid.NewGuid().ToString("N")[..16].ToUpperInvariant();

                pagamento.AtualizadoEm = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transacao.CommitAsync();

                TempData["Sucesso"] = aprovado
                    ? "Pagamento simulado como aprovado!"
                    : "Pagamento simulado como recusado.";

                return RedirectToAction(
                    nameof(Detalhes),
                    new { id = pedidoId }
                );
            }
            catch (DbUpdateException)
            {
                await transacao.RollbackAsync();

                TempData["Erro"] =
                    "Não foi possível atualizar o pagamento. Tente novamente.";

                return RedirectToAction(
                    nameof(Detalhes),
                    new { id = pedidoId }
                );
            }
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(long pedidoId)
        {
            var cliente = await ObterClienteAsync();

            if (cliente == null)
                return Forbid();

            await using var transacao =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable
                );

            try
            {
                var pedido = await _context.Pedidos
                    .Include(p => p.Itens)
                    .Include(p => p.Pagamentos)
                    .FirstOrDefaultAsync(p =>
                        p.PedidoId == pedidoId &&
                        p.ClienteId == cliente.ClienteId
                    );

                if (pedido == null)
                    return NotFound();

                if (pedido.StatusPedido != StatusPedido.AguardandoAprovacao &&
                    pedido.StatusPedido != StatusPedido.Aprovado)
                {
                    await transacao.RollbackAsync();

                    TempData["Erro"] =
                        "Este pedido não pode mais ser cancelado pelo cliente.";

                    return RedirectToAction(
                        nameof(Detalhes),
                        new { id = pedidoId }
                    );
                }

                var usuarioId = _userManager.GetUserId(User);

                if (string.IsNullOrWhiteSpace(usuarioId))
                    return Forbid();

                DateTime agora = DateTime.UtcNow;

                // Devolver produtos reservados ao estoque.
                foreach (var item in pedido.Itens)
                {
                    var estoque = await _context.Estoques
                        .FirstOrDefaultAsync(e =>
                            e.FilialId == pedido.FilialId &&
                            e.ProdutoId == item.ProdutoId
                        );

                    if (estoque == null)
                    {
                        estoque = new Estoque
                        {
                            FilialId = pedido.FilialId,
                            ProdutoId = item.ProdutoId,
                            QuantidadeAtual = 0,
                            QuantidadeMinima = 0,
                            AtualizadoEm = agora
                        };

                        _context.Estoques.Add(estoque);
                    }

                    int saldoAnterior = estoque.QuantidadeAtual;

                    int saldoPosterior = checked(
                        saldoAnterior + item.Quantidade
                    );

                    estoque.QuantidadeAtual = saldoPosterior;
                    estoque.AtualizadoEm = agora;

                    _context.MovimentacoesEstoque.Add(
                        new MovimentacaoEstoque
                        {
                            FilialId = pedido.FilialId,
                            ProdutoId = item.ProdutoId,
                            Tipo = TipoMovimentacaoEstoque.Entrada,
                            Quantidade = item.Quantidade,
                            SaldoAnterior = saldoAnterior,
                            SaldoPosterior = saldoPosterior,
                            Motivo = $"Devolução por cancelamento do pedido {pedido.CodigoPedido}",
                            CriadoEm = agora,
                            UsuarioId = usuarioId,
                            Estoque = estoque
                        }
                    );
                }

                // Atualizar pagamento simulado.
                var pagamento = pedido.Pagamentos
                    .OrderByDescending(p => p.CriadoEm)
                    .ThenByDescending(p => p.PagamentoId)
                    .FirstOrDefault();

                if (pagamento != null)
                {
                    if (pagamento.StatusPagamento ==
                        StatusPagamento.Aprovado)
                    {
                        pagamento.StatusPagamento =
                            StatusPagamento.Estornado;
                    }
                    else if (
                        pagamento.StatusPagamento == StatusPagamento.Pendente ||
                        pagamento.StatusPagamento == StatusPagamento.Recusado)
                    {
                        pagamento.StatusPagamento =
                            StatusPagamento.Cancelado;
                    }

                    pagamento.AtualizadoEm = agora;
                }

                // Atualizar status e histórico.
                var statusAnterior = pedido.StatusPedido;

                pedido.StatusPedido = StatusPedido.Cancelado;

                _context.HistoricosStatusPedido.Add(
                    new HistoricoStatusPedido
                    {
                        PedidoId = pedido.PedidoId,
                        StatusAnterior = statusAnterior,
                        StatusNovo = StatusPedido.Cancelado,
                        Observacao = "Pedido cancelado pelo cliente.",
                        UsuarioId = usuarioId,
                        DataAlteracao = agora
                    }
                );

                await _context.SaveChangesAsync();
                await transacao.CommitAsync();

                TempData["Sucesso"] =
                    "Pedido cancelado. Os produtos foram devolvidos ao estoque.";

                return RedirectToAction(
                    nameof(Detalhes),
                    new { id = pedidoId }
                );
            }
            catch (OverflowException)
            {
                await transacao.RollbackAsync();

                TempData["Erro"] =
                    "Não foi possível devolver as quantidades ao estoque.";

                return RedirectToAction(
                    nameof(Detalhes),
                    new { id = pedidoId }
                );
            }
            catch (DbUpdateException)
            {
                await transacao.RollbackAsync();

                TempData["Erro"] =
                    "Não foi possível cancelar o pedido.";

                return RedirectToAction(
                    nameof(Detalhes),
                    new { id = pedidoId }
                );
            }
        }





        // ALTERAR A FORMA DE PAGAMENTO APÓS UMA RECUSA.
        // Mantém a tentativa recusada no histórico e cria um novo registro pendente.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TrocarFormaPagamento(
            long pedidoId,
            FormaPagamento novaFormaPagamento)
        {
            var cliente = await ObterClienteAsync();

            if (cliente == null)
                return Forbid();

            if (!Enum.IsDefined(typeof(FormaPagamento), novaFormaPagamento))
                return BadRequest();

            await using var transacao =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable
                );

            try
            {
                var pedido = await _context.Pedidos
                    .Include(p => p.Pagamentos)
                    .FirstOrDefaultAsync(p =>
                        p.PedidoId == pedidoId &&
                        p.ClienteId == cliente.ClienteId
                    );

                if (pedido == null)
                    return NotFound();

                var pagamentoAtual = pedido.Pagamentos
                    .OrderByDescending(p => p.CriadoEm)
                    .ThenByDescending(p => p.PagamentoId)
                    .FirstOrDefault();

                if (pedido.StatusPedido != StatusPedido.AguardandoAprovacao ||
                    pagamentoAtual == null ||
                    pagamentoAtual.StatusPagamento != StatusPagamento.Recusado)
                {
                    await transacao.RollbackAsync();
                    TempData["Erro"] =
                        "Só é possível trocar a forma de pagamento após uma recusa, enquanto o pedido aguarda aprovação.";
                    return RedirectToAction(nameof(Detalhes), new { id = pedidoId });
                }

                if (pagamentoAtual.FormaPagamento == novaFormaPagamento)
                {
                    await transacao.RollbackAsync();
                    TempData["Erro"] =
                        "Selecione uma forma de pagamento diferente da tentativa recusada.";
                    return RedirectToAction(nameof(Detalhes), new { id = pedidoId });
                }

                if (pagamentoAtual.Valor != pedido.Total)
                {
                    await transacao.RollbackAsync();
                    TempData["Erro"] =
                        "O valor do pagamento anterior diverge do total do pedido. Entre em contato com a loja.";
                    return RedirectToAction(nameof(Detalhes), new { id = pedidoId });
                }

                var agora = DateTime.UtcNow;
                _context.Pagamentos.Add(new Pagamento
                {
                    PedidoId = pedido.PedidoId,
                    FormaPagamento = novaFormaPagamento,
                    StatusPagamento = StatusPagamento.Pendente,
                    Valor = pedido.Total,
                    CriadoEm = agora
                });

                await _context.SaveChangesAsync();
                await transacao.CommitAsync();

                TempData["Sucesso"] = novaFormaPagamento == FormaPagamento.Dinheiro
                    ? "Forma alterada para dinheiro. O pagamento será confirmado no recebimento."
                    : "Forma de pagamento alterada. Faça a nova simulação de pagamento.";

                return RedirectToAction(nameof(Detalhes), new { id = pedidoId });
            }
            catch (DbUpdateException)
            {
                await transacao.RollbackAsync();
                TempData["Erro"] = "Não foi possível alterar a forma de pagamento. Tente novamente.";
                return RedirectToAction(nameof(Detalhes), new { id = pedidoId });
            }
        }

        // CONFIRMAÇÃO DO RECEBIMENTO PELO CLIENTE.
        // Para concluir uma entrega, a loja e o cliente precisam confirmar.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarEntrega(long pedidoId)
        {
            var cliente = await ObterClienteAsync();

            if (cliente == null)
                return Forbid();

            await using var transacao =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable
                );

            try
            {
                var pedido = await _context.Pedidos
                    .Include(p => p.Pagamentos)
                    .FirstOrDefaultAsync(p =>
                        p.PedidoId == pedidoId &&
                        p.ClienteId == cliente.ClienteId
                    );

                if (pedido == null)
                    return NotFound();

                if (pedido.TipoRecebimento != TipoRecebimento.Entrega ||
                    pedido.StatusPedido != StatusPedido.SaiuParaEntrega)
                {
                    await transacao.RollbackAsync();
                    TempData["Erro"] = "Este pedido não está aguardando confirmação de entrega.";
                    return RedirectToAction(nameof(Detalhes), new { id = pedidoId });
                }

                if (pedido.EntregaConfirmadaPeloCliente)
                {
                    await transacao.RollbackAsync();
                    TempData["Erro"] = "Você já confirmou o recebimento deste pedido.";
                    return RedirectToAction(nameof(Detalhes), new { id = pedidoId });
                }

                string usuarioId = _userManager.GetUserId(User)!;
                var agora = DateTime.UtcNow;
                var statusAnterior = pedido.StatusPedido;

                pedido.EntregaConfirmadaPeloCliente = true;

                if (pedido.EntregaConfirmadaPelaLoja)
                {
                    pedido.StatusPedido = StatusPedido.Concluido;
                    ConfirmarPagamentoEmDinheiro(pedido.Pagamentos, agora);
                }

                _context.HistoricosStatusPedido.Add(new HistoricoStatusPedido
                {
                    PedidoId = pedido.PedidoId,
                    StatusAnterior = statusAnterior,
                    StatusNovo = pedido.StatusPedido,
                    Observacao = pedido.StatusPedido == StatusPedido.Concluido
                        ? "Entrega confirmada pela loja e pelo cliente. Pedido concluído."
                        : "Cliente confirmou o recebimento. Aguardando confirmação da loja.",
                    UsuarioId = usuarioId,
                    DataAlteracao = agora
                });

                await _context.SaveChangesAsync();
                await transacao.CommitAsync();

                TempData["Sucesso"] = pedido.StatusPedido == StatusPedido.Concluido
                    ? "Entrega confirmada por ambas as partes. Pedido concluído!"
                    : "Recebimento confirmado. O pedido será concluído quando a loja também confirmar.";

                return RedirectToAction(nameof(Detalhes), new { id = pedidoId });
            }
            catch (DbUpdateException)
            {
                await transacao.RollbackAsync();
                TempData["Erro"] = "Não foi possível confirmar o recebimento. Atualize a página e tente novamente.";
                return RedirectToAction(nameof(Detalhes), new { id = pedidoId });
            }
        }

        private static void ConfirmarPagamentoEmDinheiro(
            ICollection<Pagamento> pagamentos,
            DateTime agora)
        {
            var pagamento = pagamentos
                .OrderByDescending(p => p.CriadoEm)
                .ThenByDescending(p => p.PagamentoId)
                .FirstOrDefault();

            if (pagamento?.FormaPagamento == FormaPagamento.Dinheiro &&
                pagamento.StatusPagamento == StatusPagamento.Pendente)
            {
                pagamento.StatusPagamento = StatusPagamento.Aprovado;
                pagamento.CodigoTransacao =
                    "DIN-" + Guid.NewGuid().ToString("N")[..16].ToUpperInvariant();
                pagamento.AtualizadoEm = agora;
            }
        }


        private async Task<Cliente?> ObterClienteAsync()
        {
            var usuarioId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(usuarioId))
                return null;

            return await _context.Clientes
                .FirstOrDefaultAsync(c =>
                    c.UsuarioId == usuarioId &&
                    c.Ativo
                );
        }
    }
}
