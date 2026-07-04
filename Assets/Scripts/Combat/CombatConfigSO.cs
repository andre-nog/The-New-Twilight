using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Combat Config")]
public class CombatConfigSO : ScriptableObject
{
    [Tooltip("Constante K da mitigação de Armor: mitigação% = Armor / (Armor + K). Armor igual a K sempre dá 50% de redução, não importa a escala de dano.")]
    public float armorConstant = 100f;

    [Tooltip("Variação aleatória percentual aplicada ao dano final, pra cima e pra baixo (ex.: 5 = dano varia entre 95% e 105% do valor calculado).")]
    [Range(0f, 20f)]
    public float damageVariancePercent = 5f;
}
