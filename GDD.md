# Twilight — Game Design Document

> Status: documento vivo do projeto  
> Escopo: MVP para PC, single-player  
> Engine: Unity 6, URP 2D  
> Última atualização: 8 de julho de 2026

## 1. Visão geral

**Twilight** é o nome provisório de um RPG de ação 2D em pixel art, com câmera top-down, desenvolvido para PC. O projeto tem como principal inspiração o mapa **Twilight's Eve**, de Warcraft III. A intenção não é reproduzi-lo fielmente, mas usar como base suas ideias de progressão por classes, promoções e especializações.

O jogador explora o mundo com WASD, seleciona inimigos por clique ou Tab e utiliza habilidades numeradas. O combate procura combinar a leitura acessível de um sistema Tab Target com decisões de recursos e rotação inspiradas em MMORPGs.

## 2. Pilares do jogo

- **Progressão de classes:** começar como Villager, promover-se e desbloquear estilos de jogo mais especializados.
- **Combate por alvo:** selecionar inimigos e executar habilidades com alcance, aproximação automática, cooldowns e recursos.
- **Evolução por grind:** completar objetivos, derrotar monstros e obter experiência, ouro, equipamentos e materiais.
- **Preparação e desafio:** melhorar o personagem no mundo aberto para enfrentar elites, dungeons e chefes.
- **Construção de personagem:** combinar atributos, equipamentos, habilidades e especializações.

## 3. Gameplay loop

O ciclo principal planejado é:

> Explorar → aceitar quests → derrotar monstros → obter XP, ouro e recursos → aprimorar equipamentos → evoluir a classe → concluir dungeons → derrotar chefes → desbloquear especializações → repetir.

Cada ciclo deve fortalecer o personagem e liberar desafios maiores. O endgame e os chefes finais não fazem parte do MVP.

## 4. Controles e câmera

### Implementado

- Movimento por WASD ou setas.
- Seleção de alvo por clique esquerdo.
- Alternância entre inimigos visíveis com `Tab`.
- Habilidades nas teclas `1`, `2` e `3`.
- Inventário na tecla `I`.
- Cancelamento contextual com `Esc`.
- Câmera top-down com Cinemachine e área de confinamento.
- Interação com NPCs (quest givers, vendedores) por clique esquerdo, com a mesma aproximação automática do combate.
- Cursor contextual: muda ao passar sobre inimigos (padrão vermelho) ou NPCs (padrão dourado), customizável por prefab; inimigos e NPCs também ganham um destaque visual no hover (outline/brilho).
- Cliques e hover não atravessam mais janelas de UI abertas (loja, inventário, etc.) para acertar o mundo por baixo delas.
- Atalhos de debug, disponíveis só no Editor (fora de builds): `F5` quicksalva, `F9` quickcarrega e `F6` recarrega o mundo sem aplicar nenhum save — usam um slot "debug" isolado dos slots de save reais.

### Comportamento do alvo

Ao usar uma habilidade direcionada fora do alcance, o personagem calcula uma rota no NavMesh e se aproxima automaticamente. O alvo confirmado ao pressionar a habilidade fica travado durante a aproximação. Selecionar outro inimigo muda apenas a seleção visual; o novo alvo só é confirmado ao pressionar novamente uma habilidade direcionada.

## 5. Sistema de combate

O combate usa seleção de alvo, alcance, cooldown, animações e efeitos disparados por Animation Events. Uma habilidade pressionada durante outra animação pode ficar armazenada em um buffer de uma posição; a intenção mais recente substitui a anterior.

O dano atual é calculado a partir de:

- atributo de dano do jogador;
- multiplicador da habilidade;
- bônus temporários;
- passivas;
- chance e multiplicador de crítico.

O dano flutuante é branco em acertos normais e dourado em críticos.

### Habilidades implementadas

