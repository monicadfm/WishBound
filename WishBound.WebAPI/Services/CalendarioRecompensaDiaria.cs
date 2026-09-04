using WishBound.WebAPI.Models;

namespace WishBound.WebAPI.Services
{
    /// <summary>
    /// Calendário de recompensas de login diário: um ciclo de 28 dias
    /// (4 semanas) que recomeça quando chega ao fim.
    ///
    ///   Semana 1 - 20 Moedas por dia, 60 no 7.º dia
    ///   Semana 2 - 30 Moedas por dia, 90 no 14.º dia
    ///   Semana 3 - 40 Moedas por dia, 120 no 21.º dia
    ///   Semana 4 - 1 Bilhete de invocação por dia, 3 no 28.º dia
    ///
    /// Ou seja: a moeda sobe de semana para semana, o último dia de cada
    /// semana dá o triplo e a última semana do "mês" dá invocações diretas.
    /// Um ciclo completo vale 810 Moedas (81 invocações) + 9 Bilhetes.
    ///
    /// O calendário só avança nos dias em que o utilizador recebe: faltar um
    /// dia não faz perder o progresso, só adia o dia seguinte.
    /// </summary>
    public static class CalendarioRecompensaDiaria
    {
        public const int DiasPorSemana = 7;
        public const int Semanas = 4;
        public const int TotalDias = DiasPorSemana * Semanas; // 28

        /// <summary>Moedas por dia em cada uma das três primeiras semanas.</summary>
        private static readonly decimal[] MoedasPorSemana = { 20m, 30m, 40m };

        /// <summary>Bilhetes por dia na última semana.</summary>
        private const decimal BilhetesPorDia = 1m;

        /// <summary>O último dia de cada semana dá este múltiplo.</summary>
        private const int MultiplicadorFimDeSemana = 3;

        /// <summary>Recompensa de um dia do calendário (1..28).</summary>
        public static (int TipoMoedaId, decimal Quantidade, int Semana, bool FimDeSemana) Obter(int dia)
        {
            if (dia < 1 || dia > TotalDias)
            {
                throw new ArgumentOutOfRangeException(nameof(dia), "O dia do calendário tem de estar entre 1 e " + TotalDias + ".");
            }

            int semana = (dia - 1) / DiasPorSemana + 1;            // 1..4
            bool fimDeSemana = dia % DiasPorSemana == 0;            // 7, 14, 21, 28
            int multiplicador = fimDeSemana ? MultiplicadorFimDeSemana : 1;

            if (semana == Semanas)
            {
                // Última semana: invocações a sério (Bilhetes)
                return (TiposMoedaIds.Bilhetes, BilhetesPorDia * multiplicador, semana, fimDeSemana);
            }

            return (TiposMoedaIds.Moedas, MoedasPorSemana[semana - 1] * multiplicador, semana, fimDeSemana);
        }

        /// <summary>
        /// Calendário completo para mostrar na página, já marcado com o que o
        /// utilizador recebeu (diasRecebidos = 0..28) e qual é o dia de hoje.
        /// </summary>
        public static List<DiaRecompensa> Construir(int diasRecebidos, bool recebidaHoje,
            Func<int, string> nomeDaMoeda)
        {
            var lista = new List<DiaRecompensa>(TotalDias);

            // O dia "de hoje": o último recebido (se já recebeu) ou o próximo
            int diaDeHoje = recebidaHoje ? diasRecebidos : diasRecebidos + 1;

            for (int dia = 1; dia <= TotalDias; dia++)
            {
                var (tipoMoedaId, quantidade, semana, fimDeSemana) = Obter(dia);

                lista.Add(new DiaRecompensa
                {
                    Dia = dia,
                    Semana = semana,
                    TipoMoedaId = tipoMoedaId,
                    MoedaNome = nomeDaMoeda(tipoMoedaId),
                    Quantidade = quantidade,
                    FimDeSemana = fimDeSemana,
                    Recebido = dia <= diasRecebidos,
                    Hoje = dia == diaDeHoje
                });
            }

            return lista;
        }
    }
}
