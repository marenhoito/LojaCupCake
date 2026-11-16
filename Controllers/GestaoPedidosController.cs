
using System.Data;
using System.Security.Claims;
using LojaCupCake.Data;
using LojaCupCake.Models;
using LojaCupCake.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LojaCupCake.Controllers
{
    [Authorize(Roles = "Administrador,Funcionario")]
    public class GestaoPedidosController : Controller
    {
        private readonly ApplicationDbContext _context;
        public GestaoPedidosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // LISTAGEM POR FILIAL

        [HttpGet]
        public async Task<IActionResult> Index(long? filialId)
        {
            bool administrador = User.IsInRole("Administrador");

            // Administrador vê todas as filiais, ativas ou desativadas.
            IQueryable<Filial> consultaFiliais = _context.Filiais;

            // Funcionários veem apenas filiais com permissão para aprovar pedidos.
            if (!administrador)
            {
                var usuarioId = User.FindFirstValue(
                    ClaimTypes.NameIdentifier
                );

                if (string.IsNullOrWhiteSpace(usuarioId))
                    return Forbid();

                consultaFiliais = consultaFiliais
                    .Where(f => f.Funcionarios.Any(ff =>
                        ff.Funcionario.UsuarioId == usuarioId &&
                        ff.Funcionario.Ativo &&
                        ff.Ativo &&
                        ff.PodeAprovarPedidos
                    ));
            }

            var filiaisBanco = await consultaFiliais
                .AsNoTracking()
                .OrderBy(f => f.Nome)
                .ToListAsync();

            var idsFiliais = filiaisBanco
                .Select(f => f.FilialId)
                .ToList();

            // Resumir os pedidos de todas as filiais permitidas.
            var resumoPedidos = new Dictionary<long,
                (int Total, int Pendentes, int EmAndamento, int Concluidos)>();

            if (idsFiliais.Any())
            {
                var agregados = await _context.Pedidos
                    .Where(p => idsFiliais.Contains(p.FilialId))
                    .GroupBy(p => p.FilialId)
                    .Select(g => new
                    {
                        FilialId = g.Key,

                        Total = g.Count(),

                        Pendentes = g.Count(p =>
                            p.StatusPedido == StatusPedido.AguardandoAprovacao
                        ),

                        EmAndamento = g.Count(p =>
                            p.StatusPedido == StatusPedido.Aprovado ||
                            p.StatusPedido == StatusPedido.EmPreparacao ||
                            p.StatusPedido == StatusPedido.Pronto ||
                            p.StatusPedido == StatusPedido.SaiuParaEntrega
                        ),

                        Concluidos = g.Count(p =>
                            p.StatusPedido == StatusPedido.Concluido
                        )
                    })
                    .ToListAsync();

                resumoPedidos = agregados.ToDictionary(
                    a => a.FilialId,
                    a => (
                        a.Total,
                        a.Pendentes,
                        a.EmAndamento,
                        a.Concluidos
                    )
                );
            }

            // Montar os cartões das filiais.
            var model = new GestaoPedidosIndexViewModel
            {
                FilialId = filialId,

                Filiais = filiaisBanco.Select(f =>
                {
                    resumoPedidos.TryGetValue(
                        f.FilialId,
                        out var resumo
                    );

                    return new FilialGestaoPedidosViewModel
                    {
                        FilialId = f.FilialId,
                        Codigo = f.Codigo,
                        Nome = f.Nome,
                        Ativa = f.Ativo,
                        Telefone = f.Telefone,
                        Email = f.Email,
                        Logradouro = f.Logradouro,
                        Numero = f.Numero,
                        Complemento = f.Complemento,
                        Bairro = f.Bairro,
                        Cidade = f.Cidade,
                        Uf = f.Uf,

                        TotalPedidos = resumo.Total,
                        PedidosPendentes = resumo.Pendentes,
                        PedidosEmAndamento = resumo.EmAndamento,
                        PedidosConcluidos = resumo.Concluidos
                    };
                }).ToList()
            };

            // Nenhuma filial selecionada: exibir apenas os cartões.
            if (!filialId.HasValue)
                return View(model);

            // Validar se a filial pertence ao conjunto permitido.
            var filialSelecionada = model.Filiais
                .FirstOrDefault(f => f.FilialId == filialId.Value);

            if (filialSelecionada == null)
                return Forbid();

            bool autorizado = await PodeGerenciarPedidosDaFilialAsync(
                filialId.Value
            );

            if (!autorizado)
                return Forbid();

            model.FilialSelecionada = filialSelecionada;

            // Carregar pedidos da filial selecionada.
            var pedidos = await _context.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.Pagamentos)
                .Where(p => p.FilialId == filialId.Value)
                .AsNoTracking()
                .OrderByDescending(p => p.DataPedido)
                .ToListAsync();

            model.Pedidos = pedidos.Select(p =>
            {
                var pagamento = p.Pagamentos
                    .OrderByDescending(pg => pg.CriadoEm)
                    .ThenByDescending(pg => pg.PagamentoId)
                    .FirstOrDefault();

                return new PedidoResumoViewModel
                {
                    PedidoId = p.PedidoId,
                    CodigoPedido = p.CodigoPedido,
                    NomeFilial = filialSelecionada.Nome,
                    NomeCliente = p.Cliente.NomeCompleto,
                    DataPedido = p.DataPedido,
                    StatusPedido = p.StatusPedido,
                    TipoRecebimento = p.TipoRecebimento,
                    Total = p.Total,
                    FormaPagamento = pagamento?.FormaPagamento,
                    StatusPagamento = pagamento?.StatusPagamento
                };
            }).ToList();

            return View(model);
        }


        // DETALHES DO PEDIDO
        [HttpGet]
        public async Task<IActionResult> Detalhes(long id)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.Filial)
                .Include(p => p.Itens)
                .Include(p => p.Pagamentos)
                .Include(p => p.EnderecoEntrega)
                .Include(p => p.Historico)
                    .ThenInclude(h => h.Responsavel)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PedidoId == id);

            if (pedido == null)
                return NotFound();

            bool autorizado = await PodeGerenciarPedidosDaFilialAsync(
                pedido.FilialId
            );

            if (!autorizado)
                return Forbid();

            return View(pedido);
        }

        // ALTERAR STATUS
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AlterarStatus(
            long pedidoId,
            StatusPedido novoStatus,
            string? observacao)
        {
            if (!Enum.IsDefined(
                typeof(StatusPedido),
                novoStatus))
            {
                return BadRequest();
            }

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
                        p.PedidoId == pedidoId
                    );

                if (pedido == null)
                    return NotFound();

                bool autorizado = await PodeGerenciarPedidosDaFilialAsync(
                    pedido.FilialId
                );

                if (!autorizado)
                    return Forbid();

                if (!TransicaoPermitida(
                    pedido.StatusPedido,
                    novoStatus,
                    pedido.TipoRecebimento))
                {
                    TempData["Erro"] =
                        "Esta alteração de status não é permitida.";

                    return RedirectToAction(
                        nameof(Detalhes),
                        new { id = pedidoId }
                    );
                }

                var pagamento = pedido.Pagamentos
                    .OrderByDescending(p => p.CriadoEm)
                    .ThenByDescending(p => p.PagamentoId)
                    .FirstOrDefault();

                // PIX e cartão precisam estar aprovados
                // antes da aprovação do pedido.
                if (novoStatus == StatusPedido.Aprovado)
                {
                    if (pagamento == null)
                    {
                        TempData["Erro"] =
                            "O pedido não possui pagamento registrado.";

                        return RedirectToAction(
                            nameof(Detalhes),
                            new { id = pedidoId }
                        );
                    }

                    if (pagamento.FormaPagamento != FormaPagamento.Dinheiro &&
                        pagamento.StatusPagamento != StatusPagamento.Aprovado)
                    {
                        TempData["Erro"] =
                            "O pagamento precisa estar aprovado antes de aprovar o pedido.";

                        return RedirectToAction(
                            nameof(Detalhes),
                            new { id = pedidoId }
                        );
                    }

                    if (pagamento.StatusPagamento == StatusPagamento.Recusado ||
                        pagamento.StatusPagamento == StatusPagamento.Cancelado)
                    {
                        TempData["Erro"] =
                            "O pagamento não pode ser utilizado neste pedido.";

                        return RedirectToAction(
                            nameof(Detalhes),
                            new { id = pedidoId }
                        );
                    }
                }

                string usuarioId =
                    User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? throw new InvalidOperationException(
                        "Usuário autenticado não identificado."
                    );

                DateTime agora = DateTime.UtcNow;

                // Rejeição: devolver as unidades reservadas.
                if (novoStatus == StatusPedido.Rejeitado)
                {
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
                                Motivo = $"Devolução do pedido rejeitado {pedido.CodigoPedido}",
                                CriadoEm = agora,
                                UsuarioId = usuarioId,
                                Estoque = estoque
                            }
                        );
                    }

                    // Cancelar ou estornar o pagamento simulado.
                    if (pagamento != null)
                    {
                        if (pagamento.StatusPagamento ==
                            StatusPagamento.Aprovado)
                        {
                            pagamento.StatusPagamento =
                                StatusPagamento.Estornado;
                        }
                        else if (pagamento.StatusPagamento ==
                                 StatusPagamento.Pendente)
                        {
                            pagamento.StatusPagamento =
                                StatusPagamento.Cancelado;
                        }

                        pagamento.AtualizadoEm = agora;
                    }
                }

                var statusAnterior = pedido.StatusPedido;

                pedido.StatusPedido = novoStatus;

                _context.HistoricosStatusPedido.Add(
                    new HistoricoStatusPedido
                    {
                        PedidoId = pedido.PedidoId,
                        StatusAnterior = statusAnterior,
                        StatusNovo = novoStatus,
                        Observacao = observacao?.Trim(),
                        UsuarioId = usuarioId,
                        DataAlteracao = agora
                    }
                );

                await _context.SaveChangesAsync();
                await transacao.CommitAsync();

                TempData["Sucesso"] =
                    "Status do pedido atualizado com sucesso!";

                return RedirectToAction(
                    nameof(Detalhes),
                    new { id = pedidoId }
                );
            }
            catch (OverflowException)
            {
                await transacao.RollbackAsync();

                TempData["Erro"] =
                    "A devolução ultrapassa o limite de estoque permitido.";

                return RedirectToAction(
                    nameof(Detalhes),
                    new { id = pedidoId }
                );
            }
            catch (DbUpdateException)
            {
                await transacao.RollbackAsync();

                TempData["Erro"] =
                    "Não foi possível atualizar o pedido.";

                return RedirectToAction(
                    nameof(Detalhes),
                    new { id = pedidoId }
                );
            }
        }



        // FINALIZAR RETIRADA: o funcionário precisa informar o código apresentado pelo cliente.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConcluirRetirada(
            long pedidoId,
            string codigoRetirada)
        {
            if (string.IsNullOrWhiteSpace(codigoRetirada))
            {
                TempData["Erro"] = "Informe o código de retirada apresentado pelo cliente.";
                return RedirectToAction(nameof(Detalhes), new { id = pedidoId });
            }

            await using var transacao =
                await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            try
            {
                var pedido = await _context.Pedidos
                    .Include(p => p.Pagamentos)
                    .FirstOrDefaultAsync(p => p.PedidoId == pedidoId);

                if (pedido == null)
                    return NotFound();

                if (!await PodeGerenciarPedidosDaFilialAsync(pedido.FilialId))
                    return Forbid();

                if (pedido.TipoRecebimento != TipoRecebimento.Retirada ||
                    pedido.StatusPedido != StatusPedido.Pronto)
                {
                    await transacao.RollbackAsync();
                    TempData["Erro"] = "Somente pedidos de retirada prontos podem ser finalizados com código.";
                    return RedirectToAction(nameof(Detalhes), new { id = pedidoId });
                }

                if (string.IsNullOrWhiteSpace(pedido.CodigoRetirada) ||
                    !string.Equals(
                        pedido.CodigoRetirada.Trim(),
                        codigoRetirada.Trim(),
                        StringComparison.OrdinalIgnoreCase))
                {
                    await transacao.RollbackAsync();
                    TempData["Erro"] = "Código de retirada inválido. Confira o código informado pelo cliente.";
                    return RedirectToAction(nameof(Detalhes), new { id = pedidoId });
                }

                string usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? throw new InvalidOperationException("Usuário autenticado não identificado.");
                var agora = DateTime.UtcNow;
                var statusAnterior = pedido.StatusPedido;

                pedido.StatusPedido = StatusPedido.Concluido;
                ConfirmarPagamentoEmDinheiro(pedido.Pagamentos, agora);

                _context.HistoricosStatusPedido.Add(new HistoricoStatusPedido
                {
                    PedidoId = pedido.PedidoId,
                    StatusAnterior = statusAnterior,
                    StatusNovo = StatusPedido.Concluido,
                    Observacao = "Retirada confirmada pela loja mediante validação do código apresentado pelo cliente.",
                    UsuarioId = usuarioId,
                    DataAlteracao = agora
                });

                await _context.SaveChangesAsync();
                await transacao.CommitAsync();

                TempData["Sucesso"] = "Código validado. Pedido de retirada concluído.";
                return RedirectToAction(nameof(Detalhes), new { id = pedidoId });
            }
            catch (DbUpdateException)
            {
                await transacao.RollbackAsync();
                TempData["Erro"] = "Não foi possível finalizar a retirada. Tente novamente.";
                return RedirectToAction(nameof(Detalhes), new { id = pedidoId });
            }
        }

        // CONFIRMAÇÃO DA ENTREGA PELA LOJA.
        // O status só muda para Concluido quando o cliente também confirmar.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarEntregaLoja(long pedidoId)
        {
            await using var transacao =
                await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            try
            {
                var pedido = await _context.Pedidos
                    .Include(p => p.Pagamentos)
                    .FirstOrDefaultAsync(p => p.PedidoId == pedidoId);

                if (pedido == null)
                    return NotFound();

                if (!await PodeGerenciarPedidosDaFilialAsync(pedido.FilialId))
                    return Forbid();

                if (pedido.TipoRecebimento != TipoRecebimento.Entrega ||
                    pedido.StatusPedido != StatusPedido.SaiuParaEntrega)
                {
                    await transacao.RollbackAsync();
                    TempData["Erro"] = "Somente pedidos em rota de entrega podem receber esta confirmação.";
                    return RedirectToAction(nameof(Detalhes), new { id = pedidoId });
                }

                if (pedido.EntregaConfirmadaPelaLoja)
                {
                    await transacao.RollbackAsync();
                    TempData["Erro"] = "A loja já confirmou esta entrega.";
                    return RedirectToAction(nameof(Detalhes), new { id = pedidoId });
                }

                string usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? throw new InvalidOperationException("Usuário autenticado não identificado.");
                var agora = DateTime.UtcNow;
                var statusAnterior = pedido.StatusPedido;

                pedido.EntregaConfirmadaPelaLoja = true;

                if (pedido.EntregaConfirmadaPeloCliente)
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
                        : "Loja confirmou a entrega. Aguardando confirmação do cliente.",
                    UsuarioId = usuarioId,
                    DataAlteracao = agora
                });

                await _context.SaveChangesAsync();
                await transacao.CommitAsync();

                TempData["Sucesso"] = pedido.StatusPedido == StatusPedido.Concluido
                    ? "Entrega confirmada por ambas as partes. Pedido concluído!"
                    : "Confirmação da loja registrada. Aguardando o cliente confirmar o recebimento.";

                return RedirectToAction(nameof(Detalhes), new { id = pedidoId });
            }
            catch (DbUpdateException)
            {
                await transacao.RollbackAsync();
                TempData["Erro"] = "Não foi possível registrar a confirmação da loja. Tente novamente.";
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


        // AUTORIZAÇÃO POR FILIAL, INCLUINDO FILIAIS DESATIVADAS
        private async Task<bool> PodeGerenciarPedidosDaFilialAsync(
            long filialId)
        {
            // Administrador pode consultar e gerenciar qualquer filial existente.
            if (User.IsInRole("Administrador"))
            {
                return await _context.Filiais
                    .AnyAsync(f => f.FilialId == filialId);
            }

            // Funcionário precisa estar ativo, vinculado à filial e autorizado.
            // Não verificamos Filial.Ativo: pedidos históricos continuam acessíveis.
            var usuarioId = User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );

            if (string.IsNullOrWhiteSpace(usuarioId))
                return false;

            return await _context.FuncionariosFiliais
                .AnyAsync(ff =>
                    ff.FilialId == filialId &&
                    ff.Funcionario.UsuarioId == usuarioId &&
                    ff.Funcionario.Ativo &&
                    ff.Ativo &&
                    ff.PodeAprovarPedidos
                );
        }

        // REGRAS DE TRANSIÇÃO
        private static bool TransicaoPermitida(
            StatusPedido atual,
            StatusPedido novo,
            TipoRecebimento recebimento)
        {
            return atual switch
            {
                StatusPedido.AguardandoAprovacao =>
                    novo == StatusPedido.Aprovado ||
                    novo == StatusPedido.Rejeitado,

                StatusPedido.Aprovado =>
                    novo == StatusPedido.EmPreparacao,

                StatusPedido.EmPreparacao =>
                    novo == StatusPedido.Pronto,

                StatusPedido.Pronto =>
                    recebimento == TipoRecebimento.Entrega &&
                    novo == StatusPedido.SaiuParaEntrega,

                // A conclusão é feita pelas ações dedicadas: código de retirada
                // ou confirmação da entrega pelas duas partes.
                StatusPedido.SaiuParaEntrega => false,

                _ => false
            };
        }
    }
}
