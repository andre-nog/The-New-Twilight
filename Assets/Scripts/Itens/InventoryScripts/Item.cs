using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Item : MonoBehaviour
{
    [SerializeField]
    private ItemSO item;

    [SerializeField]
    private int quantity;

    // Id estável deste WorldItem autorado na cena — diferente de ItemSO.Id (que
    // identifica o TIPO de item, um asset), este identifica esta INSTÂNCIA
    // específica no mundo, pra WorldItemRegistry saber se ela já foi coletada
    // antes de um reload de cena recriá-la do zero. Gerado uma vez (GUID local,
    // não ligado a AssetDatabase — isto não é um asset) e nunca sobrescrito.
    [SerializeField, HideInInspector] private string worldItemId;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!string.IsNullOrEmpty(worldItemId))
            return;

        // Só gera id pra instâncias de CENA — o prefab-fonte (WorldItem.prefab, o
        // asset em si) nunca pode ganhar um id próprio, senão toda cópia nova
        // herdaria o MESMO id compartilhado em vez de um único por pickup autorado.
        if (PrefabUtility.IsPartOfPrefabAsset(gameObject))
            return;

        worldItemId = System.Guid.NewGuid().ToString();
        EditorUtility.SetDirty(this);
    }
#endif

    private InventoryManager inventoryManager;
    private bool canBePickedUp;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        // Já coletado numa sessão anterior (o save já tem o item) — não deixa
        // reaparecer depois de um reload, senão duplica (item no inventário E
        // ainda no chão).
        if (WorldItemRegistry.Instance != null && WorldItemRegistry.Instance.IsCollected(worldItemId))
        {
            Destroy(gameObject);
            return;
        }

        inventoryManager = InventoryManager.Instance;

        // Atualiza o sprite caso o ItemSO tenha sido atribuído pelo Inspector
        if (item != null)
        {
            spriteRenderer.sprite = item.itemSprite;
        }

        canBePickedUp = false;
        Invoke(nameof(EnablePickup), 0.3f);
    }

    private void EnablePickup()
    {
        canBePickedUp = true;
    }

    public void SetQuantity(int amount)
    {
        quantity = amount;
    }

    public void SetItem(ItemSO newItem)
    {
        item = newItem;

        if (spriteRenderer != null && item != null)
        {
            spriteRenderer.sprite = item.itemSprite;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!canBePickedUp)
            return;

        if (other.CompareTag("Player"))
        {
            int leftOverItems = inventoryManager.AddItem(item, quantity);

            if (leftOverItems <= 0)
            {
                if (WorldItemRegistry.Instance != null)
                    WorldItemRegistry.Instance.MarkCollected(worldItemId);

                Destroy(gameObject);
            }
            else
            {
                quantity = leftOverItems;
            }
        }
    }
}