
using LojaCupCake.Data;
using LojaCupCake.Models;
using LojaCupCake.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LojaCupCake.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class CategoriasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoriasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // LISTAGEM
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var categorias = await _context.Categorias
                .AsNoTracking()
                .OrderBy(c => c.Nome)
                .Select(c => new CategoriaViewModel
                {
                    CategoriaId = c.CategoriaId,
                    Nome = c.Nome,
                    Descricao = c.Descricao,
                    Ativa = c.Ativa,
                    QuantidadeProdutos = c.Produtos.Count()
                })
                .ToListAsync();

            return View(categorias);
        }

        // CADASTRO
        [HttpGet]
        public IActionResult Create()
        {
            return View(new CategoriaViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            CategoriaViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            string nome = model.Nome.Trim();

            bool existe = await _context.Categorias
                .AnyAsync(c => c.Nome == nome);

            if (existe)
            {
                ModelState.AddModelError(
                    nameof(model.Nome),
                    "Já existe uma categoria com esse nome."
                );

                return View(model);
            }

            var categoria = new Categoria
            {
                Nome = nome,
                Descricao = model.Descricao?.Trim(),
                Ativa = true,
                DataCadastro = DateTime.UtcNow
            };

            _context.Categorias.Add(categoria);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    nameof(model.Nome),
                    "Não foi possível cadastrar. Verifique se o nome já existe."
                );

                return View(model);
            }

            TempData["Sucesso"] = "Categoria cadastrada com sucesso!";

            return RedirectToAction(nameof(Index));
        }

        // EDIÇÃO
        [HttpGet]
        public async Task<IActionResult> Edit(long id)
        {
            var categoria = await _context.Categorias
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CategoriaId == id);

            if (categoria == null)
                return NotFound();

            var model = new CategoriaViewModel
            {
                CategoriaId = categoria.CategoriaId,
                Nome = categoria.Nome,
                Descricao = categoria.Descricao,
                Ativa = categoria.Ativa
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            CategoriaViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var categoria = await _context.Categorias
                .FirstOrDefaultAsync(c =>
                    c.CategoriaId == model.CategoriaId);

            if (categoria == null)
                return NotFound();

            string nome = model.Nome.Trim();

            bool existe = await _context.Categorias
                .AnyAsync(c =>
                    c.CategoriaId != model.CategoriaId &&
                    c.Nome == nome);

            if (existe)
            {
                ModelState.AddModelError(
                    nameof(model.Nome),
                    "Já existe outra categoria com esse nome."
                );

                return View(model);
            }

            categoria.Nome = nome;
            categoria.Descricao = model.Descricao?.Trim();

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Não foi possível atualizar a categoria."
                );

                return View(model);
            }

            TempData["Sucesso"] = "Categoria atualizada com sucesso!";

            return RedirectToAction(nameof(Index));
        }

        // ATIVAR / DESATIVAR
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AlterarStatus(long id)
        {
            var categoria = await _context.Categorias
                .FirstOrDefaultAsync(c => c.CategoriaId == id);

            if (categoria == null)
                return NotFound();

            categoria.Ativa = !categoria.Ativa;

            await _context.SaveChangesAsync();

            TempData["Sucesso"] = categoria.Ativa
                ? "Categoria ativada com sucesso!"
                : "Categoria desativada com sucesso!";

            return RedirectToAction(nameof(Index));
        }
    }
}
