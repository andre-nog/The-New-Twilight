using UnityEngine;

public class DamageManager : MonoBehaviour
{
    public static DamageManager Instance;

    [SerializeField] private DamagePopup damagePopupPrefab;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void CreatePopup(Vector3 position, int damage, Color color)
    {
        DamagePopup popup = Instantiate(
            damagePopupPrefab,
            position,
            Quaternion.identity
        );

        popup.Setup(damage, color);
    }
}