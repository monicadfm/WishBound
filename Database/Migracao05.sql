USE [WishBound];
GO
SET NOCOUNT ON;

-- ============================================================
--  Migracao05 - Amizade: 7 niveis, maximo por raridade e
--               recompensas por nivel (substitui as regras da 04)
--
--  NIVEIS (tabela NiveisAmizade, 7 linhas):
--     1 Desconhecido    0
--     2 Conhecido     100
--     3 Melhor Amigo  300
--     4 Confidente    700
--     5 Inseparavel  1500
--     6 Laco Especial 3000
--     7 Alma Gemea   5000
--
--  NIVEL MAXIMO conforme a raridade da personagem (Ordem + 2):
--     Comum 3 - Raro 4 - Epico 5 - Lendario 6 - Mitico 7
--  (os pontos ficam limitados ao limiar do nivel maximo)
--
--  RECOMPENSAS:
--     nivel 3 - titulo "Melhor Amigo de X" (todas as personagens)
--     nivel 4 - notificacoes personalizadas da personagem (Raro+)
--     nivel 5 - emblema da personagem, equipavel no perfil, ate 3 (Epico+)
--     nivel 6 - titulo unico e colorido (cor da raridade) (Lendario+)
--     nivel 7 - moldura de perfil (so Mitico)
--
--  O script limpa as sementes da Migracao04 (titulos em todos os
--  niveis, emblemas ao nivel 3, molduras ao nivel 5) e recalcula os
--  niveis de toda a gente. Idempotente. Correr DEPOIS da Migracao04.
-- ============================================================

-- ------------------------------------------------------------
-- 1) Colunas novas
-- ------------------------------------------------------------
IF COL_LENGTH('dbo.Titulos', 'CorHex') IS NULL
BEGIN
    ALTER TABLE dbo.Titulos ADD CorHex NVARCHAR(7) NULL;
    PRINT 'Coluna CorHex acrescentada a Titulos.';
END

IF COL_LENGTH('dbo.EmblemasUtilizador', 'IsEquipado') IS NULL
BEGIN
    ALTER TABLE dbo.EmblemasUtilizador ADD IsEquipado BIT NOT NULL
        CONSTRAINT DF_EmblemasUtilizador_IsEquipado DEFAULT (0);
    PRINT 'Coluna IsEquipado acrescentada a EmblemasUtilizador.';
END
GO

-- ------------------------------------------------------------
-- 2) Os 7 niveis
-- ------------------------------------------------------------
-- Nome tem UNIQUE: "Melhor Amigo" passa do nivel 5 para o 3, por isso
-- primeiro renomeia-se tudo para nomes temporarios e so depois os finais.
UPDATE dbo.NiveisAmizade SET Nome = N'~' + CAST(NivelAmizadeId AS NVARCHAR(10)) WHERE NivelAmizadeId BETWEEN 1 AND 7;

UPDATE dbo.NiveisAmizade SET Nome = N'Desconhecido',  PontosNecessarios = 0,    Ordem = 1 WHERE NivelAmizadeId = 1;
UPDATE dbo.NiveisAmizade SET Nome = N'Conhecido',     PontosNecessarios = 100,  Ordem = 2 WHERE NivelAmizadeId = 2;
UPDATE dbo.NiveisAmizade SET Nome = N'Melhor Amigo',  PontosNecessarios = 300,  Ordem = 3 WHERE NivelAmizadeId = 3;
UPDATE dbo.NiveisAmizade SET Nome = N'Confidente',    PontosNecessarios = 700,  Ordem = 4 WHERE NivelAmizadeId = 4;
UPDATE dbo.NiveisAmizade SET Nome = N'Inseparável',   PontosNecessarios = 1500, Ordem = 5 WHERE NivelAmizadeId = 5;
UPDATE dbo.NiveisAmizade SET Nome = N'Laço Especial', PontosNecessarios = 3000, Ordem = 6 WHERE NivelAmizadeId = 6;

IF NOT EXISTS (SELECT 1 FROM dbo.NiveisAmizade WHERE NivelAmizadeId = 7)
BEGIN
    SET IDENTITY_INSERT dbo.NiveisAmizade ON;
    INSERT INTO dbo.NiveisAmizade (NivelAmizadeId, Nome, PontosNecessarios, Ordem)
    VALUES (7, N'Alma Gémea', 5000, 7);
    SET IDENTITY_INSERT dbo.NiveisAmizade OFF;
    PRINT 'Nivel 7 (Alma Gemea) criado.';
