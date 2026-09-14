USE [WishBound];
GO
SET NOCOUNT ON;

-- ============================================================
--  Migracao08 - Eventos e banners: banner temporario com RATE-UP,
--               personagens EXCLUSIVAS e garantia 50/50 na Mitica
--
--  1) Tres personagens novas, exclusivas do evento (nao entram no
--     Banner Permanente; so podem ser invocadas enquanto o banner
--     delas estiver a decorrer):
--        9  Ondina  - Lendario
--       10  Ezra    - Lendario
--       11  Vesper  - Mitico
--
--  2) Banner de evento "Estrela do Crepusculo" (30 dias a contar do
--     momento em que este script e executado). A pool e:
--       - as mesmas Comuns, Raras e Epicas do banner permanente;
--       - Lendarias: Seraphina + Ondina + Ezra;
--       - Miticas:   Celeste + Vesper.
--     As probabilidades POR RARIDADE sao as da tabela Raridades (nao
--     mudam). O que muda e a escolha DENTRO da raridade:
--       - Lendario (2,5%): 80% desses 2,5% saem as duas RATE-UP
--         (Ondina/Ezra, ao acaso entre as duas) e 20% as restantes
--         (Seraphina);
--       - Mitico (0,5%): 50/50 entre a personagem do banner (Vesper)
--         e as do banner permanente (Celeste). Quem PERDE o 50/50 tem
--         a proxima Mitica GARANTIDA como a do banner.
--     A percentagem do rate-up vive em BannerPersonagens.ProbabilidadeExtra
--     (0.80 nas Lendarias, 0.50 na Mitica) e o RateUp = 1 marca quem
--     esta em destaque - a API le daqui, nada esta fixo no codigo.
--
--  3) PityUtilizador.GarantiaRateUp: a "moeda" do 50/50 - fica a 1
--     quando o utilizador perde o 50/50 nesse banner e volta a 0
--     quando recebe a Mitica do banner.
--
--  4) Recompensas diarias do evento (7 dias, em Bilhetes) para o
--     evento aparecer tambem na Carteira, como o Festival de Boas-Vindas.
--
--  5) Sementes das personagens novas: emblema, titulos personalizados,
--     moldura (so a Mitica) e conjuntos de mensagens (Lendario 8 linhas,
--     Mitico 10).
--
--  Script idempotente. Ordem: CriacaoBaseDados -> Migracao01 ->
--  Usados\Autenticacao -> Migracao02 -> 03 -> 04 -> 05 -> 06 -> 07 -> 08 (este).
-- ============================================================

-- ------------------------------------------------------------
-- 1) Personagens novas (Ids fixos 9, 10 e 11)
-- ------------------------------------------------------------
SET IDENTITY_INSERT dbo.Personagens ON;

INSERT INTO dbo.Personagens (PersonagemId, Nome, Descricao, RaridadeId, ImagemUrl, IsAtivo, DataCriacao)
SELECT v.PersonagemId, v.Nome, v.Descricao, r.RaridadeId, v.ImagemUrl, 1, SYSUTCDATETIME()
FROM (VALUES
    (9,  N'Ondina', N'Senhora das marés serenas. Canta para os barcos perdidos até encontrarem o caminho de volta.',                       N'Lendário', N'/img/personagens/ondina.svg'),
    (10, N'Ezra',   N'Cartógrafo dos céus. Desenha constelações que ainda não nasceram e espera pacientemente por elas.',                 N'Lendário', N'/img/personagens/ezra.svg'),
    (11, N'Vesper', N'A última estrela do crepúsculo. Ouve os desejos que ninguém se atreve a dizer em voz alta.',                        N'Mítico',   N'/img/personagens/vesper.svg')
) AS v (PersonagemId, Nome, Descricao, RaridadeNome, ImagemUrl)
INNER JOIN dbo.Raridades r ON r.Nome = v.RaridadeNome
WHERE NOT EXISTS (SELECT 1 FROM dbo.Personagens p WHERE p.PersonagemId = v.PersonagemId OR p.Nome = v.Nome);

PRINT CONCAT('Personagens novas criadas agora: ', @@ROWCOUNT);

SET IDENTITY_INSERT dbo.Personagens OFF;
GO

