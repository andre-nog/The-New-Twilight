using UnityEngine;

public class Item : MonoBehaviour
{
    [SerializeField]
    private ItemSO item;

    [SerializeField]
    private int quantity;

    private InventoryManager inventoryManager;
    private bool canBePickedUp;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    private void Start()
    {
        inventoryManager = InventoryManager.Instance;

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

        if (spriteRenderer != null)
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
                Destroy(gameObject);
            }
            else
            {
                quantity = leftOverItems;
            }
        }
    }
}