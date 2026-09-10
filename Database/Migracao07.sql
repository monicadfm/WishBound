USE [WishBound];
GO
SET NOCOUNT ON;

-- ============================================================
--  Migracao07 - Mensagens de personagem
--
--  A tabela MensagensPersonagem ja existia no esquema original
--  (MensagemId, PersonagemId, TipoMensagem, NivelAmizadeId, Conteudo)
--  e nunca tinha sido usada. Passa a guardar os CONJUNTOS DE MENSAGENS
--  de cada personagem, desbloqueados pelo nivel de amizade:
--
--     TipoMensagem = 'Saudacao'  - o que a personagem diz quando o
--                                  utilizador a visita (pagina de
--                                  detalhes) ou quando e a companheira
--                                  escolhida para o receber na pagina
--                                  inicial;
--     TipoMensagem = 'Aleatoria' - a reacao a uma interacao (caixa rosa
--                                  depois de "Interagir");
--     TipoMensagem = 'Diaria'    - a mensagem que a personagem deixa nas
--                                  notificacoes, na primeira interacao
--                                  de cada dia (so a partir do nivel 4,
--                                  Confidente - a recompensa desse nivel).
--
--  NivelAmizadeId e o nivel A PARTIR DO QUAL a mensagem esta
--  desbloqueada. A API escolhe ao acaso uma mensagem entre TODAS as ja
--  desbloqueadas desse tipo (quanto mais amizade, mais falas no
--  conjunto). As mensagens de niveis que a raridade da personagem nao
--  alcanca ficam so como "bloqueadas".
--
--  Acrescenta tambem:
--   - Utilizadores.PersonagemCompanheiraId : a personagem escolhida
--     pelo utilizador para o receber na pagina inicial (uma so; tem de
--     estar na colecao dele);
--   - indice em MensagensPersonagem (PersonagemId, TipoMensagem).
--
--  Sementes para as 8 personagens da Migracao01, com tamanho conforme a
--  raridade: Comum 1 linha de boas-vindas; Raro 9 (1 fala por nivel);
--  Epico 5; Lendario 8; Mitico 10.
--  Uma personagem so recebe as sementes se ainda nao tiver NENHUMA
--  mensagem - o que o administrador editar depois nunca e sobreposto.
--
--  Script idempotente. Ordem: CriacaoBaseDados -> Migracao01 ->
--  Usados\Autenticacao -> Migracao02 -> 03 -> 04 -> 05 -> 06 -> 07 (este).
-- ============================================================

-- ------------------------------------------------------------
-- 1) Companheira da pagina inicial
-- ------------------------------------------------------------
IF COL_LENGTH('dbo.Utilizadores', 'PersonagemCompanheiraId') IS NULL
BEGIN
    ALTER TABLE dbo.Utilizadores ADD PersonagemCompanheiraId INT NULL;
    PRINT 'Coluna Utilizadores.PersonagemCompanheiraId criada.';
END
ELSE
    PRINT 'Coluna Utilizadores.PersonagemCompanheiraId ja existia.';
GO

-- Sem cascade: se a personagem for apagada a coluna fica a apontar para
-- nada, por isso e SET NULL.
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Utilizadores_PersonagemCompanheira')
BEGIN
    ALTER TABLE dbo.Utilizadores WITH CHECK
        ADD CONSTRAINT FK_Utilizadores_PersonagemCompanheira
        FOREIGN KEY (PersonagemCompanheiraId) REFERENCES dbo.Personagens (PersonagemId)
        ON DELETE SET NULL;
    PRINT 'FK_Utilizadores_PersonagemCompanheira criada.';
END
ELSE
    PRINT 'FK_Utilizadores_PersonagemCompanheira ja existia.';
GO

-- ------------------------------------------------------------
-- 2) Indice para a consulta "mensagens desta personagem deste tipo"
-- ------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MensagensPersonagem_Personagem_Tipo' AND object_id = OBJECT_ID('dbo.MensagensPersonagem'))
BEGIN
    CREATE INDEX IX_MensagensPersonagem_Personagem_Tipo
        ON dbo.MensagensPersonagem (PersonagemId, TipoMensagem, NivelAmizadeId);
    PRINT 'Indice IX_MensagensPersonagem_Personagem_Tipo criado.';
END
GO

