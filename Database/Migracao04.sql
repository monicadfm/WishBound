USE [WishBound];
GO
SET NOCOUNT ON;

-- ============================================================
--  Migracao04 - Sistema de amizade: interacoes diarias, emblemas,
--               titulos e molduras de perfil
--
--  1) Utilizadores.InteracoesRestantes + DiaInteracoes: cada
--     utilizador tem 3 INTERACOES por dia (dia UTC) para gastar nas
--     personagens que quiser - pode gastar as 3 na mesma. E a
--     "recompensa de login" do sistema de amizade: quem entra todos
--     os dias tem 3 interacoes todos os dias.
--
--  2) Utilizadores.TituloAtualId: titulo escolhido para o perfil
--     (a moldura ja tinha coluna: MolduraPerfilAtualId).
--
--  3) Emblemas e MoldurasPerfil ganham PersonagemId + NivelAmizadeId
--     (a que personagem pertencem e em que nivel se desbloqueiam);
--     MoldurasPerfil ganha CorHex (as molduras sao desenhadas em CSS).
--
--  4) Novas tabelas Titulos e TitulosUtilizador (a BD nao tinha).
--
--  5) Sementes:
--     - 1 emblema ("rebento") por personagem, desbloqueado ao nivel 3
--       (Amigo);
--     - titulos por personagem e nivel (2..6). As personagens
--       Lendarias e Miticas tem titulos PERSONALIZADOS; as restantes
--       usam a forma generica "Amigo de X", "Melhor Amigo de X"...
--       (a API cria o titulo generico sozinha se faltar a linha -
--       personagens novas nao precisam de script);
--     - 1 moldura por personagem MITICA, desbloqueada ao nivel 5
--       (Melhor Amigo). So as Miticas dao moldura.
--
--  Script idempotente: pode ser executado mais do que uma vez.
-- ============================================================

-- ------------------------------------------------------------
-- 1) Interacoes diarias
-- ------------------------------------------------------------
IF COL_LENGTH('dbo.Utilizadores', 'InteracoesRestantes') IS NULL
BEGIN
    ALTER TABLE dbo.Utilizadores
        ADD InteracoesRestantes INT NOT NULL
            CONSTRAINT DF_Utilizadores_InteracoesRestantes DEFAULT (3);
    PRINT 'Coluna InteracoesRestantes acrescentada a Utilizadores.';
END
ELSE
    PRINT 'Coluna InteracoesRestantes ja existe - nada a fazer.';

IF COL_LENGTH('dbo.Utilizadores', 'DiaInteracoes') IS NULL
BEGIN
    ALTER TABLE dbo.Utilizadores ADD DiaInteracoes DATE NULL;
    PRINT 'Coluna DiaInteracoes acrescentada a Utilizadores.';
END
ELSE
    PRINT 'Coluna DiaInteracoes ja existe - nada a fazer.';
GO

-- ------------------------------------------------------------
-- 2) Emblemas / Molduras ligados a personagem e nivel
-- ------------------------------------------------------------
IF COL_LENGTH('dbo.Emblemas', 'PersonagemId') IS NULL
BEGIN
    ALTER TABLE dbo.Emblemas ADD PersonagemId INT NULL
        CONSTRAINT FK_Emblemas_Personagem REFERENCES dbo.Personagens (PersonagemId);
    PRINT 'Coluna PersonagemId acrescentada a Emblemas.';
END

IF COL_LENGTH('dbo.Emblemas', 'NivelAmizadeId') IS NULL
BEGIN
    ALTER TABLE dbo.Emblemas ADD NivelAmizadeId INT NULL
        CONSTRAINT FK_Emblemas_NivelAmizade REFERENCES dbo.NiveisAmizade (NivelAmizadeId);
    PRINT 'Coluna NivelAmizadeId acrescentada a Emblemas.';
END

IF COL_LENGTH('dbo.MoldurasPerfil', 'PersonagemId') IS NULL
BEGIN
    ALTER TABLE dbo.MoldurasPerfil ADD PersonagemId INT NULL
        CONSTRAINT FK_MoldurasPerfil_Personagem REFERENCES dbo.Personagens (PersonagemId);
    PRINT 'Coluna PersonagemId acrescentada a MoldurasPerfil.';
END

IF COL_LENGTH('dbo.MoldurasPerfil', 'NivelAmizadeId') IS NULL
BEGIN
    ALTER TABLE dbo.MoldurasPerfil ADD NivelAmizadeId INT NULL
        CONSTRAINT FK_MoldurasPerfil_NivelAmizade REFERENCES dbo.NiveisAmizade (NivelAmizadeId);
    PRINT 'Coluna NivelAmizadeId acrescentada a MoldurasPerfil.';
END

