using LojaCupCake.Models;

namespace LojaCupCake.ViewModels
{
    public class FuncionamentoLojaIndexViewModel
    {
        public List<FilialFuncionamentoLojaViewModel> Filiais { get; set; } = new();
    }

    public class FilialFuncionamentoLojaViewModel
    {
        public long FilialId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Endereco { get; set; } = string.Empty;
        public bool AbertaAgora { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Modo { get; set; } = string.Empty;
        public string HorariosHoje { get; set; } = string.Empty;
        public ModoFuncionamentoFilial ModoFuncionamento { get; set; }
    }
}
