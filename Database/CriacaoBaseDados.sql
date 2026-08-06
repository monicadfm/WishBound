USE [master]
GO
/****** Object:  Database [WishBound]    Script Date: 01/08/2026 21:26:50 ******/
CREATE DATABASE [WishBound]
 CONTAINMENT = NONE
 ON  PRIMARY 
( NAME = N'WishBound', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS\MSSQL\DATA\WishBound.mdf' , SIZE = 8192KB , MAXSIZE = UNLIMITED, FILEGROWTH = 65536KB )
 LOG ON 
( NAME = N'WishBound_log', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS\MSSQL\DATA\WishBound_log.ldf' , SIZE = 8192KB , MAXSIZE = 2048GB , FILEGROWTH = 65536KB )
 WITH CATALOG_COLLATION = DATABASE_DEFAULT, LEDGER = OFF
GO
ALTER DATABASE [WishBound] SET COMPATIBILITY_LEVEL = 160
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [WishBound].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [WishBound] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [WishBound] SET ANSI_NULLS OFF 
GO
ALTER DATABASE [WishBound] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [WishBound] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [WishBound] SET ARITHABORT OFF 
GO
ALTER DATABASE [WishBound] SET AUTO_CLOSE OFF 
GO
ALTER DATABASE [WishBound] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [WishBound] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [WishBound] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [WishBound] SET CURSOR_DEFAULT  GLOBAL 
GO
ALTER DATABASE [WishBound] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [WishBound] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [WishBound] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [WishBound] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [WishBound] SET  DISABLE_BROKER 
GO
ALTER DATABASE [WishBound] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [WishBound] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO
ALTER DATABASE [WishBound] SET TRUSTWORTHY OFF 
GO
ALTER DATABASE [WishBound] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO
ALTER DATABASE [WishBound] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [WishBound] SET READ_COMMITTED_SNAPSHOT OFF 
GO
ALTER DATABASE [WishBound] SET HONOR_BROKER_PRIORITY OFF 
GO
ALTER DATABASE [WishBound] SET RECOVERY SIMPLE 
GO
ALTER DATABASE [WishBound] SET  MULTI_USER 
GO
ALTER DATABASE [WishBound] SET PAGE_VERIFY CHECKSUM  
GO
ALTER DATABASE [WishBound] SET DB_CHAINING OFF 
GO
ALTER DATABASE [WishBound] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
GO
ALTER DATABASE [WishBound] SET TARGET_RECOVERY_TIME = 60 SECONDS 
GO
ALTER DATABASE [WishBound] SET DELAYED_DURABILITY = DISABLED 
GO
ALTER DATABASE [WishBound] SET ACCELERATED_DATABASE_RECOVERY = OFF  
GO
ALTER DATABASE [WishBound] SET QUERY_STORE = ON
GO
ALTER DATABASE [WishBound] SET QUERY_STORE (OPERATION_MODE = READ_WRITE, CLEANUP_POLICY = (STALE_QUERY_THRESHOLD_DAYS = 30), DATA_FLUSH_INTERVAL_SECONDS = 900, INTERVAL_LENGTH_MINUTES = 60, MAX_STORAGE_SIZE_MB = 1000, QUERY_CAPTURE_MODE = AUTO, SIZE_BASED_CLEANUP_MODE = AUTO, MAX_PLANS_PER_QUERY = 200, WAIT_STATS_CAPTURE_MODE = ON)
GO
USE [WishBound]
GO
/****** Object:  Table [dbo].[HistoricoInvocacoes]    Script Date: 01/08/2026 21:26:50 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[HistoricoInvocacoes](
	[InvocacaoId] [int] IDENTITY(1,1) NOT NULL,
	[UtilizadorId] [int] NOT NULL,
	[BannerId] [int] NOT NULL,
	[PersonagemId] [int] NOT NULL,
	[RaridadeId] [int] NOT NULL,
	[PityAtivado] [bit] NOT NULL,
	[DataInvocacao] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[InvocacaoId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Utilizadores]    Script Date: 01/08/2026 21:26:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Utilizadores](
	[UtilizadorId] [int] IDENTITY(1,1) NOT NULL,
	[NomeUtilizador] [nvarchar](50) NOT NULL,
	[Email] [nvarchar](150) NOT NULL,
	[PasswordHash] [nvarchar](255) NULL,
	[GoogleId] [nvarchar](100) NULL,
	[EmailValidado] [bit] NOT NULL,
	[FotoPerfilUrl] [nvarchar](255) NULL,
	[MolduraPerfilAtualId] [int] NULL,
	[IsAdmin] [bit] NOT NULL,
	[IsAtivo] [bit] NOT NULL,
	[DataCriacao] [datetime2](7) NOT NULL,
	[UltimoLogin] [datetime2](7) NULL,
	[UltimoLoginDiario] [date] NULL,
PRIMARY KEY CLUSTERED 
(
	[UtilizadorId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ColecaoUtilizador]    Script Date: 01/08/2026 21:26:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ColecaoUtilizador](
	[ColecaoId] [int] IDENTITY(1,1) NOT NULL,
	[UtilizadorId] [int] NOT NULL,
	[PersonagemId] [int] NOT NULL,
	[Quantidade] [int] NOT NULL,
	[IsFavorito] [bit] NOT NULL,
	[PontosAmizade] [int] NOT NULL,
	[NivelAmizadeId] [int] NOT NULL,
	[DataObtencao] [datetime2](7) NOT NULL,
	[UltimaInteracao] [date] NULL,
PRIMARY KEY CLUSTERED 
(
	[ColecaoId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  View [dbo].[vw_EstatisticasGerais]    Script Date: 01/08/2026 21:26:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [dbo].[vw_EstatisticasGerais] AS
SELECT
    (SELECT COUNT(*) FROM Utilizadores WHERE IsAtivo = 1)          AS TotalUtilizadores,
    (SELECT COUNT(*) FROM HistoricoInvocacoes)                     AS TotalInvocacoes,
    (SELECT SUM(Quantidade) FROM ColecaoUtilizador)                AS TotalPersonagensObtidas;
GO
/****** Object:  Table [dbo].[Raridades]    Script Date: 01/08/2026 21:26:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Raridades](
	[RaridadeId] [int] IDENTITY(1,1) NOT NULL,
	[Nome] [nvarchar](30) NOT NULL,
	[Probabilidade] [decimal](6, 4) NOT NULL,
	[CorHex] [nvarchar](7) NULL,
	[Ordem] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[RaridadeId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  View [dbo].[vw_DistribuicaoRaridades]    Script Date: 01/08/2026 21:26:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
 
CREATE VIEW [dbo].[vw_DistribuicaoRaridades] AS
SELECT r.Nome AS Raridade, COUNT(h.InvocacaoId) AS TotalObtidas
FROM Raridades r
LEFT JOIN HistoricoInvocacoes h ON h.RaridadeId = r.RaridadeId
GROUP BY r.Nome;
GO
/****** Object:  Table [dbo].[Personagens]    Script Date: 01/08/2026 21:26:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Personagens](
	[PersonagemId] [int] IDENTITY(1,1) NOT NULL,
	[Nome] [nvarchar](100) NOT NULL,
	[Descricao] [nvarchar](max) NULL,
	[RaridadeId] [int] NOT NULL,
	[ImagemUrl] [nvarchar](255) NULL,
	[IsAtivo] [bit] NOT NULL,
	[DataCriacao] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[PersonagemId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  View [dbo].[vw_PersonagensMaisPopulares]    Script Date: 01/08/2026 21:26:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
 
CREATE VIEW [dbo].[vw_PersonagensMaisPopulares] AS
SELECT p.PersonagemId, p.Nome, COUNT(c.ColecaoId) AS TotalObtencoes
FROM Personagens p
LEFT JOIN ColecaoUtilizador c ON c.PersonagemId = p.PersonagemId
GROUP BY p.PersonagemId, p.Nome;
GO
/****** Object:  Table [dbo].[BannerPersonagens]    Script Date: 01/08/2026 21:26:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[BannerPersonagens](
	[BannerId] [int] NOT NULL,
	[PersonagemId] [int] NOT NULL,
	[RateUp] [bit] NOT NULL,
	[ProbabilidadeExtra] [decimal](6, 4) NULL,
PRIMARY KEY CLUSTERED 
(
	[BannerId] ASC,
	[PersonagemId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Banners]    Script Date: 01/08/2026 21:26:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Banners](
	[BannerId] [int] IDENTITY(1,1) NOT NULL,
	[Nome] [nvarchar](100) NOT NULL,
	[Descricao] [nvarchar](max) NULL,
	[TipoBanner] [nvarchar](20) NOT NULL,
	[ImagemUrl] [nvarchar](255) NULL,
	[DataInicio] [datetime2](7) NOT NULL,
	[DataFim] [datetime2](7) NOT NULL,
	[IsAtivo] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[BannerId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[CarteirasUtilizador]    Script Date: 01/08/2026 21:26:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CarteirasUtilizador](
	[UtilizadorId] [int] NOT NULL,
	[TipoMoedaId] [int] NOT NULL,
	[Saldo] [decimal](12, 2) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[UtilizadorId] ASC,
	[TipoMoedaId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Emblemas]    Script Date: 01/08/2026 21:26:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Emblemas](
	[EmblemaId] [int] IDENTITY(1,1) NOT NULL,
	[Nome] [nvarchar](60) NOT NULL,
	[Descricao] [nvarchar](255) NULL,
	[ImagemUrl] [nvarchar](255) NULL,
PRIMARY KEY CLUSTERED 
(
	[EmblemaId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[EmblemasUtilizador]    Script Date: 01/08/2026 21:26:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[EmblemasUtilizador](
	[UtilizadorId] [int] NOT NULL,
	[EmblemaId] [int] NOT NULL,
	[DataObtencao] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[UtilizadorId] ASC,
	[EmblemaId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[InventarioUtilizador]    Script Date: 01/08/2026 21:26:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[InventarioUtilizador](
	[UtilizadorId] [int] NOT NULL,
	[CapacidadeBase] [int] NOT NULL,
	[CapacidadeExtra] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[UtilizadorId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[LogsAdministrador]    Script Date: 01/08/2026 21:26:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[LogsAdministrador](
	[LogId] [int] IDENTITY(1,1) NOT NULL,
	[AdminId] [int] NOT NULL,
	[Acao] [nvarchar](100) NOT NULL,
	[TabelaAlvo] [nvarchar](50) NULL,
	[RegistoAlvoId] [int] NULL,
	[DataCriacao] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[LogId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MensagensPersonagem]    Script Date: 01/08/2026 21:26:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MensagensPersonagem](
	[MensagemId] [int] IDENTITY(1,1) NOT NULL,
	[PersonagemId] [int] NOT NULL,
	[TipoMensagem] [nvarchar](20) NOT NULL,
	[NivelAmizadeId] [int] NOT NULL,
	[Conteudo] [nvarchar](max) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[MensagemId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MoldurasPerfil]    Script Date: 01/08/2026 21:26:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MoldurasPerfil](
	[MolduraId] [int] IDENTITY(1,1) NOT NULL,
	[Nome] [nvarchar](60) NOT NULL,
	[ImagemUrl] [nvarchar](255) NULL,
PRIMARY KEY CLUSTERED 
(
	[MolduraId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MoldurasUtilizador]    Script Date: 01/08/2026 21:26:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MoldurasUtilizador](
	[UtilizadorId] [int] NOT NULL,
	[MolduraId] [int] NOT NULL,
	[DataObtencao] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[UtilizadorId] ASC,
	[MolduraId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[NiveisAmizade]    Script Date: 01/08/2026 21:26:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[NiveisAmizade](
	[NivelAmizadeId] [int] IDENTITY(1,1) NOT NULL,
	[Nome] [nvarchar](30) NOT NULL,
	[PontosNecessarios] [int] NOT NULL,
	[Ordem] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[NivelAmizadeId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Notificacoes]    Script Date: 01/08/2026 21:26:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Notificacoes](
	[NotificacaoId] [int] IDENTITY(1,1) NOT NULL,
	[UtilizadorId] [int] NOT NULL,
	[Tipo] [nvarchar](30) NOT NULL,
	[Titulo] [nvarchar](100) NOT NULL,
	[Mensagem] [nvarchar](255) NOT NULL,
	[IsLida] [bit] NOT NULL,
	[DataCriacao] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[NotificacaoId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ParticipacaoEventos]    Script Date: 01/08/2026 21:26:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ParticipacaoEventos](
	[ParticipacaoId] [int] IDENTITY(1,1) NOT NULL,
	[UtilizadorId] [int] NOT NULL,
	[BannerId] [int] NOT NULL,
	[Progresso] [int] NOT NULL,
	[RecompensasResgatadas] [bit] NOT NULL,
	[DataParticipacao] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[ParticipacaoId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PityUtilizador]    Script Date: 01/08/2026 21:26:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PityUtilizador](
	[UtilizadorId] [int] NOT NULL,
	[BannerId] [int] NOT NULL,
	[ContadorAtual] [int] NOT NULL,
	[UltimaRaridadeGarantida] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[UtilizadorId] ASC,
	[BannerId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[RecompensasEvento]    Script Date: 01/08/2026 21:26:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RecompensasEvento](
	[RecompensaId] [int] IDENTITY(1,1) NOT NULL,
	[BannerId] [int] NOT NULL,
	[Descricao] [nvarchar](255) NOT NULL,
	[TipoMoedaId] [int] NULL,
	[QuantidadeMoeda] [decimal](12, 2) NULL,
	[PersonagemId] [int] NULL,
	[MetaNecessaria] [nvarchar](100) NULL,
PRIMARY KEY CLUSTERED 
(
	[RecompensaId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TiposMoeda]    Script Date: 01/08/2026 21:26:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TiposMoeda](
	[TipoMoedaId] [int] IDENTITY(1,1) NOT NULL,
	[Nome] [nvarchar](30) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[TipoMoedaId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TokensRecuperacaoPassword]    Script Date: 01/08/2026 21:26:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TokensRecuperacaoPassword](
	[TokenId] [int] IDENTITY(1,1) NOT NULL,
	[UtilizadorId] [int] NOT NULL,
	[Token] [nvarchar](255) NOT NULL,
	[DataExpiracao] [datetime2](7) NOT NULL,
	[DataCriacao] [datetime2](7) NOT NULL,
	[Utilizado] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[TokenId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TransacoesMoeda]    Script Date: 01/08/2026 21:26:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TransacoesMoeda](
	[TransacaoId] [int] IDENTITY(1,1) NOT NULL,
	[UtilizadorId] [int] NOT NULL,
	[TipoMoedaId] [int] NOT NULL,
	[Montante] [decimal](12, 2) NOT NULL,
	[TipoTransacao] [nvarchar](20) NOT NULL,
	[Origem] [nvarchar](50) NOT NULL,
	[DataCriacao] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[TransacaoId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET IDENTITY_INSERT [dbo].[NiveisAmizade] ON 

INSERT [dbo].[NiveisAmizade] ([NivelAmizadeId], [Nome], [PontosNecessarios], [Ordem]) VALUES (1, N'Desconhecido', 0, 1)
INSERT [dbo].[NiveisAmizade] ([NivelAmizadeId], [Nome], [PontosNecessarios], [Ordem]) VALUES (2, N'Conhecido', 100, 2)
INSERT [dbo].[NiveisAmizade] ([NivelAmizadeId], [Nome], [PontosNecessarios], [Ordem]) VALUES (3, N'Amigo', 300, 3)
INSERT [dbo].[NiveisAmizade] ([NivelAmizadeId], [Nome], [PontosNecessarios], [Ordem]) VALUES (4, N'Grande Amigo', 700, 4)
INSERT [dbo].[NiveisAmizade] ([NivelAmizadeId], [Nome], [PontosNecessarios], [Ordem]) VALUES (5, N'Melhor Amigo', 1500, 5)
INSERT [dbo].[NiveisAmizade] ([NivelAmizadeId], [Nome], [PontosNecessarios], [Ordem]) VALUES (6, N'Laço Especial', 3000, 6)
SET IDENTITY_INSERT [dbo].[NiveisAmizade] OFF
GO
SET IDENTITY_INSERT [dbo].[Raridades] ON 

INSERT [dbo].[Raridades] ([RaridadeId], [Nome], [Probabilidade], [CorHex], [Ordem]) VALUES (1, N'Comum', CAST(0.5500 AS Decimal(6, 4)), N'#B0B0B0', 1)
INSERT [dbo].[Raridades] ([RaridadeId], [Nome], [Probabilidade], [CorHex], [Ordem]) VALUES (2, N'Raro', CAST(0.3000 AS Decimal(6, 4)), N'#4A90D9', 2)
INSERT [dbo].[Raridades] ([RaridadeId], [Nome], [Probabilidade], [CorHex], [Ordem]) VALUES (3, N'Épico', CAST(0.1200 AS Decimal(6, 4)), N'#9B59B6', 3)
INSERT [dbo].[Raridades] ([RaridadeId], [Nome], [Probabilidade], [CorHex], [Ordem]) VALUES (4, N'Lendário', CAST(0.0250 AS Decimal(6, 4)), N'#F1C40F', 4)
INSERT [dbo].[Raridades] ([RaridadeId], [Nome], [Probabilidade], [CorHex], [Ordem]) VALUES (5, N'Mítico', CAST(0.0050 AS Decimal(6, 4)), N'#E74C3C', 5)
SET IDENTITY_INSERT [dbo].[Raridades] OFF
GO
SET IDENTITY_INSERT [dbo].[TiposMoeda] ON 

INSERT [dbo].[TiposMoeda] ([TipoMoedaId], [Nome]) VALUES (1, N'Gemas')
INSERT [dbo].[TiposMoeda] ([TipoMoedaId], [Nome]) VALUES (2, N'Moedas')
SET IDENTITY_INSERT [dbo].[TiposMoeda] OFF
GO
/****** Object:  Index [UQ_Utilizador_Personagem]    Script Date: 01/08/2026 21:26:51 ******/
ALTER TABLE [dbo].[ColecaoUtilizador] ADD  CONSTRAINT [UQ_Utilizador_Personagem] UNIQUE NONCLUSTERED 
(
	[UtilizadorId] ASC,
	[PersonagemId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Colecao_Utilizador]    Script Date: 01/08/2026 21:26:51 ******/
CREATE NONCLUSTERED INDEX [IX_Colecao_Utilizador] ON [dbo].[ColecaoUtilizador]
(
	[UtilizadorId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_HistoricoInvocacoes_User]    Script Date: 01/08/2026 21:26:51 ******/
CREATE NONCLUSTERED INDEX [IX_HistoricoInvocacoes_User] ON [dbo].[HistoricoInvocacoes]
(
	[UtilizadorId] ASC,
	[BannerId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ__NiveisAm__7D8FE3B2FFF092E4]    Script Date: 01/08/2026 21:26:51 ******/
ALTER TABLE [dbo].[NiveisAmizade] ADD UNIQUE NONCLUSTERED 
(
	[Nome] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Notificacoes_Utilizador]    Script Date: 01/08/2026 21:26:51 ******/
CREATE NONCLUSTERED INDEX [IX_Notificacoes_Utilizador] ON [dbo].[Notificacoes]
(
	[UtilizadorId] ASC,
	[IsLida] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [UQ_Utilizador_Banner]    Script Date: 01/08/2026 21:26:51 ******/
ALTER TABLE [dbo].[ParticipacaoEventos] ADD  CONSTRAINT [UQ_Utilizador_Banner] UNIQUE NONCLUSTERED 
(
	[UtilizadorId] ASC,
	[BannerId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Personagens_Raridade]    Script Date: 01/08/2026 21:26:51 ******/
CREATE NONCLUSTERED INDEX [IX_Personagens_Raridade] ON [dbo].[Personagens]
(
	[RaridadeId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ__Raridade__7D8FE3B20F936FB5]    Script Date: 01/08/2026 21:26:51 ******/
ALTER TABLE [dbo].[Raridades] ADD UNIQUE NONCLUSTERED 
(
	[Nome] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ__TiposMoe__7D8FE3B2A5189BD6]    Script Date: 01/08/2026 21:26:51 ******/
ALTER TABLE [dbo].[TiposMoeda] ADD UNIQUE NONCLUSTERED 
(
	[Nome] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Transacoes_Utilizador]    Script Date: 01/08/2026 21:26:51 ******/
CREATE NONCLUSTERED INDEX [IX_Transacoes_Utilizador] ON [dbo].[TransacoesMoeda]
(
	[UtilizadorId] ASC,
	[DataCriacao] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ__Utilizad__2A355257C73A8FA1]    Script Date: 01/08/2026 21:26:51 ******/
ALTER TABLE [dbo].[Utilizadores] ADD UNIQUE NONCLUSTERED 
(
	[NomeUtilizador] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ__Utilizad__A6FBF2FBD0F4D127]    Script Date: 01/08/2026 21:26:51 ******/
ALTER TABLE [dbo].[Utilizadores] ADD UNIQUE NONCLUSTERED 
(
	[GoogleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ__Utilizad__A9D1053455E78F10]    Script Date: 01/08/2026 21:26:51 ******/
ALTER TABLE [dbo].[Utilizadores] ADD UNIQUE NONCLUSTERED 
(
	[Email] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[BannerPersonagens] ADD  DEFAULT ((0)) FOR [RateUp]
GO
ALTER TABLE [dbo].[Banners] ADD  DEFAULT ((1)) FOR [IsAtivo]
GO
ALTER TABLE [dbo].[CarteirasUtilizador] ADD  DEFAULT ((0)) FOR [Saldo]
GO
ALTER TABLE [dbo].[ColecaoUtilizador] ADD  DEFAULT ((1)) FOR [Quantidade]
GO
ALTER TABLE [dbo].[ColecaoUtilizador] ADD  DEFAULT ((0)) FOR [IsFavorito]
GO
ALTER TABLE [dbo].[ColecaoUtilizador] ADD  DEFAULT ((0)) FOR [PontosAmizade]
GO
ALTER TABLE [dbo].[ColecaoUtilizador] ADD  DEFAULT (sysutcdatetime()) FOR [DataObtencao]
GO
ALTER TABLE [dbo].[EmblemasUtilizador] ADD  DEFAULT (sysutcdatetime()) FOR [DataObtencao]
GO
ALTER TABLE [dbo].[HistoricoInvocacoes] ADD  DEFAULT ((0)) FOR [PityAtivado]
GO
ALTER TABLE [dbo].[HistoricoInvocacoes] ADD  DEFAULT (sysutcdatetime()) FOR [DataInvocacao]
GO
ALTER TABLE [dbo].[InventarioUtilizador] ADD  DEFAULT ((100)) FOR [CapacidadeBase]
GO
ALTER TABLE [dbo].[InventarioUtilizador] ADD  DEFAULT ((0)) FOR [CapacidadeExtra]
GO
ALTER TABLE [dbo].[LogsAdministrador] ADD  DEFAULT (sysutcdatetime()) FOR [DataCriacao]
GO
ALTER TABLE [dbo].[MoldurasUtilizador] ADD  DEFAULT (sysutcdatetime()) FOR [DataObtencao]
GO
ALTER TABLE [dbo].[Notificacoes] ADD  DEFAULT ((0)) FOR [IsLida]
GO
ALTER TABLE [dbo].[Notificacoes] ADD  DEFAULT (sysutcdatetime()) FOR [DataCriacao]
GO
ALTER TABLE [dbo].[ParticipacaoEventos] ADD  DEFAULT ((0)) FOR [Progresso]
GO
ALTER TABLE [dbo].[ParticipacaoEventos] ADD  DEFAULT ((0)) FOR [RecompensasResgatadas]
GO
ALTER TABLE [dbo].[ParticipacaoEventos] ADD  DEFAULT (sysutcdatetime()) FOR [DataParticipacao]
GO
ALTER TABLE [dbo].[Personagens] ADD  DEFAULT ((1)) FOR [IsAtivo]
GO
ALTER TABLE [dbo].[Personagens] ADD  DEFAULT (sysutcdatetime()) FOR [DataCriacao]
GO
ALTER TABLE [dbo].[PityUtilizador] ADD  DEFAULT ((0)) FOR [ContadorAtual]
GO
ALTER TABLE [dbo].[TokensRecuperacaoPassword] ADD  DEFAULT (sysutcdatetime()) FOR [DataCriacao]
GO
ALTER TABLE [dbo].[TokensRecuperacaoPassword] ADD  DEFAULT ((0)) FOR [Utilizado]
GO
ALTER TABLE [dbo].[TransacoesMoeda] ADD  DEFAULT (sysutcdatetime()) FOR [DataCriacao]
GO
ALTER TABLE [dbo].[Utilizadores] ADD  DEFAULT ((0)) FOR [EmailValidado]
GO
ALTER TABLE [dbo].[Utilizadores] ADD  DEFAULT ((0)) FOR [IsAdmin]
GO
ALTER TABLE [dbo].[Utilizadores] ADD  DEFAULT ((1)) FOR [IsAtivo]
GO
ALTER TABLE [dbo].[Utilizadores] ADD  DEFAULT (sysutcdatetime()) FOR [DataCriacao]
GO
ALTER TABLE [dbo].[BannerPersonagens]  WITH CHECK ADD FOREIGN KEY([BannerId])
REFERENCES [dbo].[Banners] ([BannerId])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[BannerPersonagens]  WITH CHECK ADD FOREIGN KEY([PersonagemId])
REFERENCES [dbo].[Personagens] ([PersonagemId])
GO
ALTER TABLE [dbo].[CarteirasUtilizador]  WITH CHECK ADD FOREIGN KEY([TipoMoedaId])
REFERENCES [dbo].[TiposMoeda] ([TipoMoedaId])
GO
ALTER TABLE [dbo].[CarteirasUtilizador]  WITH CHECK ADD FOREIGN KEY([UtilizadorId])
REFERENCES [dbo].[Utilizadores] ([UtilizadorId])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ColecaoUtilizador]  WITH CHECK ADD FOREIGN KEY([NivelAmizadeId])
REFERENCES [dbo].[NiveisAmizade] ([NivelAmizadeId])
GO
ALTER TABLE [dbo].[ColecaoUtilizador]  WITH CHECK ADD FOREIGN KEY([PersonagemId])
REFERENCES [dbo].[Personagens] ([PersonagemId])
GO
ALTER TABLE [dbo].[ColecaoUtilizador]  WITH CHECK ADD FOREIGN KEY([UtilizadorId])
REFERENCES [dbo].[Utilizadores] ([UtilizadorId])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[EmblemasUtilizador]  WITH CHECK ADD FOREIGN KEY([EmblemaId])
REFERENCES [dbo].[Emblemas] ([EmblemaId])
GO
ALTER TABLE [dbo].[EmblemasUtilizador]  WITH CHECK ADD FOREIGN KEY([UtilizadorId])
REFERENCES [dbo].[Utilizadores] ([UtilizadorId])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[HistoricoInvocacoes]  WITH CHECK ADD FOREIGN KEY([BannerId])
REFERENCES [dbo].[Banners] ([BannerId])
GO
ALTER TABLE [dbo].[HistoricoInvocacoes]  WITH CHECK ADD FOREIGN KEY([PersonagemId])
REFERENCES [dbo].[Personagens] ([PersonagemId])
GO
ALTER TABLE [dbo].[HistoricoInvocacoes]  WITH CHECK ADD FOREIGN KEY([RaridadeId])
REFERENCES [dbo].[Raridades] ([RaridadeId])
GO
ALTER TABLE [dbo].[HistoricoInvocacoes]  WITH CHECK ADD FOREIGN KEY([UtilizadorId])
REFERENCES [dbo].[Utilizadores] ([UtilizadorId])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[InventarioUtilizador]  WITH CHECK ADD FOREIGN KEY([UtilizadorId])
REFERENCES [dbo].[Utilizadores] ([UtilizadorId])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[LogsAdministrador]  WITH CHECK ADD FOREIGN KEY([AdminId])
REFERENCES [dbo].[Utilizadores] ([UtilizadorId])
GO
ALTER TABLE [dbo].[MensagensPersonagem]  WITH CHECK ADD FOREIGN KEY([NivelAmizadeId])
REFERENCES [dbo].[NiveisAmizade] ([NivelAmizadeId])
GO
ALTER TABLE [dbo].[MensagensPersonagem]  WITH CHECK ADD FOREIGN KEY([PersonagemId])
REFERENCES [dbo].[Personagens] ([PersonagemId])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[MoldurasUtilizador]  WITH CHECK ADD FOREIGN KEY([MolduraId])
REFERENCES [dbo].[MoldurasPerfil] ([MolduraId])
GO
ALTER TABLE [dbo].[MoldurasUtilizador]  WITH CHECK ADD FOREIGN KEY([UtilizadorId])
REFERENCES [dbo].[Utilizadores] ([UtilizadorId])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Notificacoes]  WITH CHECK ADD FOREIGN KEY([UtilizadorId])
REFERENCES [dbo].[Utilizadores] ([UtilizadorId])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ParticipacaoEventos]  WITH CHECK ADD FOREIGN KEY([BannerId])
REFERENCES [dbo].[Banners] ([BannerId])
GO
ALTER TABLE [dbo].[ParticipacaoEventos]  WITH CHECK ADD FOREIGN KEY([UtilizadorId])
REFERENCES [dbo].[Utilizadores] ([UtilizadorId])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Personagens]  WITH CHECK ADD FOREIGN KEY([RaridadeId])
REFERENCES [dbo].[Raridades] ([RaridadeId])
GO
ALTER TABLE [dbo].[PityUtilizador]  WITH CHECK ADD FOREIGN KEY([BannerId])
REFERENCES [dbo].[Banners] ([BannerId])
GO
ALTER TABLE [dbo].[PityUtilizador]  WITH CHECK ADD FOREIGN KEY([UltimaRaridadeGarantida])
REFERENCES [dbo].[Raridades] ([RaridadeId])
GO
ALTER TABLE [dbo].[PityUtilizador]  WITH CHECK ADD FOREIGN KEY([UtilizadorId])
REFERENCES [dbo].[Utilizadores] ([UtilizadorId])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[RecompensasEvento]  WITH CHECK ADD FOREIGN KEY([BannerId])
REFERENCES [dbo].[Banners] ([BannerId])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[RecompensasEvento]  WITH CHECK ADD FOREIGN KEY([PersonagemId])
REFERENCES [dbo].[Personagens] ([PersonagemId])
GO
ALTER TABLE [dbo].[RecompensasEvento]  WITH CHECK ADD FOREIGN KEY([TipoMoedaId])
REFERENCES [dbo].[TiposMoeda] ([TipoMoedaId])
GO
ALTER TABLE [dbo].[TokensRecuperacaoPassword]  WITH CHECK ADD FOREIGN KEY([UtilizadorId])
REFERENCES [dbo].[Utilizadores] ([UtilizadorId])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[TransacoesMoeda]  WITH CHECK ADD FOREIGN KEY([TipoMoedaId])
REFERENCES [dbo].[TiposMoeda] ([TipoMoedaId])
GO
ALTER TABLE [dbo].[TransacoesMoeda]  WITH CHECK ADD FOREIGN KEY([UtilizadorId])
REFERENCES [dbo].[Utilizadores] ([UtilizadorId])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Utilizadores]  WITH CHECK ADD  CONSTRAINT [FK_Utilizadores_MolduraAtual] FOREIGN KEY([MolduraPerfilAtualId])
REFERENCES [dbo].[MoldurasPerfil] ([MolduraId])
GO
ALTER TABLE [dbo].[Utilizadores] CHECK CONSTRAINT [FK_Utilizadores_MolduraAtual]
GO
ALTER TABLE [dbo].[Banners]  WITH CHECK ADD CHECK  (([TipoBanner]='Evento' OR [TipoBanner]='Standard'))
GO
ALTER TABLE [dbo].[MensagensPersonagem]  WITH CHECK ADD CHECK  (([TipoMensagem]='Diaria' OR [TipoMensagem]='Aleatoria' OR [TipoMensagem]='Saudacao'))
GO
ALTER TABLE [dbo].[Notificacoes]  WITH CHECK ADD CHECK  (([Tipo]='MensagemPersonagem' OR [Tipo]='Banner' OR [Tipo]='Evento' OR [Tipo]='Recompensa' OR [Tipo]='LoginDiario'))
GO
ALTER TABLE [dbo].[TransacoesMoeda]  WITH CHECK ADD CHECK  (([TipoTransacao]='Gasto' OR [TipoTransacao]='Ganho'))
GO
USE [master]
GO
ALTER DATABASE [WishBound] SET  READ_WRITE 
GO
