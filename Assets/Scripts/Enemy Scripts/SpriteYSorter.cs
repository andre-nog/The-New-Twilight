using UnityEngine;

public class SpriteYSorter : MonoBehaviour
{
    private static int counter = 0;

    private SpriteRenderer sr;
    private int tieBreaker;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();

        // Cada objeto recebe um desempate fixo
        tieBreaker = counter++ % 100;
    }

    void LateUpdate()
    {
        sr.sortingOrder = Mathf.RoundToInt(-transform.position.y * 1000f) + tieBreaker;
    }
}