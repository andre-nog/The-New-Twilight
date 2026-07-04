# Twilight — Game Design Document

> Status: documento vivo do projeto  
> Escopo: MVP para PC, single-player  
> Engine: Unity 6, URP 2D  
> Última atualização: 3 de julho de 2026

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
- respawn após morrer, através de um `EnemySpawner` reutilizável com um intervalo configurável e uma opção para desligar o respawn (pensada para dungeons, que não devem repor inimigos).

O protótipo atual usa um Orc corpo a corpo, que agora também possui uma habilidade telegrafada à distância: a cada 3 segundos de engajamento, ele marca a posição do jogador com uma área vermelha no chão, fica parado por 1 segundo e então arremessa algo nessa área — só quem ainda estiver dentro dela no momento do impacto toma dano, permitindo que o jogador desvie saindo a tempo. Arqueiros, magos, elites e chefes estão planejados.

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

Quests, ouro, coleta, crafting, aprimoramento, dungeons e chefes ainda não estão implementados no protótipo atual.

## 10. Itens, inventário e equipamentos

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

Não há persistência de inventário ou equipamentos entre sessões.

## 11. Interface e feedback

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
- tela de morte com contagem regressiva até o respawn.

O input `ToggleStats` está associado à tecla `C`, mas a abertura e o fechamento do painel de atributos ainda precisam ser conectados.

Existe também um painel de leitura rápida de todos os stats calculados (Attack Power, Spell Power, atributos totais, regens, etc.), gerado junto com a barra de habilidades — ferramenta de debug para balanceamento, não é a UI final de atributos.

## 12. Escopo do MVP

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

## 13. Estado técnico atual

- Unity `6000.5.0f1`.
- Universal Render Pipeline 2D.
- Input System.
- Cinemachine.
- NavMesh 2D.
- TextMesh Pro e uGUI.
- Cena principal do build: `Assets/Scenes/SampleScene.unity`.
- Cena `Teste.unity`: ambiente mínimo de teste.
- Habilidades, passivas e itens definidos por ScriptableObjects.
- Sistema de habilidades e de recurso de combate generalizados internamente (recurso nomeável via `ResourceManager`, categorias de skill reutilizáveis via `SkillType`) para suportar futuras classes sem duplicar código.

## 14. Matriz de implementação

| Sistema | Estado |
|---|---|
| Movimento e câmera | Implementado |
| Seleção Tab Target | Implementado |
| Aproximação automática | Implementado |
| Habilidades e cooldowns | Implementado no protótipo |
| Momentum | Implementado no protótipo |
| Inimigo corpo a corpo | Implementado |
| Experiência e nível | Parcial |
| Atributos (primário/secundário, RecalculateStats) | Implementado no protótipo |
| Inventário e equipamento | Implementado no protótipo |
| UI de combate | Implementado no protótipo |
| Promoção de classes | Planejado |
| Quests e ouro | Planejado |
| Coleta de recursos | Planejado |
| Crafting e aprimoramento | Planejado |
| Dungeons, elites e chefes | Planejado |
| Morte e respawn do jogador | Implementado no protótipo |
| Respawn de inimigos | Implementado no protótipo (exemplo) |
| Salvamento e carregamento | Planejado |

## 15. Decisões de design pendentes

- Momentum permanecerá com o Villager, será transferido ao Warrior ou será renomeado para Rage?
- As fórmulas de Strength/Agility/Intelligence (ver seção 7) já existem, mas os valores de escala e o crescimento por nível são placeholders — ainda precisam de balanceamento de design.
- Quais classes existirão e qual o `PrimaryAttribute` de cada uma? (Warrior é o próximo passo, mas Agility/Assassin e Intelligence/Mage ainda não têm data.)
- Morte e respawn do jogador já funcionam (sem penalidade, cura total e reposicionamento após alguns segundos); ainda falta decidir se haverá alguma penalidade por morrer.
- Qual será a estrutura de quests e recompensas?
- Como funcionarão crafting e aprimoramento?
- Quais requisitos liberam a primeira dungeon?
- Como será feita a persistência do progresso?

## 16. Critério de manutenção deste documento

Este GDD deve ser atualizado quando uma mudança alterar regras de gameplay, controles, progressão, conteúdo, escopo do MVP, estado de implementação ou decisões técnicas relevantes. Refatorações internas sem impacto perceptível não exigem atualização.
