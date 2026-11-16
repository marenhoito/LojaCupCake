
using System.ComponentModel.DataAnnotations;

namespace LojaCupCake.Models
{
    public class Endereco
    {
        public long EnderecoId { get; set; }

        public long ClienteId { get; set; }

        [StringLength(50)]
        public string? Apelido { get; set; }

        [Required]
        [StringLength(120)]
        public string Destinatario { get; set; } = string.Empty;

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

        public bool Principal { get; set; } = false;

        public bool Ativo { get; set; } = true;

        // Relacionamento com Cliente
        public Cliente Cliente { get; set; } = null!;
    }
}
