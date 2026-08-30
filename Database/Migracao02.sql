USE [WishBound];
GO
SET NOCOUNT ON;

-- ============================================================
--  Migracao02 - Garantias (pity) e moeda inicial
--
--  1) Acrescenta o contador ContadorEpico a PityUtilizador
--     (garantia de Epica a cada 10 invocacoes). A tabela ja tinha
--     o ContadorAtual, usado para a garantia dos 90.
--
--  2) Garante que todos os utilizadores tem carteira de cada moeda
--     e oferece 500 Moedas a cada conta ja existente, para poderem
--     invocar (cada invocacao custa 10 Moedas). As contas novas ja
--     nascem com 500 - ver ContaController.
--
--  Script idempotente: pode ser executado mais do que uma vez.
--  O bonus so e dado a quem ainda nao tiver o movimento
--  "Bonus inicial" registado em TransacoesMoeda.
-- ============================================================

-- ------------------------------------------------------------
-- 1) Contador da garantia de Epica
-- ------------------------------------------------------------
IF COL_LENGTH('dbo.PityUtilizador', 'ContadorEpico') IS NULL
BEGIN
    ALTER TABLE dbo.PityUtilizador
        ADD ContadorEpico INT NOT NULL
            CONSTRAINT DF_PityUtilizador_ContadorEpico DEFAULT (0);

    PRINT 'Coluna ContadorEpico acrescentada a PityUtilizador.';
END
ELSE
    PRINT 'Coluna ContadorEpico ja existe - nada a fazer.';
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
-- 3) Bonus inicial de 500 Moedas (TipoMoedaId = 2)
-- ------------------------------------------------------------
DECLARE @MoedasId INT = 2;          -- 1 = Gemas, 2 = Moedas
DECLARE @Bonus DECIMAL(12,2) = 500;
DECLARE @Origem NVARCHAR(50) = N'Bonus inicial';

-- Quem ainda nao recebeu o bonus
DECLARE @PorReceber TABLE (UtilizadorId INT PRIMARY KEY);

INSERT INTO @PorReceber (UtilizadorId)
SELECT u.UtilizadorId
FROM dbo.Utilizadores u
WHERE NOT EXISTS (SELECT 1 FROM dbo.TransacoesMoeda t
                  WHERE t.UtilizadorId = u.UtilizadorId
                    AND t.TipoMoedaId = @MoedasId
                    AND t.Origem = @Origem);

BEGIN TRANSACTION;

    UPDATE c
       SET c.Saldo = c.Saldo + @Bonus
      FROM dbo.CarteirasUtilizador c
     INNER JOIN @PorReceber p ON p.UtilizadorId = c.UtilizadorId
     WHERE c.TipoMoedaId = @MoedasId;

    INSERT INTO dbo.TransacoesMoeda (UtilizadorId, TipoMoedaId, Montante, TipoTransacao, Origem, DataCriacao)
    SELECT p.UtilizadorId, @MoedasId, @Bonus, N'Ganho', @Origem, SYSUTCDATETIME()
    FROM @PorReceber p;

COMMIT;

PRINT CONCAT('Contas que receberam as 500 Moedas: ', (SELECT COUNT(*) FROM @PorReceber));
GO

-- ------------------------------------------------------------
-- Resumo
-- ------------------------------------------------------------
DECLARE @Linhas INT = (SELECT COUNT(*) FROM dbo.PityUtilizador);
DECLARE @SaldoTotal DECIMAL(12,2) = (SELECT ISNULL(SUM(Saldo), 0) FROM dbo.CarteirasUtilizador WHERE TipoMoedaId = 2);
PRINT CONCAT('Linhas em PityUtilizador: ', @Linhas);
PRINT CONCAT('Total de Moedas na plataforma: ', @SaldoTotal);
PRINT 'Migracao02 concluida.';
GO