-- As exclusivas NAO entram no Banner Permanente. Se algum script antigo
-- as tiver associado (Migracao01/03 corridas depois desta), tira-as.
DELETE FROM dbo.BannerPersonagens
WHERE BannerId = 1 AND PersonagemId IN (9, 10, 11);
GO

-- ------------------------------------------------------------
-- 2) Coluna do 50/50 nos contadores de garantia
-- ------------------------------------------------------------
IF COL_LENGTH('dbo.PityUtilizador', 'GarantiaRateUp') IS NULL
BEGIN
    ALTER TABLE dbo.PityUtilizador
        ADD GarantiaRateUp BIT NOT NULL
            CONSTRAINT DF_PityUtilizador_GarantiaRateUp DEFAULT (0);
    PRINT 'Coluna PityUtilizador.GarantiaRateUp criada.';
END
ELSE
    PRINT 'Coluna PityUtilizador.GarantiaRateUp ja existia.';
GO

-- ------------------------------------------------------------
-- 3) Banner de evento com rate-up (30 dias a partir de agora)
-- ------------------------------------------------------------
DECLARE @NomeBanner NVARCHAR(100) = N'Estrela do Crepúsculo';
DECLARE @BannerId INT = (SELECT TOP 1 BannerId FROM dbo.Banners WHERE Nome = @NomeBanner);

IF @BannerId IS NULL
BEGIN
    DECLARE @Inicio DATETIME2(7) = SYSUTCDATETIME();

    INSERT INTO dbo.Banners (Nome, Descricao, TipoBanner, ImagemUrl, DataInicio, DataFim, IsAtivo)
    VALUES (@NomeBanner,
            N'Vesper, a última estrela do crepúsculo, desce ao WishBound por 30 dias — acompanhada por Ondina e Ezra. As três só podem ser invocadas neste banner. Lendárias: 80% das vezes saem Ondina ou Ezra. Mítica: 50/50 entre Vesper e Celeste — se perderes, a próxima Mítica é Vesper garantida.',
            N'Evento', N'/img/personagens/vesper.svg', @Inicio, DATEADD(DAY, 30, @Inicio), 1);
            -- TipoBanner: a BD so aceita 'Standard' ou 'Evento' (CHECK)

    SET @BannerId = SCOPE_IDENTITY();
    PRINT CONCAT('Banner "Estrela do Crepusculo" criado (BannerId = ', @BannerId, '), a decorrer ate ', CONVERT(NVARCHAR(16), DATEADD(DAY, 30, @Inicio), 120), ' UTC.');
END
ELSE
BEGIN
    PRINT CONCAT('Banner "Estrela do Crepusculo" ja existe (BannerId = ', @BannerId, ').');

    -- Quem correu a primeira versao deste script (14 dias) fica com os 30
    -- dias: prolonga o fim para inicio + 30 dias se estiver mais curto.
    UPDATE dbo.Banners
    SET DataFim = DATEADD(DAY, 30, DataInicio),
        Descricao = REPLACE(Descricao, N'por 14 dias', N'por 30 dias')
    WHERE BannerId = @BannerId AND DataFim < DATEADD(DAY, 30, DataInicio);

    IF @@ROWCOUNT > 0 PRINT 'Banner prolongado para 30 dias.';
END

-- Pool do banner: Comuns/Raras/Epicas do permanente + TODAS as Lendarias
-- e Miticas (incluindo as exclusivas). RateUp/ProbabilidadeExtra marcam
-- o destaque: 0.80 = 80% das Lendarias sao Ondina/Ezra; 0.50 = 50/50 na Mitica.
INSERT INTO dbo.BannerPersonagens (BannerId, PersonagemId, RateUp, ProbabilidadeExtra)
SELECT @BannerId, p.PersonagemId,
       CASE WHEN p.PersonagemId IN (9, 10, 11) THEN 1 ELSE 0 END,
       CASE WHEN p.PersonagemId IN (9, 10) THEN CAST(0.8000 AS DECIMAL(6,4))
            WHEN p.PersonagemId = 11        THEN CAST(0.5000 AS DECIMAL(6,4))
            ELSE NULL END
FROM dbo.Personagens p
INNER JOIN dbo.Raridades r ON r.RaridadeId = p.RaridadeId
WHERE p.IsAtivo = 1
  AND (r.Ordem >= 4                                                        -- todas as Lendarias e Miticas
       OR EXISTS (SELECT 1 FROM dbo.BannerPersonagens bp1                  -- + o resto do permanente
                  WHERE bp1.BannerId = 1 AND bp1.PersonagemId = p.PersonagemId))
  AND NOT EXISTS (SELECT 1 FROM dbo.BannerPersonagens bp
                  WHERE bp.BannerId = @BannerId AND bp.PersonagemId = p.PersonagemId);