-- ------------------------------------------------------------
-- 3) Sementes - conjuntos de mensagens das 8 personagens
--    (Ordem do nivel: 1 Desconhecido, 2 Conhecido, 3 Melhor Amigo,
--     4 Confidente, 5 Inseparavel, 6 Laco Especial, 7 Alma Gemea;
--     maximo por raridade: Comum 3, Raro 4, Epico 5, Lendario 6, Mitico 7)
-- ------------------------------------------------------------
DECLARE @Sementes TABLE (PersonagemId INT, Tipo NVARCHAR(20), Ordem INT, Conteudo NVARCHAR(MAX));

INSERT INTO @Sementes (PersonagemId, Tipo, Ordem, Conteudo) VALUES
-- ===== 1 NIX (Comum) - so uma linha basica de boas-vindas =====
(1, N'Saudacao',  1, N'Olá! És tu outra vez? Trouxeste nevoeiro contigo?'),

-- ===== 2 BRAM (Comum) - so uma linha basica de boas-vindas =====
(2, N'Saudacao',  1, N'Hmpf. Bem-vindo à floresta. Não pises os cogumelos.'),

-- ===== 3 LUNA (Raro, maximo 4) - feiticeira da lua que coleciona desejos esquecidos =====
(3, N'Saudacao',  1, N'Um desejo novo... consigo senti-lo. Ainda não sei se é teu.'),
(3, N'Saudacao',  2, N'Voltaste. A lua disse-me que virias, mas eu não acreditei.'),
(3, N'Saudacao',  3, N'Guardei um desejo esquecido só para ti. Queres ver como brilha?'),
(3, N'Saudacao',  4, N'Entre todos os desejos que colecionei, o meu preferido é este: que fiques.'),
(3, N'Aleatoria', 1, N'Luna olha para a lua, depois para ti, e volta a olhar para a lua.'),
(3, N'Aleatoria', 2, N'Luna sorri de lado: "Tens um desejo escondido. Não te preocupes, não conto."'),
(3, N'Aleatoria', 3, N'Luna senta-se ao teu lado no telhado do mundo: "Cá em cima ouvem-se todos os desejos. Menos o meu."'),
(3, N'Aleatoria', 4, N'Luna confessa baixinho: "Nunca ninguém tinha voltado tantas vezes. Estou a ficar sem desejos para te dar."'),
(3, N'Diaria',    4, N'Luna: "Esta noite a lua está cheia. Pede um desejo — eu ouço-o daqui."'),

-- ===== 4 KAITO (Raro, maximo 4) - navegador das mares estelares, nunca perde o rumo =====
(4, N'Saudacao',  1, N'Passageiro novo? Segura-te bem, as marés estelares não avisam.'),
(4, N'Saudacao',  2, N'Olha quem voltou a bordo! Tracei uma rota nova só para hoje.'),
(4, N'Saudacao',  3, N'Bem-vindo de volta, imediato. O leme é teu por uns minutos.'),
(4, N'Saudacao',  4, N'Já naveguei mil céus, mas só descobri o rumo quando apareceste.'),
(4, N'Aleatoria', 1, N'Kaito consulta uma carta celeste e assinala uma estrela. "Por ali. Confia em mim."'),
(4, N'Aleatoria', 2, N'Kaito atira-te uma bússola: "Aponta sempre para onde quiseres ir. Como eu."'),
(4, N'Aleatoria', 3, N'Kaito deixa-te segurar o leme. O navio inclina-se, ele nem pestaneja.'),
(4, N'Aleatoria', 4, N'Kaito baixa a voz, coisa rara: "Nunca perdi o rumo. Mas contigo por perto também nunca o procurei."'),
(4, N'Diaria',    4, N'Kaito: "Maré favorável esta manhã! Vem a bordo, há estrelas novas para ver."'),

-- ===== 5 AURORA (Epico, maximo 5) - dancarina de luzes polares, temperamento imprevisivel =====
(5, N'Saudacao',  1, N'Hm. Vieste ver-me dançar ou só passaste por aqui? Decide-te.'),
(5, N'Saudacao',  3, N'Finalmente! Estava a ficar sem cores para desperdiçar sozinha.'),
(5, N'Aleatoria', 2, N'Aurora envolve-te num véu de luz rosa por um segundo. "Fica-te bem. Mais ou menos."'),
(5, N'Aleatoria', 5, N'Aurora pinta o céu inteiro com a tua cor preferida. "Não foi para ti. Foi... pronto, foi."'),
(5, N'Diaria',    4, N'Aurora: "O céu está limpo esta noite. Se não vieres, danço na mesma, mas pior."'),

