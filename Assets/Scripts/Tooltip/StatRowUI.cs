using TMPro;
using UnityEngine;

public class StatRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text statName;
    [SerializeField] private TMP_Text statValue;

    public void Setup(string name, string value)
    {
        statName.text = name;
        statName.color = Color.white;
        statValue.text = value;
        statValue.color = Color.white;
    }
}