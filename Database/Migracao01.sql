USE [WishBound];
GO
SET NOCOUNT ON;

-- ------------------------------------------------------------
-- 1) Personagens da versão mini
-- ------------------------------------------------------------
SET IDENTITY_INSERT dbo.Personagens ON;

INSERT INTO dbo.Personagens (PersonagemId, Nome, Descricao, RaridadeId, ImagemUrl, IsAtivo, DataCriacao)
SELECT v.PersonagemId, v.Nome, v.Descricao, r.RaridadeId, v.ImagemUrl, 1, '2026-07-01'
FROM (VALUES
    (1, N'Nix',       N'Um pequeno espírito curioso que aparece nas noites de nevoeiro.',                          N'Comum',    N'/img/personagens/nix.svg'),
    (2, N'Bram',      N'Guarda da floresta, leal e teimoso como uma rocha.',                                       N'Comum',    N'/img/personagens/bram.svg'),
    (3, N'Luna',      N'Feiticeira da lua que colecciona desejos esquecidos.',                                     N'Raro',     N'/img/personagens/luna.svg'),
    (4, N'Kaito',     N'Navegador das marés estelares, nunca perde o rumo.',                                       N'Raro',     N'/img/personagens/kaito.svg'),
    (5, N'Aurora',    N'Dançarina de luzes polares com um temperamento imprevisível.',                             N'Épico',    N'/img/personagens/aurora.svg'),
    (6, N'Draven',    N'Cavaleiro sombrio em busca de redenção.',                                                  N'Épico',    N'/img/personagens/draven.svg'),
    (7, N'Seraphina', N'Guardiã alada dos portões do amanhecer.',                                                  N'Lendário', N'/img/personagens/seraphina.svg'),
    (8, N'Celeste',   N'A primeira estrela a ouvir um desejo. Diz-se que só aparece uma vez na vida.',             N'Mítico',   N'/img/personagens/celeste.svg')
) AS v (PersonagemId, Nome, Descricao, RaridadeNome, ImagemUrl)
INNER JOIN dbo.Raridades r ON r.Nome = v.RaridadeNome
WHERE NOT EXISTS (SELECT 1 FROM dbo.Personagens p WHERE p.PersonagemId = v.PersonagemId OR p.Nome = v.Nome);

SET IDENTITY_INSERT dbo.Personagens OFF;

DECLARE @TotalPersonagens INT = (SELECT COUNT(*) FROM dbo.Personagens);
PRINT CONCAT('Personagens na base de dados: ', @TotalPersonagens);

-- ------------------------------------------------------------
-- 2) Utilizador "Sistema" (para invocações antes de existir login)
-- ------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Utilizadores WHERE UtilizadorId = 1)
BEGIN
    SET IDENTITY_INSERT dbo.Utilizadores ON;

    INSERT INTO dbo.Utilizadores
        (UtilizadorId, NomeUtilizador, Email, PasswordHash, EmailValidado, IsAdmin, IsAtivo, DataCriacao)
    VALUES
        (1, N'Sistema', N'sistema@wishbound.local', NULL, 1, 0, 1, SYSUTCDATETIME());

    SET IDENTITY_INSERT dbo.Utilizadores OFF;
    PRINT 'Utilizador "Sistema" criado (UtilizadorId = 1).';
END
ELSE
    PRINT 'Utilizador com Id 1 já existe — nada a fazer.';

-- ------------------------------------------------------------
-- 3) Banner Permanente + associação de todas as personagens
-- ------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Banners WHERE BannerId = 1)
BEGIN
    SET IDENTITY_INSERT dbo.Banners ON;

    INSERT INTO dbo.Banners
        (BannerId, Nome, Descricao, TipoBanner, ImagemUrl, DataInicio, DataFim, IsAtivo)
    VALUES
        (1, N'Banner Permanente',
            N'Banner base sempre disponível com todas as personagens da coleção.',
            N'Standard', NULL, '2026-01-01', '2099-12-31', 1);
            -- TipoBanner: a BD só aceita 'Standard' ou 'Evento' (CHECK constraint)

    SET IDENTITY_INSERT dbo.Banners OFF;
    PRINT 'Banner Permanente criado (BannerId = 1).';
END
ELSE
    PRINT 'Banner com Id 1 já existe — nada a fazer.';

-- Associa todas as personagens ainda não ligadas ao banner permanente
INSERT INTO dbo.BannerPersonagens (BannerId, PersonagemId, RateUp, ProbabilidadeExtra)
SELECT 1, p.PersonagemId, 0, NULL
FROM dbo.Personagens p
WHERE NOT EXISTS (SELECT 1 FROM dbo.BannerPersonagens bp
                  WHERE bp.BannerId = 1 AND bp.PersonagemId = p.PersonagemId);

DECLARE @TotalNoBanner INT = (SELECT COUNT(*) FROM dbo.BannerPersonagens WHERE BannerId = 1);
PRINT CONCAT('Personagens associadas ao Banner Permanente: ', @TotalNoBanner);

-- ------------------------------------------------------------
-- Resumo final
-- ------------------------------------------------------------
SELECT p.PersonagemId, p.Nome, r.Nome AS Raridade, p.IsAtivo
FROM dbo.Personagens p
INNER JOIN dbo.Raridades r ON r.RaridadeId = p.RaridadeId
ORDER BY r.Ordem, p.Nome;
GO
