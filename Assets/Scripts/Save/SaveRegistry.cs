using System.Collections.Generic;

// Registro passivo de ISaveParticipant atualmente vivos na cena. Cada participante
// se auto-registra no próprio Awake()/OnDestroy() — SaveSystem nunca caça managers
// por Instance/FindAnyObjectByType. Funciona sem depender de ordem de execução
// porque a Unity garante que todo OnDestroy() da cena antiga termina antes de
// qualquer Awake() da cena nova começar (SceneManager.LoadScene modo Single).
public static class SaveRegistry
{
    private static readonly List<ISaveParticipant> participants = new();

    public static void Register(ISaveParticipant participant)
    {
        if (participant == null || participants.Contains(participant))
            return;

        participants.Add(participant);
    }

    public static void Unregister(ISaveParticipant participant)
    {
        participants.Remove(participant);
    }

    public static bool Has(string key)
    {
        foreach (ISaveParticipant participant in participants)
        {
            if (participant.SaveKey == key)
                return true;
        }

        return false;
    }

    // Cópia ordenada por Order (menor primeiro) — nunca a lista interna direto, pra
    // quem itera não conseguir mutar o registro por acidente.
    public static List<ISaveParticipant> OrderedParticipants()
    {
        List<ISaveParticipant> ordered = new(participants);
        ordered.Sort((a, b) => a.Order.CompareTo(b.Order));
        return ordered;
    }
}