PRINT CONCAT('Personagens ligadas ao banner agora: ', @@ROWCOUNT);

-- Garante o rate-up mesmo que as linhas ja existissem de uma versao anterior
UPDATE dbo.BannerPersonagens SET RateUp = 1, ProbabilidadeExtra = 0.8000 WHERE BannerId = @BannerId AND PersonagemId IN (9, 10);
UPDATE dbo.BannerPersonagens SET RateUp = 1, ProbabilidadeExtra = 0.5000 WHERE BannerId = @BannerId AND PersonagemId = 11;

-- Recompensas diarias do evento (7 dias em Bilhetes = 12 invocacoes),
-- para aparecer na Carteira ao lado do calendario.
IF NOT EXISTS (SELECT 1 FROM dbo.RecompensasEvento WHERE BannerId = @BannerId)
BEGIN
    INSERT INTO dbo.RecompensasEvento (BannerId, Descricao, TipoMoedaId, QuantidadeMoeda, PersonagemId, MetaNecessaria)
    VALUES
        (@BannerId, N'Dia 1 - 1 bilhete de invocação',  3, 1, NULL, N'1'),
        (@BannerId, N'Dia 2 - 1 bilhete de invocação',  3, 1, NULL, N'2'),
        (@BannerId, N'Dia 3 - 2 bilhetes de invocação', 3, 2, NULL, N'3'),
        (@BannerId, N'Dia 4 - 1 bilhete de invocação',  3, 1, NULL, N'4'),
        (@BannerId, N'Dia 5 - 1 bilhete de invocação',  3, 1, NULL, N'5'),
        (@BannerId, N'Dia 6 - 2 bilhetes de invocação', 3, 2, NULL, N'6'),
        (@BannerId, N'Dia 7 - 4 bilhetes de invocação', 3, 4, NULL, N'7');
    PRINT 'Recompensas do evento criadas (7 dias, 12 bilhetes).';
END
ELSE
    PRINT 'Recompensas do evento ja existem - nada a fazer.';
GO

-- ------------------------------------------------------------
-- 4) Amizade: emblema, titulos, moldura das personagens novas
-- ------------------------------------------------------------
-- 4a) Emblema ("rebento") ao nivel 5 - igual ao das outras (Migracao04/05)
INSERT INTO dbo.Emblemas (Nome, Descricao, ImagemUrl, PersonagemId, NivelAmizadeId)
SELECT N'Rebento de ' + p.Nome,
       N'Chegaste a Inseparável de ' + p.Nome + N'. Podes equipar este rebento no teu perfil (até 3).',
       p.ImagemUrl, p.PersonagemId, n.NivelAmizadeId
FROM dbo.Personagens p
CROSS JOIN (SELECT TOP 1 NivelAmizadeId FROM dbo.NiveisAmizade WHERE Ordem = 5) n
WHERE p.PersonagemId IN (9, 10, 11)
  AND NOT EXISTS (SELECT 1 FROM dbo.Emblemas e WHERE e.PersonagemId = p.PersonagemId);
PRINT CONCAT('Emblemas criados agora: ', @@ROWCOUNT);

-- 4b) Titulos (regras da Migracao05): nivel 3 = "Melhor Amigo de X"
--     (todas); nivel 6 = titulo UNICO e colorido (Lendarias/Miticas).
INSERT INTO dbo.Titulos (Nome, Descricao, PersonagemId, NivelAmizadeId, IsPersonalizado, CorHex)
SELECT N'Melhor Amigo de ' + p.Nome, N'Chegaste a Melhor Amigo de ' + p.Nome + N'.', p.PersonagemId, 3, 0, NULL
FROM dbo.Personagens p
WHERE p.PersonagemId IN (9, 10, 11)
  AND NOT EXISTS (SELECT 1 FROM dbo.Titulos t WHERE t.PersonagemId = p.PersonagemId AND t.NivelAmizadeId = 3);
PRINT CONCAT('Titulos de nivel 3 criados agora: ', @@ROWCOUNT);

