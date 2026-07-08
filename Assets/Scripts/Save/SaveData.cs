using System;
using System.Collections.Generic;
using UnityEngine;

// DTOs puros pra JsonUtility — só campos serializáveis (listas, sem dicionário).
// version existe desde o v1 para permitir migração futura sem quebrar saves antigos.
[Serializable]
public class SaveData
{
    // v2: QuestSave.state passou de int (cast direto do enum, frágil a reordenar
    // QuestState) pra string (nome do enum). Saves v1 são migrados por
    // SaveMigrations — ver SaveService.TryLoadFrom.
    public int version = 2;

    // Não consumido hoje (jogo é cena única) — gravado desde já pra quando dungeons
    // existirem e o load precisar saber qual cena carregar antes de aplicar o resto.
    public string sceneId;

    public PlayerSave player = new();
    public List<ItemStackSave> inventory = new();
    public List<EquippedSave> equipment = new();
    public List<QuestSave> quests = new();
    public int gold;

    // Ids de WorldItem (Item.cs) já coletados — sem isso, um reload de cena
    // recria itens autorados já pegos, duplicando-os. Ver WorldItemRegistry.
    // Campo puramente aditivo — não precisou de bump de versão (v2 continua
    // valendo, um save antigo sem este campo só volta com a lista vazia).
    public List<string> collectedWorldItems = new();
}

[Serializable]
public class PlayerSave
{
    public string classId;
    public int level;
    public int currentExp;
    public int currentHealth;
    public int currentMana;
    public Vector3 position;
}

[Serializable]
public class ItemStackSave
{
    public int slotIndex;
    public string itemId;
    public int quantity;
}

[Serializable]
public class EquippedSave
{
    public int slotIndex;
    public string itemId;
}

[Serializable]
public class QuestSave
{
    public string questId;

    // Nome do enum QuestState (não o int cast) — reordenar o enum nunca corrompe
    // um save antigo. Ver SaveMigrations pra saves v1 (que guardavam int).
    public string state;

    public int progress;
}

// DTO de transferência entre StatsManager e GameManager (não faz parte da árvore
// JSON diretamente — GameManager copia estes campos pra dentro de PlayerSave).
// Existe pra GameManager parar de ler currentClass/level/currentHealth/currentMana
// direto de StatsManager, mesmo contrato GetState/ApplyState de InventoryManager/
// EquipmentManager/QuestManager/GoldManager.
public class StatsSave
{
    public string classId;
    public int level;
    public int currentHealth;
    public int currentMana;
}
