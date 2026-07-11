using System;
using System.Collections.Generic;
using UnityEngine;

// DTOs puros pra JsonUtility — só campos serializáveis (listas, sem dicionário).
// version existe desde o v1 para permitir migração futura sem quebrar saves antigos.
//
// v3: inventory/equipment/quests/gold/collectedWorldItems deixaram de ser campos
// nomeados aqui — cada um agora viaja como um SaveEntry opaco em "entries",
// capturado/restaurado por um ISaveParticipant (ver SaveRegistry/SaveSystem).
// Adicionar um sistema persistente novo não toca mais este arquivo. Saves v1/v2
// são migrados por SaveMigrations — ver SaveService.TryLoadFrom.
[Serializable]
public class SaveData
{
    public int version = 3;

    // Não consumido hoje (jogo é cena única) — gravado desde já pra quando dungeons
    // existirem e o load precisar saber qual cena carregar antes de aplicar o resto.
    public string sceneId;

    // Player fica fora do loop de participantes de propósito — é dono do GameManager/
    // entidade Player, não um manager plugável (ver StatsManager.SetVitals/Player.position
    // como os dois passos finais hardcoded de SaveSystem.LoadInto).
    public PlayerSave player = new();

    public List<SaveEntry> entries = new();
}

// Payload opaco de um ISaveParticipant — SaveSystem nunca olha dentro do json,
// só roteia pelo key. schemaVersion é do PRÓPRIO participante, independente da
// versão do envelope (SaveData.version) acima.
[Serializable]
public class SaveEntry
{
    public string key;
    public int schemaVersion;
    public string json;
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

// DTO de transferência entre StatsManager e SaveSystem (não faz parte da árvore
// JSON diretamente — SaveSystem copia estes campos pra dentro de PlayerSave).
// StatsManager fica fora da interface ISaveParticipant de propósito — ver
// comentário em SaveSystem.LoadInto sobre vitals/posição serem os passos finais.
public class StatsSave
{
    public string classId;
    public int level;
    public int currentHealth;
    public int currentMana;
}
