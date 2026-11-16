using LojaCupCake.Data;
using LojaCupCake.ViewModels.ProdutoImagens;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LojaCupCake.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class ProdutoImagensController : Controller
    {
        private const long TamanhoMaximoArquivo = 5 * 1024 * 1024; // 5 MB
        private static readonly HashSet<string> ExtensoesPermitidas = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ProdutoImagensController> _logger;

        public ProdutoImagensController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            ILogger<ProdutoImagensController> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var produtos = await _context.Produtos
                .AsNoTracking()
                .OrderBy(produto => produto.Nome)
                .Select(produto => new ProdutoImagemItemViewModel
                {
                    ProdutoId = produto.ProdutoId,
                    Codigo = produto.Codigo,
                    Nome = produto.Nome,
                    ImagemUrl = produto.ImagemUrl,
                    Ativo = produto.Ativo
                })
                .ToListAsync();

            return View(new ProdutoImagemIndexViewModel { Produtos = produtos });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(TamanhoMaximoArquivo + 256 * 1024)]
        public async Task<IActionResult> Upload(long produtoId, IFormFile? imagem)
        {
            var produto = await _context.Produtos
                .FirstOrDefaultAsync(item => item.ProdutoId == produtoId);

            if (produto == null)
                return NotFound();

            if (imagem == null || imagem.Length == 0)
            {
                TempData["Erro"] = "Selecione uma imagem antes de enviar.";
                return RedirectToAction(nameof(Index));
            }

            if (imagem.Length > TamanhoMaximoArquivo)
            {
                TempData["Erro"] = "A imagem deve ter no máximo 5 MB.";
                return RedirectToAction(nameof(Index));
            }

            var extensao = Path.GetExtension(imagem.FileName);
            if (string.IsNullOrWhiteSpace(extensao) || !ExtensoesPermitidas.Contains(extensao))
            {
                TempData["Erro"] = "Formato não permitido. Envie JPG, PNG ou WEBP.";
                return RedirectToAction(nameof(Index));
            }

            if (!await AssinaturaValidaAsync(imagem, extensao))
            {
                TempData["Erro"] = "O conteúdo do arquivo não corresponde a uma imagem JPG, PNG ou WEBP válida.";
                return RedirectToAction(nameof(Index));
            }

            var pasta = ObterPastaImagens();
            Directory.CreateDirectory(pasta);

            var nomeArquivo = $"{Guid.NewGuid():N}{extensao.ToLowerInvariant()}";
            var caminhoCompleto = Path.Combine(pasta, nomeArquivo);
            var imagemAnterior = produto.ImagemUrl;

            try
            {
                await using (var stream = new FileStream(
                    caminhoCompleto,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 81920,
                    useAsync: true))
                {
                    await imagem.CopyToAsync(stream);
                }

                produto.ImagemUrl = $"/uploads/produtos/{nomeArquivo}";
                await _context.SaveChangesAsync();

                ExcluirImagemLocalAnterior(imagemAnterior);
                TempData["Sucesso"] = $"Imagem de {produto.Nome} atualizada com sucesso.";
            }
            catch (Exception ex)
            {
                ExcluirArquivo(caminhoCompleto);
                _logger.LogError(ex, "Falha ao salvar imagem do produto {ProdutoId}.", produtoId);
                TempData["Erro"] = "Não foi possível salvar a imagem. Tente novamente.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remover(long produtoId)
        {
            var produto = await _context.Produtos
                .FirstOrDefaultAsync(item => item.ProdutoId == produtoId);

            if (produto == null)
                return NotFound();

            var imagemAnterior = produto.ImagemUrl;
            produto.ImagemUrl = null;

            try
            {
                await _context.SaveChangesAsync();
                ExcluirImagemLocalAnterior(imagemAnterior);
                TempData["Sucesso"] = $"Imagem de {produto.Nome} removida.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao remover imagem do produto {ProdutoId}.", produtoId);
                TempData["Erro"] = "Não foi possível remover a imagem.";
            }

            return RedirectToAction(nameof(Index));
        }

        private string ObterPastaImagens()
        {
            var webRoot = _environment.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRoot))
                webRoot = Path.Combine(_environment.ContentRootPath, "wwwroot");

            return Path.Combine(webRoot, "uploads", "produtos");
        }

        private void ExcluirImagemLocalAnterior(string? url)
        {
            const string prefixo = "/uploads/produtos/";
            if (string.IsNullOrWhiteSpace(url) || !url.StartsWith(prefixo, StringComparison.OrdinalIgnoreCase))
                return;

            var nomeArquivo = Path.GetFileName(url);
            if (string.IsNullOrWhiteSpace(nomeArquivo))
                return;

            var caminho = Path.Combine(ObterPastaImagens(), nomeArquivo);
            ExcluirArquivo(caminho);
        }

        private static void ExcluirArquivo(string caminho)
        {
            try
            {
                if (System.IO.File.Exists(caminho))
                    System.IO.File.Delete(caminho);
            }
            catch
            {
                // A remoção do arquivo antigo não deve derrubar a operação principal.
            }
        }

        private static async Task<bool> AssinaturaValidaAsync(IFormFile arquivo, string extensao)
        {
            var buffer = new byte[12];
            await using var stream = arquivo.OpenReadStream();
            var lidos = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length));

            if (extensao.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                extensao.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
            {
                return lidos >= 3 && buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF;
            }

            if (extensao.Equals(".png", StringComparison.OrdinalIgnoreCase))
            {
                byte[] assinaturaPng = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
                return lidos >= assinaturaPng.Length && buffer.Take(assinaturaPng.Length).SequenceEqual(assinaturaPng);
            }

            if (extensao.Equals(".webp", StringComparison.OrdinalIgnoreCase))
            {
                return lidos >= 12 &&
                    buffer[0] == (byte)'R' && buffer[1] == (byte)'I' &&
                    buffer[2] == (byte)'F' && buffer[3] == (byte)'F' &&
                    buffer[8] == (byte)'W' && buffer[9] == (byte)'E' &&
                    buffer[10] == (byte)'B' && buffer[11] == (byte)'P';
            }

            return false;
        }
    }
}
