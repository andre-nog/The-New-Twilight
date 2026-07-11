#if UNITY_EDITOR
// Implementado por ScriptableObjects que precisam de um id estável (GUID do
// próprio asset) pra referência em save/load — ver ItemSO/Skill. Permite o
// AssetPostprocessor (Assets/Editor/StableAssetIdPostprocessor.cs) preencher o id
// automaticamente no import de QUALQUER tipo que implemente isto, sem precisar
// conhecer cada tipo concreto (Item, Skill, e o que vier depois — Talents, Pets...).
//
// Só existe no Editor — EnsureId() depende de AssetDatabase (ver implementações),
// que não existe em build. Nada em runtime consome esta interface.
public interface IStableAssetId
{
    void EnsureId();
}
#endif