| Tecla | Habilidade | Funcionamento atual |
|---|---|---|
| `1` | Auto Attack | Ataque direcionado, alcance 1,2, multiplicador de dano 1, cooldown de 1,5 s e geração de 1 Momentum. |
| `2` | Power Strike | Ataque direcionado, alcance 1,2, multiplicador de dano 2,5, cooldown de 4 s e geração de 2 Momentum. |
| `3` | Stomp | Ataque em área de raio 2,5 ao redor do jogador. Consome todo o Momentum e recebe +50% de multiplicador por ponto consumido. Atualmente não possui cooldown. |

### Momentum

Momentum é o recurso de combate atualmente implementado. Seu máximo padrão é 6.

- Auto Attack gera 1 ponto.
- Power Strike gera 2 pontos.
- Stomp consome todo o recurso para escalar seu dano.
- Com Momentum máximo, a passiva atual aumenta o dano do Auto Attack em 50%.

O GDD original previa Rage/Fúria para o Warrior. Momentum está sendo usado no protótipo para validar essa dinâmica de geração, manutenção e consumo; a associação definitiva do recurso a Villager ou Warrior ainda é uma decisão de design pendente.

### Interface de habilidades

Uma barra runtime exibe as habilidades `1`, `2` e `3` no centro inferior da tela. Cada slot mostra nome, tecla, ícone quando disponível, máscara radial e contagem regressiva de cooldown.

Logo acima dela, uma barra visual divide o Momentum em seis segmentos. Cada segmento preenchido representa um ponto acumulado e se apaga quando o recurso é gasto. Essa barra substitui o contador numérico de Momentum durante o jogo.

## 6. Classes e progressão

### Progressão planejada

O jogador inicia como **Villager**, uma classe básica e introdutória. Ao atingir o nível 10, poderá realizar sua primeira promoção para **Warrior**. A promoção reinicia o nível da classe e introduz novos atributos, habilidades e estilo de combate.

```text
Villager
└── Warrior
    ├── Tank
    └── DPS
        └── Especializações avançadas
```

Archer, Mage, especializações avançadas e endgame ficam para versões futuras.

### Villager

Deve apresentar os conceitos básicos com ataques simples, poucas habilidades ofensivas e rotação curta.

### Warrior

Deve exigir mais tomada de decisão por meio de um recurso de combate:

- ataques básicos geram recurso;
- algumas habilidades também geram recurso;
- habilidades fortes consomem recurso;
- otimizar a rotação aumenta a eficiência durante grind e dungeons.

### Estado atual

O projeto já possui nível, experiência e crescimento da experiência necessária. A promoção de classe, o reinício de nível, os ganhos de atributos por nível e as árvores de especialização ainda não estão implementados.

Subir de nível restaura HP e mana por completo — recompensa clássica de RPG que também evita morrer "no meio" de um level up durante o grind.

## 7. Atributos

### Implementado

O sistema separa atributos primários (crescimento do personagem) de atributos secundários (usados pelas fórmulas de combate). O jogador não distribui pontos manualmente — todo crescimento primário é automático.

**Primários** — Strength, Agility, Intelligence. Cada um guarda um valor Base (ganho por level up) e um Bonus (equipamentos, buffs, passivas); o resto do jogo consome sempre o Total (Base + Bonus).

**Secundários** — Attack Power, Spell Power, Max Health, Health Regen, Max Mana, Mana Regen, Critical Chance, Critical Damage, Haste, Armor, Move Speed. São recalculados a partir dos primários por um método centralizado (`StatsManager.RecalculateStats`), chamado sempre que algo muda (level up, equipar/desequipar item). O resto do jogo (ex.: `Player_Combat`) só lê os valores já calculados, nunca refaz a conta.

Regras de derivação atuais:

- Strength gera Max Health, Health Regen, e Attack Power apenas se a classe atual tiver Strength como atributo primário;
- Agility gera Critical Chance, Haste, e Attack Power apenas se a classe atual tiver Agility como atributo primário;
- Intelligence gera Max Mana, Mana Regen, e Spell Power apenas se a classe atual tiver Intelligence como atributo primário;
- Armor não deriva de nenhum atributo — só vem de equipamentos, buffs e passivas;
- Critical Damage e Move Speed também não derivam de atributo — têm um valor base configurável mais o bônus de equipamentos.

