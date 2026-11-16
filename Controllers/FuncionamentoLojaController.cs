using System.Security.Claims;
using LojaCupCake.Data;
using LojaCupCake.Models;
using LojaCupCake.Services;
using LojaCupCake.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LojaCupCake.Controllers
{
    [Authorize(Roles = "Administrador,Funcionario")]
    public class FuncionamentoLojaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FuncionamentoLojaController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            IQueryable<Filial> consulta = _context.Filiais
                .Where(f => f.Ativo)
                .Include(f => f.Funcionamentos)
                .AsNoTracking();

            if (!User.IsInRole("Administrador"))
            {
                var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(usuarioId))
                    return Forbid();

                consulta = consulta.Where(f => f.Funcionarios.Any(ff =>
                    ff.Funcionario.UsuarioId == usuarioId &&
                    ff.Funcionario.Ativo &&
                    ff.Ativo &&
                    ff.PodeControlarFuncionamento));
            }

            var filiais = await consulta.OrderBy(f => f.Nome).ToListAsync();
            var agora = FuncionamentoLojaHelper.ObterHorarioLocal();

            var model = new FuncionamentoLojaIndexViewModel
            {
                Filiais = filiais.Select(f =>
                {
                    var estado = FuncionamentoLojaHelper.Avaliar(f, agora);
                    int dia = agora.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)agora.DayOfWeek;
                    var horariosHoje = f.Funcionamentos
                        .Where(h => (int)h.DiaSemana == dia)
                        .OrderBy(h => h.HoraAbertura)
                        .ToList();

                    return new FilialFuncionamentoLojaViewModel
                    {
                        FilialId = f.FilialId,
                        Nome = f.Nome,
                        Endereco = $"{f.Logradouro}, {f.Numero} — {f.Bairro}, {f.Cidade}/{f.Uf}",
                        AbertaAgora = estado.Aberta,
                        Status = estado.Status,
                        Modo = estado.Modo,
                        ModoFuncionamento = f.ModoFuncionamento,
                        HorariosHoje = horariosHoje.Count == 0
                            ? "Fechada hoje"
                            : string.Join(" • ", horariosHoje.Select(h => $"{h.HoraAbertura:hh\\:mm}–{h.HoraFechamento:hh\\:mm}"))
                    };
                }).ToList()
            };

            return View("~/Views/FuncionamentoLoja/Index.cshtml", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AlterarModo(long filialId, ModoFuncionamentoFilial modo)
        {
            if (!Enum.IsDefined(typeof(ModoFuncionamentoFilial), modo))
                return BadRequest();

            var filial = await _context.Filiais
                .Include(f => f.Funcionamentos)
                .FirstOrDefaultAsync(f => f.FilialId == filialId && f.Ativo);

            if (filial == null)
                return NotFound();

            if (!User.IsInRole("Administrador"))
            {
                var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(usuarioId))
                    return Forbid();

                bool autorizado = await _context.FuncionariosFiliais.AnyAsync(ff =>
                    ff.FilialId == filialId &&
                    ff.Funcionario.UsuarioId == usuarioId &&
                    ff.Funcionario.Ativo &&
                    ff.Ativo &&
                    ff.PodeControlarFuncionamento);

                if (!autorizado)
                    return Forbid();
            }

            filial.ModoFuncionamento = modo;
            await _context.SaveChangesAsync();

            TempData["Sucesso"] = modo switch
            {
                ModoFuncionamentoFilial.ForcarAberta => $"{filial.Nome} foi aberta manualmente.",
                ModoFuncionamentoFilial.ForcarFechada => $"{filial.Nome} foi fechada manualmente.",
                _ => $"{filial.Nome} voltou ao funcionamento automático."
            };

            return RedirectToAction(nameof(Index));
        }
    }
}
