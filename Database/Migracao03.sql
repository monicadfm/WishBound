USE [WishBound];
GO
SET NOCOUNT ON;

-- ============================================================
--  Migracao03 - Economia: recompensa diaria, bilhetes e evento
--
--  1) Nova "moeda" 3 = Bilhetes (bilhetes de invocacao): cada bilhete
--     paga uma invocacao inteira, sem gastar Moedas. E assim que a
--     recompensa diaria da ultima semana e o evento de 7 dias dao
--     "invocacoes a seco".
--
--  2) Carteira de Bilhetes para todos os utilizadores (as contas
--     novas ja a recebem no registo, porque o INSERT do registo faz
--     CROSS JOIN com TiposMoeda).
--
--  3) Coluna Utilizadores.DiaRecompensaDiaria - em que dia do
--     calendario de 28 dias (4 semanas) o utilizador esta. A coluna
--     UltimoLoginDiario (ja existia) guarda o ultimo dia em que
--     recebeu, para nao receber duas vezes no mesmo dia.
--
--  4) Evento "Festival de Boas-Vindas": banner de tipo Evento com
--     7 dias, com todas as personagens, e 7 recompensas diarias em
--     Bilhetes (1,1,1,1,1,2,3 = 10 invocacoes gratis). O evento
--     comeca no momento em que este script e executado.
--
--  Script idempotente: pode ser executado mais do que uma vez.
-- ============================================================

-- ------------------------------------------------------------
-- 1) Tipo de moeda 3 = Bilhetes
-- ------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.TiposMoeda WHERE TipoMoedaId = 3)
BEGIN
    SET IDENTITY_INSERT dbo.TiposMoeda ON;
    INSERT INTO dbo.TiposMoeda (TipoMoedaId, Nome) VALUES (3, N'Bilhetes');
    SET IDENTITY_INSERT dbo.TiposMoeda OFF;
    PRINT 'Tipo de moeda 3 (Bilhetes) criado.';
END
ELSE
    PRINT 'Tipo de moeda 3 ja existe - nada a fazer.';
GO

-- ------------------------------------------------------------
-- 2) Carteiras em falta (uma por utilizador e tipo de moeda)
-- ------------------------------------------------------------
INSERT INTO dbo.CarteirasUtilizador (UtilizadorId, TipoMoedaId, Saldo)
SELECT u.UtilizadorId, m.TipoMoedaId, 0
FROM dbo.Utilizadores u
CROSS JOIN dbo.TiposMoeda m
WHERE NOT EXISTS (SELECT 1 FROM dbo.CarteirasUtilizador c
                  WHERE c.UtilizadorId = u.UtilizadorId AND c.TipoMoedaId = m.TipoMoedaId);

PRINT CONCAT('Carteiras criadas agora: ', @@ROWCOUNT);
GO

-- ------------------------------------------------------------
-- 3) Dia do calendario de recompensas diarias (0 = ainda nao recebeu)
-- ------------------------------------------------------------
IF COL_LENGTH('dbo.Utilizadores', 'DiaRecompensaDiaria') IS NULL
BEGIN
    ALTER TABLE dbo.Utilizadores
        ADD DiaRecompensaDiaria INT NOT NULL
            CONSTRAINT DF_Utilizadores_DiaRecompensaDiaria DEFAULT (0);

    PRINT 'Coluna DiaRecompensaDiaria acrescentada a Utilizadores.';
END
ELSE
    PRINT 'Coluna DiaRecompensaDiaria ja existe - nada a fazer.';
GO

-- ------------------------------------------------------------
-- 4) Evento de 7 dias com bilhetes de invocacao
-- ------------------------------------------------------------
DECLARE @NomeEvento NVARCHAR(100) = N'Festival de Boas-Vindas';
DECLARE @BannerId INT = (SELECT TOP 1 BannerId FROM dbo.Banners WHERE Nome = @NomeEvento);

