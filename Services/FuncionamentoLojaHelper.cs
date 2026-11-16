using LojaCupCake.Models;

namespace LojaCupCake.Services
{
    public sealed class EstadoFuncionamentoLoja
    {
        public bool Aberta { get; init; }
        public string Status { get; init; } = string.Empty;
        public string Modo { get; init; } = string.Empty;
        public DateTime AgoraLocal { get; init; }
    }

    public static class FuncionamentoLojaHelper
    {
        private static readonly Lazy<TimeZoneInfo> FusoBrasilia = new(() =>
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
            }
            catch (InvalidTimeZoneException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
            }
        });

        public static DateTime ObterHorarioLocal()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, FusoBrasilia.Value);
        }

        public static EstadoFuncionamentoLoja Avaliar(Filial filial)
        {
            return Avaliar(filial, ObterHorarioLocal());
        }

        public static EstadoFuncionamentoLoja Avaliar(Filial filial, DateTime agoraLocal)
        {
            if (!filial.Ativo)
            {
                return new EstadoFuncionamentoLoja
                {
                    Aberta = false,
                    Status = "Desativada",
                    Modo = "Desativada pelo administrador",
                    AgoraLocal = agoraLocal
                };
            }

            if (filial.ModoFuncionamento == ModoFuncionamentoFilial.ForcarAberta)
            {
                return new EstadoFuncionamentoLoja
                {
                    Aberta = true,
                    Status = "Aberta agora · abertura manual",
                    Modo = "Abertura manual",
                    AgoraLocal = agoraLocal
                };
            }

            if (filial.ModoFuncionamento == ModoFuncionamentoFilial.ForcarFechada)
            {
                return new EstadoFuncionamentoLoja
                {
                    Aberta = false,
                    Status = "Fechada agora · fechamento manual",
                    Modo = "Fechamento manual",
                    AgoraLocal = agoraLocal
                };
            }

            bool abertaPeloHorario = EstaDentroDoHorario(filial, agoraLocal);
            bool possuiHorario = filial.Funcionamentos != null && filial.Funcionamentos.Any();

            return new EstadoFuncionamentoLoja
            {
                Aberta = abertaPeloHorario,
                Status = !possuiHorario
                    ? "Horário não cadastrado"
                    : abertaPeloHorario ? "Aberta agora" : "Fechada agora",
                Modo = "Automático",
                AgoraLocal = agoraLocal
            };
        }

        public static bool EstaAberta(Filial filial)
        {
            return Avaliar(filial).Aberta;
        }

        public static bool EstaDentroDoHorario(Filial filial, DateTime agoraLocal)
        {
            if (filial.Funcionamentos == null || !filial.Funcionamentos.Any())
                return false;

            var diaHoje = ConverterDia(agoraLocal.DayOfWeek);
            var diaAnterior = ConverterDia(agoraLocal.AddDays(-1).DayOfWeek);
            var hora = agoraLocal.TimeOfDay;

            // Horários convencionais do dia atual.
            bool dentroDoDiaAtual = filial.Funcionamentos.Any(h =>
                h.DiaSemana == diaHoje &&
                (h.HoraFechamento >= h.HoraAbertura
                    ? hora >= h.HoraAbertura && hora < h.HoraFechamento
                    : hora >= h.HoraAbertura));

            if (dentroDoDiaAtual)
                return true;

            // Também contempla períodos que eventualmente atravessam a meia-noite.
            return filial.Funcionamentos.Any(h =>
                h.DiaSemana == diaAnterior &&
                h.HoraFechamento < h.HoraAbertura &&
                hora < h.HoraFechamento);
        }

        private static DiaSemana ConverterDia(DayOfWeek dia)
        {
            return dia == DayOfWeek.Sunday
                ? DiaSemana.Domingo
                : (DiaSemana)(int)dia;
        }
    }
}
