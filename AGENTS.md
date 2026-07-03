# Instruções para agentes

## Fonte de verdade do projeto

Antes de planejar ou implementar mudanças, leia [`GDD.md`](./GDD.md). Use-o como fonte de verdade para visão, escopo, regras de gameplay, sistemas implementados e decisões ainda pendentes.

Não trate sistemas marcados como **planejados** ou **parciais** como se já estivessem concluídos. Quando o código e o GDD divergirem, verifique o estado real do projeto e registre claramente a divergência.

## Manutenção do GDD

Atualize `GDD.md` na mesma alteração sempre que o trabalho modificar de forma relevante:

- regras ou fórmulas de gameplay;
- controles ou comportamento do jogador;
- habilidades, recursos, classes ou progressão;
- itens, inimigos, quests, dungeons ou conteúdo;
- interface percebida pelo jogador;
- escopo ou estado de implementação do MVP;
- decisões técnicas que ajudem futuros colaboradores a entender o projeto.

Não é necessário atualizar o GDD para refatorações internas, correções de formatação ou mudanças sem impacto no design, no comportamento ou no estado documentado.

Ao atualizar o GDD:

1. preserve a distinção entre **implementado**, **parcial** e **planejado**;
2. atualize a data no início do documento;
3. evite inventar decisões de design que ainda não foram tomadas;
4. mantenha o texto em português e os nomes técnicos usados no projeto;
5. prefira alterar a seção específica em vez de acrescentar notas soltas ao final.

## Cuidados com Unity

- Preserve arquivos `.meta` e seus GUIDs.
- Evite renomear classes `MonoBehaviour`, campos serializados ou Animation Events sem migrar as referências.
- Faça mudanças incrementais e verifique cenas, prefabs e ScriptableObjects afetados.
- A cena principal do build é `Assets/Scenes/SampleScene.unity`.
