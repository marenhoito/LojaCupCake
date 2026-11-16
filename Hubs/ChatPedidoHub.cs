using System.Security.Claims;
using LojaCupCake.Data;
using LojaCupCake.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace LojaCupCake.Hubs
{
    [Authorize(Roles = "Cliente,Funcionario,Administrador")]
    public class ChatPedidoHub : Hub
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ChatPedidoHub(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task EntrarNoPedido(long pedidoId)
        {
            var usuarioId = ObterUsuarioId();
            if (usuarioId == null || !await PodeAcessarPedidoAsync(pedidoId, usuarioId))
                throw new HubException("Você não tem acesso à conversa deste pedido.");

            await MarcarMensagensComoLidasAsync(pedidoId, usuarioId);

            var historico = await _context.ChatMensagens
                .AsNoTracking()
                .Where(m => m.PedidoId == pedidoId)
                .OrderByDescending(m => m.EnviadaEm)
                .ThenByDescending(m => m.ChatMensagemId)
                .Take(200)
                .OrderBy(m => m.EnviadaEm)
                .ThenBy(m => m.ChatMensagemId)
                .Select(m => new
                {
                    mensagemId = m.ChatMensagemId,
                    pedidoId = m.PedidoId,
                    remetenteUsuarioId = m.RemetenteUsuarioId,
                    remetenteNome = m.RemetenteNome,
                    conteudo = m.Conteudo,
                    enviadaEm = m.EnviadaEm
                })
                .ToListAsync();

            await Clients.Caller.SendAsync("HistoricoCarregado", historico);
            await NotificarLeituraAsync(pedidoId);
        }

        public async Task MarcarConversaComoLida(long pedidoId)
        {
            var usuarioId = ObterUsuarioId();
            if (usuarioId == null || !await PodeAcessarPedidoAsync(pedidoId, usuarioId))
                throw new HubException("Você não tem acesso à conversa deste pedido.");

            await MarcarMensagensComoLidasAsync(pedidoId, usuarioId);
            await NotificarLeituraAsync(pedidoId);
        }

        public async Task EnviarMensagem(long pedidoId, string? conteudo)
        {
            var texto = (conteudo ?? string.Empty).Trim();
            if (texto.Length == 0)
                throw new HubException("Digite uma mensagem antes de enviar.");
            if (texto.Length > 2000)
                throw new HubException("A mensagem deve ter no máximo 2.000 caracteres.");

            var usuarioId = ObterUsuarioId();
            if (usuarioId == null || !await PodeAcessarPedidoAsync(pedidoId, usuarioId))
                throw new HubException("Você não tem acesso à conversa deste pedido.");

            var nome = await ObterNomeRemetenteAsync(usuarioId);
            var mensagem = new ChatMensagem
            {
                PedidoId = pedidoId,
                RemetenteUsuarioId = usuarioId,
                RemetenteNome = nome,
                Conteudo = texto,
                EnviadaEm = DateTime.UtcNow
            };

            _context.ChatMensagens.Add(mensagem);
            await _context.SaveChangesAsync();

            var resposta = new
            {
                mensagemId = mensagem.ChatMensagemId,
                pedidoId = mensagem.PedidoId,
                remetenteUsuarioId = mensagem.RemetenteUsuarioId,
                remetenteNome = mensagem.RemetenteNome,
                conteudo = mensagem.Conteudo,
                enviadaEm = mensagem.EnviadaEm
            };

            // Recalcula os destinatários autorizados no momento do envio.
            // Assim, uma conexão antiga não continua recebendo mensagens
            // depois que o vínculo ou a permissão do funcionário é revogado.
            var destinatarios = await ObterUsuariosAutorizadosAsync(pedidoId);
            if (!destinatarios.Contains(usuarioId, StringComparer.Ordinal))
                destinatarios.Add(usuarioId);

            if (destinatarios.Count > 0)
                await Clients.Users(destinatarios).SendAsync("NovaMensagem", resposta);
        }

        private async Task MarcarMensagensComoLidasAsync(long pedidoId, string usuarioId)
        {
            DateTime agora = DateTime.UtcNow;
            bool ladoCliente = Context.User?.IsInRole("Cliente") == true &&
                               Context.User?.IsInRole("Administrador") != true;

            IQueryable<ChatMensagem> consulta = _context.ChatMensagens
                .Where(m => m.PedidoId == pedidoId);

            if (ladoCliente)
            {
                consulta = consulta.Where(m =>
                    m.RemetenteUsuarioId != usuarioId &&
                    m.LidaPeloClienteEm == null);

                var mensagens = await consulta.ToListAsync();
                foreach (var mensagem in mensagens)
                    mensagem.LidaPeloClienteEm = agora;
            }
            else
            {
                var usuarioClienteId = await _context.Pedidos
                    .Where(p => p.PedidoId == pedidoId)
                    .Select(p => p.Cliente.UsuarioId)
                    .FirstOrDefaultAsync();

                if (string.IsNullOrWhiteSpace(usuarioClienteId))
                    return;

                consulta = consulta.Where(m =>
                    m.RemetenteUsuarioId == usuarioClienteId &&
                    m.LidaPelaLojaEm == null);

                var mensagens = await consulta.ToListAsync();
                foreach (var mensagem in mensagens)
                    mensagem.LidaPelaLojaEm = agora;
            }

            await _context.SaveChangesAsync();
        }

        private async Task NotificarLeituraAsync(long pedidoId)
        {
            var destinatarios = await ObterUsuariosAutorizadosAsync(pedidoId);
            if (destinatarios.Count > 0)
                await Clients.Users(destinatarios).SendAsync("MensagensLidas", pedidoId);
        }

        private async Task<List<string>> ObterUsuariosAutorizadosAsync(long pedidoId)
        {
            var pedido = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.PedidoId == pedidoId)
                .Select(p => new
                {
                    ClienteUsuarioId = p.Cliente.UsuarioId,
                    ClienteAtivo = p.Cliente.Ativo,
                    p.FilialId
                })
                .FirstOrDefaultAsync();

            if (pedido == null)
                return new List<string>();

            var destinatarios = new List<string>();

            // Além do cadastro ativo, confirme que a conta ainda possui o papel
            // correspondente no Identity, inclusive para conexões já abertas.
            var usuariosCliente = await _userManager.GetUsersInRoleAsync("Cliente");
            var idsClientes = usuariosCliente
                .Select(u => u.Id)
                .ToHashSet(StringComparer.Ordinal);

            if (pedido.ClienteAtivo &&
                !string.IsNullOrWhiteSpace(pedido.ClienteUsuarioId) &&
                idsClientes.Contains(pedido.ClienteUsuarioId))
            {
                destinatarios.Add(pedido.ClienteUsuarioId);
            }

            // Vínculos de funcionários continuam válidos para consulta histórica,
            // inclusive quando a filial estiver inativa.
            var usuariosFuncionario = await _userManager.GetUsersInRoleAsync("Funcionario");
            var idsFuncionariosNoPapel = usuariosFuncionario
                .Select(u => u.Id)
                .ToHashSet(StringComparer.Ordinal);

            var funcionariosIds = await _context.FuncionariosFiliais
                .Where(ff =>
                    ff.FilialId == pedido.FilialId &&
                    ff.Funcionario.Ativo &&
                    ff.Ativo &&
                    ff.PodeAprovarPedidos)
                .Select(ff => ff.Funcionario.UsuarioId)
                .ToListAsync();

            destinatarios.AddRange(funcionariosIds.Where(id =>
                !string.IsNullOrWhiteSpace(id) && idsFuncionariosNoPapel.Contains(id)));

            var administradores = await _userManager.GetUsersInRoleAsync("Administrador");
            destinatarios.AddRange(administradores.Select(u => u.Id));

            return destinatarios
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        private string? ObterUsuarioId()
            => Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        private async Task<bool> PodeAcessarPedidoAsync(long pedidoId, string usuarioId)
        {
            var pedido = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.PedidoId == pedidoId)
                .Select(p => new { p.PedidoId, p.ClienteId, p.FilialId })
                .FirstOrDefaultAsync();

            if (pedido == null)
                return false;

            if (Context.User?.IsInRole("Administrador") == true)
                return true;

            if (Context.User?.IsInRole("Cliente") == true)
            {
                return await _context.Clientes.AnyAsync(c =>
                    c.ClienteId == pedido.ClienteId &&
                    c.UsuarioId == usuarioId &&
                    c.Ativo);
            }

            if (Context.User?.IsInRole("Funcionario") == true)
            {
                return await _context.FuncionariosFiliais.AnyAsync(ff =>
                    ff.Funcionario.UsuarioId == usuarioId &&
                    ff.Funcionario.Ativo &&
                    ff.Ativo &&
                    ff.PodeAprovarPedidos &&
                    ff.FilialId == pedido.FilialId);
            }

            return false;
        }

        private async Task<string> ObterNomeRemetenteAsync(string usuarioId)
        {
            if (Context.User?.IsInRole("Cliente") == true &&
                Context.User?.IsInRole("Administrador") != true)
            {
                var nomeCliente = await _context.Clientes
                    .Where(c => c.UsuarioId == usuarioId)
                    .Select(c => c.NomeCompleto)
                    .FirstOrDefaultAsync();
                if (!string.IsNullOrWhiteSpace(nomeCliente))
                    return nomeCliente;
            }

            if (Context.User?.IsInRole("Funcionario") == true &&
                Context.User?.IsInRole("Administrador") != true)
            {
                var nomeFuncionario = await _context.Funcionarios
                    .Where(f => f.UsuarioId == usuarioId)
                    .Select(f => f.NomeCompleto)
                    .FirstOrDefaultAsync();
                if (!string.IsNullOrWhiteSpace(nomeFuncionario))
                    return nomeFuncionario;
            }

            return "Administrador";
        }
    }
}
