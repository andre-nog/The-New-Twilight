using System;
using System.Collections.Generic;
using UnityEngine;

// DTOs puros pra JsonUtility — só campos serializáveis (listas, sem dicionário).
// version existe desde o v1 para permitir migração futura sem quebrar saves antigos.
[Serializable]
public class SaveData
{
    public int version = 1;
    public PlayerSave player = new();
    public List<ItemStackSave> inventory = new();
    public List<EquippedSave> equipment = new();
    public List<QuestSave> quests = new();
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
    public int state;
    public int progress;
}
