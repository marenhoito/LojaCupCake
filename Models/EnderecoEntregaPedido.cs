
using System.ComponentModel.DataAnnotations;

namespace LojaCupCake.Models
{
    public class EnderecoEntregaPedido
    {
        // PK e FK para Pedido
        public long PedidoId { get; set; }

        // Endereço cadastrado que originou a entrega
        public long? EnderecoId { get; set; }

        [Required]
        [StringLength(120)]
        public string Destinatario { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string TelefoneContato { get; set; } = string.Empty;

        [Required]
        [StringLength(9)]
        public string Cep { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string Logradouro { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Numero { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Complemento { get; set; }

        [Required]
        [StringLength(100)]
        public string Bairro { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Cidade { get; set; } = string.Empty;

        [Required]
        [StringLength(2)]
        public string Uf { get; set; } = string.Empty;

        [StringLength(255)]
        public string? PontoReferencia { get; set; }

        // Relacionamentos
        public Pedido Pedido { get; set; } = null!;

        public Endereco? Endereco { get; set; }
    }
}