INSERT INTO dbo.Titulos (Nome, Descricao, PersonagemId, NivelAmizadeId, IsPersonalizado, CorHex)
SELECT v.Nome, v.Descricao, p.PersonagemId, 6, 1, r.CorHex
FROM (VALUES
    (N'Ondina', N'Maré Serena',       N'Contigo, o mar de Ondina nunca mais se agitou.'),
    (N'Ezra',   N'Constelação Nova',  N'A constelação que Ezra mais esperava eras tu.'),
    (N'Vesper', N'Crepúsculo Eterno', N'Entre o dia e a noite há um lugar que é só vosso.')
) AS v (Personagem, Nome, Descricao)
INNER JOIN dbo.Personagens p ON p.Nome = v.Personagem
INNER JOIN dbo.Raridades r ON r.RaridadeId = p.RaridadeId
WHERE NOT EXISTS (SELECT 1 FROM dbo.Titulos t WHERE t.PersonagemId = p.PersonagemId AND t.NivelAmizadeId = 6);
PRINT CONCAT('Titulos unicos (nivel 6) criados agora: ', @@ROWCOUNT);

-- 4d) Moldura: so a Mitica (Vesper), ao nivel 7
INSERT INTO dbo.MoldurasPerfil (Nome, ImagemUrl, PersonagemId, NivelAmizadeId, CorHex)
SELECT N'Moldura de ' + p.Nome, NULL, p.PersonagemId, n.NivelAmizadeId, r.CorHex
FROM dbo.Personagens p
INNER JOIN dbo.Raridades r ON r.RaridadeId = p.RaridadeId
CROSS JOIN (SELECT TOP 1 NivelAmizadeId FROM dbo.NiveisAmizade WHERE Ordem = 7) n
WHERE p.PersonagemId = 11 AND r.Nome = N'Mítico'
  AND NOT EXISTS (SELECT 1 FROM dbo.MoldurasPerfil m WHERE m.PersonagemId = p.PersonagemId);
PRINT CONCAT('Molduras criadas agora: ', @@ROWCOUNT);
GO

-- ------------------------------------------------------------
-- 5) Conjuntos de mensagens (Lendario 8 linhas, Mitico 10)
-- ------------------------------------------------------------
DECLARE @Sementes TABLE (PersonagemId INT, Tipo NVARCHAR(20), Ordem INT, Conteudo NVARCHAR(MAX));

INSERT INTO @Sementes (PersonagemId, Tipo, Ordem, Conteudo) VALUES
-- ===== 9 ONDINA (Lendario, maximo 6) - senhora das mares serenas =====
(9,  N'Saudacao',  1, N'Shh... ouves? O mar está calmo hoje. Foi por tua causa, ou foste tu que vieste pela calma?'),
(9,  N'Saudacao',  3, N'Voltaste com a maré. Sabia que sim — cantei até te ver.'),
(9,  N'Saudacao',  6, N'Já não canto para os barcos perdidos. Canto para ti, que ficaste.'),
(9,  N'Aleatoria', 1, N'Ondina passa a mão pela água e o mar fica liso como um espelho. Não diz nada.'),
(9,  N'Aleatoria', 2, N'Ondina sorri: "Trazes sal nos olhos. Andaste perdido, não andaste?"'),
(9,  N'Aleatoria', 4, N'Ondina confessa, baixinho: "Há barcos que não quero que voltem. O teu não é um deles."'),
(9,  N'Aleatoria', 5, N'Ondina encosta-se a ti na areia. A maré sobe e nenhuma onda vos toca.'),
(9,  N'Diaria',    4, N'Ondina: "A maré virou ao teu favor esta manhã. Vem ver o mar comigo."'),

-- ===== 10 EZRA (Lendario, maximo 6) - cartografo dos ceus =====
(10, N'Saudacao',  1, N'Cuidado onde pisas, isto está cheio de mapas. Não... esse ainda não existe, não olhes.'),
(10, N'Saudacao',  3, N'Ah, és tu! Desenhei uma estrela nova esta noite. Queres dar-lhe nome?'),
(10, N'Saudacao',  6, N'Todos os mapas que fiz levavam ao mesmo sítio. Só percebi quando chegaste.'),
(10, N'Aleatoria', 1, N'Ezra traça uma linha entre duas estrelas e franze a testa. "Ainda não. Falta-lhe alguma coisa."'),
(10, N'Aleatoria', 2, N'Ezra entrega-te um pedaço de céu dobrado: "Guarda isso. Um dia serve."'),
(10, N'Aleatoria', 4, N'Ezra mostra-te um mapa antigo, cheio de rasuras: "Este é o das constelações que desisti de esperar. Fica só com este."'),
(10, N'Aleatoria', 5, N'Ezra escreve o teu nome ao lado de uma estrela por nascer. "Agora ela tem por quem esperar."'),
(10, N'Diaria',    4, N'Ezra: "Nasceu uma constelação esta noite. Tem a tua cara. Vem confirmar."'),

