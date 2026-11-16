using System.Security.Claims;
using LojaCupCake.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LojaCupCake.ViewComponents
{
    public class ChatNotificacoesViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public ChatNotificacoesViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var usuarioId = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(usuarioId))
                return Content(string.Empty);

            int total;
            bool administrador = UserClaimsPrincipal.IsInRole("Administrador");
            bool cliente = !administrador && UserClaimsPrincipal.IsInRole("Cliente");
            bool funcionario = !administrador && !cliente && UserClaimsPrincipal.IsInRole("Funcionario");

            if (administrador)
            {
                total = await _context.ChatMensagens.CountAsync(m =>
                    m.RemetenteUsuarioId == m.Pedido.Cliente.UsuarioId &&
                    m.LidaPelaLojaEm == null);
            }
            else if (cliente)
            {
                total = await _context.ChatMensagens.CountAsync(m =>
                    m.Pedido.Cliente.UsuarioId == usuarioId &&
                    m.Pedido.Cliente.Ativo &&
                    m.RemetenteUsuarioId != usuarioId &&
                    m.LidaPeloClienteEm == null);
            }
            else if (funcionario)
            {
                total = await _context.ChatMensagens.CountAsync(m =>
                    m.RemetenteUsuarioId == m.Pedido.Cliente.UsuarioId &&
                    m.LidaPelaLojaEm == null &&
                    m.Pedido.Filial.Funcionarios.Any(ff =>
                        ff.Funcionario.UsuarioId == usuarioId &&
                        ff.Funcionario.Ativo &&
                        ff.Ativo &&
                        ff.PodeAprovarPedidos));
            }
            else
            {
                return Content(string.Empty);
            }

            return View(total);
        }
    }
}
