using Microsoft.EntityFrameworkCore;
using WishBound.WebAPI.Models;

namespace WishBound.WebAPI.Data
{
    /// <summary>
    /// Contexto do Entity Framework Core - faz a ponte entre as classes
    /// (modelos) e as tabelas da base de dados SQL Server.
    /// Inclui dados iniciais (seed) para a aplicação arrancar já com conteúdo.
    /// </summary>
    public class WishBoundContext : DbContext
    {
        public WishBoundContext(DbContextOptions<WishBoundContext> options) : base(options)
        {
        }

        public DbSet<Raridade> Raridades { get; set; }
        public DbSet<Personagem> Personagens { get; set; }
        public DbSet<Invocacao> Invocacoes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Se uma personagem for apagada, apagam-se também as suas invocações
            modelBuilder.Entity<Invocacao>()
                .HasOne(i => i.Personagem)
                .WithMany()
                .HasForeignKey(i => i.PersonagemId)
                .OnDelete(DeleteBehavior.Cascade);

            // ----- Dados iniciais: Raridades (a soma das probabilidades = 100) -----
            modelBuilder.Entity<Raridade>().HasData(
                new Raridade { Id = 1, Nome = "Comum", Cor = "#9aa5b1", Probabilidade = 55 },
                new Raridade { Id = 2, Nome = "Raro", Cor = "#3b82f6", Probabilidade = 25 },
                new Raridade { Id = 3, Nome = "Épico", Cor = "#a855f7", Probabilidade = 12 },
                new Raridade { Id = 4, Nome = "Lendário", Cor = "#f59e0b", Probabilidade = 6 },
                new Raridade { Id = 5, Nome = "Mítico", Cor = "#ef4444", Probabilidade = 2 }
            );

            // ----- Dados iniciais: Personagens -----
            var dataSeed = new DateTime(2026, 7, 1);
            modelBuilder.Entity<Personagem>().HasData(
                new Personagem { Id = 1, Nome = "Nix", Descricao = "Um pequeno espírito curioso que aparece nas noites de nevoeiro.", ImagemUrl = "/img/personagens/nix.svg", RaridadeId = 1, DataCriacao = dataSeed },
                new Personagem { Id = 2, Nome = "Bram", Descricao = "Guarda da floresta, leal e teimoso como uma rocha.", ImagemUrl = "/img/personagens/bram.svg", RaridadeId = 1, DataCriacao = dataSeed },
                new Personagem { Id = 3, Nome = "Luna", Descricao = "Feiticeira da lua que colecciona desejos esquecidos.", ImagemUrl = "/img/personagens/luna.svg", RaridadeId = 2, DataCriacao = dataSeed },
                new Personagem { Id = 4, Nome = "Kaito", Descricao = "Navegador das marés estelares, nunca perde o rumo.", ImagemUrl = "/img/personagens/kaito.svg", RaridadeId = 2, DataCriacao = dataSeed },
                new Personagem { Id = 5, Nome = "Aurora", Descricao = "Dançarina de luzes polares com um temperamento imprevisível.", ImagemUrl = "/img/personagens/aurora.svg", RaridadeId = 3, DataCriacao = dataSeed },
                new Personagem { Id = 6, Nome = "Draven", Descricao = "Cavaleiro sombrio em busca de redenção.", ImagemUrl = "/img/personagens/draven.svg", RaridadeId = 3, DataCriacao = dataSeed },
                new Personagem { Id = 7, Nome = "Seraphina", Descricao = "Guardiã alada dos portões do amanhecer.", ImagemUrl = "/img/personagens/seraphina.svg", RaridadeId = 4, DataCriacao = dataSeed },
                new Personagem { Id = 8, Nome = "Celeste", Descricao = "A primeira estrela a ouvir um desejo. Diz-se que só aparece uma vez na vida.", ImagemUrl = "/img/personagens/celeste.svg", RaridadeId = 5, DataCriacao = dataSeed }
            );
        }
    }
}
