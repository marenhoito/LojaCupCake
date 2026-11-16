
using LojaCupCake.Data;
using LojaCupCake.Models;
using LojaCupCake.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LojaCupCake.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class FiliaisController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FiliaisController(ApplicationDbContext context)
        {
            _context = context;
        }

        // LISTAR FILIAIS

        public async Task<IActionResult> Index()
        {
            var filiais = await _context.Filiais
                .AsNoTracking()
                .OrderBy(f => f.Nome)
                .ToListAsync();

            return View(filiais);
        }

        // CADASTRAR FILIAL

        [HttpGet]
        public IActionResult Create()
        {
            return View(new FilialViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            FilialViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var filial = new Filial
            {
                Codigo = model.Codigo.Trim(),
                Nome = model.Nome.Trim(),
                Telefone = model.Telefone?.Trim(),
                Email = model.Email?.Trim(),
                Cep = model.Cep.Trim(),
                Logradouro = model.Logradouro.Trim(),
                Numero = model.Numero.Trim(),
                Complemento = model.Complemento?.Trim(),
                Bairro = model.Bairro.Trim(),
                Cidade = model.Cidade.Trim(),
                Uf = model.Uf.Trim().ToUpper(),
                Ativo = true,
                DataCadastro = DateTime.UtcNow
            };

            _context.Filiais.Add(filial);

            try
            {
                await _context.SaveChangesAsync();

                TempData["Sucesso"] =
                    "Filial cadastrada com sucesso!";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    nameof(model.Codigo),
                    "Não foi possível cadastrar. Verifique se o código já existe."
                );

                return View(model);
            }
        }

        // EDITAR FILIAL

        [HttpGet]
        public async Task<IActionResult> Edit(long id)
        {
            var filial = await _context.Filiais
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.FilialId == id);

            if (filial == null)
                return NotFound();

            var model = new FilialViewModel
            {
                FilialId = filial.FilialId,
                Codigo = filial.Codigo,
                Nome = filial.Nome,
                Telefone = filial.Telefone,
                Email = filial.Email,
                Cep = filial.Cep,
                Logradouro = filial.Logradouro,
                Numero = filial.Numero,
                Complemento = filial.Complemento,
                Bairro = filial.Bairro,
                Cidade = filial.Cidade,
                Uf = filial.Uf
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            FilialViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var filial = await _context.Filiais
                .FirstOrDefaultAsync(f =>
                    f.FilialId == model.FilialId);

            if (filial == null)
                return NotFound();

            filial.Codigo = model.Codigo.Trim();
            filial.Nome = model.Nome.Trim();
            filial.Telefone = model.Telefone?.Trim();
            filial.Email = model.Email?.Trim();
            filial.Cep = model.Cep.Trim();
            filial.Logradouro = model.Logradouro.Trim();
            filial.Numero = model.Numero.Trim();
            filial.Complemento = model.Complemento?.Trim();
            filial.Bairro = model.Bairro.Trim();
            filial.Cidade = model.Cidade.Trim();
            filial.Uf = model.Uf.Trim().ToUpper();

            try
            {
                await _context.SaveChangesAsync();

                TempData["Sucesso"] =
                    "Filial atualizada com sucesso!";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    nameof(model.Codigo),
                    "Não foi possível atualizar. Verifique o código da filial."
                );

                return View(model);
            }
        }

        // ATIVAR / DESATIVAR FILIAL

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AlterarStatus(long id)
        {
            var filial = await _context.Filiais
                .FirstOrDefaultAsync(f => f.FilialId == id);

            if (filial == null)
                return NotFound();

            filial.Ativo = !filial.Ativo;

            await _context.SaveChangesAsync();

            TempData["Sucesso"] = filial.Ativo
                ? "Filial ativada com sucesso!"
                : "Filial desativada com sucesso!";

            return RedirectToAction(nameof(Index));
        }
    }
}
