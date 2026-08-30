USE [WishBound];
GO

-- ============================================================
--  Diagnostico + correcao da coluna ContadorEpico
--
--  COMO CORRER (importante):
--   1. No SSMS, ligar a instancia  .\SQLEXPRESS  (a mesma da API);
--   2. Abrir este ficheiro e NAO deixar nada selecionado no editor
--      (com texto selecionado, o F5 executa APENAS a selecao);
--   3. F5 e ver o separador "Results" (as tabelas em baixo) e o
--      separador "Messages".
-- ============================================================

-- 1) Onde e que estamos mesmo ligadas?
SELECT
    @@SERVERNAME                                     AS Servidor,
    DB_NAME()                                        AS BaseDados,
    CASE WHEN OBJECT_ID('dbo.PityUtilizador') IS NULL
         THEN 'NAO EXISTE' ELSE 'existe' END          AS Tabela_PityUtilizador,
    CASE WHEN COL_LENGTH('dbo.PityUtilizador', 'ContadorEpico') IS NULL
         THEN 'EM FALTA' ELSE 'ja existe' END         AS Coluna_ContadorEpico;
GO

-- 2) Acrescenta a coluna se faltar
IF OBJECT_ID('dbo.PityUtilizador') IS NULL
BEGIN
    RAISERROR('A tabela dbo.PityUtilizador nao existe nesta base de dados. Esta ligada a base de dados certa (WishBound em .\SQLEXPRESS)?', 16, 1);
END
ELSE IF COL_LENGTH('dbo.PityUtilizador', 'ContadorEpico') IS NULL
BEGIN
    ALTER TABLE dbo.PityUtilizador
        ADD ContadorEpico INT NOT NULL
            CONSTRAINT DF_PityUtilizador_ContadorEpico DEFAULT (0);

    PRINT 'Coluna ContadorEpico acrescentada.';
END
ELSE
    PRINT 'Coluna ContadorEpico ja existia.';
GO

-- 3) Confirmacao final: colunas da tabela
SELECT c.name AS Coluna, t.name AS Tipo, c.is_nullable AS Aceita_Nulos
FROM sys.columns c
INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
WHERE c.object_id = OBJECT_ID('dbo.PityUtilizador')
ORDER BY c.column_id;
GO