IF @BannerId IS NULL
BEGIN
    DECLARE @Inicio DATETIME2(7) = SYSUTCDATETIME();

    INSERT INTO dbo.Banners (Nome, Descricao, TipoBanner, ImagemUrl, DataInicio, DataFim, IsAtivo)
    VALUES (@NomeEvento,
            N'Durante 7 dias, entra todos os dias e recebe bilhetes de invocação: 1 por dia nos primeiros cinco dias, 2 no sexto e 3 no sétimo — 10 invocações grátis ao todo.',
            N'Evento', NULL, @Inicio, DATEADD(DAY, 7, @Inicio), 1);
            -- TipoBanner: a BD so aceita 'Standard' ou 'Evento' (CHECK)

    SET @BannerId = SCOPE_IDENTITY();
    PRINT CONCAT('Banner de evento criado (BannerId = ', @BannerId, '), a decorrer ate ', CONVERT(NVARCHAR(16), DATEADD(DAY, 7, @Inicio), 120), ' UTC.');
END
ELSE
    PRINT CONCAT('Banner de evento ja existe (BannerId = ', @BannerId, ').');

-- O evento tambem e um banner de invocacao: leva todas as personagens
-- (rate-up fica para a funcionalidade de eventos/banners).
INSERT INTO dbo.BannerPersonagens (BannerId, PersonagemId, RateUp, ProbabilidadeExtra)
SELECT @BannerId, p.PersonagemId, 0, NULL
FROM dbo.Personagens p
WHERE NOT EXISTS (SELECT 1 FROM dbo.BannerPersonagens bp
                  WHERE bp.BannerId = @BannerId AND bp.PersonagemId = p.PersonagemId);

-- Recompensas diarias do evento: uma linha por dia, em Bilhetes (TipoMoedaId 3).
-- MetaNecessaria guarda o dia ('1'..'7'); a ordem dos dias e a ordem das linhas.
IF NOT EXISTS (SELECT 1 FROM dbo.RecompensasEvento WHERE BannerId = @BannerId)
BEGIN
    INSERT INTO dbo.RecompensasEvento (BannerId, Descricao, TipoMoedaId, QuantidadeMoeda, PersonagemId, MetaNecessaria)
    VALUES
        (@BannerId, N'Dia 1 - 1 bilhete de invocação',  3, 1, NULL, N'1'),
        (@BannerId, N'Dia 2 - 1 bilhete de invocação',  3, 1, NULL, N'2'),
        (@BannerId, N'Dia 3 - 1 bilhete de invocação',  3, 1, NULL, N'3'),
        (@BannerId, N'Dia 4 - 1 bilhete de invocação',  3, 1, NULL, N'4'),
        (@BannerId, N'Dia 5 - 1 bilhete de invocação',  3, 1, NULL, N'5'),
        (@BannerId, N'Dia 6 - 2 bilhetes de invocação', 3, 2, NULL, N'6'),
        (@BannerId, N'Dia 7 - 3 bilhetes de invocação', 3, 3, NULL, N'7');

    PRINT 'Recompensas do evento criadas (7 dias, 10 bilhetes).';
END
ELSE
    PRINT 'Recompensas do evento ja existem - nada a fazer.';
GO

-- ------------------------------------------------------------
-- Resumo
-- ------------------------------------------------------------
-- (PRINT nao aceita subqueries: os valores vao primeiro para variaveis)
DECLARE @TiposMoeda INT = (SELECT COUNT(*) FROM dbo.TiposMoeda);
DECLARE @BannersADecorrer INT = (SELECT COUNT(*) FROM dbo.Banners
                                 WHERE IsAtivo = 1 AND DataInicio <= SYSUTCDATETIME() AND DataFim >= SYSUTCDATETIME());
PRINT CONCAT('Tipos de moeda: ', @TiposMoeda);
PRINT CONCAT('Banners a decorrer: ', @BannersADecorrer);
PRINT 'Migracao03 concluida.';
GO
