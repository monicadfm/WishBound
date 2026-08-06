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
    }
}