Cada classe terá um `PrimaryAttribute` (Strength, Agility ou Intelligence) que decide qual dos dois poderes ofensivos (Attack Power ou Spell Power) ela usa. Como a promoção de classes ainda não existe, isso hoje é só um campo configurável no `StatsManager`, pronto para quando houver Villager/Warrior/Assassin/Mage de fato.

Haste é modelada como multiplicador de velocidade (`tempoFinal = tempoBase / (1 + Haste)`), não como redução de cooldown — pensada para no futuro acelerar auto attack, cast time e animações, e não apenas os cooldowns de habilidade.

Equipamentos continuam adicionando e removendo modificadores (agora tipados por atributo primário ou por stat secundário) ao equipar/desequipar.

Mana agora é um recurso gastável de verdade: cada Skill pode ter um custo de Mana próprio, verificado e descontado do `currentMana` do jogador (vive no `StatsManager`, ao lado da vida — separado do `ResourceManager` genérico, que continua sendo usado só para Momentum). Mana volta ao máximo no respawn, igual à vida. Health Regen e Mana Regen continuam existindo como números calculados, mas ainda sem nenhum sistema de tique de regeneração passiva — hoje só sobem juntando pontos de atributo, não com o tempo.

## 8. Inimigos

Os inimigos atuais possuem uma máquina de estados simples:

```text
Idle → Chasing → Attacking
  ↑         ↓         ↓
  └──── Returning ←───┘
```

### Implementado

- detecção do jogador por raio;
- perseguição via NavMesh;
- alcance e cooldown de ataque;
- abandono da perseguição por distância do próprio spawn (o inimigo desiste quando ele mesmo já perseguiu longe demais de onde nasceu, não quando o jogador está longe desse ponto);
- retorno ao ponto inicial;
- recuperação de vida após retornar;
- recompensa de experiência ao morrer;
- barra de vida e dano flutuante;
- ordenação visual baseada na posição Y;
- respawn após morrer, através de um `EnemySpawner` reutilizável com um intervalo configurável e uma opção para desligar o respawn (pensada para dungeons, que não devem repor inimigos);
- separação entre inimigos (estilo boids): um `EnemyFlockManager` compartilhado mantém um hash espacial de todos os inimigos e calcula, por inimigo, um empurrão local contra vizinhos próximos. O `NavMeshAgent` de cada inimigo é usado só para calcular a rota; o movimento de fato é integrado manualmente combinando a direção da rota com esse empurrão de separação, e sempre re-encaixado no NavMesh a cada frame. Isso evita que grupos formem uma "parede" travada e faz o grupo fluir ao redor do jogador para cercá-lo;
- detecção de "engasgo": se um inimigo tentou se mover e avançou muito pouco por tempo suficiente, recebe um viés lateral temporário para quebrar impasses simétricos (ex.: vários inimigos alinhados direto na frente uns dos outros);
- direção do sprite (flip) decidida pela velocidade horizontal suavizada do próprio inimigo, não pela posição do alvo — um empurrão passageiro de separação não vira mais o sprite; só um deslocamento sustentado num sentido troca a direção.

Inimigos agora são definidos por dado (`EnemyArchetypeSO`: vida máxima, armadura, poder de ataque, chance e multiplicador de crítico, recompensa de experiência), lido pelos componentes do prefab — o mesmo archetype pode alimentar variantes "Elite" ajustadas direto no prefab, mas nenhum conteúdo Elite publicado existe ainda.

