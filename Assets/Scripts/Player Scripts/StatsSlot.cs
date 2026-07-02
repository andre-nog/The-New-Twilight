using TMPro;
using UnityEngine;

public class StatsSlot : MonoBehaviour
{
    [SerializeField]
    private TMP_Text statValue;

    public void SetValue(int value)
    {
        statValue.text = value.ToString();
    }
}