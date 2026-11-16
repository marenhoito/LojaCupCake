
using System.ComponentModel.DataAnnotations;
using LojaCupCake.Models;

namespace LojaCupCake.ViewModels
{
    public class FuncionamentoFilialViewModel
    {
        public long FilialId { get; set; }

        public string NomeFilial { get; set; } = string.Empty;

        [Required(ErrorMessage = "Selecione o dia da semana.")]
        public DiaSemana DiaSemana { get; set; }

        [Required(ErrorMessage = "Informe o horário de abertura.")]
        public TimeSpan HoraAbertura { get; set; }

        [Required(ErrorMessage = "Informe o horário de fechamento.")]
        public TimeSpan HoraFechamento { get; set; }

        public List<HorarioFilialItemViewModel> Horarios { get; set; }
            = new List<HorarioFilialItemViewModel>();
    }

    public class HorarioFilialItemViewModel
    {
        public long FuncionamentoFilialId { get; set; }

        public DiaSemana DiaSemana { get; set; }

        public TimeSpan HoraAbertura { get; set; }

        public TimeSpan HoraFechamento { get; set; }
    }
}