O protótipo atual tem dois inimigos: o Goblin, corpo a corpo, e o Orc, que também possui uma habilidade telegrafada à distância (componente `Enemy_RangedAttack`, reaproveitável por qualquer inimigo à distância futuro): a cada 3 segundos de engajamento, ele marca a posição do jogador com uma área vermelha no chão, fica parado por 1 segundo e então arremessa algo nessa área — só quem ainda estiver dentro dela no momento do impacto toma dano, permitindo que o jogador desvie saindo a tempo. Ao morrer, um inimigo dispara um evento com a experiência concedida e o `EnemyArchetypeSO` de origem — é esse evento que alimenta o progresso de quests de matar inimigos (ver seção 10). Arqueiros, magos, elites de conteúdo e chefes estão planejados.

## 9. Estrutura de progressão de conteúdo

### Grind

A área de grind deve sustentar a evolução constante por quests de combate e recompensar:

- experiência;
- ouro;
- recursos;
- equipamentos.

Essas recompensas serão usadas para comprar, fabricar e aprimorar equipamentos, preparando o personagem para dungeons. Os inimigos comuns devem ter mecânicas simples e permitir enfrentar grupos progressivamente maiores.

### Dungeons

As dungeons representam o principal desafio e devem conter:

- inimigos mais resistentes;
- elites com mecânicas próprias;
- chefes com combates elaborados;
- equipamentos e materiais raros como recompensa.

Quests de matar inimigos e a economia de ouro (compra e venda numa loja) já estão implementadas no protótipo atual (ver seções 10 e 11). Coleta de recursos, crafting, aprimoramento, dungeons e chefes ainda não estão implementados.

## 10. Quests

### Implementado

- `QuestSO` descreve uma quest como dado: tipo de objetivo (hoje só `KillEnemies`), referência direta a um `EnemyArchetypeSO` alvo (em vez de comparar por texto/nome — nunca dessincroniza se o inimigo for renomeado), quantidade necessária e recompensa de experiência;
- `QuestManager` é a fonte única de verdade do estado de quests em runtime, com quatro estados por quest: Available, InProgress, ReadyToComplete e TurnedIn;
- progresso avança automaticamente ao ouvir o evento de morte de inimigo (ver seção 8), filtrando pelo archetype alvo da quest;
- NPCs (`NPCInteractable`, que implementa a mesma interface `IInteractable` usada pela loja) guardam uma única quest cada — um NPC com múltiplas quests simultâneas ainda não é suportado;
- ao chegar perto de um NPC, a `QuestWindow` abre a tela adequada ao estado atual: aceitar, progresso em andamento, entregar, ou uma linha de diálogo sorteada quando não há quest disponível;
- um indicador acima da cabeça do NPC (`QuestGiverIndicator`) mostra um ícone de "disponível" ou "pronta para entregar", atualizado por evento sempre que o estado da quest muda;
- uma HUD de tracker (`QuestTrackerHUD`) lista as quests em andamento e seu progresso na tela;
- completar uma quest concede a experiência configurada através do `ExpManager`;
- estado de quests é salvo e carregado (ver seção 13); um `questId` salvo que não bate com nenhum `QuestSO` atual (asset renomeado/removido) é preservado cru em vez de descartado, até o id voltar a existir.

### Conteúdo atual

- KillGoblins: matar Goblins, recompensa de experiência.

## 11. Loja e economia

### Implementado

- `GoldManager`: fonte única de verdade do ouro do jogador em runtime, com evento de mudança para a UI e valor inicial configurável (hoje 100);
- `ShopSO` descreve uma loja como dado puro (nome, lista de itens com preço, multiplicador de venda) — novas lojas são só novos assets, sem código novo; estoque é sempre infinito;
- vendedores usam `ShopInteractable` (mesmo `IInteractable`/aproximação automática das quests) para abrir a `ShopWindow` ao chegar perto;
- a `ShopWindow` reposiciona a grade de inventário ao lado da loja (escondendo o painel de equipamento nesse modo) para comprar e vender no mesmo layout;
- comprar parte de um slot da loja, vender parte de um slot do inventário — ambos abrem a mesma `PurchaseConfirmWindow`, com seletor de quantidade para itens empilháveis, cálculo de total e validação (ouro insuficiente, inventário cheio ao comprar, quantidade indisponível ao vender);
- preço de venda = `floor(valor base do item × multiplicador de venda da loja)`;
- a janela fecha sozinha se o jogador se afastar, igual à `QuestWindow`.