END
ELSE
    UPDATE dbo.NiveisAmizade SET Nome = N'Alma Gémea', PontosNecessarios = 5000, Ordem = 7 WHERE NivelAmizadeId = 7;
GO

-- ------------------------------------------------------------
-- 3) Titulos: so nivel 3 (generico) e nivel 6 (unico, colorido)
-- ------------------------------------------------------------
-- Titulos que deixam de existir (niveis 2, 4, 5 e o 6 das personagens
-- abaixo de Lendario): tirar do perfil, dos utilizadores e apagar.
;WITH ApagarTitulos AS (
    SELECT t.TituloId
    FROM dbo.Titulos t
    INNER JOIN dbo.NiveisAmizade n ON n.NivelAmizadeId = t.NivelAmizadeId
    LEFT JOIN dbo.Personagens p ON p.PersonagemId = t.PersonagemId
    LEFT JOIN dbo.Raridades r ON r.RaridadeId = p.RaridadeId
    WHERE t.PersonagemId IS NOT NULL
      AND (n.Ordem NOT IN (3, 6) OR (n.Ordem = 6 AND ISNULL(r.Ordem, 0) < 4))
)
UPDATE dbo.Utilizadores SET TituloAtualId = NULL
WHERE TituloAtualId IN (SELECT TituloId FROM ApagarTitulos);

;WITH ApagarTitulos AS (
    SELECT t.TituloId
    FROM dbo.Titulos t
    INNER JOIN dbo.NiveisAmizade n ON n.NivelAmizadeId = t.NivelAmizadeId
    LEFT JOIN dbo.Personagens p ON p.PersonagemId = t.PersonagemId
    LEFT JOIN dbo.Raridades r ON r.RaridadeId = p.RaridadeId
    WHERE t.PersonagemId IS NOT NULL
      AND (n.Ordem NOT IN (3, 6) OR (n.Ordem = 6 AND ISNULL(r.Ordem, 0) < 4))
)
DELETE tu FROM dbo.TitulosUtilizador tu
WHERE tu.TituloId IN (SELECT TituloId FROM ApagarTitulos);

DELETE t
FROM dbo.Titulos t
INNER JOIN dbo.NiveisAmizade n ON n.NivelAmizadeId = t.NivelAmizadeId
LEFT JOIN dbo.Personagens p ON p.PersonagemId = t.PersonagemId
LEFT JOIN dbo.Raridades r ON r.RaridadeId = p.RaridadeId
WHERE t.PersonagemId IS NOT NULL
  AND (n.Ordem NOT IN (3, 6) OR (n.Ordem = 6 AND ISNULL(r.Ordem, 0) < 4));

PRINT CONCAT('Titulos removidos: ', @@ROWCOUNT);

-- Nivel 3: "Melhor Amigo de X" para TODAS as personagens (cria ou renomeia)
INSERT INTO dbo.Titulos (Nome, Descricao, PersonagemId, NivelAmizadeId, IsPersonalizado, CorHex)
SELECT N'Melhor Amigo de ' + p.Nome, N'Chegaste a Melhor Amigo de ' + p.Nome + N'.', p.PersonagemId, 3, 0, NULL
FROM dbo.Personagens p
WHERE NOT EXISTS (SELECT 1 FROM dbo.Titulos t WHERE t.PersonagemId = p.PersonagemId AND t.NivelAmizadeId = 3);

UPDATE t SET t.Nome = N'Melhor Amigo de ' + p.Nome,
             t.Descricao = N'Chegaste a Melhor Amigo de ' + p.Nome + N'.',
             t.IsPersonalizado = 0, t.CorHex = NULL
FROM dbo.Titulos t
INNER JOIN dbo.Personagens p ON p.PersonagemId = t.PersonagemId
WHERE t.NivelAmizadeId = 3;

