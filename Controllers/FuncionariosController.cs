
using System.Collections.Generic;
using System.Linq;
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
    [Authorize(Roles = "Administrador,Funcionario")]
    public class FuncionariosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public FuncionariosController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Listagem geral: somente administrador.
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Index()
        {
            var funcionarios = await _context.Funcionarios
                .Include(f => f.Usuario)
                .Include(f => f.Filiais)
                    .ThenInclude(ff => ff.Filial)
                .AsNoTracking()
                .OrderBy(f => f.NomeCompleto)
                .ToListAsync();

            return View(funcionarios);
        }

        // Exibir cadastro
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var filiaisPermitidas =
                await ObterFiliaisPermitidasParaCadastroAsync();

            // Funcionário sem permissão em nenhuma filial não pode cadastrar.
            if (!User.IsInRole("Administrador") && !filiaisPermitidas.Any())
                return Forbid();

            var model = new CadastroFuncionarioViewModel
            {
                Filiais = filiaisPermitidas.Select(f =>
                    new FuncionarioFilialViewModel
                    {
                        FilialId = f.FilialId,
                        NomeFilial = f.Nome
                    }).ToList()
            };

            return View(model);
        }

        // Processar cadastro
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            CadastroFuncionarioViewModel model)
        {
            bool administrador = User.IsInRole("Administrador");
            var filiaisPermitidas =
                await ObterFiliaisPermitidasParaCadastroAsync();

            if (!administrador && !filiaisPermitidas.Any())
                return Forbid();

            // Nunca confiar nos FilialIds enviados pelo navegador.
            var dadosEnviados = (model.Filiais ?? new List<FuncionarioFilialViewModel>())
                .GroupBy(f => f.FilialId)
                .ToDictionary(g => g.Key, g => g.First());

            var idsPermitidos = filiaisPermitidas
                .Select(f => f.FilialId)
                .ToHashSet();

            var idsSolicitados = dadosEnviados.Values
                .Where(f => f.Selecionada)
                .Select(f => f.FilialId)
                .Distinct()
                .ToList();

            if (idsSolicitados.Any(id => !idsPermitidos.Contains(id)))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Você tentou vincular o funcionário a uma filial sem permissão."
                );
            }

            // Reconstruir a lista somente com filiais permitidas e existentes.
            model.Filiais = filiaisPermitidas.Select(f =>
            {
                dadosEnviados.TryGetValue(f.FilialId, out var enviado);

                return new FuncionarioFilialViewModel
                {
                    FilialId = f.FilialId,
                    NomeFilial = f.Nome,
                    Selecionada = enviado?.Selecionada ?? false,
                    PodeAprovarPedidos = enviado?.PodeAprovarPedidos ?? false,
                    // Somente administrador concede permissões especiais.
                    PodeCadastrarFuncionarios = administrador &&
                        (enviado?.PodeCadastrarFuncionarios ?? false),
                    PodeControlarFuncionamento = administrador &&
                        (enviado?.PodeControlarFuncionamento ?? false)
                };
            }).ToList();

            if (!model.Filiais.Any(f => f.Selecionada))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Selecione pelo menos uma filial autorizada."
                );
            }

            // Verificar matrícula duplicada
            bool matriculaExiste = await _context.Funcionarios
                .AnyAsync(f => f.Matricula == model.Matricula);

            if (matriculaExiste)
            {
                ModelState.AddModelError(
                    nameof(model.Matricula),
                    "Essa matrícula já está cadastrada."
                );
            }

            // Verificar e-mail duplicado
            var usuarioExistente =
                await _userManager.FindByEmailAsync(model.Email);

            if (usuarioExistente != null)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "Esse e-mail já está cadastrado."
                );
            }

            if (!ModelState.IsValid)
                return View(model);

            await using var transacao =
                await _context.Database.BeginTransactionAsync();

            // Criar conta de acesso
            var usuario = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = false
            };

            var resultadoUsuario = await _userManager.CreateAsync(
                usuario,
                model.Senha
            );

            if (!resultadoUsuario.Succeeded)
            {
                await transacao.RollbackAsync();

                foreach (var erro in resultadoUsuario.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        erro.Description
                    );
                }

                return View(model);
            }

            // Atribuir função de funcionário
            var resultadoRole = await _userManager.AddToRoleAsync(
                usuario,
                "Funcionario"
            );

            if (!resultadoRole.Succeeded)
            {
                await transacao.RollbackAsync();

                foreach (var erro in resultadoRole.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        erro.Description
                    );
                }

                return View(model);
            }

            // Criar perfil profissional e vínculos com filiais
            var funcionario = new Funcionario
            {
                UsuarioId = usuario.Id,
                NomeCompleto = model.NomeCompleto.Trim(),
                Matricula = model.Matricula.Trim(),
                Ativo = true,

                Filiais = model.Filiais
                    .Where(f => f.Selecionada)
                    .Select(f => new FuncionarioFilial
                    {
                        FilialId = f.FilialId,
                        PodeAprovarPedidos = f.PodeAprovarPedidos,
                        PodeCadastrarFuncionarios =
                            f.PodeCadastrarFuncionarios,
                        PodeControlarFuncionamento =
                            f.PodeControlarFuncionamento,
                        Ativo = true
                    })
                    .ToList()
            };

            _context.Funcionarios.Add(funcionario);

            await _context.SaveChangesAsync();

            await transacao.CommitAsync();

            TempData["Sucesso"] =
                "Funcionário cadastrado com sucesso!";

            // A listagem geral continua exclusiva do administrador.
            if (administrador)
                return RedirectToAction(nameof(Index));

            return RedirectToAction("Index", "AreaFuncionario");
        }

        // Administrador pode escolher qualquer filial ativa.
        // Funcionário só pode cadastrar em filiais onde recebeu essa permissão.
        private async Task<List<Filial>> ObterFiliaisPermitidasParaCadastroAsync()
        {
            if (User.IsInRole("Administrador"))
            {
                return await _context.Filiais
                    .Where(f => f.Ativo)
                    .AsNoTracking()
                    .OrderBy(f => f.Nome)
                    .ToListAsync();
            }

            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(usuarioId))
                return new List<Filial>();

            return await _context.FuncionariosFiliais
                .Where(ff =>
                    ff.Funcionario.UsuarioId == usuarioId &&
                    ff.Funcionario.Ativo &&
                    ff.Ativo &&
                    ff.PodeCadastrarFuncionarios &&
                    ff.Filial.Ativo)
                .Select(ff => ff.Filial)
                .AsNoTracking()
                .OrderBy(f => f.Nome)
                .ToListAsync();
        }

        [Authorize(Roles = "Administrador")]
        [HttpGet]
        public async Task<IActionResult> Edit(long id)
        {
            var funcionario = await _context.Funcionarios
                .Include(f => f.Usuario)
                .Include(f => f.Filiais)
                    .ThenInclude(ff => ff.Filial)
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.FuncionarioId == id);

            if (funcionario == null)
                return NotFound();

            var model = new EditarFuncionarioViewModel
            {
                FuncionarioId = funcionario.FuncionarioId,
                NomeCompleto = funcionario.NomeCompleto,
                Matricula = funcionario.Matricula,
                Email = funcionario.Usuario.Email ?? string.Empty,
                Ativo = funcionario.Ativo,

                Filiais = funcionario.Filiais
                    .Where(ff => ff.Ativo && ff.Filial.Ativo)
                    .Select(ff => new FuncionarioFilialViewModel
                    {
                        FilialId = ff.FilialId,
                        NomeFilial = ff.Filial.Nome,
                        Selecionada = true,
                        PodeAprovarPedidos = ff.PodeAprovarPedidos,
                        PodeCadastrarFuncionarios =
                            ff.PodeCadastrarFuncionarios,
                        PodeControlarFuncionamento =
                            ff.PodeControlarFuncionamento
                    })
                    .ToList()
            };

            await PrepararFiliais(model);

            return View(model);
        }

        [Authorize(Roles = "Administrador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            EditarFuncionarioViewModel model)
        {
            // Recarregar filiais válidas e preservar as seleções enviadas.
            await PrepararFiliais(model);

            if (!model.Filiais.Any(f => f.Selecionada))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Selecione pelo menos uma filial."
                );
            }

            bool matriculaExiste = await _context.Funcionarios
                .AnyAsync(f =>
                    f.FuncionarioId != model.FuncionarioId &&
                    f.Matricula == model.Matricula.Trim());

            if (matriculaExiste)
            {
                ModelState.AddModelError(
                    nameof(model.Matricula),
                    "Essa matrícula já está cadastrada."
                );
            }

            if (!ModelState.IsValid)
                return View(model);

            var funcionario = await _context.Funcionarios
                .Include(f => f.Filiais)
                .FirstOrDefaultAsync(f =>
                    f.FuncionarioId == model.FuncionarioId);

            if (funcionario == null)
                return NotFound();

            bool estavaAtivo = funcionario.Ativo;

            funcionario.NomeCompleto = model.NomeCompleto.Trim();
            funcionario.Matricula = model.Matricula.Trim();
            funcionario.Ativo = model.Ativo;

            foreach (var filialModel in model.Filiais)
            {
                var vinculo = funcionario.Filiais
                    .FirstOrDefault(ff =>
                        ff.FilialId == filialModel.FilialId);

                if (filialModel.Selecionada)
                {
                    if (vinculo == null)
                    {
                        funcionario.Filiais.Add(
                            new FuncionarioFilial
                            {
                                FuncionarioId = funcionario.FuncionarioId,
                                FilialId = filialModel.FilialId,
                                PodeAprovarPedidos =
                                    filialModel.PodeAprovarPedidos,
                                PodeCadastrarFuncionarios =
                                    filialModel.PodeCadastrarFuncionarios,
                                PodeControlarFuncionamento =
                                    filialModel.PodeControlarFuncionamento,
                                Ativo = true
                            }
                        );
                    }
                    else
                    {
                        vinculo.Ativo = true;
                        vinculo.PodeAprovarPedidos =
                            filialModel.PodeAprovarPedidos;
                        vinculo.PodeCadastrarFuncionarios =
                            filialModel.PodeCadastrarFuncionarios;
                        vinculo.PodeControlarFuncionamento =
                            filialModel.PodeControlarFuncionamento;
                    }
                }
                else if (vinculo != null)
                {
                    vinculo.Ativo = false;
                }
            }

            await using var transacao =
    await _context.Database.BeginTransactionAsync();

            await _context.SaveChangesAsync();

            // Se o funcionário foi desativado, invalidar suas sessões.
            if (estavaAtivo && !funcionario.Ativo)
            {
                var usuario = await _userManager.FindByIdAsync(
                    funcionario.UsuarioId
                );

                if (usuario != null)
                {
                    var resultado = await _userManager.UpdateSecurityStampAsync(
                        usuario
                    );

                    if (!resultado.Succeeded)
                    {
                        await transacao.RollbackAsync();

                        foreach (var erro in resultado.Errors)
                        {
                            ModelState.AddModelError(
                                string.Empty,
                                erro.Description
                            );
                        }

                        return View(model);
                    }
                }
            }

            await transacao.CommitAsync();

            TempData["Sucesso"] =
                "Funcionário atualizado com sucesso!";

            return RedirectToAction(nameof(Index));
        }


        private async Task PrepararFiliais(
            EditarFuncionarioViewModel model)
        {
            var dadosEnviados = model.Filiais
                .GroupBy(f => f.FilialId)
                .ToDictionary(g => g.Key, g => g.First());

            var filiais = await _context.Filiais
                .Where(f => f.Ativo)
                .AsNoTracking()
                .OrderBy(f => f.Nome)
                .ToListAsync();

            model.Filiais = filiais.Select(f =>
            {
                dadosEnviados.TryGetValue(
                    f.FilialId,
                    out var enviado
                );

                return new FuncionarioFilialViewModel
                {
                    FilialId = f.FilialId,
                    NomeFilial = f.Nome,
                    Selecionada = enviado?.Selecionada ?? false,
                    PodeAprovarPedidos =
                        enviado?.PodeAprovarPedidos ?? false,
                    PodeCadastrarFuncionarios =
                        enviado?.PodeCadastrarFuncionarios ?? false,
                    PodeControlarFuncionamento =
                        enviado?.PodeControlarFuncionamento ?? false
                };
            }).ToList();
        }



    }




}
