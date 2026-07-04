using UnityEngine;

// Único lugar do projeto com referência serializada ao CombatConfigSO — mesmo
// padrão de singleton já usado por StatsManager/EquipmentManager/DamageManager/
// TooltipManager. Diferente de carregar por Resources.Load, essa referência é
// visível no Inspector e rastreável pela Unity (Find References, etc.).
public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance;

    [SerializeField] private CombatConfigSO combatConfig;

    public static CombatConfigSO CombatConfig =>
        Instance != null ? Instance.combatConfig : null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // Recompilar scripts no Editor zera campos static (domain reload) sem rodar Awake()
    // de novo para objetos que já existiam na cena — só OnEnable roda.
    private void OnEnable()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
