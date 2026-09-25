using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WishBound.WebAPI.Models
{
    /// <summary>
    /// Registo de uma ação de administração (tabela [LogsAdministrador]).
    ///
    /// A tabela vem do esquema original; a Migracao06 acrescentou
    /// UtilizadorAlvoId e Detalhes para ela servir de REGISTO DE AÇÕES da
    /// área de administração: quem fez (AdminId), a quem (UtilizadorAlvoId),
    /// o quê (Acao, TabelaAlvo, RegistoAlvoId) e porquê (Detalhes).
    /// Cada endpoint de api/admin grava uma linha dentro da própria
    /// transação da ação — ou fica tudo, ou não fica nada.
    /// </summary>
    [Table("LogsAdministrador")]
    public class LogAdministrador
    {
        // Categorias usadas no campo Acao (prefixo), para o painel filtrar
        public const string AcaoEstado = "Estado";
        public const string AcaoPassword = "Password";
        public const string AcaoMoeda = "Moeda";
        public const string AcaoPersonagem = "Personagem";
        public const string AcaoInventario = "Inventario";
        public const string AcaoAmizade = "Amizade";
        public const string AcaoRecompensa = "Recompensa";

        // Gestão da plataforma (fase de administração & estatísticas)
        public const string AcaoRaridade = "Raridade";
        public const string AcaoBanner = "Banner";
        public const string AcaoNotificacao = "Notificacao";
        public const string AcaoExportacao = "Exportacao";

        [Key]
        [Column("LogId")]
        public int Id { get; set; }

        /// <summary>Administrador que fez a ação.</summary>
        public int AdminId { get; set; }

        /// <summary>Conta sobre a qual a ação foi feita (Migracao06).</summary>
        public int? UtilizadorAlvoId { get; set; }

        /// <summary>Resumo curto: "Moeda: +100 Moedas", "Estado: desativada", ...</summary>
        [StringLength(100)]
        public string Acao { get; set; } = string.Empty;

        [StringLength(50)]
        public string? TabelaAlvo { get; set; }

        /// <summary>Id do registo tocado (PersonagemId, TituloId, TipoMoedaId, ...).</summary>
        public int? RegistoAlvoId { get; set; }

        /// <summary>Valores e motivo, em texto (Migracao06).</summary>
        [StringLength(500)]
        public string? Detalhes { get; set; }

        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    }
}
