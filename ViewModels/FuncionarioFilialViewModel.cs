
namespace LojaCupCake.ViewModels
{
    public class FuncionarioFilialViewModel
    {
        public long FilialId { get; set; }

        public string NomeFilial { get; set; } = string.Empty;

        public bool Selecionada { get; set; }

        public bool PodeAprovarPedidos { get; set; }

        public bool PodeCadastrarFuncionarios { get; set; }

        public bool PodeControlarFuncionamento { get; set; } = false;
    }
}