-- ===== 11 VESPER (Mitico, maximo 7) - a ultima estrela do crepusculo =====
(11, N'Saudacao',  1, N'...disseste um desejo sem abrir a boca. Eu ouvi. Não te preocupes, guardo-o.'),
(11, N'Saudacao',  3, N'O dia acabou outra vez. E outra vez vieste ter comigo. Começo a contar com isso.'),
(11, N'Saudacao',  5, N'Sabes porque sou a última a apagar-me? Para ter a certeza de que chegaste bem.'),
(11, N'Saudacao',  7, N'Entre o dia e a noite há um instante que é só nosso. Bem-vindo a casa.'),
(11, N'Aleatoria', 1, N'Vesper brilha um pouco mais forte por um segundo, como se tivesse ouvido qualquer coisa.'),
(11, N'Aleatoria', 2, N'Vesper inclina-se: "Esse desejo que engoliste agora mesmo... queres dizê-lo? Não? Também vale assim."'),
(11, N'Aleatoria', 4, N'Vesper conta-te o único desejo que ela própria pediu, há muito tempo. Era pequeno. Era alguém que ficasse.'),
(11, N'Aleatoria', 6, N'Vesper pousa a última luz do dia nas tuas mãos: "Leva-a. Amanhã trago-te outra."'),
(11, N'Diaria',    4, N'Vesper: "Ouvi um desejo teu ao pôr do sol. Não o disseste a ninguém. Está a caminho."'),
(11, N'Diaria',    7, N'Vesper: "O crepúsculo demorou mais hoje. Pedi-lhe. Queria ver-te mais um bocadinho."');

INSERT INTO dbo.MensagensPersonagem (PersonagemId, TipoMensagem, NivelAmizadeId, Conteudo)
SELECT s.PersonagemId, s.Tipo, n.NivelAmizadeId, s.Conteudo
FROM @Sementes s
INNER JOIN dbo.NiveisAmizade n ON n.Ordem = s.Ordem
INNER JOIN dbo.Personagens p ON p.PersonagemId = s.PersonagemId
WHERE NOT EXISTS (SELECT 1 FROM dbo.MensagensPersonagem m WHERE m.PersonagemId = s.PersonagemId);

PRINT CONCAT('Mensagens semeadas agora: ', @@ROWCOUNT);
GO

-- ------------------------------------------------------------
-- Resumo
-- ------------------------------------------------------------
DECLARE @Personagens INT = (SELECT COUNT(*) FROM dbo.Personagens);
DECLARE @NoPermanente INT = (SELECT COUNT(*) FROM dbo.BannerPersonagens WHERE BannerId = 1);
DECLARE @BannersADecorrer INT = (SELECT COUNT(*) FROM dbo.Banners
                                 WHERE IsAtivo = 1 AND DataInicio <= SYSUTCDATETIME() AND DataFim >= SYSUTCDATETIME());
PRINT CONCAT('Personagens: ', @Personagens, ' (no Banner Permanente: ', @NoPermanente, ')');
PRINT CONCAT('Banners a decorrer: ', @BannersADecorrer);

SELECT b.Nome AS Banner, p.Nome AS Personagem, r.Nome AS Raridade, bp.RateUp, bp.ProbabilidadeExtra
FROM dbo.BannerPersonagens bp
INNER JOIN dbo.Banners b ON b.BannerId = bp.BannerId
INNER JOIN dbo.Personagens p ON p.PersonagemId = bp.PersonagemId
INNER JOIN dbo.Raridades r ON r.RaridadeId = p.RaridadeId
WHERE b.Nome = N'Estrela do Crepúsculo'
ORDER BY r.Ordem DESC, bp.RateUp DESC, p.Nome;

PRINT 'Migracao08 concluida.';
GO
