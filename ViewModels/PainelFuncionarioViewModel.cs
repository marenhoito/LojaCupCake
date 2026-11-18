
namespace LojaCupCake.ViewModels
{
    public class PainelFuncionarioViewModel
    {


        public string NomeCompleto { get; set; } = string.Empty;

        public List<FuncionarioFilialViewModel> Filiais { get; set; }
            = new();

        public FuncionarioFilialViewModel? FilialAtual { get; set; }

        public int PedidosAprovados { get; set; }

        public int DiasCadastrado { get; set; }


    }
}
