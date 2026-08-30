using Microsoft.EntityFrameworkCore;
using WishBound.WebAPI.Models;

namespace WishBound.WebAPI.Data
{
    /// <summary>
    /// Contexto do Entity Framework Core - faz a ponte entre as classes
    /// (modelos) e as tabelas da base de dados SQL Server.
    ///
    /// NOTA (versão final): a base de dados WishBound é criada e gerida
    /// diretamente no SQL Server Express ("database first") — o esquema
    /// completo, os dados iniciais (raridades, níveis de amizade, tipos de
    /// moeda) e as personagens migradas vivem na base de dados, não aqui.
    /// Por isso este contexto já NÃO tem seed (HasData) nem EnsureCreated.
    ///
    /// As entidades são adicionadas ao contexto à medida que as
    /// funcionalidades vão sendo desenvolvidas (utilizadores, coleção,
    /// carteiras, banners, ...).
    /// </summary>
    public class WishBoundContext : DbContext
    {
        public WishBoundContext(DbContextOptions<WishBoundContext> options) : base(options)
        {
        }

        public DbSet<Raridade> Raridades { get; set; }
        public DbSet<Personagem> Personagens { get; set; }
        public DbSet<Invocacao> Invocacoes { get; set; }
        public DbSet<Utilizador> Utilizadores { get; set; }
        public DbSet<TokenRecuperacaoPassword> TokensRecuperacao { get; set; }
        public DbSet<ItemColecao> Colecoes { get; set; }
        public DbSet<Inventario> Inventarios { get; set; }
        public DbSet<Carteira> Carteiras { get; set; }
        public DbSet<TransacaoMoeda> TransacoesMoeda { get; set; }
        public DbSet<Banner> Banners { get; set; }
        public DbSet<BannerPersonagem> BannerPersonagens { get; set; }
        public DbSet<Pity> Pity { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // A carteira não tem chave própria: é identificada pelo par
            // utilizador + tipo de moeda (chave composta na base de dados).
            modelBuilder.Entity<Carteira>()
                .HasKey(c => new { c.UtilizadorId, c.TipoMoedaId });

            // Personagens de um banner: chave banner + personagem
            modelBuilder.Entity<BannerPersonagem>()
                .HasKey(bp => new { bp.BannerId, bp.PersonagemId });

            // Contadores de pity: um por utilizador e banner
            modelBuilder.Entity<Pity>()
                .HasKey(p => new { p.UtilizadorId, p.BannerId });
        }
    }
}
