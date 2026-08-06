-- ============================================================
--  WishBound - Script: Autenticação (Autenticacao.sql)
--  Executar UMA VEZ no SSMS depois do Migracao01.sql.
--  (É idempotente: pode ser executado várias vezes sem duplicar nada.)
--
--  1) Corrige a restrição UNIQUE da coluna GoogleId;
--  2) Cria a conta de administrador inicial.
-- ============================================================
USE [WishBound];
GO
SET NOCOUNT ON;

-- ------------------------------------------------------------
-- 1) Correção: UNIQUE em GoogleId
--
-- PROBLEMA: a restrição UNIQUE criada com a tabela só permite
-- UM valor NULL em toda a coluna. Como as contas normais (sem
-- login Google) ficam todas com GoogleId = NULL, o SEGUNDO
-- registo de utilizador falharia com erro de chave duplicada!
--
-- SOLUÇÃO: substituir a restrição por um índice único FILTRADO,
-- que garante a unicidade apenas nos valores NÃO nulos.
-- ------------------------------------------------------------
DECLARE @NomeRestricao NVARCHAR(128);

SELECT @NomeRestricao = i.name
FROM sys.indexes i
INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE i.object_id = OBJECT_ID('dbo.Utilizadores')
  AND i.is_unique = 1
  AND i.has_filter = 0        -- só a restrição original (não filtrada)
  AND c.name = 'GoogleId';

IF @NomeRestricao IS NOT NULL
BEGIN
    -- O nome é gerado automaticamente pelo SQL Server, por isso é
    -- preciso descobri-lo e usar SQL dinâmico para o remover.
    DECLARE @Sql NVARCHAR(400);

    IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = @NomeRestricao)
        SET @Sql = N'ALTER TABLE dbo.Utilizadores DROP CONSTRAINT [' + @NomeRestricao + N'];';
    ELSE
        SET @Sql = N'DROP INDEX [' + @NomeRestricao + N'] ON dbo.Utilizadores;';

    EXEC sp_executesql @Sql;
    PRINT 'Restrição UNIQUE original de GoogleId removida (' + @NomeRestricao + ').';
END
ELSE
    PRINT 'Restrição UNIQUE original de GoogleId já não existe — nada a fazer.';

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE object_id = OBJECT_ID('dbo.Utilizadores') AND name = 'UX_Utilizadores_GoogleId')
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_Utilizadores_GoogleId
        ON dbo.Utilizadores (GoogleId)
        WHERE GoogleId IS NOT NULL;   -- índice filtrado: NULLs ilimitados
    PRINT 'Índice único filtrado UX_Utilizadores_GoogleId criado.';
END
ELSE
    PRINT 'Índice UX_Utilizadores_GoogleId já existe — nada a fazer.';

-- ------------------------------------------------------------
-- 2) Conta de administrador inicial
--
-- Credenciais para desenvolvimento/demonstração:
--   Utilizador: Admin
--   Password:   Admin123!
-- (Alterar a password depois da defesa, se o site ficar publicado.)
--
-- O hash abaixo foi gerado com o MESMO algoritmo do PasswordHasher
-- da WebAPI: PBKDF2-SHA256, 100000 iterações, formato
-- {iterações}.{sal Base64}.{hash Base64}.
-- ------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Utilizadores WHERE NomeUtilizador = N'Admin')
BEGIN
    INSERT INTO dbo.Utilizadores
        (NomeUtilizador, Email, PasswordHash, EmailValidado, IsAdmin, IsAtivo, DataCriacao)
    VALUES
        (N'Admin',
         N'admin@wishbound.local',
         N'100000.Aibz3hHobFqu9IKMez4MMQ==.mHrTX264QH2Cqzanh06GMKL+hWt+S8hpUxJmr3dTdZI=',
         1,   -- email já validado
         1,   -- administrador
         1,   -- ativo
         SYSUTCDATETIME());

    -- Inventário e carteiras do administrador (como no registo normal)
    INSERT INTO dbo.InventarioUtilizador (UtilizadorId, CapacidadeBase, CapacidadeExtra)
    SELECT u.UtilizadorId, 100, 0
    FROM dbo.Utilizadores u
    WHERE u.NomeUtilizador = N'Admin'
      AND NOT EXISTS (SELECT 1 FROM dbo.InventarioUtilizador i WHERE i.UtilizadorId = u.UtilizadorId);

    INSERT INTO dbo.CarteirasUtilizador (UtilizadorId, TipoMoedaId, Saldo)
    SELECT u.UtilizadorId, tm.TipoMoedaId, 0
    FROM dbo.Utilizadores u
    CROSS JOIN dbo.TiposMoeda tm
    WHERE u.NomeUtilizador = N'Admin'
      AND NOT EXISTS (SELECT 1 FROM dbo.CarteirasUtilizador c
                      WHERE c.UtilizadorId = u.UtilizadorId AND c.TipoMoedaId = tm.TipoMoedaId);

    PRINT 'Conta de administrador criada (Admin / Admin123!).';
END
ELSE
    PRINT 'A conta "Admin" já existe — nada a fazer.';
GO
