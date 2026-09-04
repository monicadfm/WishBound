using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WishBound.WebAPI.Models
{
    /// <summary>
    /// Ids dos tipos de moeda semeados na base de dados (tabela [TiposMoeda]),
    /// para não andarem números soltos pelos controllers.
    ///   1 = Gemas    - moeda premium, ainda sem uso;
    ///   2 = Moedas   - moeda normal: paga invocações e expansões do inventário;
    ///   3 = Bilhetes - bilhetes de invocação (Migracao03): cada um paga uma
    ///                  invocação inteira. Vêm da recompensa diária e dos eventos.
    /// </summary>
    public static class TiposMoedaIds
    {
        public const int Gemas = 1;
        public const int Moedas = 2;
        public const int Bilhetes = 3;
    }

    /// <summary>Tipo de moeda (tabela [TiposMoeda]) — só para ler os nomes.</summary>
    [Table("TiposMoeda")]
    public class TipoMoeda
    {
        [Key]
        [Column("TipoMoedaId")]
        public int Id { get; set; }

        [StringLength(30)]
        public string Nome { get; set; } = string.Empty;
    }

    /// <summary>
    /// Saldo de um utilizador numa moeda (tabela [CarteirasUtilizador]).
    /// A chave é composta — utilizador + tipo de moeda — e por isso é
    /// declarada no WishBoundContext (OnModelCreating).
    ///
    /// Tipos de moeda: ver TiposMoedaIds.
    /// </summary>
    [Table("CarteirasUtilizador")]
    public class Carteira
    {
        public int UtilizadorId { get; set; }

        public int TipoMoedaId { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal Saldo { get; set; }
    }

    /// <summary>
    /// Movimento de moeda (tabela [TransacoesMoeda]): fica o registo de tudo
    /// o que o utilizador ganha e gasta, para o histórico da economia.
    ///
    /// ATENÇÃO: a base de dados tem um CHECK que só aceita "Ganho" ou "Gasto"
    /// em TipoTransacao.
    /// </summary>
    [Table("TransacoesMoeda")]
    public class TransacaoMoeda
    {
        public const string TipoGanho = "Ganho";
        public const string TipoGasto = "Gasto";

        [Key]
        [Column("TransacaoId")]
        public int Id { get; set; }

        public int UtilizadorId { get; set; }

        public int TipoMoedaId { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal Montante { get; set; }

        [StringLength(20)]
        public string TipoTransacao { get; set; } = TipoGanho;

        [StringLength(50)]
        public string Origem { get; set; } = string.Empty;

        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    }
}