-- Nivel 6: titulo unico e colorido para Lendarias e Miticas (cor = raridade)
INSERT INTO dbo.Titulos (Nome, Descricao, PersonagemId, NivelAmizadeId, IsPersonalizado, CorHex)
SELECT N'Laço Especial com ' + p.Nome, N'Um laço especial com ' + p.Nome + N'.', p.PersonagemId, 6, 1, r.CorHex
FROM dbo.Personagens p
INNER JOIN dbo.Raridades r ON r.RaridadeId = p.RaridadeId
WHERE r.Ordem >= 4
  AND NOT EXISTS (SELECT 1 FROM dbo.Titulos t WHERE t.PersonagemId = p.PersonagemId AND t.NivelAmizadeId = 6);

-- Nomes proprios das personagens Lendarias/Miticas que ja existem
UPDATE t SET t.Nome = v.Nome, t.Descricao = v.Descricao, t.IsPersonalizado = 1, t.CorHex = r.CorHex
FROM dbo.Titulos t
INNER JOIN dbo.Personagens p ON p.PersonagemId = t.PersonagemId
INNER JOIN dbo.Raridades r ON r.RaridadeId = p.RaridadeId
INNER JOIN (VALUES
    (N'Seraphina', N'Aurora Eterna',    N'Um laço com Seraphina que nem a noite consegue apagar.'),
    (N'Celeste',   N'Desejo Realizado', N'Diz-se que Celeste só aparece uma vez na vida. A ti, ficou.')
) AS v (Personagem, Nome, Descricao) ON v.Personagem = p.Nome
WHERE t.NivelAmizadeId = 6;

-- Cor de qualquer titulo de nivel 6 que ainda nao a tenha
UPDATE t SET t.CorHex = r.CorHex
FROM dbo.Titulos t
INNER JOIN dbo.Personagens p ON p.PersonagemId = t.PersonagemId
INNER JOIN dbo.Raridades r ON r.RaridadeId = p.RaridadeId
WHERE t.NivelAmizadeId = 6 AND t.CorHex IS NULL;
GO

-- ------------------------------------------------------------
-- 4) Emblemas passam para o nivel 5 (so Epico+ la chega)
-- ------------------------------------------------------------
UPDATE dbo.Emblemas SET NivelAmizadeId = 5,
    Descricao = N'Chegaste a Inseparável de ' + p.Nome + N'. Podes equipar este rebento no teu perfil (até 3).'
FROM dbo.Emblemas e
INNER JOIN dbo.Personagens p ON p.PersonagemId = e.PersonagemId;

-- Emblemas dados pela regra antiga (nivel 3) a quem nao chega ao 5: retirar
DELETE eu
FROM dbo.EmblemasUtilizador eu
INNER JOIN dbo.Emblemas e ON e.EmblemaId = eu.EmblemaId
INNER JOIN dbo.Personagens p ON p.PersonagemId = e.PersonagemId
INNER JOIN dbo.Raridades r ON r.RaridadeId = p.RaridadeId
WHERE r.Ordem + 2 < 5;

PRINT CONCAT('Emblemas retirados (regra antiga): ', @@ROWCOUNT);

-- ------------------------------------------------------------
-- 5) Molduras passam para o nivel 7 (so Mitico la chega)
-- ------------------------------------------------------------
UPDATE dbo.MoldurasPerfil SET NivelAmizadeId = 7 WHERE PersonagemId IS NOT NULL;
GO

-- ------------------------------------------------------------
-- 6) Recalcular pontos (limitados ao maximo) e niveis de toda a gente
-- ------------------------------------------------------------
UPDATE c
SET c.PontosAmizade = CASE WHEN c.PontosAmizade > m.PontosNecessarios THEN m.PontosNecessarios ELSE c.PontosAmizade END
FROM dbo.ColecaoUtilizador c
INNER JOIN dbo.Personagens p ON p.PersonagemId = c.PersonagemId
INNER JOIN dbo.Raridades r ON r.RaridadeId = p.RaridadeId
INNER JOIN dbo.NiveisAmizade m ON m.Ordem = r.Ordem + 2;

