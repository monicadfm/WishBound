USE [WishBound];
GO
SET NOCOUNT ON;

-- ============================================================
--  Utilitario - dar 500 Moedas a TODAS as contas existentes
--
--  ATENCAO: este script NAO e idempotente, de proposito. Cada
--  execucao acrescenta mais 500 Moedas a cada conta. E para ser
--  corrido a pedido (por exemplo para testar as invocacoes, que
--  custam 10 Moedas cada).
--
--  Nota: o Migracao02.sql ja da 500 Moedas uma unica vez a cada
--  conta que ainda nao tenha o movimento "Bonus inicial". Este
--  script serve para reforcar o saldo depois disso.
--
--  Fica tudo registado em TransacoesMoeda com a origem "Bonus 500".
-- ============================================================

DECLARE @MoedasId INT = 2;              -- 1 = Gemas, 2 = Moedas
DECLARE @Montante DECIMAL(12,2) = 500;
DECLARE @Origem NVARCHAR(50) = N'Bonus 500';

BEGIN TRANSACTION;

    -- Carteiras em falta (contas criadas por script podem nao as ter)
    INSERT INTO dbo.CarteirasUtilizador (UtilizadorId, TipoMoedaId, Saldo)
    SELECT u.UtilizadorId, m.TipoMoedaId, 0
    FROM dbo.Utilizadores u
    CROSS JOIN dbo.TiposMoeda m
    WHERE NOT EXISTS (SELECT 1 FROM dbo.CarteirasUtilizador c
                      WHERE c.UtilizadorId = u.UtilizadorId AND c.TipoMoedaId = m.TipoMoedaId);

    -- Credita as Moedas
    UPDATE dbo.CarteirasUtilizador
       SET Saldo = Saldo + @Montante
     WHERE TipoMoedaId = @MoedasId;

    DECLARE @Contas INT = @@ROWCOUNT;

    -- Regista o movimento de cada conta
    INSERT INTO dbo.TransacoesMoeda (UtilizadorId, TipoMoedaId, Montante, TipoTransacao, Origem, DataCriacao)
    SELECT UtilizadorId, @MoedasId, @Montante, N'Ganho', @Origem, SYSUTCDATETIME()
    FROM dbo.CarteirasUtilizador
    WHERE TipoMoedaId = @MoedasId;

COMMIT;

PRINT CONCAT('Contas creditadas com 500 Moedas: ', @Contas);
GO

-- Saldos depois do reforco
SELECT u.UtilizadorId,
       u.NomeUtilizador,
       c.Saldo AS Moedas
FROM dbo.Utilizadores u
INNER JOIN dbo.CarteirasUtilizador c
        ON c.UtilizadorId = u.UtilizadorId AND c.TipoMoedaId = 2
ORDER BY u.UtilizadorId;
GO
