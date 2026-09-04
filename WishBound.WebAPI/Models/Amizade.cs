using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WishBound.WebAPI.Models
{
    // ============================================================
    //  Entidades do SISTEMA DE AMIZADE (Migracao04).
    //
    //  A amizade é por utilizador E por personagem: os pontos e o nível
    //  vivem na linha da coleção (ItemColecao.PontosAmizade /
    //  NivelAmizadeId). Aqui ficam os níveis, as recompensas (emblemas,
    //  títulos, molduras) e o que cada utilizador já desbloqueou.
    // ============================================================

    /// <summary>
    /// Os 7 níveis de amizade (tabela [NiveisAmizade], Migracao05):
    /// Desconhecido 0 → Conhecido 100 → Melhor Amigo 300 → Confidente 700
    /// → Inseparável 1500 → Laço Especial 3000 → Alma Gémea 5000.
    /// Cada personagem só chega ao nível (ordem da raridade + 2):
    /// Comum 3 · Raro 4 · Épico 5 · Lendário 6 · Mítico 7.
    /// </summary>
    [Table("NiveisAmizade")]
    public class NivelAmizade
    {
        [Key]
        [Column("NivelAmizadeId")]
        public int Id { get; set; }

        [StringLength(30)]
        public string Nome { get; set; } = string.Empty;

        /// <summary>Pontos de amizade a partir dos quais se está neste nível.</summary>
        public int PontosNecessarios { get; set; }

        /// <summary>1 = Desconhecido ... 7 = Alma Gémea.</summary>
        public int Ordem { get; set; }
    }

    /// <summary>
    /// Emblema ("rebento") da personagem (tabela [Emblemas]), desbloqueado
    /// ao nível 5 (Inseparável — só Épico ou melhor lá chega). O utilizador
    /// pode equipar até 3 no perfil (EmblemaUtilizador.IsEquipado).
    /// </summary>
    [Table("Emblemas")]
    public class Emblema
    {
        [Key]
        [Column("EmblemaId")]
        public int Id { get; set; }

        [StringLength(60)]
        public string Nome { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Descricao { get; set; }

        [StringLength(255)]
        public string? ImagemUrl { get; set; }

        public int? PersonagemId { get; set; }

        public int? NivelAmizadeId { get; set; }
    }

    /// <summary>Emblema já ganho por um utilizador (tabela [EmblemasUtilizador], chave composta).</summary>
    [Table("EmblemasUtilizador")]
    public class EmblemaUtilizador
    {
        public int UtilizadorId { get; set; }

        public int EmblemaId { get; set; }

        public DateTime DataObtencao { get; set; } = DateTime.UtcNow;

        /// <summary>Mostrado no perfil (máximo 3 ao mesmo tempo). Migracao05.</summary>
        public bool IsEquipado { get; set; }
    }

    /// <summary>
    /// Título que o utilizador pode escolher para o perfil (tabela [Titulos],
    /// nova na Migracao04, regras da Migracao05). Dois por personagem:
    ///   - nível 3: "Melhor Amigo de X" (todas as personagens);
    ///   - nível 6: título único, na cor da raridade (só Lendário/Mítico;
    ///     IsPersonalizado = true, CorHex preenchido).
    /// Se faltar a linha para uma personagem nova, o ServicoAmizade cria-a.
    /// </summary>
    [Table("Titulos")]
    public class Titulo
    {
        [Key]
        [Column("TituloId")]
        public int Id { get; set; }

        [StringLength(60)]
        public string Nome { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Descricao { get; set; }

        public int? PersonagemId { get; set; }

        public int? NivelAmizadeId { get; set; }

        public bool IsPersonalizado { get; set; }

        /// <summary>Cor do título (níveis 6: a cor da raridade). Migracao05.</summary>
        [StringLength(7)]
        public string? CorHex { get; set; }
    }

    /// <summary>Título já ganho por um utilizador (tabela [TitulosUtilizador], chave composta).</summary>
    [Table("TitulosUtilizador")]
    public class TituloUtilizador
    {
        public int UtilizadorId { get; set; }

        public int TituloId { get; set; }

        public DateTime DataObtencao { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Moldura de perfil (tabela [MoldurasPerfil]), desbloqueada ao nível 7
    /// (Alma Gémea) — só as personagens MÍTICAS lá chegam. Sem imagens por agora: a moldura é um anel
    /// desenhado em CSS com a cor CorHex.
    /// </summary>
    [Table("MoldurasPerfil")]
    public class MolduraPerfil
    {
        [Key]
        [Column("MolduraId")]
        public int Id { get; set; }

        [StringLength(60)]
        public string Nome { get; set; } = string.Empty;

        [StringLength(255)]
        public string? ImagemUrl { get; set; }

        public int? PersonagemId { get; set; }

        public int? NivelAmizadeId { get; set; }

        [StringLength(7)]
        public string? CorHex { get; set; }
    }

    /// <summary>Moldura já ganha por um utilizador (tabela [MoldurasUtilizador], chave composta).</summary>
    [Table("MoldurasUtilizador")]
    public class MolduraUtilizador
    {
        public int UtilizadorId { get; set; }

        public int MolduraId { get; set; }

        public DateTime DataObtencao { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Notificação para o utilizador (tabela [Notificacoes]). O sistema de
    /// amizade grava uma por subida de nível e, a partir do nível 4 de uma
    /// personagem, mensagens personalizadas dessa personagem (uma por dia)
    /// — todas com Tipo "MensagemPersonagem", por causa do CHECK da tabela. A página que as mostra
    /// fica para a funcionalidade de notificações.
    /// </summary>
    [Table("Notificacoes")]
    public class Notificacao
    {
        // ATENÇÃO: a base de dados tem um CHECK em Tipo que só aceita
        // 'MensagemPersonagem', 'Banner', 'Evento', 'Recompensa' e
        // 'LoginDiario'. As notificações do sistema de amizade (subidas de
        // nível e mensagens diárias das personagens) usam todas
        // 'MensagemPersonagem' — o Titulo diz de que personagem vem.
        public const string TipoMensagemPersonagem = "MensagemPersonagem";
        public const string TipoAmizade = TipoMensagemPersonagem;
        public const string TipoPersonagem = TipoMensagemPersonagem;

        [Key]
        [Column("NotificacaoId")]
        public int Id { get; set; }

        public int UtilizadorId { get; set; }

        [StringLength(30)]
        public string Tipo { get; set; } = TipoMensagemPersonagem;

        [StringLength(100)]
        public string Titulo { get; set; } = string.Empty;

        [StringLength(255)]
        public string Mensagem { get; set; } = string.Empty;

        public bool IsLida { get; set; }

        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    }
}