### Conteúdo atual

- uma vendedora (Milena) vendendo Potion, Sword, Adaga e Helmet.

## 12. Itens, inventário e equipamentos

### Implementado

- inventário com 20 slots;
- itens empilháveis e não empilháveis;
- coleta de itens no mundo;
- consumo de poções;
- descarte de uma unidade no mundo;
- tooltips com descrição e modificadores;
- slots para Head, Body, Legs, Feet, Main Hand, Off Hand, Necklace e Ring;
- aplicação e remoção de modificadores ao equipar e desequipar.

### Conteúdo atual

- Potion: consumível empilhável que recupera 3 HP;
- Sword: equipamento de Main Hand;
- Adaga: equipamento de Main Hand;
- Helmet: equipamento de Head.

Itens no mundo já coletados são lembrados por um `WorldItemRegistry` (por id do objeto), para um reload de cena não recriar pickups já pegos. Inventário, equipamento e itens de mundo coletados agora persistem entre sessões — ver seção 13.

## 13. Salvamento e carregamento

### Implementado

- `SaveService` grava um arquivo JSON versionado por slot em `Application.persistentDataPath`, com escrita atômica (grava em arquivo temporário, sobe o save atual para `.bak`, só então substitui o real) — se o processo morrer no meio, o pior caso é perder só a gravação mais recente, nunca a única cópia;
- 5 slots dedicados que nunca se sobrescrevem entre si: 3 slots manuais, 1 de autosave e 1 de debug (usado só pelos atalhos `F5`/`F9` do Editor, isolado dos slots "reais");
- se o save principal de um slot estiver ausente ou corrompido, o carregamento cai automaticamente para o `.bak`;
- `SaveMigrations` converte saves de versões antigas para a atual sem quebrá-los (ex.: `QuestSave.state` era um int cast do enum, agora é o nome do enum como string — resiliente a reordenar o enum);
- o que é salvo: classe, nível, experiência, vida e mana atuais e posição do jogador; inventário e equipamento (por id de item); estado e progresso de cada quest; ouro; itens de mundo já coletados;
- `GameManager.EnterWorld` é o ponto único de carregamento: recarrega a cena inteira (destruindo tudo local à cena — inimigos, spawners, UI) e só então reaplica o slot pedido (ou nenhum, para um "novo jogo" limpo). Inimigos não são serializados individualmente — eles renascem do zero nos spawners autorados, já com os stats corretos.

Ainda não existe uma UI de jogador para escolher, criar ou apagar slots — hoje o pipeline só é acionado por código/atalhos de debug.

## 14. Interface e feedback

### Implementado

- vida atual e máxima;
- Mana atual e máxima;
- experiência e nível;
- Momentum segmentado em seis unidades;
- painel de atributos;
- inventário e equipamentos;
- tooltips;
- barra de habilidades e cooldowns;
- barras de vida dos inimigos;
- dano flutuante normal e crítico (agora também quando o jogador recebe dano, em vermelho);
- tela de morte com contagem regressiva até o respawn;
- contador de ouro;
- janela de quests (aceitar, progresso, entregar, diálogo idle), indicador de disponibilidade acima da cabeça do NPC e tracker de quests ativas na tela;
- janela de loja e janela de confirmação de compra/venda.

O input `ToggleStats` está associado à tecla `C`, mas a abertura e o fechamento do painel de atributos ainda precisam ser conectados.

Existe também um painel de leitura rápida de todos os stats calculados (Attack Power, Spell Power, atributos totais, regens, etc.), gerado junto com a barra de habilidades — ferramenta de debug para balanceamento, não é a UI final de atributos.

## 15. Escopo do MVP

O MVP deve validar o ciclo principal. O jogador deverá:

1. iniciar como Villager;
2. evoluir até o nível 10;
3. ser promovido para Warrior;
4. experimentar o novo sistema de combate;
5. continuar evoluindo;
6. obter equipamentos e recursos;
7. concluir a primeira dungeon;
8. derrotar o primeiro chefe.

A experiência do MVP termina após o primeiro chefe. Novas classes, sistema completo de especializações e endgame ficam fora desse escopo.

## 16. Estado técnico atual

- Unity `6000.5.0f1`.
- Universal Render Pipeline 2D.
- Input System.
- Cinemachine.
- NavMesh 2D.
- TextMesh Pro e uGUI.
- Cena principal do build: `Assets/Scenes/SampleScene.unity`.
- Cena `Teste.unity`: ambiente mínimo de teste.
- Habilidades, passivas, itens, inimigos (`EnemyArchetypeSO`), quests (`QuestSO`) e lojas (`ShopSO`) definidos por ScriptableObjects.
- Sistema de habilidades e de recurso de combate generalizados internamente (recurso nomeável via `ResourceManager`, categorias de skill reutilizáveis via `SkillType`) para suportar futuras classes sem duplicar código.
- Persistência versionada com migração automática entre versões (`SaveService`/`SaveMigrations`), ver seção 13.
- Scripts de Editor (`Assets/Editor`) geram e conectam as canvases de UI (inventário, loja, confirmação de compra, quests, barra/livro de habilidades) em vez de montagem manual repetida no Inspector.

## 17. Matriz de implementação

| Sistema | Estado |
|---|---|
| Movimento e câmera | Implementado |
| Seleção Tab Target | Implementado |
| Aproximação automática (combate e NPCs) | Implementado |
| Habilidades e cooldowns | Implementado no protótipo |
| Momentum | Implementado no protótipo |
| IA de inimigos (perseguição, separação entre inimigos, telegraph à distância) | Implementado no protótipo |
| Experiência e nível | Parcial |
| Atributos (primário/secundário, RecalculateStats) | Implementado no protótipo |
| Inventário e equipamento | Implementado no protótipo |
| UI de combate | Implementado no protótipo |
| Quests (matar inimigos, 1 por NPC) | Implementado no protótipo |
| Loja e economia (ouro, compra, venda) | Implementado no protótipo |
| Salvamento e carregamento | Implementado no protótipo (multi-slot, com migração) |
| Promoção de classes | Planejado |
| Coleta de recursos | Planejado |
| Crafting e aprimoramento | Planejado |
| Dungeons, elites (conteúdo) e chefes | Planejado |
| Morte e respawn do jogador | Implementado no protótipo |
| Respawn de inimigos | Implementado no protótipo (exemplo) |

## 18. Decisões de design pendentes

- Momentum permanecerá com o Villager, será transferido ao Warrior ou será renomeado para Rage?
- As fórmulas de Strength/Agility/Intelligence (ver seção 7) já existem, mas os valores de escala e o crescimento por nível são placeholders — ainda precisam de balanceamento de design.
- Quais classes existirão e qual o `PrimaryAttribute` de cada uma? (Warrior é o próximo passo, mas Agility/Assassin e Intelligence/Mage ainda não têm data.)
- Morte e respawn do jogador já funcionam (sem penalidade, cura total e reposicionamento após alguns segundos); ainda falta decidir se haverá alguma penalidade por morrer.
- Quests hoje só suportam matar inimigos e um quest por NPC. Haverá outros tipos de objetivo (coleta, entrega, escolta)? Um NPC poderá oferecer mais de um quest?
- Como funcionarão crafting e aprimoramento?
- Quais requisitos liberam a primeira dungeon?
- Falta uma UI de jogador para escolher, criar e apagar slots de save — o pipeline hoje só é acionado por código/atalhos de debug.

## 19. Critério de manutenção deste documento

Este GDD deve ser atualizado quando uma mudança alterar regras de gameplay, controles, progressão, conteúdo, escopo do MVP, estado de implementação ou decisões técnicas relevantes. Refatorações internas sem impacto perceptível não exigem atualização.
