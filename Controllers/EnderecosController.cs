
using LojaCupCake.Data;
using LojaCupCake.Models;
using LojaCupCake.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LojaCupCake.Controllers
{
    [Authorize(Roles = "Cliente")]
    public class EnderecosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EnderecosController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // LISTAGEM
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var cliente = await ObterClienteAsync();

            if (cliente == null)
                return Forbid();

            var enderecos = await _context.Enderecos
                .Where(e =>
                    e.ClienteId == cliente.ClienteId &&
                    e.Ativo)
                .AsNoTracking()
                .OrderByDescending(e => e.Principal)
                .ThenBy(e => e.Apelido)
                .ToListAsync();

            return View(enderecos);
        }

        // CADASTRO
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var cliente = await ObterClienteAsync();

            if (cliente == null)
                return Forbid();

            bool possuiEnderecoPrincipal = await _context.Enderecos
                .AnyAsync(e =>
                    e.ClienteId == cliente.ClienteId &&
                    e.Ativo &&
                    e.Principal);

            return View(new EnderecoViewModel
            {
                Principal = !possuiEnderecoPrincipal
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            EnderecoViewModel model)
        {
            var cliente = await ObterClienteAsync();

            if (cliente == null)
                return Forbid();

            if (!ModelState.IsValid)
                return View(model);

            var possuiPrincipal = await _context.Enderecos
                .AnyAsync(e =>
                    e.ClienteId == cliente.ClienteId &&
                    e.Ativo &&
                    e.Principal);

            var endereco = new Endereco
            {
                ClienteId = cliente.ClienteId,
                Apelido = model.Apelido?.Trim(),
                Destinatario = model.Destinatario.Trim(),
                Cep = model.Cep.Trim(),
                Logradouro = model.Logradouro.Trim(),
                Numero = model.Numero.Trim(),
                Complemento = model.Complemento?.Trim(),
                Bairro = model.Bairro.Trim(),
                Cidade = model.Cidade.Trim(),
                Uf = model.Uf.Trim().ToUpperInvariant(),
                PontoReferencia = model.PontoReferencia?.Trim(),
                Principal = model.Principal || !possuiPrincipal,
                Ativo = true
            };

            if (endereco.Principal)
            {
                var outros = await _context.Enderecos
                    .Where(e =>
                        e.ClienteId == cliente.ClienteId &&
                        e.Ativo &&
                        e.Principal)
                    .ToListAsync();

                foreach (var outro in outros)
                    outro.Principal = false;
            }

            _context.Enderecos.Add(endereco);

            await _context.SaveChangesAsync();

            TempData["Sucesso"] =
                "Endereço cadastrado com sucesso!";

            return RedirectToAction(nameof(Index));
        }

        // EDIÇÃO
        [HttpGet]
        public async Task<IActionResult> Edit(long id)
        {
            var cliente = await ObterClienteAsync();

            if (cliente == null)
                return Forbid();

            var endereco = await _context.Enderecos
                .AsNoTracking()
                .FirstOrDefaultAsync(e =>
                    e.EnderecoId == id &&
                    e.ClienteId == cliente.ClienteId &&
                    e.Ativo);

            if (endereco == null)
                return NotFound();

            var model = new EnderecoViewModel
            {
                EnderecoId = endereco.EnderecoId,
                Apelido = endereco.Apelido,
                Destinatario = endereco.Destinatario,
                Cep = endereco.Cep,
                Logradouro = endereco.Logradouro,
                Numero = endereco.Numero,
                Complemento = endereco.Complemento,
                Bairro = endereco.Bairro,
                Cidade = endereco.Cidade,
                Uf = endereco.Uf,
                PontoReferencia = endereco.PontoReferencia,
                Principal = endereco.Principal
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            EnderecoViewModel model)
        {
            var cliente = await ObterClienteAsync();

            if (cliente == null)
                return Forbid();

            if (!ModelState.IsValid)
                return View(model);

            var endereco = await _context.Enderecos
                .FirstOrDefaultAsync(e =>
                    e.EnderecoId == model.EnderecoId &&
                    e.ClienteId == cliente.ClienteId &&
                    e.Ativo);

            if (endereco == null)
                return NotFound();

            endereco.Apelido = model.Apelido?.Trim();
            endereco.Destinatario = model.Destinatario.Trim();
            endereco.Cep = model.Cep.Trim();
            endereco.Logradouro = model.Logradouro.Trim();
            endereco.Numero = model.Numero.Trim();
            endereco.Complemento = model.Complemento?.Trim();
            endereco.Bairro = model.Bairro.Trim();
            endereco.Cidade = model.Cidade.Trim();
            endereco.Uf = model.Uf.Trim().ToUpperInvariant();
            endereco.PontoReferencia = model.PontoReferencia?.Trim();

            if (model.Principal)
            {
                var outros = await _context.Enderecos
                    .Where(e =>
                        e.ClienteId == cliente.ClienteId &&
                        e.EnderecoId != endereco.EnderecoId &&
                        e.Ativo &&
                        e.Principal)
                    .ToListAsync();

                foreach (var outro in outros)
                    outro.Principal = false;
            }

            endereco.Principal = model.Principal;

            await _context.SaveChangesAsync();

            TempData["Sucesso"] =
                "Endereço atualizado com sucesso!";

            return RedirectToAction(nameof(Index));
        }

        // DEFINIR ENDEREÇO PRINCIPAL
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DefinirPrincipal(long id)
        {
            var cliente = await ObterClienteAsync();

            if (cliente == null)
                return Forbid();

            var endereco = await _context.Enderecos
                .FirstOrDefaultAsync(e =>
                    e.EnderecoId == id &&
                    e.ClienteId == cliente.ClienteId &&
                    e.Ativo);

            if (endereco == null)
                return NotFound();

            var enderecos = await _context.Enderecos
                .Where(e =>
                    e.ClienteId == cliente.ClienteId &&
                    e.Ativo)
                .ToListAsync();

            foreach (var item in enderecos)
                item.Principal = item.EnderecoId == id;

            await _context.SaveChangesAsync();

            TempData["Sucesso"] =
                "Endereço principal atualizado!";

            return RedirectToAction(nameof(Index));
        }

        // ATIVAR / DESATIVAR
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AlterarStatus(long id)
        {
            var cliente = await ObterClienteAsync();

            if (cliente == null)
                return Forbid();

            var endereco = await _context.Enderecos
                .FirstOrDefaultAsync(e =>
                    e.EnderecoId == id &&
                    e.ClienteId == cliente.ClienteId);

            if (endereco == null)
                return NotFound();

            if (endereco.Ativo)
            {
                bool eraPrincipal = endereco.Principal;

                endereco.Ativo = false;
                endereco.Principal = false;

                if (eraPrincipal)
                {
                    var proximo = await _context.Enderecos
                        .Where(e =>
                            e.ClienteId == cliente.ClienteId &&
                            e.Ativo &&
                            e.EnderecoId != id)
                        .OrderByDescending(e => e.Principal)
                        .ThenBy(e => e.EnderecoId)
                        .FirstOrDefaultAsync();

                    if (proximo != null)
                        proximo.Principal = true;
                }
            }
            else
            {
                endereco.Ativo = true;

                bool existePrincipal = await _context.Enderecos
                    .AnyAsync(e =>
                        e.ClienteId == cliente.ClienteId &&
                        e.Ativo &&
                        e.Principal &&
                        e.EnderecoId != id);

                if (!existePrincipal)
                    endereco.Principal = true;
            }

            await _context.SaveChangesAsync();

            TempData["Sucesso"] = endereco.Ativo
                ? "Endereço reativado!"
                : "Endereço desativado!";

            return RedirectToAction(nameof(Index));
        }

        private async Task<Cliente?> ObterClienteAsync()
        {
            var usuarioId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(usuarioId))
                return null;

            return await _context.Clientes
                .FirstOrDefaultAsync(c =>
                    c.UsuarioId == usuarioId &&
                    c.Ativo);
        }
    }
}
