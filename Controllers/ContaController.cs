
using LojaCupCake.Data;
using LojaCupCake.Models;
using LojaCupCake.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LojaCupCake.Controllers
{

    public class ContaController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public ContaController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        // CADASTRO

        [HttpGet]
        public IActionResult Cadastro()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cadastro(
            CadastroClienteViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var usuario = new ApplicationUser
                {
                    UserName = model.Email.Trim(),
                    Email = model.Email.Trim(),
                    PhoneNumber = model.Telefone
                };

                var resultado = await _userManager.CreateAsync(
                    usuario,
                    model.Senha
                );

                if (!resultado.Succeeded)
                {
                    foreach (var erro in resultado.Errors)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            erro.Description
                        );
                    }

                    return View(model);
                }

                // Associar o usuário à função Cliente
                var resultadoRole = await _userManager.AddToRoleAsync(
                    usuario,
                    "Cliente"
                );

                if (!resultadoRole.Succeeded)
                {
                    foreach (var erro in resultadoRole.Errors)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            erro.Description
                        );
                    }

                    return View(model);
                }

                // Criar o perfil do cliente
                var cliente = new Cliente
                {
                    UsuarioId = usuario.Id,
                    NomeCompleto = model.NomeCompleto.Trim(),
                    Cpf = string.IsNullOrWhiteSpace(model.Cpf)
                        ? null
                        : model.Cpf.Trim(),
                    Ativo = true,
                    DataCadastro = DateTime.UtcNow
                };

                _context.Clientes.Add(cliente);

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                TempData["Sucesso"] =
                    "Cadastro realizado com sucesso! Faça seu login.";

                return RedirectToAction(nameof(Login));
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError(
                    string.Empty,
                    "Não foi possível concluir o cadastro. " +
                    "Verifique se o CPF já está cadastrado."
                );

                return View(model);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // LOGIN

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginViewModel model,
            string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            var resultado = await _signInManager.PasswordSignInAsync(
                model.Email.Trim(),
                model.Senha,
                model.LembrarDeMim,
                lockoutOnFailure: true
            );


            if (resultado.Succeeded)
            {
                var usuario = await _userManager.FindByEmailAsync(
                    model.Email.Trim()
                );

                // Verificar se o usuário é funcionário
                if (usuario != null &&
                    await _userManager.IsInRoleAsync(usuario, "Funcionario"))
                {
                    // Verificar se possui cadastro profissional ativo
                    bool funcionarioAtivo = await _context.Funcionarios
                        .AnyAsync(f =>
                            f.UsuarioId == usuario.Id &&
                            f.Ativo
                        );

                    if (!funcionarioAtivo)
                    {
                        await _signInManager.SignOutAsync();

                        ModelState.AddModelError(
                            string.Empty,
                            "Seu acesso de funcionário está inativo. " +
                            "Entre em contato com o administrador."
                        );

                        return View(model);
                    }
                }

                if (!string.IsNullOrEmpty(returnUrl)
                    && Url.IsLocalUrl(returnUrl))
                {
                    return LocalRedirect(returnUrl);
                }

                if (usuario != null &&
                  await _userManager.IsInRoleAsync(usuario, "Funcionario"))
                  {
                    return RedirectToAction("Index", "AreaFuncionario");
                }

                return RedirectToAction("Index", "Home");
            }


            if (resultado.IsLockedOut)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Conta temporariamente bloqueada. Tente novamente mais tarde."
                );
            }
            else
            {
                ModelState.AddModelError(
                    string.Empty,
                    "E-mail ou senha incorretos."
                );
            }

            return View(model);
        }

        // LOGOUT

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction("Index", "Home");
        }

        // ACESSO NEGADO

        [HttpGet]
        public IActionResult AcessoNegado()
        {
            return View();
        }
    }
}
