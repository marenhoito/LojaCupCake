
using LojaCupCake.Data;
using LojaCupCake.Models;
using LojaCupCake.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LojaCupCake.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class FuncionamentoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FuncionamentoController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        // LISTAR HORÁRIOS DA FILIAL

        [HttpGet]
        public async Task<IActionResult> Index(long? filialId)
        {
            // Sem filialId, direciona para a tela geral de funcionamento das lojas.
            // Com filialId, mantém a edição dos horários daquela filial.
            if (!filialId.HasValue || filialId.Value <= 0)
            {
                return RedirectToAction("Index", "FuncionamentoLoja");
            }

            var model = await MontarViewModel(filialId.Value);

            if (model == null)
                return NotFound();

            return View(model);
        }

        // ADICIONAR HORÁRIO

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Adicionar(
            FuncionamentoFilialViewModel model)
        {
            var filial = await _context.Filiais
                .FirstOrDefaultAsync(f => f.FilialId == model.FilialId);

            if (filial == null)
                return NotFound();

            if (!filial.Ativo)
                ModelState.AddModelError(
                    string.Empty,
                    "Não é possível alterar o funcionamento de uma filial inativa."
                );

            if (!Enum.IsDefined(typeof(DiaSemana), model.DiaSemana))
            {
                ModelState.AddModelError(
                    nameof(model.DiaSemana),
                    "Selecione um dia válido."
                );
            }

            if (model.HoraAbertura < TimeSpan.Zero ||
                model.HoraFechamento > TimeSpan.FromDays(1) ||
                model.HoraAbertura >= model.HoraFechamento)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "O horário de fechamento deve ser posterior ao de abertura."
                );
            }

            // Verificar sobreposição de horários
            var conflito = await _context.FuncionamentosFiliais
                .AnyAsync(f =>
                    f.FilialId == model.FilialId &&
                    f.DiaSemana == model.DiaSemana &&
                    f.HoraAbertura < model.HoraFechamento &&
                    f.HoraFechamento > model.HoraAbertura
                );

            if (conflito)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Esse horário se sobrepõe a outro funcionamento cadastrado."
                );
            }

            if (!ModelState.IsValid)
            {
                var pagina = await MontarViewModel(model.FilialId);

                if (pagina == null)
                    return NotFound();

                pagina.DiaSemana = model.DiaSemana;
                pagina.HoraAbertura = model.HoraAbertura;
                pagina.HoraFechamento = model.HoraFechamento;

                return View("Index", pagina);
            }

            var funcionamento = new FuncionamentoFilial
            {
                FilialId = model.FilialId,
                DiaSemana = model.DiaSemana,
                HoraAbertura = model.HoraAbertura,
                HoraFechamento = model.HoraFechamento
            };

            _context.FuncionamentosFiliais.Add(funcionamento);

            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Horário cadastrado com sucesso!";

            return RedirectToAction(
                nameof(Index),
                new { filialId = model.FilialId }
            );
        }

        // REMOVER HORÁRIO

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remover(
            long id,
            long filialId)
        {
            var horario = await _context.FuncionamentosFiliais
                .FirstOrDefaultAsync(f =>
                    f.FuncionamentoFilialId == id &&
                    f.FilialId == filialId
                );

            if (horario == null)
                return NotFound();

            _context.FuncionamentosFiliais.Remove(horario);

            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Horário removido com sucesso!";

            return RedirectToAction(
                nameof(Index),
                new { filialId }
            );
        }

        // MONTAR VIEWMODEL

        private async Task<FuncionamentoFilialViewModel?> MontarViewModel(
            long filialId)
        {
            var filial = await _context.Filiais
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.FilialId == filialId);

            if (filial == null)
                return null;

            var horarios = await _context.FuncionamentosFiliais
                .AsNoTracking()
                .Where(f => f.FilialId == filialId)
                .OrderBy(f => f.DiaSemana)
                .ThenBy(f => f.HoraAbertura)
                .ToListAsync();

            return new FuncionamentoFilialViewModel
            {
                FilialId = filial.FilialId,
                NomeFilial = filial.Nome,

                Horarios = horarios.Select(h =>
                    new HorarioFilialItemViewModel
                    {
                        FuncionamentoFilialId = h.FuncionamentoFilialId,
                        DiaSemana = h.DiaSemana,
                        HoraAbertura = h.HoraAbertura,
                        HoraFechamento = h.HoraFechamento
                    }).ToList()
            };
        }
    }




}
