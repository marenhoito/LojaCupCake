using System.Security.Claims;
using LojaCupCake.Data;
using LojaCupCake.Models;
using LojaCupCake.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LojaCupCake.Controllers
{
    [Authorize(Roles = "Cliente,Funcionario,Administrador")]
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ChatController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Caixa de entrada: reúne pedidos que já possuem conversa.
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var usuarioId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(usuarioId))
                return Challenge();

            bool administrador = User.IsInRole("Administrador");
            bool cliente = !administrador && User.IsInRole("Cliente");
            bool funcionario = !administrador && !cliente && User.IsInRole("Funcionario");

            IQueryable<Pedido> consulta = _context.Pedidos
                .AsNoTracking()
                .Where(p => p.MensagensChat.Any());

            if (administrador)
            {
                // Administrador consulta todas as conversas.
            }
            else if (cliente)
            {
                consulta = consulta.Where(p =>
                    p.Cliente.UsuarioId == usuarioId && p.Cliente.Ativo);
            }
            else if (funcionario)
            {
                // Inclui filiais inativas para preservar a consulta do histórico.
                consulta = consulta.Where(p => p.Filial.Funcionarios.Any(ff =>
                    ff.Funcionario.UsuarioId == usuarioId &&
                    ff.Funcionario.Ativo &&
                    ff.Ativo &&
                    ff.PodeAprovarPedidos));
            }
            else
            {
                return Forbid();
            }

            var conversas = await consulta
                .Select(p => new ChatConversaViewModel
                {
                    PedidoId = p.PedidoId,
                    CodigoPedido = p.CodigoPedido,
                    NomeFilial = p.Filial.Nome,
                    NomeCliente = p.Cliente.NomeCompleto,
                    UltimaMensagem = p.MensagensChat
                        .OrderByDescending(m => m.EnviadaEm)
                        .ThenByDescending(m => m.ChatMensagemId)
                        .Select(m => m.Conteudo)
                        .FirstOrDefault() ?? string.Empty,
                    DataUltimaMensagem = p.MensagensChat
                        .Max(m => (DateTime?)m.EnviadaEm),
                    MensagensNaoLidas = cliente
                        ? p.MensagensChat.Count(m =>
                            m.RemetenteUsuarioId != usuarioId &&
                            m.LidaPeloClienteEm == null)
                        : p.MensagensChat.Count(m =>
                            m.RemetenteUsuarioId == p.Cliente.UsuarioId &&
                            m.LidaPelaLojaEm == null)
                })
                .OrderByDescending(c => c.DataUltimaMensagem)
                .ToListAsync();

            ViewData["EhCliente"] = cliente;
            return View(conversas);
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> ContarNaoLidas()
        {
            var usuarioId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(usuarioId))
                return Unauthorized();

            int total = await ContarNaoLidasAsync(usuarioId);
            return Json(new { total });
        }

        [HttpGet]
        public async Task<IActionResult> Pedido(long id)
        {
            var usuarioId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(usuarioId))
                return Challenge();

            var pedido = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.PedidoId == id)
                .Select(p => new
                {
                    p.PedidoId,
                    p.CodigoPedido,
                    NomeFilial = p.Filial.Nome
                })
                .FirstOrDefaultAsync();

            if (pedido == null || !await PodeAcessarPedidoAsync(id, usuarioId))
                return NotFound();

            var model = new ChatPedidoViewModel
            {
                PedidoId = pedido.PedidoId,
                CodigoPedido = pedido.CodigoPedido,
                NomeFilial = pedido.NomeFilial,
                UsuarioId = usuarioId
            };

            return View(model);
        }

        private async Task<int> ContarNaoLidasAsync(string usuarioId)
        {
            if (User.IsInRole("Administrador"))
            {
                return await _context.ChatMensagens.CountAsync(m =>
                    m.RemetenteUsuarioId == m.Pedido.Cliente.UsuarioId &&
                    m.LidaPelaLojaEm == null);
            }

            if (User.IsInRole("Cliente"))
            {
                return await _context.ChatMensagens.CountAsync(m =>
                    m.Pedido.Cliente.UsuarioId == usuarioId &&
                    m.Pedido.Cliente.Ativo &&
                    m.RemetenteUsuarioId != usuarioId &&
                    m.LidaPeloClienteEm == null);
            }

            if (User.IsInRole("Funcionario"))
            {
                return await _context.ChatMensagens.CountAsync(m =>
                    m.RemetenteUsuarioId == m.Pedido.Cliente.UsuarioId &&
                    m.LidaPelaLojaEm == null &&
                    m.Pedido.Filial.Funcionarios.Any(ff =>
                        ff.Funcionario.UsuarioId == usuarioId &&
                        ff.Funcionario.Ativo &&
                        ff.Ativo &&
                        ff.PodeAprovarPedidos));
            }

            return 0;
        }

        private async Task<bool> PodeAcessarPedidoAsync(long pedidoId, string usuarioId)
        {
            if (User.IsInRole("Administrador"))
                return await _context.Pedidos.AnyAsync(p => p.PedidoId == pedidoId);

            if (User.IsInRole("Cliente"))
            {
                return await _context.Pedidos.AnyAsync(p =>
                    p.PedidoId == pedidoId &&
                    p.Cliente.UsuarioId == usuarioId &&
                    p.Cliente.Ativo);
            }

            if (User.IsInRole("Funcionario"))
            {
                return await _context.FuncionariosFiliais.AnyAsync(ff =>
                    ff.Filial.Pedidos.Any(p => p.PedidoId == pedidoId) &&
                    ff.Funcionario.UsuarioId == usuarioId &&
                    ff.Funcionario.Ativo &&
                    ff.Ativo &&
                    ff.PodeAprovarPedidos);
            }

            return false;
        }
    }
}
