
using System.Security.Claims;
using LojaCupCake.Data;
using LojaCupCake.Models;
using Microsoft.EntityFrameworkCore;

namespace LojaCupCake.Services
{
    public class AutorizacaoFuncionarioService
    {
        private readonly ApplicationDbContext _context;

        public AutorizacaoFuncionarioService(
            ApplicationDbContext context)
        {
            _context = context;
        }

        // Recuperar o vínculo ativo do usuário com uma filial
        public async Task<FuncionarioFilial?> ObterVinculoAtivoAsync(
            ClaimsPrincipal usuario,
            long filialId)
        {
            var usuarioId = usuario.FindFirstValue(
                ClaimTypes.NameIdentifier
            );

            if (string.IsNullOrWhiteSpace(usuarioId))
                return null;

            return await _context.FuncionariosFiliais
                .Include(ff => ff.Funcionario)
                .Include(ff => ff.Filial)
                .AsNoTracking()
                .FirstOrDefaultAsync(ff =>
                    ff.Funcionario.UsuarioId == usuarioId &&
                    ff.Funcionario.Ativo &&
                    ff.Ativo &&
                    ff.Filial.Ativo &&
                    ff.FilialId == filialId
                );
        }

        // Verificar se pode acessar determinada filial
        public async Task<bool> PodeAcessarFilialAsync(
            ClaimsPrincipal usuario,
            long filialId)
        {
            if (usuario.IsInRole("Administrador"))
                return true;

            if (!usuario.IsInRole("Funcionario"))
                return false;

            var vinculo = await ObterVinculoAtivoAsync(
                usuario,
                filialId
            );

            return vinculo != null;
        }

        // Verificar permissão para aprovar pedidos
        public async Task<bool> PodeAprovarPedidosAsync(
            ClaimsPrincipal usuario,
            long filialId)
        {
            if (usuario.IsInRole("Administrador"))
                return true;

            if (!usuario.IsInRole("Funcionario"))
                return false;

            var vinculo = await ObterVinculoAtivoAsync(
                usuario,
                filialId
            );

            return vinculo?.PodeAprovarPedidos ?? false;
        }

        // Verificar permissão para cadastrar funcionários
        public async Task<bool> PodeCadastrarFuncionariosAsync(
            ClaimsPrincipal usuario,
            long filialId)
        {
            if (usuario.IsInRole("Administrador"))
                return true;

            if (!usuario.IsInRole("Funcionario"))
                return false;

            var vinculo = await ObterVinculoAtivoAsync(
                usuario,
                filialId
            );

            return vinculo?.PodeCadastrarFuncionarios ?? false;
        }
    }
}
