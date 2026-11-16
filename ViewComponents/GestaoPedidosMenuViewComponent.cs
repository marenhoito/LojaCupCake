
using System.Security.Claims;
using LojaCupCake.Data;
using LojaCupCake.Models;
using LojaCupCake.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LojaCupCake.ViewComponents
{
    public class GestaoPedidosMenuViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public GestaoPedidosMenuViewComponent(
            ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {


            var usuario = ViewContext.HttpContext.User;


            var model = new GestaoPedidosMenuViewModel();

            bool administrador =
            usuario.IsInRole("Administrador");

            bool funcionario =
                usuario.IsInRole("Funcionario");

            // Clientes não visualizam a gestão de pedidos.
            if (!administrador && !funcionario)
                return View(model);

            List<long> filiaisIds;

            if (administrador)
            {
                filiaisIds = await _context.Filiais
                    .Where(f => f.Ativo)
                    .Select(f => f.FilialId)
                    .ToListAsync();
            }
            else
            {
                var usuarioId = usuario.FindFirstValue(
                  ClaimTypes.NameIdentifier
                    );

                if (string.IsNullOrWhiteSpace(usuarioId))
                    return View(model);

                filiaisIds = await _context.FuncionariosFiliais
                    .Where(ff =>
                        ff.Funcionario.UsuarioId == usuarioId &&
                        ff.Funcionario.Ativo &&
                        ff.Ativo &&
                        ff.PodeAprovarPedidos &&
                        ff.Filial.Ativo
                    )
                    .Select(ff => ff.FilialId)
                    .Distinct()
                    .ToListAsync();

                // Funcionário sem permissão não vê o menu.
                if (!filiaisIds.Any())
                    return View(model);
            }

            model.Exibir = true;

            model.PedidosPendentes = await _context.Pedidos
                .CountAsync(p =>
                    filiaisIds.Contains(p.FilialId) &&
                    p.StatusPedido ==
                        StatusPedido.AguardandoAprovacao
                );

            return View(model);
        }
    }
}
