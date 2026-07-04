using TMPro;
using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    [SerializeField] private TextMeshPro textMesh;

    [Header("Lifetime")]
    [SerializeField] private float lifetime = 1.2f;

    [Header("Movement")]
    [SerializeField] private float startSpeed = 1.4f;
    [SerializeField] private float endSpeed = 0.15f;

    [Header("Scale")]
    [SerializeField] private float maxScale = 1.2f;

    [Header("Curve")]
    [SerializeField] private float maxCurve = 0.18f;

    private float timer;
    private float curveDirection;

    // Devolve o popup ao pool do DamageManager em vez de Destroy — o mesmo objeto
    // é reusado, então Setup precisa resetar tudo que o Update anterior mexeu.
    private System.Action<DamagePopup> onFinished;

    public void Setup(int damage, Color color, System.Action<DamagePopup> onFinished)
    {
        this.onFinished = onFinished;

        textMesh.text = damage.ToString();

        // Alpha explícito: o uso anterior pode ter terminado com fade quase em zero.
        color.a = 1f;
        textMesh.color = color;

        transform.localScale = Vector3.one;

        timer = lifetime;

        // Escolhe apenas se a curva será para esquerda ou direita
        curveDirection = Random.value < 0.5f ? -1f : 1f;
    }

    private void Update()
    {
        timer -= Time.deltaTime;

        float progress = 1f - (timer / lifetime);

        // -------------------------
        // Movimento vertical
        // -------------------------
        float speed = Mathf.Lerp(startSpeed, endSpeed, progress);

        transform.position += Vector3.up * speed * Time.deltaTime;

        // -------------------------
        // Curva suave
        // -------------------------
        float curve =
            Mathf.Sin(progress * Mathf.PI) *
            maxCurve *
            curveDirection;

        transform.position += Vector3.right * curve * Time.deltaTime;

        // -------------------------
        // Escala
        // -------------------------
        float scale = Mathf.Lerp(
            1f,
            maxScale,
            Mathf.Sin(progress * Mathf.PI)
        );

        transform.localScale = Vector3.one * scale;

        // -------------------------
        // Fade apenas no final
        // -------------------------
        Color color = textMesh.color;

        float fadeStart = lifetime * 0.4f;

        if (timer > fadeStart)
        {
            color.a = 1f;
        }
        else
        {
            color.a = timer / fadeStart;
        }

        textMesh.color = color;

        if (timer <= 0f)
        {
            if (onFinished != null)
                onFinished(this);
            else
                Destroy(gameObject); // fallback se alguém instanciar fora do pool
        }
    }
}