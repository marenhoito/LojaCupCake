using LojaCupCake.Data;
using LojaCupCake.Models;
using LojaCupCake.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LojaCupCake.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class ProdutosController : Controller
    {
        private const long TamanhoMaximoImagem = 5 * 1024 * 1024;
        private const string PastaPublicaImagens = "/uploads/produtos/";

        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ProdutosController(
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // LISTAGEM
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var produtos = await _context.Produtos
                .AsNoTracking()
                .OrderBy(p => p.Nome)
                .Select(p => new ProdutoViewModel
                {
                    ProdutoId = p.ProdutoId,
                    Codigo = p.Codigo,
                    Nome = p.Nome,
                    Descricao = p.Descricao,
                    PrecoAtual = p.PrecoAtual,
                    ImagemUrl = p.ImagemUrl,
                    Ativo = p.Ativo,
                    CategoriaId = p.CategoriaId,
                    NomeCategoria = p.Categoria.Nome
                })
                .ToListAsync();

            return View(produtos);
        }

        // CADASTRO
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new ProdutoViewModel();
            await CarregarCategorias(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProdutoViewModel model)
        {
            await CarregarCategorias(model);

            bool categoriaValida = await _context.Categorias
                .AnyAsync(c => c.CategoriaId == model.CategoriaId && c.Ativa);

            if (!categoriaValida)
            {
                ModelState.AddModelError(
                    nameof(model.CategoriaId),
                    "Selecione uma categoria ativa.");
            }

            string codigo = (model.Codigo ?? string.Empty).Trim();

            bool codigoExiste = await _context.Produtos
                .AnyAsync(p => p.Codigo == codigo);

            if (codigoExiste)
            {
                ModelState.AddModelError(
                    nameof(model.Codigo),
                    "Já existe um produto com esse código.");
            }

            string? extensaoImagem = await ValidarArquivoImagemAsync(
                model.ArquivoImagem,
                nameof(model.ArquivoImagem));

            if (!ModelState.IsValid)
                return View(model);

            string? novaImagemUrl = null;

            try
            {
                if (model.ArquivoImagem is { Length: > 0 } && extensaoImagem != null)
                    novaImagemUrl = await SalvarImagemAsync(model.ArquivoImagem, extensaoImagem);

                var produto = new Produto
                {
                    CategoriaId = model.CategoriaId,
                    Codigo = codigo,
                    Nome = model.Nome.Trim(),
                    Descricao = model.Descricao?.Trim(),
                    PrecoAtual = model.PrecoAtual,
                    ImagemUrl = novaImagemUrl ?? NormalizarUrlImagem(model.ImagemUrl),
                    Ativo = true,
                    DataCadastro = DateTime.UtcNow
                };

                _context.Produtos.Add(produto);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ExcluirImagemLocal(novaImagemUrl);

                ModelState.AddModelError(
                    string.Empty,
                    "Não foi possível cadastrar o produto. Verifique se o código já está em uso.");

                return View(model);
            }
            catch (IOException)
            {
                ExcluirImagemLocal(novaImagemUrl);
                ModelState.AddModelError(
                    nameof(model.ArquivoImagem),
                    "Não foi possível salvar a imagem. Tente selecionar outro arquivo.");
                return View(model);
            }
            catch (UnauthorizedAccessException)
            {
                ExcluirImagemLocal(novaImagemUrl);
                ModelState.AddModelError(
                    nameof(model.ArquivoImagem),
                    "O servidor não tem permissão para salvar imagens na pasta wwwroot/uploads/produtos.");
                return View(model);
            }

            TempData["Sucesso"] = "Produto cadastrado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        // EDIÇÃO
        [HttpGet]
        public async Task<IActionResult> Edit(long id)
        {
            var produto = await _context.Produtos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProdutoId == id);

            if (produto == null)
                return NotFound();

            var model = new ProdutoViewModel
            {
                ProdutoId = produto.ProdutoId,
                CategoriaId = produto.CategoriaId,
                Codigo = produto.Codigo,
                Nome = produto.Nome,
                Descricao = produto.Descricao,
                PrecoAtual = produto.PrecoAtual,
                ImagemUrl = produto.ImagemUrl,
                Ativo = produto.Ativo
            };

            await CarregarCategorias(model, produto.CategoriaId);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProdutoViewModel model)
        {
            var produto = await _context.Produtos
                .FirstOrDefaultAsync(p => p.ProdutoId == model.ProdutoId);

            if (produto == null)
                return NotFound();

            await CarregarCategorias(model, produto.CategoriaId);

            string codigo = (model.Codigo ?? string.Empty).Trim();

            bool codigoExiste = await _context.Produtos
                .AnyAsync(p => p.ProdutoId != model.ProdutoId && p.Codigo == codigo);

            if (codigoExiste)
            {
                ModelState.AddModelError(
                    nameof(model.Codigo),
                    "Já existe outro produto com esse código.");
            }

            // Permite manter uma categoria que foi inativada,
            // mas não permite atribuir uma nova categoria inativa.
            bool categoriaValida = await _context.Categorias
                .AnyAsync(c =>
                    c.CategoriaId == model.CategoriaId &&
                    (c.Ativa || c.CategoriaId == produto.CategoriaId));

            if (!categoriaValida)
            {
                ModelState.AddModelError(
                    nameof(model.CategoriaId),
                    "Selecione uma categoria ativa.");
            }

            string? extensaoImagem = await ValidarArquivoImagemAsync(
                model.ArquivoImagem,
                nameof(model.ArquivoImagem));

            if (!ModelState.IsValid)
            {
                // A imagem atual continua sendo exibida ao retornar com erros.
                model.ImagemUrl = produto.ImagemUrl;
                return View(model);
            }

            string? imagemAntiga = produto.ImagemUrl;
            string? novaImagemUrl = null;

            try
            {
                if (model.ArquivoImagem is { Length: > 0 } && extensaoImagem != null)
                {
                    novaImagemUrl = await SalvarImagemAsync(
                        model.ArquivoImagem,
                        extensaoImagem);
                }

                produto.CategoriaId = model.CategoriaId;
                produto.Codigo = codigo;
                produto.Nome = model.Nome.Trim();
                produto.Descricao = model.Descricao?.Trim();
                produto.PrecoAtual = model.PrecoAtual;
                produto.Ativo = model.Ativo;

                if (novaImagemUrl != null)
                {
                    // O novo upload tem prioridade sobre a opção de remover.
                    produto.ImagemUrl = novaImagemUrl;
                }
                else if (model.RemoverImagem)
                {
                    produto.ImagemUrl = null;
                }
                else
                {
                    // Mantém a URL atual ou permite substituir por uma URL externa.
                    produto.ImagemUrl = NormalizarUrlImagem(model.ImagemUrl);
                }

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ExcluirImagemLocal(novaImagemUrl);

                ModelState.AddModelError(
                    string.Empty,
                    "Não foi possível atualizar o produto.");

                model.ImagemUrl = imagemAntiga;
                return View(model);
            }
            catch (IOException)
            {
                ExcluirImagemLocal(novaImagemUrl);

                ModelState.AddModelError(
                    nameof(model.ArquivoImagem),
                    "Não foi possível salvar a imagem. Tente selecionar outro arquivo.");

                model.ImagemUrl = imagemAntiga;
                return View(model);
            }
            catch (UnauthorizedAccessException)
            {
                ExcluirImagemLocal(novaImagemUrl);

                ModelState.AddModelError(
                    nameof(model.ArquivoImagem),
                    "O servidor não tem permissão para salvar imagens na pasta wwwroot/uploads/produtos.");

                model.ImagemUrl = imagemAntiga;
                return View(model);
            }

            // Só exclui a imagem anterior depois que o banco foi atualizado.
            if (!string.Equals(imagemAntiga, produto.ImagemUrl, StringComparison.OrdinalIgnoreCase))
                ExcluirImagemLocal(imagemAntiga);

            TempData["Sucesso"] = "Produto atualizado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        // ATIVAR / DESATIVAR
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AlterarStatus(long id)
        {
            var produto = await _context.Produtos
                .FirstOrDefaultAsync(p => p.ProdutoId == id);

            if (produto == null)
                return NotFound();

            produto.Ativo = !produto.Ativo;
            await _context.SaveChangesAsync();

            TempData["Sucesso"] = produto.Ativo
                ? "Produto ativado com sucesso!"
                : "Produto desativado com sucesso!";

            return RedirectToAction(nameof(Index));
        }

        // VALIDA EXTENSÃO, TAMANHO E ASSINATURA REAL DO ARQUIVO.
        private async Task<string?> ValidarArquivoImagemAsync(
            IFormFile? arquivo,
            string campoModelState)
        {
            if (arquivo == null || arquivo.Length == 0)
                return null;

            if (arquivo.Length > TamanhoMaximoImagem)
            {
                ModelState.AddModelError(
                    campoModelState,
                    "A imagem deve ter no máximo 5 MB.");
                return null;
            }

            string extensao = Path.GetExtension(arquivo.FileName).ToLowerInvariant();

            if (extensao is not (".jpg" or ".jpeg" or ".png" or ".webp"))
            {
                ModelState.AddModelError(
                    campoModelState,
                    "Formato inválido. Envie uma imagem JPG, PNG ou WEBP.");
                return null;
            }

            byte[] cabecalho = new byte[12];
            int totalLido = 0;

            await using (var stream = arquivo.OpenReadStream())
            {
                while (totalLido < cabecalho.Length)
                {
                    int lidos = await stream.ReadAsync(
                        cabecalho.AsMemory(totalLido, cabecalho.Length - totalLido));

                    if (lidos == 0)
                        break;

                    totalLido += lidos;
                }
            }

            bool jpegValido =
                (extensao is ".jpg" or ".jpeg") &&
                totalLido >= 3 &&
                cabecalho[0] == 0xFF &&
                cabecalho[1] == 0xD8 &&
                cabecalho[2] == 0xFF;

            bool pngValido =
                extensao == ".png" &&
                totalLido >= 8 &&
                cabecalho[0] == 0x89 &&
                cabecalho[1] == 0x50 &&
                cabecalho[2] == 0x4E &&
                cabecalho[3] == 0x47 &&
                cabecalho[4] == 0x0D &&
                cabecalho[5] == 0x0A &&
                cabecalho[6] == 0x1A &&
                cabecalho[7] == 0x0A;

            bool webpValido =
                extensao == ".webp" &&
                totalLido >= 12 &&
                cabecalho[0] == (byte)'R' &&
                cabecalho[1] == (byte)'I' &&
                cabecalho[2] == (byte)'F' &&
                cabecalho[3] == (byte)'F' &&
                cabecalho[8] == (byte)'W' &&
                cabecalho[9] == (byte)'E' &&
                cabecalho[10] == (byte)'B' &&
                cabecalho[11] == (byte)'P';

            if (!jpegValido && !pngValido && !webpValido)
            {
                ModelState.AddModelError(
                    campoModelState,
                    "O conteúdo do arquivo não corresponde a uma imagem JPG, PNG ou WEBP válida.");
                return null;
            }

            return extensao;
        }

        private async Task<string> SalvarImagemAsync(
            IFormFile arquivo,
            string extensao)
        {
            string pasta = ObterPastaImagens();
            Directory.CreateDirectory(pasta);

            string nomeArquivo = $"{Guid.NewGuid():N}{extensao}";
            string caminhoCompleto = Path.Combine(pasta, nomeArquivo);

            try
            {
                await using var destino = new FileStream(
                    caminhoCompleto,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 81920,
                    useAsync: true);

                await arquivo.CopyToAsync(destino);
            }
            catch
            {
                try
                {
                    if (System.IO.File.Exists(caminhoCompleto))
                        System.IO.File.Delete(caminhoCompleto);
                }
                catch
                {
                    // Preserva a exceção original do upload.
                }

                throw;
            }

            return $"{PastaPublicaImagens}{nomeArquivo}";
        }

        private string ObterPastaImagens()
        {
            string webRoot = _environment.WebRootPath;

            if (string.IsNullOrWhiteSpace(webRoot))
                webRoot = Path.Combine(_environment.ContentRootPath, "wwwroot");

            return Path.Combine(webRoot, "uploads", "produtos");
        }

        private void ExcluirImagemLocal(string? imagemUrl)
        {
            if (string.IsNullOrWhiteSpace(imagemUrl) ||
                !imagemUrl.StartsWith(PastaPublicaImagens, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string nomeArquivo = imagemUrl[PastaPublicaImagens.Length..];

            // Só remove nomes simples de arquivos gerados pelo próprio sistema.
            if (string.IsNullOrWhiteSpace(nomeArquivo) ||
                nomeArquivo != Path.GetFileName(nomeArquivo))
            {
                return;
            }

            string caminho = Path.Combine(ObterPastaImagens(), nomeArquivo);

            try
            {
                if (System.IO.File.Exists(caminho))
                    System.IO.File.Delete(caminho);
            }
            catch
            {
                // A falha ao remover um arquivo antigo não deve desfazer a edição do produto.
            }
        }

        private static string? NormalizarUrlImagem(string? imagemUrl)
        {
            return string.IsNullOrWhiteSpace(imagemUrl)
                ? null
                : imagemUrl.Trim();
        }

        // CARREGAR CATEGORIAS DISPONÍVEIS
        private async Task CarregarCategorias(
            ProdutoViewModel model,
            long? categoriaAtualId = null)
        {
            var categorias = await _context.Categorias
                .Where(c =>
                    c.Ativa ||
                    (categoriaAtualId.HasValue &&
                     c.CategoriaId == categoriaAtualId.Value))
                .AsNoTracking()
                .OrderBy(c => c.Nome)
                .ToListAsync();

            model.Categorias = categorias.Select(c =>
                new SelectListItem
                {
                    Value = c.CategoriaId.ToString(),
                    Text = c.Ativa
                        ? c.Nome
                        : $"{c.Nome} (Inativa)"
                }).ToList();
        }
    }
}
