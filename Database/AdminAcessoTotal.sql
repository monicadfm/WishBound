USE [WishBound];
GO
SET NOCOUNT ON;

-- ============================================================
--  AdminAcessoTotal - conta Admin com acesso a tudo
--
--  Script utilitario (nao e uma migracao): pode correr-se as vezes
--  que forem precisas, por exemplo depois de criar personagens
--  novas. Faz, para a conta 'Admin':
--
--   1) todas as personagens na colecao (1 copia de cada);
--   2) amizade no MAXIMO com todas (nivel maximo da raridade e os
--      pontos desse nivel);
--   3) todos os titulos, emblemas e molduras desbloqueados; um
--      titulo, 3 emblemas e a moldura ja equipados no perfil;
--   4) 999.999.999 Moedas, 99.999 Bilhetes e 99.999 Gemas ("ilimitado"
--      na pratica - as invocacoes continuam a ser registadas);
--   5) inventario com 100.000 lugares.
--
--  As interacoes ilimitadas sao regra de codigo: a API nao gasta
--  interacoes a contas IsAdmin (ver AmizadeController).
--  Correr DEPOIS de todas as migracoes (ate a Migracao05).
-- ============================================================

DECLARE @Admin INT = (SELECT TOP 1 UtilizadorId FROM dbo.Utilizadores WHERE NomeUtilizador = N'Admin');

IF @Admin IS NULL
BEGIN
    PRINT 'Conta Admin nao encontrada - correr primeiro Database\Usados\Autenticacao.sql.';
    RETURN;
END

-- ------------------------------------------------------------
-- 1) Todas as personagens na colecao
-- ------------------------------------------------------------
INSERT INTO dbo.ColecaoUtilizador (UtilizadorId, PersonagemId, Quantidade, IsFavorito, PontosAmizade, NivelAmizadeId, DataObtencao)
SELECT @Admin, p.PersonagemId, 1, 0, 0, 1, SYSUTCDATETIME()
FROM dbo.Personagens p
WHERE NOT EXISTS (SELECT 1 FROM dbo.ColecaoUtilizador c
                  WHERE c.UtilizadorId = @Admin AND c.PersonagemId = p.PersonagemId);

PRINT CONCAT('Personagens acrescentadas a colecao: ', @@ROWCOUNT);

-- ------------------------------------------------------------
-- 2) Amizade no maximo (nivel = ordem da raridade + 2)
-- ------------------------------------------------------------
UPDATE c
SET c.PontosAmizade = m.PontosNecessarios,
    c.NivelAmizadeId = m.NivelAmizadeId,
    c.UltimaInteracao = CAST(SYSUTCDATETIME() AS date)
FROM dbo.ColecaoUtilizador c
INNER JOIN dbo.Personagens p ON p.PersonagemId = c.PersonagemId
INNER JOIN dbo.Raridades r ON r.RaridadeId = p.RaridadeId
INNER JOIN dbo.NiveisAmizade m ON m.Ordem = r.Ordem + 2
WHERE c.UtilizadorId = @Admin;

PRINT CONCAT('Amizades no maximo: ', @@ROWCOUNT);

-- ------------------------------------------------------------
-- 3) Todas as recompensas
-- ------------------------------------------------------------
INSERT INTO dbo.TitulosUtilizador (UtilizadorId, TituloId, DataObtencao)
SELECT @Admin, t.TituloId, SYSUTCDATETIME()
FROM dbo.Titulos t
WHERE NOT EXISTS (SELECT 1 FROM dbo.TitulosUtilizador tu WHERE tu.UtilizadorId = @Admin AND tu.TituloId = t.TituloId);
PRINT CONCAT('Titulos desbloqueados: ', @@ROWCOUNT);

INSERT INTO dbo.EmblemasUtilizador (UtilizadorId, EmblemaId, DataObtencao, IsEquipado)
SELECT @Admin, e.EmblemaId, SYSUTCDATETIME(), 0
FROM dbo.Emblemas e
WHERE NOT EXISTS (SELECT 1 FROM dbo.EmblemasUtilizador eu WHERE eu.UtilizadorId = @Admin AND eu.EmblemaId = e.EmblemaId);
PRINT CONCAT('Emblemas desbloqueados: ', @@ROWCOUNT);

