using TMPro;
using UnityEngine;

// Uma entrada da lista do tracker: ícone + título (dourado) + objetivo (menor,
// embaixo do título) — mesmo papel que StatRowUI cumpre pro tooltip.
public class QuestTrackerRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text objectiveText;

    public void Configure(TMP_Text title, TMP_Text objective)
    {
        titleText = title;
        objectiveText = objective;
    }

    public void Setup(string title, string objective)
    {
        titleText.text = title;
        objectiveText.text = objective;
    }
}