IF COL_LENGTH('dbo.MoldurasPerfil', 'CorHex') IS NULL
BEGIN
    ALTER TABLE dbo.MoldurasPerfil ADD CorHex NVARCHAR(7) NULL;
    PRINT 'Coluna CorHex acrescentada a MoldurasPerfil.';
END
GO

-- ------------------------------------------------------------
-- 3) Titulos (tabela nova) e titulos de cada utilizador
-- ------------------------------------------------------------
IF OBJECT_ID('dbo.Titulos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Titulos (
        TituloId        INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Nome            NVARCHAR(60)  NOT NULL,
        Descricao       NVARCHAR(255) NULL,
        PersonagemId    INT NULL CONSTRAINT FK_Titulos_Personagem REFERENCES dbo.Personagens (PersonagemId),
        NivelAmizadeId  INT NULL CONSTRAINT FK_Titulos_NivelAmizade REFERENCES dbo.NiveisAmizade (NivelAmizadeId),
        -- 1 = titulo escrito a mao para a personagem (Lendarias/Miticas);
        -- 0 = forma generica ("Amigo de X")
        IsPersonalizado BIT NOT NULL CONSTRAINT DF_Titulos_IsPersonalizado DEFAULT (0)
    );
    -- Um titulo por personagem e nivel (os titulos sem personagem ficam de fora do indice)
    CREATE UNIQUE INDEX UX_Titulos_Personagem_Nivel
        ON dbo.Titulos (PersonagemId, NivelAmizadeId)
        WHERE PersonagemId IS NOT NULL AND NivelAmizadeId IS NOT NULL;
    PRINT 'Tabela Titulos criada.';
END
ELSE
    PRINT 'Tabela Titulos ja existe - nada a fazer.';

IF OBJECT_ID('dbo.TitulosUtilizador', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TitulosUtilizador (
        UtilizadorId INT NOT NULL CONSTRAINT FK_TitulosUtilizador_Utilizador REFERENCES dbo.Utilizadores (UtilizadorId) ON DELETE CASCADE,
        TituloId     INT NOT NULL CONSTRAINT FK_TitulosUtilizador_Titulo REFERENCES dbo.Titulos (TituloId),
        DataObtencao DATETIME2(7) NOT NULL CONSTRAINT DF_TitulosUtilizador_DataObtencao DEFAULT (SYSUTCDATETIME()),
        PRIMARY KEY (UtilizadorId, TituloId)
    );
    PRINT 'Tabela TitulosUtilizador criada.';
END
ELSE
    PRINT 'Tabela TitulosUtilizador ja existe - nada a fazer.';
GO

IF COL_LENGTH('dbo.Utilizadores', 'TituloAtualId') IS NULL
BEGIN
    ALTER TABLE dbo.Utilizadores ADD TituloAtualId INT NULL
        CONSTRAINT FK_Utilizadores_TituloAtual REFERENCES dbo.Titulos (TituloId);
    PRINT 'Coluna TituloAtualId acrescentada a Utilizadores.';
END
ELSE
    PRINT 'Coluna TituloAtualId ja existe - nada a fazer.';
GO

-- ------------------------------------------------------------
-- 4) Sementes
-- ------------------------------------------------------------

-- 4a) Emblemas: um "rebento" por personagem, ao nivel 3 (Amigo).
--     A imagem do emblema e a propria imagem da personagem.
INSERT INTO dbo.Emblemas (Nome, Descricao, ImagemUrl, PersonagemId, NivelAmizadeId)
SELECT N'Rebento de ' + p.Nome,
       N'Chegaste a Amigo de ' + p.Nome + N'. Este rebento fica no teu perfil.',
       p.ImagemUrl, p.PersonagemId, 3
FROM dbo.Personagens p
WHERE NOT EXISTS (SELECT 1 FROM dbo.Emblemas e WHERE e.PersonagemId = p.PersonagemId);

PRINT CONCAT('Emblemas criados agora: ', @@ROWCOUNT);

-- 4b) Titulos genericos para TODAS as personagens (niveis 2..6).
--     As Lendarias/Miticas recebem a seguir os titulos personalizados
--     (UPDATE por cima da linha generica).
INSERT INTO dbo.Titulos (Nome, Descricao, PersonagemId, NivelAmizadeId, IsPersonalizado)
SELECT CASE n.Ordem
           WHEN 2 THEN N'Conhecido de ' + p.Nome
           WHEN 3 THEN N'Amigo de ' + p.Nome
           WHEN 4 THEN N'Grande Amigo de ' + p.Nome
           WHEN 5 THEN N'Melhor Amigo de ' + p.Nome
           ELSE        N'Laço Especial com ' + p.Nome
       END,
       N'Nível de amizade "' + n.Nome + N'" com ' + p.Nome + N'.',
       p.PersonagemId, n.NivelAmizadeId, 0
