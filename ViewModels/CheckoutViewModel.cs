
using System.ComponentModel.DataAnnotations;
using LojaCupCake.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LojaCupCake.ViewModels
{
    public class CheckoutViewModel
    {
        public string NomeFilial { get; set; } = string.Empty;
        public bool FilialDisponivel { get; set; }

        public TipoRecebimento TipoRecebimento { get; set; }
            = TipoRecebimento.Entrega;

        public FormaPagamento FormaPagamento { get; set; }
            = FormaPagamento.Pix;

        [Display(Name = "Endereço cadastrado")]
        public long? EnderecoId { get; set; }

        public List<SelectListItem> Enderecos { get; set; }
            = new();

        [StringLength(120)]
        [Display(Name = "Destinatário")]
        public string? Destinatario { get; set; }

        [StringLength(20)]
        [Display(Name = "Telefone de contato")]
        public string? TelefoneContato { get; set; }

        [StringLength(9)]
        public string? Cep { get; set; }

        [StringLength(150)]
        public string? Logradouro { get; set; }

        [StringLength(20)]
        public string? Numero { get; set; }

        [StringLength(100)]
        public string? Complemento { get; set; }

        [StringLength(100)]
        public string? Bairro { get; set; }

        [StringLength(100)]
        public string? Cidade { get; set; }

        [StringLength(2)]
        public string? Uf { get; set; }

        [StringLength(255)]
        [Display(Name = "Ponto de referência")]
        public string? PontoReferencia { get; set; }

        [StringLength(500)]
        [Display(Name = "Observações")]
        public string? Observacoes { get; set; }

        public decimal Subtotal { get; set; }

        public decimal TaxaEntrega { get; set; }

        public decimal Total { get; set; }

        public bool TodosItensDisponiveis { get; set; } = true;

        public List<CheckoutItemViewModel> Itens { get; set; }
            = new();
    }

    public class CheckoutItemViewModel
    {
        public long ProdutoId { get; set; }

        public string NomeProduto { get; set; } = string.Empty;

        public int Quantidade { get; set; }

        public decimal PrecoUnitario { get; set; }

        public decimal TotalItem { get; set; }

        public int EstoqueDisponivel { get; set; }

        public bool Disponivel { get; set; }

        public string? ImagemUrl { get; set; }
    }
}
