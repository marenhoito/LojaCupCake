
using LojaCupCake.Data;
using LojaCupCake.Services;
using LojaCupCake.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace LojaCupCake.Controllers
{
    [Authorize(Roles = "Funcionario")]
    public class AreaFuncionarioController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Models.ApplicationUser> _userManager;
        private readonly AutorizacaoFuncionarioService _autorizacao;


        public AreaFuncionarioController(
        ApplicationDbContext context,
         UserManager<Models.ApplicationUser> userManager,
        AutorizacaoFuncionarioService autorizacao)
        {
            _context = context;
            _userManager = userManager;
            _autorizacao = autorizacao;
        }

        [HttpGet]
        public async Task<IActionResult> Index(long? filialId)
        {
            var usuarioId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(usuarioId))
                return RedirectToAction("Login", "Conta");

            var funcionario = await _context.Funcionarios
                .Include(f => f.Filiais)
                    .ThenInclude(ff => ff.Filial)
                .AsNoTracking()
                .FirstOrDefaultAsync(f =>
                    f.UsuarioId == usuarioId &&
                    f.Ativo
                );

            if (funcionario == null)
                return RedirectToAction("AcessoNegado", "Conta");

            var vinculosAtivos = funcionario.Filiais
                .Where(ff => ff.Ativo && ff.Filial.Ativo)
                .ToList();

            var model = new PainelFuncionarioViewModel
            {
                NomeCompleto = funcionario.NomeCompleto,

                Filiais = vinculosAtivos
                    .OrderBy(ff => ff.Filial.Nome)
                    .Select(ff => new FuncionarioFilialViewModel
                    {
                        FilialId = ff.FilialId,
                        NomeFilial = ff.Filial.Nome,
                        PodeAprovarPedidos = ff.PodeAprovarPedidos,
                        PodeCadastrarFuncionarios =
                            ff.PodeCadastrarFuncionarios
                    })
                    .ToList()
            };

            // Validar a filial selecionada
            if (filialId.HasValue)
            {
                bool podeAcessar = await _autorizacao
                    .PodeAcessarFilialAsync(User, filialId.Value);

                if (!podeAcessar)
                    return Forbid();

                var vinculo = vinculosAtivos
                    .First(ff => ff.FilialId == filialId.Value);

                model.FilialAtual = new FuncionarioFilialViewModel
                {
                    FilialId = vinculo.FilialId,
                    NomeFilial = vinculo.Filial.Nome,
                    Selecionada = true,
                    PodeAprovarPedidos = vinculo.PodeAprovarPedidos,
                    PodeCadastrarFuncionarios =
                        vinculo.PodeCadastrarFuncionarios
                };
            }

            return View(model);
        }
    }
}