FROM dbo.Personagens p
CROSS JOIN dbo.NiveisAmizade n
WHERE n.Ordem >= 2
  AND NOT EXISTS (SELECT 1 FROM dbo.Titulos t
                  WHERE t.PersonagemId = p.PersonagemId AND t.NivelAmizadeId = n.NivelAmizadeId);

PRINT CONCAT('Titulos genericos criados agora: ', @@ROWCOUNT);

-- 4c) Titulos PERSONALIZADOS das personagens Lendarias e Miticas.
--     Sao escritos por cima das linhas genericas, so onde ainda nao ha
--     titulo personalizado (nao estraga alteracoes feitas a mao).
UPDATE t
SET t.Nome = v.Nome,
    t.Descricao = v.Descricao,
    t.IsPersonalizado = 1
FROM dbo.Titulos t
INNER JOIN dbo.Personagens p ON p.PersonagemId = t.PersonagemId
INNER JOIN dbo.NiveisAmizade n ON n.NivelAmizadeId = t.NivelAmizadeId
INNER JOIN (VALUES
    -- Seraphina (Lendária) - guardiã alada dos portões do amanhecer
    (N'Seraphina', 2, N'Peregrino do Amanhecer',        N'Seraphina reparou em ti nos portões do amanhecer.'),
    (N'Seraphina', 3, N'Escudeiro Alado',               N'Seraphina confia-te a guarda de um portão.'),
    (N'Seraphina', 4, N'Guardião do Amanhecer',         N'Guardas os portões lado a lado com Seraphina.'),
    (N'Seraphina', 5, N'Asa Direita de Seraphina',      N'Seraphina não voa sem ti.'),
    (N'Seraphina', 6, N'Aurora Eterna',                 N'Um laço que nem a noite consegue apagar.'),
    -- Celeste (Mítica) - a primeira estrela a ouvir um desejo
    (N'Celeste',   2, N'Olhos no Céu',                  N'Viste Celeste uma vez. Poucos podem dizê-lo.'),
    (N'Celeste',   3, N'Pedinte de Desejos',            N'Celeste ouviu o teu primeiro desejo.'),
    (N'Celeste',   4, N'Confidente das Estrelas',       N'Celeste conta-te os desejos que guarda.'),
    (N'Celeste',   5, N'Portador da Primeira Luz',      N'Celeste brilha por ti — e só por ti.'),
    (N'Celeste',   6, N'Desejo Realizado',              N'Diz-se que Celeste só aparece uma vez na vida. A ti, ficou.')
) AS v (Personagem, Ordem, Nome, Descricao)
    ON v.Personagem = p.Nome AND v.Ordem = n.Ordem
WHERE t.IsPersonalizado = 0;

PRINT CONCAT('Titulos personalizados aplicados agora: ', @@ROWCOUNT);

-- 4d) Molduras: SO as personagens Miticas dao moldura, ao nivel 5 (Melhor Amigo).
--     Sem imagens: a moldura e um anel desenhado em CSS com a cor CorHex.
INSERT INTO dbo.MoldurasPerfil (Nome, ImagemUrl, PersonagemId, NivelAmizadeId, CorHex)
SELECT N'Moldura de ' + p.Nome, NULL, p.PersonagemId, 5, r.CorHex
FROM dbo.Personagens p
INNER JOIN dbo.Raridades r ON r.RaridadeId = p.RaridadeId
WHERE r.Nome = N'Mítico'
  AND NOT EXISTS (SELECT 1 FROM dbo.MoldurasPerfil m WHERE m.PersonagemId = p.PersonagemId);

PRINT CONCAT('Molduras criadas agora: ', @@ROWCOUNT);
GO

-- ------------------------------------------------------------
-- Resumo
-- ------------------------------------------------------------
DECLARE @Emblemas INT = (SELECT COUNT(*) FROM dbo.Emblemas WHERE PersonagemId IS NOT NULL);
DECLARE @Titulos INT = (SELECT COUNT(*) FROM dbo.Titulos);
DECLARE @Personalizados INT = (SELECT COUNT(*) FROM dbo.Titulos WHERE IsPersonalizado = 1);
DECLARE @Molduras INT = (SELECT COUNT(*) FROM dbo.MoldurasPerfil WHERE PersonagemId IS NOT NULL);
PRINT CONCAT('Emblemas de personagem: ', @Emblemas);
PRINT CONCAT('Titulos: ', @Titulos, ' (personalizados: ', @Personalizados, ')');
PRINT CONCAT('Molduras de personagem: ', @Molduras);
PRINT 'Migracao04 concluida.';
GO