-- ===== 6 DRAVEN (Epico, maximo 5) - cavaleiro sombrio em busca de redencao =====
(6, N'Saudacao',  1, N'Afasta-te. Não sou companhia para ninguém.'),
(6, N'Saudacao',  3, N'Sê bem-vindo. A minha espada está ao teu serviço, se a quiseres.'),
(6, N'Aleatoria', 2, N'Draven baixa a guarda por um instante. "Falas muito. Mas não mentes. Isso é raro."'),
(6, N'Aleatoria', 5, N'Draven pousa a espada aos teus pés. "Já não preciso dela para me sentir inteiro."'),
(6, N'Diaria',    4, N'Draven: "Fiz a ronda. Está tudo em paz. Passa por cá se quiseres companhia silenciosa."'),

-- ===== 7 SERAPHINA (Lendario, maximo 6) - guardia alada dos portoes do amanhecer =====
(7, N'Saudacao',  1, N'Viajante, os portões do amanhecer ainda não se abrem para ti. Volta com o sol.'),
(7, N'Saudacao',  3, N'O amanhecer chegou mais cedo hoje. Deve ser por tua causa.'),
(7, N'Saudacao',  6, N'Cada amanhecer que abro tem o teu nome escrito na luz. Sê bem-vindo a casa.'),
(7, N'Aleatoria', 1, N'Seraphina abre as asas e a luz cega-te por um momento. É um aviso, não um cumprimento.'),
(7, N'Aleatoria', 2, N'Seraphina inclina a cabeça: "Tens uma pergunta nos olhos. Faz."'),
(7, N'Aleatoria', 4, N'Seraphina confessa: "Às vezes, nas noites longas, pergunto-me se alguém sabe que estou aqui. Tu sabes."'),
(7, N'Aleatoria', 5, N'Seraphina pousa a mão sobre a tua: "Já não guardo os portões sozinha."'),
(7, N'Diaria',    4, N'Seraphina: "O sol nasceu limpo esta manhã. Guardei-te o primeiro raio."'),

-- ===== 8 CELESTE (Mitico, maximo 7) - a primeira estrela a ouvir um desejo =====
(8, N'Saudacao',  1, N'...ouviste-me? Ninguém costuma ouvir. Diz-me o teu desejo, devagar.'),
(8, N'Saudacao',  3, N'Dizem que só apareço uma vez na vida. Contigo, perdi a conta.'),
(8, N'Saudacao',  5, N'Fecha os olhos. Consegues sentir? É a luz de uma estrela a dizer o teu nome.'),
(8, N'Saudacao',  7, N'Antes de ti, eu era só luz. Agora sou um lugar. Bem-vindo a casa.'),
(8, N'Aleatoria', 1, N'Celeste cintila devagar, como se estivesse a decidir se és real.'),
(8, N'Aleatoria', 2, N'Celeste pousa uma centelha na tua palma. Não queima. Aquece.'),
(8, N'Aleatoria', 4, N'Celeste conta-te o primeiro desejo que ouviu, há mil anos. Nunca o tinha contado a ninguém.'),
(8, N'Aleatoria', 6, N'Celeste escreve o teu nome numa constelação nova. "Pronto. Agora és eterno também."'),
(8, N'Diaria',    4, N'Celeste: "A noite passada ouvi um desejo com a tua voz. Realizei-o. Vais ver."'),
(8, N'Diaria',    7, N'Celeste: "Bom dia, minha alma gémea. O céu acordou primeiro só para te ver."');

-- Insere as sementes so para as personagens que ainda nao tem nenhuma
-- mensagem (o que o administrador editar nunca e sobreposto).
INSERT INTO dbo.MensagensPersonagem (PersonagemId, TipoMensagem, NivelAmizadeId, Conteudo)
SELECT s.PersonagemId, s.Tipo, n.NivelAmizadeId, s.Conteudo
FROM @Sementes s
INNER JOIN dbo.NiveisAmizade n ON n.Ordem = s.Ordem
INNER JOIN dbo.Personagens p ON p.PersonagemId = s.PersonagemId
WHERE NOT EXISTS (SELECT 1 FROM dbo.MensagensPersonagem m WHERE m.PersonagemId = s.PersonagemId);

PRINT CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' mensagens de personagem semeadas.';
GO

