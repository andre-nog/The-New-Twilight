// Contrato que todo manager com estado persistente implementa. SaveSystem só
// conhece esta interface — nunca os tipos concretos (Gold/Inventory/Equipment/...),
// então adicionar um sistema novo nunca exige mexer em SaveSystem/SaveData.
public interface ISaveParticipant
{
    // Estável entre versões — identifica a entrada no save (ex.: "gold", "inventory",
    // "skills.progression"). Nunca renomear um key já em uso sem migração própria.
    string SaveKey { get; }

    // Versão do DTO deste participante, independente da versão do envelope
    // (SaveData.version) — cada participante migra o próprio schema sozinho.
    int SchemaVersion { get; }

    // Ordem de aplicação no Load, menor primeiro. Banda 0 = padrão (a maioria dos
    // sistemas); 50/100 reservado pra sistemas com dependência de stat derivado
    // (Equipment/Inventory) — StatsManager.SetVitals e Player.position continuam
    // hardcoded como os dois passos finais do SaveSystem, fora deste loop.
    int Order { get; }

    // Chamado SÓ pelo SaveSystem, SÓ em Novo Jogo — nunca como efeito colateral de
    // Awake/OnEnable/Start. É aqui que "estado inicial de gameplay" é decidido.
    void InitializeNewGame();

    // Serializa o estado atual num JSON pequeno e opaco (JsonUtility.ToJson de um
    // DTO próprio do participante) — SaveSystem nunca vê o tipo concreto do DTO.
    string CaptureState();

    // Restaura a partir de um JSON capturado por uma versão (possivelmente antiga,
    // ver schemaVersion) deste mesmo participante. Migração de schema própria, se
    // schemaVersion < SchemaVersion, é responsabilidade de quem implementa.
    void RestoreState(string json, int schemaVersion);
}