UPDATE c
SET c.NivelAmizadeId = n.NivelAmizadeId
FROM dbo.ColecaoUtilizador c
INNER JOIN dbo.Personagens p ON p.PersonagemId = c.PersonagemId
INNER JOIN dbo.Raridades r ON r.RaridadeId = p.RaridadeId
CROSS APPLY (SELECT TOP 1 NivelAmizadeId
             FROM dbo.NiveisAmizade
             WHERE PontosNecessarios <= c.PontosAmizade AND Ordem <= r.Ordem + 2
             ORDER BY Ordem DESC) n;

PRINT CONCAT('Linhas da colecao recalculadas: ', @@ROWCOUNT);

-- Recompensas ja merecidas com as regras novas (INSERT idempotente):
-- titulos de nivel 3 e 6, emblemas de nivel 5, molduras de nivel 7.
INSERT INTO dbo.TitulosUtilizador (UtilizadorId, TituloId, DataObtencao)
SELECT c.UtilizadorId, t.TituloId, SYSUTCDATETIME()
FROM dbo.ColecaoUtilizador c
INNER JOIN dbo.NiveisAmizade n ON n.NivelAmizadeId = c.NivelAmizadeId
INNER JOIN dbo.Titulos t ON t.PersonagemId = c.PersonagemId
INNER JOIN dbo.NiveisAmizade nt ON nt.NivelAmizadeId = t.NivelAmizadeId
WHERE nt.Ordem <= n.Ordem
  AND NOT EXISTS (SELECT 1 FROM dbo.TitulosUtilizador tu WHERE tu.UtilizadorId = c.UtilizadorId AND tu.TituloId = t.TituloId);

INSERT INTO dbo.EmblemasUtilizador (UtilizadorId, EmblemaId, DataObtencao, IsEquipado)
SELECT c.UtilizadorId, e.EmblemaId, SYSUTCDATETIME(), 0
FROM dbo.ColecaoUtilizador c
INNER JOIN dbo.NiveisAmizade n ON n.NivelAmizadeId = c.NivelAmizadeId
INNER JOIN dbo.Emblemas e ON e.PersonagemId = c.PersonagemId
INNER JOIN dbo.NiveisAmizade ne ON ne.NivelAmizadeId = e.NivelAmizadeId
WHERE ne.Ordem <= n.Ordem
  AND NOT EXISTS (SELECT 1 FROM dbo.EmblemasUtilizador eu WHERE eu.UtilizadorId = c.UtilizadorId AND eu.EmblemaId = e.EmblemaId);

INSERT INTO dbo.MoldurasUtilizador (UtilizadorId, MolduraId, DataObtencao)
SELECT c.UtilizadorId, m.MolduraId, SYSUTCDATETIME()
FROM dbo.ColecaoUtilizador c
INNER JOIN dbo.NiveisAmizade n ON n.NivelAmizadeId = c.NivelAmizadeId
INNER JOIN dbo.MoldurasPerfil m ON m.PersonagemId = c.PersonagemId
INNER JOIN dbo.NiveisAmizade nm ON nm.NivelAmizadeId = m.NivelAmizadeId
WHERE nm.Ordem <= n.Ordem
  AND NOT EXISTS (SELECT 1 FROM dbo.MoldurasUtilizador mu WHERE mu.UtilizadorId = c.UtilizadorId AND mu.MolduraId = m.MolduraId);
GO

-- ------------------------------------------------------------
-- Resumo
-- ------------------------------------------------------------
DECLARE @Niveis INT = (SELECT COUNT(*) FROM dbo.NiveisAmizade);
DECLARE @Titulos3 INT = (SELECT COUNT(*) FROM dbo.Titulos WHERE NivelAmizadeId = 3);
DECLARE @Titulos6 INT = (SELECT COUNT(*) FROM dbo.Titulos WHERE NivelAmizadeId = 6);
DECLARE @Emblemas INT = (SELECT COUNT(*) FROM dbo.Emblemas WHERE NivelAmizadeId = 5);
DECLARE @Molduras INT = (SELECT COUNT(*) FROM dbo.MoldurasPerfil WHERE NivelAmizadeId = 7);
PRINT CONCAT('Niveis: ', @Niveis, ' | Titulos nivel 3: ', @Titulos3, ' | Titulos nivel 6: ', @Titulos6,
             ' | Emblemas (nivel 5): ', @Emblemas, ' | Molduras (nivel 7): ', @Molduras);
PRINT 'Migracao05 concluida.';
GO