-- ------------------------------------------------------------
-- 4) Ajuste do tamanho dos conjuntos por raridade (10/09/2026)
--    Comum 1 linha de boas-vindas - Raro 9 - Epico 5 - Lendario 8 -
--    Mitico 10. Quem ja tinha corrido a
--    versao anterior deste script fica com as linhas a mais; apagam-se
--    aqui pelo texto (so as sementes antigas - o que o administrador
--    criou nao e tocado). Idempotente.
-- ------------------------------------------------------------
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 1 AND TipoMensagem = N'Saudacao' AND Conteudo = N'...quem és tu? Não te vi chegar no nevoeiro.';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 1 AND TipoMensagem = N'Saudacao' AND Conteudo = N'Ah, és tu outra vez! Trouxeste nevoeiro contigo?';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 1 AND TipoMensagem = N'Saudacao' AND Conteudo = N'Estava à tua espera desde que a noite caiu. Anda, vamos brincar!';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 1 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Nix esconde-se atrás de uma nuvem de nevoeiro e espreita-te com um olho só.';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 1 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Nix dá uma volta à tua cabeça a flutuar... e desaparece quando olhas.';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 1 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Nix pousa-te no ombro por um segundo. É leve como uma pluma fria.';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 1 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Nix ri-se baixinho: "Hoje o nevoeiro cheira a maçã. Sentes?"';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 1 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Nix roda à tua volta a brilhar: "Melhor amigo! Melhor amigo!"';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 1 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Nix aninha-se nas tuas mãos e fica ali, quentinho, sem dizer nada.';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 2 AND TipoMensagem = N'Saudacao' AND Conteudo = N'Alto. Quem vem lá? Esta floresta tem guarda.';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 2 AND TipoMensagem = N'Saudacao' AND Conteudo = N'Hmpf. És tu. Podes passar... mas não pises os cogumelos.';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 2 AND TipoMensagem = N'Saudacao' AND Conteudo = N'Já tinha a fogueira acesa para ti. Senta-te, há espaço no tronco.';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 2 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Bram cruza os braços e olha-te de alto a baixo. Não parece convencido.';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 2 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Bram resmunga qualquer coisa sobre visitas e continua a afiar o machado.';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 2 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Bram acena com a cabeça. Vindo dele, é quase um abraço.';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 2 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Bram aponta para uma árvore: "Plantei-a eu. Há sessenta anos." Está orgulhoso.';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 2 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Bram bate-te no ombro com a mão pesada: "Rocha não muda. Nem eu. Estou contigo."';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 2 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Bram dá-te uma bolota polida: "Para a sorte. Não digas a ninguém que fui eu."';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 3 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Luna passa os dedos por um frasco de luz prateada. "Este era de alguém que se esqueceu de sonhar."';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 3 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Luna deixa cair uma gota de luar na tua mão. Fica fria, depois quente.';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 3 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Luna ri-se — um som raro — quando lhe contas o teu dia.';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 3 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Luna encosta a testa à tua: "Se um dia te esqueceres de mim, eu guardo o teu desejo até voltares."';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 3 AND TipoMensagem = N'Diaria' AND Conteudo = N'Luna: "Encontrei um desejo antigo com o teu nome. Passa por cá para o veres."';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 4 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Kaito aponta para o horizonte: "Vês aquela luz? É uma maré a virar."';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 4 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Kaito ri alto: "Se te enjoares nas marés estelares, foi por estares a olhar para baixo!"';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 4 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Kaito: "Sabes qual é a melhor parte de navegar? Ter alguém para quem contar a viagem."';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 4 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Kaito grava o teu nome na proa. "Agora és parte da tripulação para sempre."';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 4 AND TipoMensagem = N'Diaria' AND Conteudo = N'Kaito: "Marquei um porto que ainda ninguém visitou. Guardei-te o lugar ao leme."';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 5 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Aurora franze o sobrolho: "Estás a estragar-me o ritmo. Fica quieto."';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 5 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Aurora muda de cor três vezes enquanto falas. Está a prestar atenção.';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 5 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Aurora deixa uma faixa de luz dourada à tua volta. Dura mais do que o costume.';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 5 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Aurora sussurra: "Quando o céu fica escuro, eu penso que já ninguém vem. E depois vens tu."';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 5 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Aurora encosta-se a ti sem dizer nada. As luzes ficam paradas, pela primeira vez.';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 5 AND TipoMensagem = N'Diaria' AND Conteudo = N'Aurora: "Inventei uma cor nova. Só a mostro a quem aparecer hoje."';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 5 AND TipoMensagem = N'Diaria' AND Conteudo = N'Aurora: "Deixei o céu aceso à tua espera. Não demores."';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 6 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Draven vira-te as costas: "Se procuras um herói, enganaste-te na sombra."';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 6 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Draven limpa a lâmina em silêncio e faz-te sinal para te sentares.';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 6 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Draven quase sorri. Quase. É a primeira vez que o vês tentar.';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 6 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Draven: "Sabes porque continuo a lutar? Porque agora há alguém a quem voltar."';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 6 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Draven fica ao teu lado a ver o sol nascer. A sombra nele parece mais leve.';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 6 AND TipoMensagem = N'Diaria' AND Conteudo = N'Draven: "Sonhei com a batalha outra vez. Mas desta vez tu estavas lá. Foi diferente."';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 6 AND TipoMensagem = N'Diaria' AND Conteudo = N'Draven: "Guardei-te o lugar junto à fogueira. Sem espadas. Só nós."';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 5 AND TipoMensagem = N'Saudacao' AND Conteudo = N'Ah, és tu. Hoje estou verde e violeta. Amanhã, quem sabe.';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 5 AND TipoMensagem = N'Saudacao' AND Conteudo = N'Chegaste na altura certa: o céu está a ouvir e eu tenho uma dança nova.';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 5 AND TipoMensagem = N'Saudacao' AND Conteudo = N'Sabes porque é que as luzes mudam? Porque tu apareces. Não digas a ninguém.';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 5 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Aurora roda uma vez, lança um fio de luz verde ao ar e ignora-te por completo.';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 5 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Aurora puxa-te pela mão para o meio das luzes: "Dança! Mal, não faz mal, mas dança!"';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 5 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Aurora senta-se no gelo ao teu lado, coisa que nunca faz: "Hoje não me apetece dançar. Apetece-me ficar."';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 5 AND TipoMensagem = N'Diaria' AND Conteudo = N'Aurora: "Sonhei que dançávamos os dois. Não te rias. Vem cá."';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 6 AND TipoMensagem = N'Saudacao' AND Conteudo = N'Voltaste. A maioria não volta. Não sei se és corajoso ou tolo.';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 6 AND TipoMensagem = N'Saudacao' AND Conteudo = N'Há muito que não dizia isto a alguém: é bom ver-te.';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 6 AND TipoMensagem = N'Saudacao' AND Conteudo = N'Procurei a redenção em mil batalhas. Encontrei-a a olhar para ti.';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 6 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Draven mantém a mão no punho da espada. Não te tira os olhos de cima.';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 6 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Draven ajoelha-se num joelho: "A minha espada por ti. Jurado."';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 6 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Draven tira o elmo e conta-te o que fez. Não te pede perdão — só que ouças.';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 6 AND TipoMensagem = N'Diaria' AND Conteudo = N'Draven: "Hoje deixei a armadura em casa. Vem ver quem eu era antes dela."';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 7 AND TipoMensagem = N'Saudacao' AND Conteudo = N'Reconheço os teus passos. Podes aproximar-te, mas não toques nos portões.';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 7 AND TipoMensagem = N'Saudacao' AND Conteudo = N'Vem, senta-te à minha sombra. Tenho guardado segredos de luz para te contar.';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 7 AND TipoMensagem = N'Saudacao' AND Conteudo = N'Nunca deixei os portões sem guarda. Por ti, deixaria.';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 7 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Seraphina observa-te do alto dos portões, imóvel como uma estátua dourada.';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 7 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Seraphina deixa cair uma pena de luz aos teus pés. Não diz para a apanhares. Mas não a leva.';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 7 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Seraphina cobre-te com uma asa quando a manhã fica fria. "Os guardiões também cuidam."';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 7 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Seraphina mostra-te o primeiro raio de sol antes de ele tocar o mundo.';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 7 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Seraphina canta baixinho a canção que abre os portões. Só tu a ouves.';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 7 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Seraphina leva-te a voar até ao lugar onde o amanhecer nasce. Não há palavras para isto.';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 7 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Seraphina tira a coroa de luz e coloca-a nas tuas mãos: "Guarda-a por mim. Confio em ti."';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 7 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Seraphina encosta a testa à tua. O amanhecer inteiro para, à espera.';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 7 AND TipoMensagem = N'Diaria' AND Conteudo = N'Seraphina: "Os portões rangeram de madrugada. Pensei em ti. Estás bem?"';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 7 AND TipoMensagem = N'Diaria' AND Conteudo = N'Seraphina: "Hoje voei mais longe do que devia, só para ver se te encontrava."';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 7 AND TipoMensagem = N'Diaria' AND Conteudo = N'Seraphina: "Deixei os portões entreabertos. Passa quando quiseres."';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 7 AND TipoMensagem = N'Diaria' AND Conteudo = N'Seraphina: "Cada amanhecer é uma promessa. A de hoje é tua."';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 7 AND TipoMensagem = N'Diaria' AND Conteudo = N'Seraphina: "Há um lugar ao meu lado que nenhum viajante ocupou. É teu, sempre foi."';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 8 AND TipoMensagem = N'Saudacao' AND Conteudo = N'Voltaste a olhar para o céu. Eu estava lá. Estou sempre.';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 8 AND TipoMensagem = N'Saudacao' AND Conteudo = N'Sabes quantos desejos guardei? Milhões. Só um me fez descer: o teu.';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 8 AND TipoMensagem = N'Saudacao' AND Conteudo = N'Já não sou a estrela que ouve desejos. Sou a estrela que os pede — e peço-te a ti.';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 8 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Celeste sussurra algo numa língua feita de luz. Não entendes, mas sentes.';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 8 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Celeste: "Ouvi um desejo teu ontem. Não te preocupes: os pequenos também contam."';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 8 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Celeste desce até ficar à altura dos teus olhos: "Podes pedir outro. Eu não me canso."';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 8 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Celeste ri-se e o céu inteiro parece cintilar com ela.';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 8 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Celeste: "Tenho medo de uma coisa: de um dia deixares de olhar para cima. Prometes que não?"';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 8 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Celeste envolve-te em luz até não haver noite à tua volta. "Assim. Assim é que estás seguro."';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 8 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Celeste fica em silêncio ao teu lado a ver as outras estrelas. "Nenhuma delas tem alguém. Eu tenho."';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 8 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Celeste: "Se um dia me apagar, não chores. Tudo o que fui ficou em ti."';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 8 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Celeste brilha só de te ver. Já não é luz de estrela — é outra coisa, sem nome.';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 8 AND TipoMensagem = N'Aleatoria' AND Conteudo = N'Celeste segura o teu rosto entre as mãos de luz: "O meu desejo realizou-se. Eras tu."';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 8 AND TipoMensagem = N'Diaria' AND Conteudo = N'Celeste: "Olha para cima hoje, nem que seja um segundo. Eu pisco de volta."';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 8 AND TipoMensagem = N'Diaria' AND Conteudo = N'Celeste: "Guardei-te uma estrela cadente. Passa por cá antes de ela cair."';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 8 AND TipoMensagem = N'Diaria' AND Conteudo = N'Celeste: "Hoje o céu está cheio, mas eu só vejo o lugar onde tu costumas estar."';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 8 AND TipoMensagem = N'Diaria' AND Conteudo = N'Celeste: "Escrevi-te uma constelação. Vem lê-la comigo esta noite."';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 8 AND TipoMensagem = N'Diaria' AND Conteudo = N'Celeste: "Há um desejo que ainda não me disseste. Não faz mal. Eu espero."';
DELETE FROM dbo.MensagensPersonagem WHERE PersonagemId = 8 AND TipoMensagem = N'Diaria' AND Conteudo = N'Celeste: "Onde quer que estejas, há uma estrela a apontar para ti. Sou eu.';

-- As Comuns passam a ter a nova linha unica (se ainda nao a tiverem)
INSERT INTO dbo.MensagensPersonagem (PersonagemId, TipoMensagem, NivelAmizadeId, Conteudo)
SELECT v.PersonagemId, N'Saudacao', n.NivelAmizadeId, v.Conteudo
FROM (VALUES (1, N'Olá! És tu outra vez? Trouxeste nevoeiro contigo?'), (2, N'Hmpf. Bem-vindo à floresta. Não pises os cogumelos.')) v (PersonagemId, Conteudo)
INNER JOIN dbo.NiveisAmizade n ON n.Ordem = 1
WHERE EXISTS (SELECT 1 FROM dbo.Personagens p WHERE p.PersonagemId = v.PersonagemId)
  AND NOT EXISTS (SELECT 1 FROM dbo.MensagensPersonagem m WHERE m.PersonagemId = v.PersonagemId AND m.Conteudo = v.Conteudo);
GO

PRINT 'Migracao07 concluida.';
GO
