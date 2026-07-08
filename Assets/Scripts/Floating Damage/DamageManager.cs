using UnityEngine;
using UnityEngine.Pool;

public class DamageManager : MonoBehaviour
{
    public static DamageManager Instance;

    [SerializeField] private DamagePopup damagePopupPrefab;

    // Popups aparecem a cada hit — reciclar via pool evita Instantiate/Destroy
    // (e o lixo de GC correspondente) no caminho mais quente do combate.
    private ObjectPool<DamagePopup> pool;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // Recompilar scripts no Editor zera campos static/não-seriais (domain reload)
    // sem rodar Awake() de novo — mesmo padrão dos outros managers.
    private void OnEnable()
    {
        if (Instance != null && Instance != this)
            return;

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void CreatePopup(Vector3 position, int damage, Color color)
    {
        CreatePopup(position, damage.ToString(), color, isReward: false);
    }

    // Recompensas fora de dano (ouro/XP ao matar um inimigo) — mesmo visual/pool
    // do dano, mas menor e sempre desenhado atrás dele (ver DamagePopup).
    public void CreateRewardPopup(Vector3 position, string text, Color color)
    {
        CreatePopup(position, text, color, isReward: true);
    }

    private void CreatePopup(Vector3 position, string text, Color color, bool isReward)
    {
        // Criado sob demanda (e não no Awake) porque domain reload zera o campo.
        pool ??= new ObjectPool<DamagePopup>(
            createFunc: () => Instantiate(damagePopupPrefab, transform),
            actionOnGet: popup => popup.gameObject.SetActive(true),
            actionOnRelease: popup => popup.gameObject.SetActive(false),
            actionOnDestroy: popup => Destroy(popup.gameObject),
            defaultCapacity: 16);

        DamagePopup popup = pool.Get();
        popup.transform.SetPositionAndRotation(position, Quaternion.identity);
        popup.Setup(text, color, isReward, ReleasePopup);
    }

    private void ReleasePopup(DamagePopup popup)
    {
        pool.Release(popup);
    }
}
