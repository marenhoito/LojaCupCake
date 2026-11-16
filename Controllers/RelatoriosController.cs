
using System.Globalization;
using ClosedXML.Excel;
using LojaCupCake.Data;
using LojaCupCake.Models;
using LojaCupCake.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LojaCupCake.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class RelatoriosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RelatoriosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // EXPORTAR EXCEL
        [HttpGet]
        public async Task<IActionResult> Excel(
            DateTime? dataInicial,
            DateTime? dataFinal,
            long[]? filialIds,
            long? filialId,
            bool filiaisSelecionadas = false)
        {
            DashboardAdminViewModel model;

            try
            {
                model = await ObterDadosAsync(
                    dataInicial,
                    dataFinal,
                    filialIds,
                    filialId,
                    filiaisSelecionadas
                );
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }

            using var workbook = new XLWorkbook();

            CriarPlanilhaResumo(workbook, model);
            CriarPlanilhaVendas(workbook, model);
            CriarPlanilhaProdutos(workbook, model);
            CriarPlanilhaEstoque(workbook, model);

            using var stream = new MemoryStream();

            workbook.SaveAs(stream);

            string nomeArquivo =
                $"LojaCupCake_Relatorio_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                nomeArquivo
            );
        }

        // EXPORTAR PDF
        [HttpGet]
        public async Task<IActionResult> Pdf(
            DateTime? dataInicial,
            DateTime? dataFinal,
            long[]? filialIds,
            long? filialId,
            bool filiaisSelecionadas = false)
        {
            DashboardAdminViewModel model;

            try
            {
                model = await ObterDadosAsync(
                    dataInicial,
                    dataFinal,
                    filialIds,
                    filialId,
                    filiaisSelecionadas
                );
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }

            string nomeFilial = ObterDescricaoFiliaisSelecionadas(model);

            var cultura =
                CultureInfo.GetCultureInfo("pt-BR");

            var documento = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);

                    page.DefaultTextStyle(style =>
                        style.FontSize(9)
                    );

                    page.Header().Column(header =>
                    {
                        header.Item()
                            .Text("LojaCupCake")
                            .FontSize(20)
                            .Bold()
                            .FontColor(Colors.Pink.Darken2);

                        header.Item()
                            .Text("Relatório gerencial")
                            .FontSize(13)
                            .SemiBold();

                        header.Item()
                            .Text(
                                $"Período: {model.DataInicial:dd/MM/yyyy} " +
                                $"até {model.DataFinal:dd/MM/yyyy}"
                            );

                        header.Item()
                            .Text($"Filial: {nomeFilial}");

                        header.Item()
                            .PaddingBottom(8)
                            .LineHorizontal(1)
                            .LineColor(Colors.Grey.Lighten2);
                    });

                    page.Content().Column(content =>
                    {
                        content.Spacing(15);

                        // INDICADORES
                        content.Item()
                            .Text("Indicadores do período")
                            .FontSize(13)
                            .Bold();

                        content.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                            });

                            table.Header(header =>
                            {
                                header.Cell()
                                    .Element(CabecalhoPdf)
                                    .Text("Indicador").Bold();

                                header.Cell()
                                    .Element(CabecalhoPdf)
                                    .Text("Resultado").Bold();
                            });

                            LinhaPdf(table, "Total de pedidos",
                                model.TotalPedidos.ToString("N0", cultura));

                            LinhaPdf(table, "Aguardando aprovação",
                                model.PedidosPendentes.ToString("N0", cultura));

                            LinhaPdf(table, "Pedidos concluídos",
                                model.PedidosConcluidos.ToString("N0", cultura));

                            LinhaPdf(table, "Faturamento",
                                model.Faturamento.ToString("C", cultura));

                            LinhaPdf(table, "Ticket médio",
                                model.TicketMedio.ToString("C", cultura));

                            LinhaPdf(table, "Produtos com estoque baixo",
                                model.ProdutosEstoqueBaixo.ToString("N0", cultura));
                        });

                        // VENDAS POR DIA
                        content.Item()
                            .Text("Vendas por dia")
                            .FontSize(13)
                            .Bold();

                        if (!model.VendasPorDia.Any())
                        {
                            content.Item().Text(
                                "Não existem vendas concluídas e pagas no período."
                            );
                        }
                        else
                        {
                            content.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(CabecalhoPdf)
                                        .Text("Data").Bold();

                                    header.Cell().Element(CabecalhoPdf)
                                        .Text("Pedidos").Bold();

                                    header.Cell().Element(CabecalhoPdf)
                                        .Text("Total").Bold();
                                });

                                foreach (var venda in model.VendasPorDia)
                                {
                                    LinhaPdf(
                                        table,
                                        venda.Data.ToString("dd/MM/yyyy"),
                                        venda.QuantidadePedidos.ToString("N0", cultura),
                                        venda.TotalVendido.ToString("C", cultura)
                                    );
                                }
                            });
                        }

                        // PRODUTOS MAIS VENDIDOS
                        content.Item()
                            .Text("Produtos mais vendidos")
                            .FontSize(13)
                            .Bold();

                        if (!model.ProdutosMaisVendidos.Any())
                        {
                            content.Item().Text(
                                "Não existem produtos vendidos no período."
                            );
                        }
                        else
                        {
                            content.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(CabecalhoPdf)
                                        .Text("Produto").Bold();

                                    header.Cell().Element(CabecalhoPdf)
                                        .Text("Quantidade").Bold();

                                    header.Cell().Element(CabecalhoPdf)
                                        .Text("Total").Bold();
                                });

                                foreach (var produto in model.ProdutosMaisVendidos)
                                {
                                    LinhaPdf(
                                        table,
                                        produto.NomeProduto,
                                        produto.QuantidadeVendida.ToString("N0", cultura),
                                        produto.TotalVendido.ToString("C", cultura)
                                    );
                                }
                            });
                        }

                        // ESTOQUE BAIXO
                        content.Item()
                            .Text("Alertas de estoque")
                            .FontSize(13)
                            .Bold();

                        if (!model.EstoquesBaixos.Any())
                        {
                            content.Item().Text(
                                "Nenhum produto está abaixo do estoque mínimo."
                            );
                        }
                        else
                        {
                            content.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(CabecalhoPdf)
                                        .Text("Filial").Bold();

                                    header.Cell().Element(CabecalhoPdf)
                                        .Text("Status").Bold();

                                    header.Cell().Element(CabecalhoPdf)
                                        .Text("Produto").Bold();

                                    header.Cell().Element(CabecalhoPdf)
                                        .Text("Atual").Bold();

                                    header.Cell().Element(CabecalhoPdf)
                                        .Text("Mínimo").Bold();
                                });

                                foreach (var estoque in model.EstoquesBaixos)
                                {
                                    LinhaPdf(
                                        table,
                                        estoque.NomeFilial,
                                        estoque.FilialAtiva ? "Ativa" : "Desativada",
                                        estoque.NomeProduto,
                                        estoque.QuantidadeAtual.ToString(),
                                        estoque.QuantidadeMinima.ToString()
                                    );
                                }
                            });
                        }
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("LojaCupCake | Página ");
                        text.CurrentPageNumber();
                        text.Span(" de ");
                        text.TotalPages();
                    });
                });
            });

            byte[] arquivo = documento.GeneratePdf();

            string nomeArquivo =
                $"LojaCupCake_Relatorio_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

            return File(
                arquivo,
                "application/pdf",
                nomeArquivo
            );
        }

        // BUSCAR DADOS DO RELATÓRIO
        private async Task<DashboardAdminViewModel> ObterDadosAsync(
            DateTime? dataInicial,
            DateTime? dataFinal,
            long[]? filialIds,
            long? filialId,
            bool filiaisSelecionadas)
        {
            var fuso = ObterFusoBrasilia();

            var hoje = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                fuso
            ).Date;

            var inicio = (dataInicial ?? hoje.AddDays(-29)).Date;
            var fim = (dataFinal ?? hoje).Date;

            if (inicio > fim)
            {
                throw new ArgumentException(
                    "A data inicial não pode ser posterior à data final."
                );
            }

            if ((fim - inicio).TotalDays > 365)
            {
                throw new ArgumentException(
                    "O período máximo permitido é de 366 dias."
                );
            }

            // Os relatórios devem considerar também filiais desativadas,
            // desde que tenham sido selecionadas no Dashboard.
            var filiais = await _context.Filiais
                .AsNoTracking()
                .OrderBy(f => f.Nome)
                .ToListAsync();

            var idsDisponiveis = filiais
                .Select(f => f.FilialId)
                .ToHashSet();

            // Compatibilidade com links antigos que ainda enviam filialId.
            var idsRecebidos = (filialIds ?? Array.Empty<long>()).ToList();

            if (idsRecebidos.Count == 0 && filialId.HasValue)
            {
                idsRecebidos.Add(filialId.Value);
            }

            // Sem parâmetros de seleção: todas as filiais.
            // Com filiaisSelecionadas=true e nenhuma marcada: nenhuma filial.
            var idsSelecionados = filiaisSelecionadas || filialId.HasValue
                ? idsRecebidos
                    .Where(idsDisponiveis.Contains)
                    .Distinct()
                    .ToList()
                : filiais
                    .Select(f => f.FilialId)
                    .ToList();

            var model = new DashboardAdminViewModel
            {
                FilialId = idsSelecionados.Count == 1
                    ? idsSelecionados[0]
                    : null,
                FilialIds = idsSelecionados,
                DataInicial = inicio,
                DataFinal = fim,

                Filiais = filiais.Select(f =>
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = f.FilialId.ToString(),
                        Text = $"{f.Nome} — {(f.Ativo ? "Ativa" : "Desativada")}",
                        Selected = idsSelecionados.Contains(f.FilialId)
                    }).ToList()
            };

            DateTime inicioUtc = TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(inicio, DateTimeKind.Unspecified),
                fuso
            );

            DateTime fimUtc = TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(
                    fim.AddDays(1),
                    DateTimeKind.Unspecified),
                fuso
            );

            var pedidos = _context.Pedidos
                .Where(p =>
                    p.DataPedido >= inicioUtc &&
                    p.DataPedido < fimUtc &&
                    idsSelecionados.Contains(p.FilialId)
                );

            model.TotalPedidos = await pedidos.CountAsync();

            model.PedidosPendentes = await pedidos.CountAsync(p =>
                p.StatusPedido == StatusPedido.AguardandoAprovacao
            );

            var pedidosConcluidos = pedidos.Where(p =>
                p.StatusPedido == StatusPedido.Concluido &&
                p.Pagamentos.Any(pg =>
                    pg.StatusPagamento == StatusPagamento.Aprovado)
            );

            model.PedidosConcluidos =
                await pedidosConcluidos.CountAsync();

            model.Faturamento =
                await pedidosConcluidos
                    .Select(p => (decimal?)p.Total)
                    .SumAsync() ?? 0m;

            model.TicketMedio = model.PedidosConcluidos > 0
                ? model.Faturamento / model.PedidosConcluidos
                : 0m;

            var consultaEstoque = _context.Estoques
                .Where(e =>
                    e.Produto.Ativo &&
                    e.QuantidadeAtual <= e.QuantidadeMinima &&
                    idsSelecionados.Contains(e.FilialId)
                );

            model.ProdutosEstoqueBaixo =
                await consultaEstoque.CountAsync();

            model.EstoquesBaixos = await consultaEstoque
                .OrderBy(e => e.QuantidadeAtual)
                .Select(e => new EstoqueAlertaViewModel
                {
                    NomeFilial = e.Filial.Nome,
                    FilialAtiva = e.Filial.Ativo,
                    NomeProduto = e.Produto.Nome,
                    QuantidadeAtual = e.QuantidadeAtual,
                    QuantidadeMinima = e.QuantidadeMinima
                })
                .Take(20)
                .AsNoTracking()
                .ToListAsync();

            var vendas = await pedidosConcluidos
                .Select(p => new
                {
                    p.DataPedido,
                    p.Total
                })
                .ToListAsync();

            model.VendasPorDia = vendas
                .Select(v => new
                {
                    Data = TimeZoneInfo.ConvertTimeFromUtc(
                        DateTime.SpecifyKind(
                            v.DataPedido,
                            DateTimeKind.Utc),
                        fuso
                    ).Date,
                    v.Total
                })
                .GroupBy(v => v.Data)
                .Select(g => new VendaDiaViewModel
                {
                    Data = g.Key,
                    QuantidadePedidos = g.Count(),
                    TotalVendido = g.Sum(v => v.Total)
                })
                .OrderBy(v => v.Data)
                .ToList();

            var itensVendidos = await _context.ItensPedido
                .Where(i =>
                    i.Pedido.DataPedido >= inicioUtc &&
                    i.Pedido.DataPedido < fimUtc &&
                    i.Pedido.StatusPedido == StatusPedido.Concluido &&
                    i.Pedido.Pagamentos.Any(pg =>
                        pg.StatusPagamento == StatusPagamento.Aprovado) &&
                    idsSelecionados.Contains(i.Pedido.FilialId)
                )
                .Select(i => new
                {
                    i.NomeProdutoSnapshot,
                    i.Quantidade,
                    i.TotalItem
                })
                .ToListAsync();

            model.ProdutosMaisVendidos = itensVendidos
                .GroupBy(i => i.NomeProdutoSnapshot)
                .Select(g => new ProdutoVendidoViewModel
                {
                    NomeProduto = g.Key,
                    QuantidadeVendida = g.Sum(i => i.Quantidade),
                    TotalVendido = g.Sum(i => i.TotalItem)
                })
                .OrderByDescending(p => p.QuantidadeVendida)
                .Take(10)
                .ToList();

            return model;
        }

        // PLANILHA: RESUMO
        private static void CriarPlanilhaResumo(
            XLWorkbook workbook,
            DashboardAdminViewModel model)
        {
            var ws = workbook.Worksheets.Add("Resumo");

            ws.Cell("A1").Value = "LOJACUPCAKE - RELATÓRIO GERENCIAL";
            ws.Range("A1:B1").Merge();

            ws.Cell("A2").Value = "Data inicial";
            ws.Cell("B2").Value = model.DataInicial;

            ws.Cell("A3").Value = "Data final";
            ws.Cell("B3").Value = model.DataFinal;

            ws.Cell("A4").Value = "Filiais selecionadas";
            ws.Cell("B4").Value = ObterDescricaoFiliaisSelecionadas(model);

            ws.Cell("A6").Value = "Indicador";
            ws.Cell("B6").Value = "Resultado";

            ws.Cell("A7").Value = "Total de pedidos";
            ws.Cell("B7").Value = model.TotalPedidos;

            ws.Cell("A8").Value = "Aguardando aprovação";
            ws.Cell("B8").Value = model.PedidosPendentes;

            ws.Cell("A9").Value = "Pedidos concluídos";
            ws.Cell("B9").Value = model.PedidosConcluidos;

            ws.Cell("A10").Value = "Faturamento";
            ws.Cell("B10").Value = model.Faturamento;

            ws.Cell("A11").Value = "Ticket médio";
            ws.Cell("B11").Value = model.TicketMedio;

            ws.Cell("A12").Value = "Produtos com estoque baixo";
            ws.Cell("B12").Value = model.ProdutosEstoqueBaixo;

            EstilizarTitulo(ws.Range("A1:B1"));
            EstilizarCabecalho(ws.Range("A6:B6"));

            ws.Range("B2:B3").Style.DateFormat.Format = "dd/mm/yyyy";
            ws.Range("B10:B11").Style.NumberFormat.Format = "R$ #,##0.00";

            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(6);
        }

        // PLANILHA: VENDAS POR DIA
        private static void CriarPlanilhaVendas(
            XLWorkbook workbook,
            DashboardAdminViewModel model)
        {
            var ws = workbook.Worksheets.Add("Vendas por dia");

            ws.Cell("A1").Value = "Data";
            ws.Cell("B1").Value = "Quantidade de pedidos";
            ws.Cell("C1").Value = "Total vendido";

            int linha = 2;

            foreach (var venda in model.VendasPorDia)
            {
                ws.Cell(linha, 1).Value = venda.Data;
                ws.Cell(linha, 2).Value = venda.QuantidadePedidos;
                ws.Cell(linha, 3).Value = venda.TotalVendido;

                linha++;
            }

            EstilizarCabecalho(ws.Range("A1:C1"));

            if (linha > 2)
            {
                ws.Range(1, 1, linha - 1, 3).SetAutoFilter();
                ws.Column(1).Style.DateFormat.Format = "dd/mm/yyyy";
                ws.Column(3).Style.NumberFormat.Format = "R$ #,##0.00";
            }

            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(1);
        }

        // PLANILHA: PRODUTOS MAIS VENDIDOS
        private static void CriarPlanilhaProdutos(
            XLWorkbook workbook,
            DashboardAdminViewModel model)
        {
            var ws = workbook.Worksheets.Add("Produtos vendidos");

            ws.Cell("A1").Value = "Produto";
            ws.Cell("B1").Value = "Quantidade vendida";
            ws.Cell("C1").Value = "Total vendido";

            int linha = 2;

            foreach (var produto in model.ProdutosMaisVendidos)
            {
                ws.Cell(linha, 1).Value = produto.NomeProduto;
                ws.Cell(linha, 2).Value = produto.QuantidadeVendida;
                ws.Cell(linha, 3).Value = produto.TotalVendido;

                linha++;
            }

            EstilizarCabecalho(ws.Range("A1:C1"));

            if (linha > 2)
            {
                ws.Range(1, 1, linha - 1, 3).SetAutoFilter();
                ws.Column(3).Style.NumberFormat.Format = "R$ #,##0.00";
            }

            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(1);
        }

        // PLANILHA: ESTOQUE BAIXO
        private static void CriarPlanilhaEstoque(
            XLWorkbook workbook,
            DashboardAdminViewModel model)
        {
            var ws = workbook.Worksheets.Add("Estoque baixo");

            ws.Cell("A1").Value = "Filial";
            ws.Cell("B1").Value = "Status da filial";
            ws.Cell("C1").Value = "Produto";
            ws.Cell("D1").Value = "Quantidade atual";
            ws.Cell("E1").Value = "Quantidade mínima";

            int linha = 2;

            foreach (var estoque in model.EstoquesBaixos)
            {
                ws.Cell(linha, 1).Value = estoque.NomeFilial;
                ws.Cell(linha, 2).Value = estoque.FilialAtiva ? "Ativa" : "Desativada";
                ws.Cell(linha, 3).Value = estoque.NomeProduto;
                ws.Cell(linha, 4).Value = estoque.QuantidadeAtual;
                ws.Cell(linha, 5).Value = estoque.QuantidadeMinima;

                linha++;
            }

            EstilizarCabecalho(ws.Range("A1:E1"));

            if (linha > 2)
                ws.Range(1, 1, linha - 1, 5).SetAutoFilter();

            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(1);
        }

        // FORMATAÇÃO DO EXCEL
        private static void EstilizarTitulo(IXLRange range)
        {
            range.Style.Font.Bold = true;
            range.Style.Font.FontSize = 16;
            range.Style.Font.FontColor = XLColor.White;

            range.Style.Fill.BackgroundColor =
                XLColor.FromHtml("#9B5873");

            range.Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Center;
        }

        private static void EstilizarCabecalho(IXLRange range)
        {
            range.Style.Font.Bold = true;
            range.Style.Font.FontColor = XLColor.White;
            range.Style.Fill.BackgroundColor =
                XLColor.FromHtml("#9B5873");

            range.Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Center;
        }

        private static string ObterDescricaoFiliaisSelecionadas(
            DashboardAdminViewModel model)
        {
            var nomes = model.Filiais
                .Where(f => f.Selected)
                .Select(f => f.Text)
                .ToList();

            if (nomes.Count == 0)
                return "Nenhuma filial selecionada";

            return string.Join(", ", nomes);
        }

        // FORMATAÇÃO DAS TABELAS PDF
        private static IContainer CabecalhoPdf(IContainer container)
        {
            return container
                .Background(Colors.Grey.Lighten3)
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten1)
                .Padding(5);
        }

        private static IContainer CelulaPdf(IContainer container)
        {
            return container
                .BorderBottom(0.5f)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(5);
        }

        private static void LinhaPdf(
            TableDescriptor table,
            string coluna1,
            string coluna2)
        {
            table.Cell().Element(CelulaPdf).Text(coluna1);
            table.Cell().Element(CelulaPdf).Text(coluna2);
        }

        private static void LinhaPdf(
            TableDescriptor table,
            string coluna1,
            string coluna2,
            string coluna3)
        {
            table.Cell().Element(CelulaPdf).Text(coluna1);
            table.Cell().Element(CelulaPdf).Text(coluna2);
            table.Cell().Element(CelulaPdf).Text(coluna3);
        }

        private static void LinhaPdf(
            TableDescriptor table,
            string coluna1,
            string coluna2,
            string coluna3,
            string coluna4)
        {
            table.Cell().Element(CelulaPdf).Text(coluna1);
            table.Cell().Element(CelulaPdf).Text(coluna2);
            table.Cell().Element(CelulaPdf).Text(coluna3);
            table.Cell().Element(CelulaPdf).Text(coluna4);
        }

        private static void LinhaPdf(
            TableDescriptor table,
            string coluna1,
            string coluna2,
            string coluna3,
            string coluna4,
            string coluna5)
        {
            table.Cell().Element(CelulaPdf).Text(coluna1);
            table.Cell().Element(CelulaPdf).Text(coluna2);
            table.Cell().Element(CelulaPdf).Text(coluna3);
            table.Cell().Element(CelulaPdf).Text(coluna4);
            table.Cell().Element(CelulaPdf).Text(coluna5);
        }

        private static TimeZoneInfo ObterFusoBrasilia()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(
                    "America/Sao_Paulo"
                );
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById(
                    "E. South America Standard Time"
                );
            }
        }
    }
}