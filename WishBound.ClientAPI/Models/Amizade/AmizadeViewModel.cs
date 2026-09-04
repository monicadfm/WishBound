namespace WishBound.ClientAPI.Models.Amizade
{
    // ============================================================
    //  Modelos do sistema de amizade (espelho dos DTOs da WebAPI).
    //
    //  7 níveis: Desconhecido 0 → Conhecido 100 → Melhor Amigo 300 →
    //  Confidente 700 → Inseparável 1500 → Laço Especial 3000 → Alma
    //  Gémea 5000. Cada personagem só chega ao nível (raridade + 2):
    //  Comum 3 · Raro 4 · Épico 5 · Lendário 6 · Mítico 7.
    //  Nível 3 título · 4 notificações · 5 emblema (até 3 no perfil) ·
    //  6 título único colorido · 7 moldura.
    // ============================================================

    /// <summary>Um nível de amizade e o que desbloqueia.</summary>
    public class NivelAmizade
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int PontosNecessarios { get; set; }
        public int Ordem { get; set; }
        public string Recompensa { get; set; } = string.Empty;
    }

    /// <summary>Um nível visto da amizade com UMA personagem (página de detalhes).</summary>
    public class NivelPersonagem
    {
        public int Ordem { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int PontosNecessarios { get; set; }
        public string Recompensa { get; set; } = string.Empty;
        public bool Atingido { get; set; }
        public bool Alcancavel { get; set; }
    }

    /// <summary>Emblema ("rebento") de uma personagem, equipável no perfil (até 3).</summary>
    public class Emblema
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public string? ImagemUrl { get; set; }
        public int? PersonagemId { get; set; }
        public string? PersonagemNome { get; set; }
        public string? NivelNome { get; set; }
        public bool Obtido { get; set; }
        public bool Equipado { get; set; }
        public DateTime? DataObtencao { get; set; }

        public string Imagem => string.IsNullOrEmpty(ImagemUrl) ? "/img/personagens/desconhecido.svg" : ImagemUrl;
    }

    /// <summary>Título para o perfil (nível 3 genérico; nível 6 único e colorido).</summary>
    public class Titulo
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public int? PersonagemId { get; set; }
        public string? PersonagemNome { get; set; }
        public string? NivelNome { get; set; }
        public int NivelOrdem { get; set; }
        public bool IsPersonalizado { get; set; }
        public string? CorHex { get; set; }
        public bool Obtido { get; set; }
        public bool Equipado { get; set; }
        public DateTime? DataObtencao { get; set; }
    }

    /// <summary>Moldura de perfil (só as personagens Míticas dão moldura).</summary>
    public class Moldura
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? ImagemUrl { get; set; }
        public string? CorHex { get; set; }
        public int? PersonagemId { get; set; }
        public string? PersonagemNome { get; set; }
        public string? NivelNome { get; set; }
        public bool Obtida { get; set; }
        public bool Equipada { get; set; }
        public DateTime? DataObtencao { get; set; }

        public string Cor => string.IsNullOrEmpty(CorHex) ? "#f3c04f" : CorHex;
    }

    /// <summary>
    /// Detalhe da amizade com uma personagem — o bloco por baixo da
    /// descrição na página de detalhes.
    /// </summary>
    public class AmizadePersonagem
    {
        public int PersonagemId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string RaridadeNome { get; set; } = string.Empty;
        public string? RaridadeCor { get; set; }
        public int RaridadeOrdem { get; set; }

        public int PontosAmizade { get; set; }
        public string NivelAmizadeNome { get; set; } = string.Empty;
        public int NivelOrdem { get; set; }
        public int NivelMaximoOrdem { get; set; }
        public string NivelMaximoNome { get; set; } = string.Empty;
        public int PontosNivelAtual { get; set; }
        public int? PontosProximoNivel { get; set; }
        public DateOnly? UltimaInteracao { get; set; }

        public int InteracoesRestantes { get; set; }
        public int InteracoesPorDia { get; set; } = 3;
        public int PontosPorInteracao { get; set; } = 25;

        public List<NivelPersonagem> Niveis { get; set; } = new List<NivelPersonagem>();

        public Titulo? Titulo { get; set; }
        public Titulo? TituloUnico { get; set; }
        public Emblema? Emblema { get; set; }
        public Moldura? Moldura { get; set; }
        public bool NotificacoesAtivas { get; set; }
        public int EmblemasEquipados { get; set; }
        public int MaximoEmblemasEquipados { get; set; } = 3;

        public bool NivelMaximo => !PontosProximoNivel.HasValue;
        public bool TemInteracoes => InteracoesRestantes > 0;
        public int Percentagem => ProgressoAmizade.Percentagem(PontosAmizade, PontosNivelAtual, PontosProximoNivel);
        public string ProgressoTexto => ProgressoAmizade.Texto(PontosAmizade, PontosProximoNivel, NivelMaximoNome);
    }

    /// <summary>Resumo da amizade para o perfil: o que já ganhou e o que está equipado.</summary>
    public class AmizadeViewModel
    {
        public int InteracoesRestantes { get; set; }
        public int InteracoesPorDia { get; set; } = 3;
        public int PontosPorInteracao { get; set; } = 25;
        public Dictionary<int, int> PontosPorRepetida { get; set; } = new Dictionary<int, int>();
        public int MultiplicadorEvento { get; set; } = 2;
        public int MaximoEmblemasEquipados { get; set; } = 3;

        public List<NivelAmizade> Niveis { get; set; } = new List<NivelAmizade>();
        public List<Titulo> Titulos { get; set; } = new List<Titulo>();
        public List<Emblema> Emblemas { get; set; } = new List<Emblema>();
        public List<Moldura> Molduras { get; set; } = new List<Moldura>();

        public int? TituloAtualId { get; set; }
        public string? TituloAtualNome { get; set; }
        public string? TituloAtualCor { get; set; }
        public int? MolduraAtualId { get; set; }
        public string? MolduraAtualCor { get; set; }
        public string? MolduraAtualNome { get; set; }

        public bool TemInteracoes => InteracoesRestantes > 0;
        public List<Emblema> EmblemasEquipados => Emblemas.Where(e => e.Equipado).ToList();
    }

    /// <summary>Resposta da API a uma interação.</summary>
    public class ResultadoInteracao
    {
        public string Mensagem { get; set; } = string.Empty;
        public string Reacao { get; set; } = string.Empty;
        public int PersonagemId { get; set; }
        public string PersonagemNome { get; set; } = string.Empty;
        public int PontosGanhos { get; set; }
        public int PontosAmizade { get; set; }
        public string NivelAnterior { get; set; } = string.Empty;
        public string NivelAtual { get; set; } = string.Empty;
        public bool SubiuDeNivel { get; set; }
        public bool NivelMaximo { get; set; }
        public int InteracoesRestantes { get; set; }
        public int MensagensDiarias { get; set; }
    }

    /// <summary>Contas da barra de progresso, partilhadas pela coleção e pelos detalhes.</summary>
    public static class ProgressoAmizade
    {
        /// <summary>Pontos por cópia repetida, por ordem de raridade (igual à regra da API).</summary>
        public static int PontosRepetida(int ordemRaridade) => ordemRaridade switch
        {
            5 => 160,
            4 => 80,
            3 => 40,
            2 => 20,
            _ => 10
        };

        public static int Percentagem(int pontos, int inicioNivel, int? proximoNivel)
        {
            if (!proximoNivel.HasValue)
            {
                return 100;
            }

            int largura = proximoNivel.Value - inicioNivel;
            if (largura <= 0)
            {
                return 100;
            }

            return (int)Math.Clamp(Math.Round((pontos - inicioNivel) * 100.0 / largura), 0, 100);
        }

        public static string Texto(int pontos, int? proximoNivel, string? nivelMaximoNome = null)
        {
            if (proximoNivel.HasValue)
            {
                return pontos + " / " + proximoNivel.Value + " pontos";
            }

            return string.IsNullOrEmpty(nivelMaximoNome)
                ? pontos + " pontos (nível máximo)"
                : pontos + " pontos — nível máximo (" + nivelMaximoNome + ")";
        }
    }
}
