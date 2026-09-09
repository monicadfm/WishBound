USE [WishBound];
GO
SET NOCOUNT ON;

-- ============================================================
--  Migracao06 - Administracao de contas (registo de acoes)
--
--  A tabela LogsAdministrador ja existia no esquema original
--  (AdminId, Acao, TabelaAlvo, RegistoAlvoId, DataCriacao) mas nunca
--  tinha sido usada. Passa a ser o REGISTO DE ACOES da area de
--  administracao: cada coisa que um administrador faz a uma conta
--  (ativar/desativar, promover, dar ou tirar moeda, personagens,
--  titulos, emblemas, molduras, pontos de amizade, repor password)
--  fica aqui, com quem fez, a quem, o que e porque.
--
--  Acrescenta:
--   - UtilizadorAlvoId : a conta sobre a qual a acao foi feita (FK);
--   - Detalhes         : texto livre com o motivo e os valores
--                        ("+100 Moedas (saldo 600). Motivo: ...").
--  RegistoAlvoId continua a guardar o Id do registo tocado
--  (PersonagemId, TituloId, TipoMoedaId, ...), e TabelaAlvo a tabela.
--
--  Script idempotente: pode correr-se mais do que uma vez.
--  Ordem: CriacaoBaseDados -> Migracao01 -> Usados\Autenticacao ->
--         Migracao02 -> 03 -> 04 -> 05 -> Migracao06 (este).
-- ============================================================

-- ------------------------------------------------------------
-- 1) Colunas novas em LogsAdministrador
-- ------------------------------------------------------------
IF COL_LENGTH('dbo.LogsAdministrador', 'UtilizadorAlvoId') IS NULL
BEGIN
    ALTER TABLE dbo.LogsAdministrador ADD UtilizadorAlvoId INT NULL;
    PRINT 'Coluna LogsAdministrador.UtilizadorAlvoId criada.';
END
ELSE
    PRINT 'Coluna LogsAdministrador.UtilizadorAlvoId ja existia.';

IF COL_LENGTH('dbo.LogsAdministrador', 'Detalhes') IS NULL
BEGIN
    ALTER TABLE dbo.LogsAdministrador ADD Detalhes NVARCHAR(500) NULL;
    PRINT 'Coluna LogsAdministrador.Detalhes criada.';
END
ELSE
    PRINT 'Coluna LogsAdministrador.Detalhes ja existia.';
GO

-- ------------------------------------------------------------
-- 2) Chave estrangeira para a conta alvo (sem cascade: um log nunca
--    apaga contas; e as contas nao se apagam, desativam-se)
-- ------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_LogsAdministrador_UtilizadorAlvo')
BEGIN
    ALTER TABLE dbo.LogsAdministrador WITH CHECK
        ADD CONSTRAINT FK_LogsAdministrador_UtilizadorAlvo
        FOREIGN KEY (UtilizadorAlvoId) REFERENCES dbo.Utilizadores (UtilizadorId);
    PRINT 'FK_LogsAdministrador_UtilizadorAlvo criada.';
END
ELSE
    PRINT 'FK_LogsAdministrador_UtilizadorAlvo ja existia.';
GO

-- ------------------------------------------------------------
-- 3) Indices para as duas consultas do painel: "o que foi feito a
--    esta conta" e "o que fez este administrador", mais recentes primeiro
-- ------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LogsAdministrador_Alvo_Data' AND object_id = OBJECT_ID('dbo.LogsAdministrador'))
BEGIN
    CREATE INDEX IX_LogsAdministrador_Alvo_Data ON dbo.LogsAdministrador (UtilizadorAlvoId, DataCriacao DESC);
    PRINT 'Indice IX_LogsAdministrador_Alvo_Data criado.';
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LogsAdministrador_Admin_Data' AND object_id = OBJECT_ID('dbo.LogsAdministrador'))
BEGIN
    CREATE INDEX IX_LogsAdministrador_Admin_Data ON dbo.LogsAdministrador (AdminId, DataCriacao DESC);
    PRINT 'Indice IX_LogsAdministrador_Admin_Data criado.';
END
GO

-- ------------------------------------------------------------
-- 4) Indice para a pesquisa de contas no painel (nome / email)
-- ------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Utilizadores_Nome' AND object_id = OBJECT_ID('dbo.Utilizadores'))
BEGIN
    CREATE INDEX IX_Utilizadores_Nome ON dbo.Utilizadores (NomeUtilizador);
    PRINT 'Indice IX_Utilizadores_Nome criado.';
END
GO

PRINT 'Migracao06 concluida.';
GO
