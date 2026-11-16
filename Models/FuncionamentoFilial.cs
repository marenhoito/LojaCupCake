
using System.ComponentModel.DataAnnotations;

namespace LojaCupCake.Models
{
    public enum DiaSemana : byte
    {
        Segunda = 1,
        Terca = 2,
        Quarta = 3,
        Quinta = 4,
        Sexta = 5,
        Sabado = 6,
        Domingo = 7
    }

    public class FuncionamentoFilial
    {
        public long FuncionamentoFilialId { get; set; }

        public long FilialId { get; set; }

        public DiaSemana DiaSemana { get; set; }

        [Required]
        public TimeSpan HoraAbertura { get; set; }

        [Required]
        public TimeSpan HoraFechamento { get; set; }

        public Filial Filial { get; set; } = null!;
    }
}
