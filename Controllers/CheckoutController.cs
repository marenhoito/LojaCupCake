
using System.Data;
using System.Globalization;
using LojaCupCake.Data;
using LojaCupCake.Models;
using LojaCupCake.Services;
using LojaCupCake.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LojaCupCake.Controllers
{
    [Authorize(Roles = "Cliente")]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly CarrinhoService _carrinho;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        public CheckoutController(
            ApplicationDbContext context,
            CarrinhoService carrinho,
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration)
        {
            _context = context;
            _carrinho = carrinho;
            _userManager = userManager;
            _configuration = configuration;
        }

        // ABRIR CHECKOUT
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var cliente = await ObterClienteAsync();

            if (cliente == null)
                return Forbid();

            var carrinho = _carrinho.Obter();

            if (!carrinho.Itens.Any() ||
                !carrinho.FilialId.HasValue)
            {
                TempData["Erro"] = "Seu carrinho está vazio.";
                return RedirectToAction("Index", "Carrinho");
            }

            var enderecoPrincipal = await _context.Enderecos
                .Where(e =>
                    e.ClienteId == cliente.ClienteId &&
                    e.Ativo)
                .OrderByDescending(e => e.Principal)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            var model = new CheckoutViewModel
            {
                EnderecoId = enderecoPrincipal?.EnderecoId,
                TelefoneContato = cliente.Usuario.PhoneNumber
            };

            await PrepararTelaAsync(model, carrinho, cliente.ClienteId);

            return View(model);
        }

        // FINALIZAR PEDIDO
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Finalizar(
            CheckoutViewModel model)
        {
            var cliente = await ObterClienteAsync();

            if (cliente == null)
                return Forbid();

            var carrinho = _carrinho.Obter();

            if (!carrinho.Itens.Any() ||
                !carrinho.FilialId.HasValue)
            {
                TempData["Erro"] = "Seu carrinho está vazio.";
                return RedirectToAction("Index", "Carrinho");
            }

            if (!Enum.IsDefined(
                typeof(TipoRecebimento),
                model.TipoRecebimento))
            {
                ModelState.AddModelError(
                    nameof(model.TipoRecebimento),
                    "Selecione uma modalidade válida."
                );
            }

            if (!Enum.IsDefined(
                typeof(FormaPagamento),
                model.FormaPagamento))
            {
                ModelState.AddModelError(
                    nameof(model.FormaPagamento),
                    "Selecione uma forma de pagamento válida."
                );
            }

            await PrepararTelaAsync(
                model,
                carrinho,
                cliente.ClienteId
            );

            // Validar dados de entrega.
            if (model.TipoRecebimento == TipoRecebimento.Entrega)
            {
                ValidarCampo(
                    model.TelefoneContato,
                    nameof(model.TelefoneContato),
                    "Informe o telefone de contato."
                );

                if (model.EnderecoId.HasValue)
                {
                    bool enderecoValido =
                        await _context.Enderecos.AnyAsync(e =>
                            e.EnderecoId == model.EnderecoId.Value &&
                            e.ClienteId == cliente.ClienteId &&
                            e.Ativo
                        );

                    if (!enderecoValido)
                    {
                        ModelState.AddModelError(
                            nameof(model.EnderecoId),
                            "Selecione um endereço válido."
                        );
                    }
                }
                else
                {
                    ValidarCampo(
                        model.Destinatario,
                        nameof(model.Destinatario),
                        "Informe o destinatário."
                    );

                    ValidarCampo(
                        model.Cep,
                        nameof(model.Cep),
                        "Informe o CEP."
                    );

                    ValidarCampo(
                        model.Logradouro,
                        nameof(model.Logradouro),
                        "Informe o logradouro."
                    );

                    ValidarCampo(
                        model.Numero,
                        nameof(model.Numero),
                        "Informe o número."
                    );

                    ValidarCampo(
                        model.Bairro,
                        nameof(model.Bairro),
                        "Informe o bairro."
                    );

                    ValidarCampo(
                        model.Cidade,
                        nameof(model.Cidade),
                        "Informe a cidade."
                    );

                    ValidarCampo(
                        model.Uf,
                        nameof(model.Uf),
                        "Informe a UF."
                    );
                }
            }

            if (!model.TodosItensDisponiveis)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Existem produtos indisponíveis ou com estoque insuficiente."
                );
            }

            if (!ModelState.IsValid)
                return View("Index", model);

            await using var transacao =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable
                );

            try
            {
                long filialId = carrinho.FilialId.Value;
                string usuarioId = _userManager.GetUserId(User)!;

                var filial = await _context.Filiais
                    .Include(f => f.Funcionamentos)
                    .FirstOrDefaultAsync(f =>
                        f.FilialId == filialId &&
                        f.Ativo
                    );

                if (filial == null)
                {
                    await transacao.RollbackAsync();

                    ModelState.AddModelError(
                        string.Empty,
                        "A filial selecionada não está mais disponível."
                    );

                    await PrepararTelaAsync(
                        model, carrinho, cliente.ClienteId);

                    return View("Index", model);
                }

                if (!FuncionamentoLojaHelper.EstaAberta(filial))
                {
                    await transacao.RollbackAsync();
                    ModelState.AddModelError(
                        string.Empty,
                        "A loja está fechada no momento. O pedido não pode ser finalizado agora."
                    );
                    await PrepararTelaAsync(model, carrinho, cliente.ClienteId);
                    return View("Index", model);
                }

                var quantidades = carrinho.Itens
                    .GroupBy(i => i.ProdutoId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Sum(i => i.Quantidade)
                    );

                var idsProdutos = quantidades.Keys.ToList();

                var produtos = await _context.Produtos
                    .Include(p => p.Categoria)
                    .Where(p => idsProdutos.Contains(p.ProdutoId))
                    .ToDictionaryAsync(p => p.ProdutoId);

                var estoques = await _context.Estoques
                    .Where(e =>
                        e.FilialId == filialId &&
                        idsProdutos.Contains(e.ProdutoId)
                    )
                    .ToDictionaryAsync(e => e.ProdutoId);

                var linhasPedido =
                    new List<(Produto Produto, Estoque Estoque, int Quantidade)>();

                // Validar novamente produtos e quantidades dentro da transação.
                foreach (var item in quantidades)
                {
                    if (!produtos.TryGetValue(
                        item.Key,
                        out var produto) ||
                        !produto.Ativo ||
                        !produto.Categoria.Ativa)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            "Um dos produtos do carrinho não está mais disponível."
                        );

                        continue;
                    }

                    if (!estoques.TryGetValue(
                        item.Key,
                        out var estoque) ||
                        estoque.QuantidadeAtual < item.Value)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            $"Estoque insuficiente para {produto.Nome}."
                        );

                        continue;
                    }

                    linhasPedido.Add((
                        produto,
                        estoque,
                        item.Value
                    ));
                }

                if (!ModelState.IsValid ||
                    linhasPedido.Count != quantidades.Count)
                {
                    await transacao.RollbackAsync();

                    await PrepararTelaAsync(
                        model, carrinho, cliente.ClienteId);

                    return View("Index", model); ;
                }

                // Endereço cadastrado, se selecionado.
                Endereco? endereco = null;

                if (model.TipoRecebimento == TipoRecebimento.Entrega &&
                    model.EnderecoId.HasValue)
                {
                    endereco = await _context.Enderecos
                        .FirstOrDefaultAsync(e =>
                            e.EnderecoId == model.EnderecoId.Value &&
                            e.ClienteId == cliente.ClienteId &&
                            e.Ativo
                        );

                    if (endereco == null)
                    {
                        await transacao.RollbackAsync();

                        ModelState.AddModelError(
                            nameof(model.EnderecoId),
                            "O endereço selecionado não está mais disponível."
                        );

                        await PrepararTelaAsync(
                            model, carrinho, cliente.ClienteId);

                        return View("Index", model); ;
                    }
                }

                DateTime agora = DateTime.UtcNow;

                decimal subtotal = linhasPedido.Sum(l =>
                    l.Produto.PrecoAtual * l.Quantidade
                );

                decimal taxaEntrega =
                    model.TipoRecebimento == TipoRecebimento.Entrega
                        ? ObterTaxaEntrega()
                        : 0m;

                decimal total = subtotal + taxaEntrega;

                string codigoPedido =
                    "LC-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();

                var pedido = new Pedido
                {
                    CodigoPedido = codigoPedido,
                    ClienteId = cliente.ClienteId,
                    FilialId = filialId,
                    DataPedido = agora,
                    TipoRecebimento = model.TipoRecebimento,
                    StatusPedido = StatusPedido.AguardandoAprovacao,
                    Subtotal = subtotal,
                    Desconto = 0,
                    TaxaEntrega = taxaEntrega,
                    Total = total,
                    Observacoes = model.Observacoes?.Trim(),

                    CodigoRetirada =
                        model.TipoRecebimento == TipoRecebimento.Retirada
                            ? Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()
                            : null
                };

                // Criar itens e reservar o estoque.
                foreach (var linha in linhasPedido)
                {
                    decimal totalItem =
                        linha.Produto.PrecoAtual * linha.Quantidade;

                    pedido.Itens.Add(new ItemPedido
                    {
                        ProdutoId = linha.Produto.ProdutoId,
                        NomeProdutoSnapshot = linha.Produto.Nome,
                        PrecoUnitario = linha.Produto.PrecoAtual,
                        Quantidade = linha.Quantidade,
                        DescontoItem = 0,
                        TotalItem = totalItem
                    });

                    int saldoAnterior =
                        linha.Estoque.QuantidadeAtual;

                    int saldoPosterior =
                        saldoAnterior - linha.Quantidade;

                    linha.Estoque.QuantidadeAtual = saldoPosterior;
                    linha.Estoque.AtualizadoEm = agora;

                    _context.MovimentacoesEstoque.Add(
                        new MovimentacaoEstoque
                        {
                            FilialId = filialId,
                            ProdutoId = linha.Produto.ProdutoId,
                            Tipo = TipoMovimentacaoEstoque.Saida,
                            Quantidade = linha.Quantidade,
                            SaldoAnterior = saldoAnterior,
                            SaldoPosterior = saldoPosterior,
                            Motivo = $"Reserva do pedido {codigoPedido}",
                            CriadoEm = agora,
                            UsuarioId = usuarioId,
                            Estoque = linha.Estoque
                        }
                    );
                }

                // Salvar uma cópia do endereço utilizado na entrega.
                if (model.TipoRecebimento == TipoRecebimento.Entrega)
                {
                    pedido.EnderecoEntrega = new EnderecoEntregaPedido
                    {
                        EnderecoId = endereco?.EnderecoId,

                        Destinatario = endereco?.Destinatario
                            ?? model.Destinatario!.Trim(),

                        TelefoneContato = model.TelefoneContato!.Trim(),

                        Cep = endereco?.Cep ?? model.Cep!.Trim(),
                        Logradouro = endereco?.Logradouro
                            ?? model.Logradouro!.Trim(),
                        Numero = endereco?.Numero ?? model.Numero!.Trim(),
                        Complemento = endereco?.Complemento
                            ?? model.Complemento?.Trim(),
                        Bairro = endereco?.Bairro ?? model.Bairro!.Trim(),
                        Cidade = endereco?.Cidade ?? model.Cidade!.Trim(),
                        Uf = (endereco?.Uf ?? model.Uf!.Trim())
                            .ToUpperInvariant(),
                        PontoReferencia = endereco?.PontoReferencia
                            ?? model.PontoReferencia?.Trim()
                    };
                }

                // Registrar pagamento pendente.
                pedido.Pagamentos.Add(new Pagamento
                {
                    FormaPagamento = model.FormaPagamento,
                    StatusPagamento = StatusPagamento.Pendente,
                    Valor = total,
                    CriadoEm = agora
                });

                // Registrar o status inicial.
                pedido.Historico.Add(new HistoricoStatusPedido
                {
                    StatusAnterior = null,
                    StatusNovo = StatusPedido.AguardandoAprovacao,
                    Observacao = "Pedido criado pelo cliente.",
                    UsuarioId = usuarioId,
                    DataAlteracao = agora
                });

                _context.Pedidos.Add(pedido);

                await _context.SaveChangesAsync();
                await transacao.CommitAsync();

                _carrinho.Limpar();

                TempData["Sucesso"] =
                    "Pedido criado com sucesso!";

                return RedirectToAction(
                    nameof(Confirmacao),
                    new { id = pedido.PedidoId }
                );
            }
            catch (DbUpdateException)
            {
                await transacao.RollbackAsync();

                ModelState.AddModelError(
                    string.Empty,
                    "Não foi possível concluir o pedido. " +
                    "Verifique os dados e tente novamente."
                );

                await PrepararTelaAsync(
                    model, carrinho, cliente.ClienteId);

                return View(model);
            }
        }

        // CONFIRMAÇÃO DO PEDIDO
        [HttpGet]
        public async Task<IActionResult> Confirmacao(long id)
        {
            var cliente = await ObterClienteAsync();

            if (cliente == null)
                return Forbid();

            var pedido = await _context.Pedidos
                .Include(p => p.Filial)
                .Include(p => p.Itens)
                .Include(p => p.Pagamentos)
                .Include(p => p.EnderecoEntrega)
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.PedidoId == id &&
                    p.ClienteId == cliente.ClienteId
                );

            if (pedido == null)
                return NotFound();

            return View(pedido);
        }

        // RECUPERAR CLIENTE AUTENTICADO
        private async Task<Cliente?> ObterClienteAsync()
        {
            var usuarioId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(usuarioId))
                return null;

            return await _context.Clientes
                .Include(c => c.Usuario)
                .FirstOrDefaultAsync(c =>
                    c.UsuarioId == usuarioId &&
                    c.Ativo
                );
        }

        // PREPARAR DADOS DA TELA
        private async Task PrepararTelaAsync(
            CheckoutViewModel model,
            CarrinhoSessao carrinho,
            long clienteId)
        {
            var filialId = carrinho.FilialId!.Value;

            var filial = await _context.Filiais
                .Include(f => f.Funcionamentos)
                .AsNoTracking()
                .FirstOrDefaultAsync(f =>
                    f.FilialId == filialId &&
                    f.Ativo
                );

            model.FilialDisponivel = filial != null;
            model.NomeFilial = filial?.Nome ?? "Filial indisponível";
            bool filialAberta = filial != null && FuncionamentoLojaHelper.EstaAberta(filial);
            ViewData["FilialFechada"] = filial != null && !filialAberta;
            ViewData["StatusFuncionamentoFilial"] = filial == null
                ? "Filial indisponível"
                : FuncionamentoLojaHelper.Avaliar(filial).Status;
            if (filial != null && !filialAberta)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "A loja está fechada no momento. Você pode revisar o carrinho, mas não pode concluir o pedido."
                );
            }

            var enderecos = await _context.Enderecos
                .Where(e =>
                    e.ClienteId == clienteId &&
                    e.Ativo
                )
                .AsNoTracking()
                .OrderByDescending(e => e.Principal)
                .ToListAsync();

            model.Enderecos = enderecos.Select(e =>
                new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = e.EnderecoId.ToString(),
                    Text = $"{e.Apelido ?? e.Logradouro}, {e.Numero} - {e.Cidade}",
                    Selected = e.EnderecoId == model.EnderecoId
                }).ToList();

            var quantidades = carrinho.Itens
                .GroupBy(i => i.ProdutoId)
                .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantidade));

            var idsProdutos = quantidades.Keys.ToList();

            var produtos = await _context.Produtos
                .Include(p => p.Categoria)
                .Where(p => idsProdutos.Contains(p.ProdutoId))
                .AsNoTracking()
                .ToDictionaryAsync(p => p.ProdutoId);

            var estoques = await _context.Estoques
                .Where(e =>
                    e.FilialId == filialId &&
                    idsProdutos.Contains(e.ProdutoId)
                )
                .AsNoTracking()
                .ToDictionaryAsync(e => e.ProdutoId, e => e.QuantidadeAtual);

            model.Itens.Clear();
            model.Subtotal = 0;
            model.TodosItensDisponiveis = filialAberta;

            foreach (var item in quantidades)
            {
                produtos.TryGetValue(item.Key, out var produto);

                if (produto == null)
                {
                    model.TodosItensDisponiveis = false;

                    model.Itens.Add(new CheckoutItemViewModel
                    {
                        ProdutoId = item.Key,
                        NomeProduto = "Produto indisponível",
                        Quantidade = item.Value,
                        Disponivel = false
                    });

                    continue;
                }

                estoques.TryGetValue(
                    item.Key,
                    out int saldo
                );

                bool disponivel =
                    filialAberta &&
                    produto.Ativo &&
                    produto.Categoria.Ativa &&
                    saldo >= item.Value;

                decimal totalItem =
                    produto.PrecoAtual * item.Value;

                model.Itens.Add(new CheckoutItemViewModel
                {
                    ProdutoId = produto.ProdutoId,
                    NomeProduto = produto.Nome,
                    Quantidade = item.Value,
                    PrecoUnitario = produto.PrecoAtual,
                    TotalItem = totalItem,
                    ImagemUrl = produto.ImagemUrl,
                    EstoqueDisponivel = saldo,
                    Disponivel = disponivel
                });

                model.Subtotal += totalItem;

                if (!disponivel)
                    model.TodosItensDisponiveis = false;
            }

            decimal taxaEntregaConfigurada = ObterTaxaEntrega();
            ViewData["TaxaEntregaConfigurada"] = taxaEntregaConfigurada;

            model.TaxaEntrega =
                model.TipoRecebimento == TipoRecebimento.Entrega
                    ? taxaEntregaConfigurada
                    : 0m;

            model.Total = model.Subtotal + model.TaxaEntrega;
        }

        // TAXA CONFIGURADA
        private decimal ObterTaxaEntrega()
        {
            var valor = _configuration["Checkout:TaxaEntrega"];

            if (string.IsNullOrWhiteSpace(valor))
                return 0m;

            if (!decimal.TryParse(
                valor,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out decimal taxa) || taxa < 0)
            {
                throw new InvalidOperationException(
                    "A taxa de entrega configurada é inválida."
                );
            }

            return taxa;
        }

        // VALIDAÇÃO CONDICIONAL DE ENDEREÇO
        private void ValidarCampo(
            string? valor,
            string propriedade,
            string mensagem)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                ModelState.AddModelError(
                    propriedade,
                    mensagem
                );
            }
        }
    }
}
