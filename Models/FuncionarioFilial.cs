using System;

namespace LojaCupCake.Models
{
    public class FuncionarioFilial
    {
        public long FuncionarioId { get; set; }

        public long FilialId { get; set; }

        // Permissões específicas por filial.
        public bool PodeAprovarPedidos { get; set; } = false;

        public bool PodeCadastrarFuncionarios { get; set; } = false;

        // Permite abrir/fechar manualmente a loja e retornar ao modo automático.
        public bool PodeControlarFuncionamento { get; set; } = false;

        public bool Ativo { get; set; } = true;

        public DateTime AtribuidoEm { get; set; } = DateTime.UtcNow;

        // Relacionamentos
        public Funcionario Funcionario { get; set; } = null!;

        public Filial Filial { get; set; } = null!;
    }
}