INSERT INTO dbo.MoldurasUtilizador (UtilizadorId, MolduraId, DataObtencao)
SELECT @Admin, m.MolduraId, SYSUTCDATETIME()
FROM dbo.MoldurasPerfil m
WHERE NOT EXISTS (SELECT 1 FROM dbo.MoldurasUtilizador mu WHERE mu.UtilizadorId = @Admin AND mu.MolduraId = m.MolduraId);
PRINT CONCAT('Molduras desbloqueadas: ', @@ROWCOUNT);

-- Equipar: o titulo colorido da personagem mais rara, 3 emblemas e a moldura
-- (so se ainda nao houver nada equipado - nao estraga escolhas feitas no site)
IF NOT EXISTS (SELECT 1 FROM dbo.EmblemasUtilizador WHERE UtilizadorId = @Admin AND IsEquipado = 1)
BEGIN
    UPDATE eu SET eu.IsEquipado = 1
    FROM dbo.EmblemasUtilizador eu
    WHERE eu.UtilizadorId = @Admin
      AND eu.EmblemaId IN (SELECT TOP 3 e.EmblemaId
                           FROM dbo.Emblemas e
                           INNER JOIN dbo.Personagens p ON p.PersonagemId = e.PersonagemId
                           INNER JOIN dbo.Raridades r ON r.RaridadeId = p.RaridadeId
                           ORDER BY r.Ordem DESC, p.Nome);
END

UPDATE u
SET u.TituloAtualId = ISNULL(u.TituloAtualId,
        (SELECT TOP 1 t.TituloId
         FROM dbo.Titulos t
         INNER JOIN dbo.Personagens p ON p.PersonagemId = t.PersonagemId
         INNER JOIN dbo.Raridades r ON r.RaridadeId = p.RaridadeId
         INNER JOIN dbo.NiveisAmizade n ON n.NivelAmizadeId = t.NivelAmizadeId
         ORDER BY n.Ordem DESC, r.Ordem DESC)),
    u.MolduraPerfilAtualId = ISNULL(u.MolduraPerfilAtualId,
        (SELECT TOP 1 m.MolduraId FROM dbo.MoldurasPerfil m ORDER BY m.MolduraId))
FROM dbo.Utilizadores u
WHERE u.UtilizadorId = @Admin;

-- ------------------------------------------------------------
-- 4) Moedas "ilimitadas"
-- ------------------------------------------------------------
INSERT INTO dbo.CarteirasUtilizador (UtilizadorId, TipoMoedaId, Saldo)
SELECT @Admin, m.TipoMoedaId, 0
FROM dbo.TiposMoeda m
WHERE NOT EXISTS (SELECT 1 FROM dbo.CarteirasUtilizador c WHERE c.UtilizadorId = @Admin AND c.TipoMoedaId = m.TipoMoedaId);

UPDATE dbo.CarteirasUtilizador SET Saldo = 999999999 WHERE UtilizadorId = @Admin AND TipoMoedaId = 2;   -- Moedas
UPDATE dbo.CarteirasUtilizador SET Saldo = 99999     WHERE UtilizadorId = @Admin AND TipoMoedaId IN (1, 3); -- Gemas, Bilhetes

-- ------------------------------------------------------------
-- 5) Inventario enorme e interacoes
-- ------------------------------------------------------------
INSERT INTO dbo.InventarioUtilizador (UtilizadorId, CapacidadeBase, CapacidadeExtra)
SELECT @Admin, 100, 0
WHERE NOT EXISTS (SELECT 1 FROM dbo.InventarioUtilizador WHERE UtilizadorId = @Admin);

UPDATE dbo.InventarioUtilizador SET CapacidadeBase = 100000 WHERE UtilizadorId = @Admin;

UPDATE dbo.Utilizadores SET InteracoesRestantes = 3, DiaInteracoes = NULL WHERE UtilizadorId = @Admin;

-- ------------------------------------------------------------
-- Resumo
-- ------------------------------------------------------------
DECLARE @Personagens INT = (SELECT COUNT(*) FROM dbo.ColecaoUtilizador WHERE UtilizadorId = @Admin);
DECLARE @Moedas DECIMAL(12,2) = (SELECT Saldo FROM dbo.CarteirasUtilizador WHERE UtilizadorId = @Admin AND TipoMoedaId = 2);
PRINT CONCAT('Admin (Id ', @Admin, '): ', @Personagens, ' personagens, ', CAST(@Moedas AS INT), ' Moedas, capacidade 100000.');
PRINT 'AdminAcessoTotal concluido.';
GO
