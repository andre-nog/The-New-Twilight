using TMPro;
using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    [SerializeField] private TextMeshPro textMesh;
    [SerializeField] private MeshRenderer meshRenderer;

    // Dano tem que ficar sempre acima de popups de recompensa (ouro/XP), mesmo
    // quando os dois nascem quase na mesma posição — sortingOrder maior sempre
    // desenha na frente dentro da mesma Sorting Layer, independente da posição Y
    // (que muda a cada frame conforme o popup sobe, então não dava pra confiar
    // nela pra garantir prioridade).
    private const int DamageSortingOrder = 1;
    private const int RewardSortingOrder = 0;

    // Popups de recompensa usam uma fração do tamanho de fonte autorado no
    // prefab — cacheado uma vez (não a partir do valor atual, que um Setup
    // anterior no mesmo objeto pooled pode ter deixado menor).
    private const float RewardFontScale = 0.6f;
    private float baseFontSize;

    [Header("Lifetime (Damage)")]
    [SerializeField] private float lifetime = 1.2f;

    [Header("Movement (Damage)")]
    [SerializeField] private float startSpeed = 1.4f;
    [SerializeField] private float endSpeed = 0.15f;

    [Header("Scale (Damage)")]
    [SerializeField] private float maxScale = 1.2f;

    [Header("Curve (Damage)")]
    [SerializeField] private float maxCurve = 0.18f;

    // Perfil de recompensa (XP/ouro): bem menor, mais perto do inimigo, some rápido —
    // não deve competir com o número de dano, que continua sendo o popup "principal".
    [Header("Reward Movement (XP/Gold)")]
    [SerializeField] private float rewardLifetime = 0.7f;
    [SerializeField] private float rewardStartSpeed = 0.5f;
    [SerializeField] private float rewardEndSpeed = 0.1f;
    [SerializeField] private float rewardMaxScale = 1.05f;
    [SerializeField] private float rewardMaxCurve = 0.04f;

    // Resolvidos em Setup a partir do perfil (dano ou recompensa) — Update() só lê
    // estes, sem precisar checar isReward a cada frame.
    private float activeLifetime;
    private float activeStartSpeed;
    private float activeEndSpeed;
    private float activeMaxScale;
    private float activeMaxCurve;

    private float timer;
    private float curveDirection;

    // Devolve o popup ao pool do DamageManager em vez de Destroy — o mesmo objeto
    // é reusado, então Setup precisa resetar tudo que o Update anterior mexeu.
    private System.Action<DamagePopup> onFinished;

    private void Awake()
    {
        if (baseFontSize <= 0f)
            baseFontSize = textMesh.fontSize;
    }

    public void Setup(string text, Color color, bool isReward, System.Action<DamagePopup> onFinished)
    {
        this.onFinished = onFinished;

        textMesh.text = text;
        textMesh.fontSize = isReward ? baseFontSize * RewardFontScale : baseFontSize;
        meshRenderer.sortingOrder = isReward ? RewardSortingOrder : DamageSortingOrder;

        // Mathf.Max: um lifetime configurado (ou serializado) como 0 faria
        // timer/activeLifetime virar Infinity/NaN em Update() e travar a posição do
        // popup em NaN — nunca deixa activeLifetime chegar a zero, nem por engano.
        activeLifetime = Mathf.Max(0.05f, isReward ? rewardLifetime : lifetime);
        activeStartSpeed = isReward ? rewardStartSpeed : startSpeed;
        activeEndSpeed = isReward ? rewardEndSpeed : endSpeed;
        activeMaxScale = isReward ? rewardMaxScale : maxScale;
        activeMaxCurve = isReward ? rewardMaxCurve : maxCurve;

        // Alpha explícito: o uso anterior pode ter terminado com fade quase em zero.
        color.a = 1f;
        textMesh.color = color;

        transform.localScale = Vector3.one;

        timer = activeLifetime;

        // Escolhe apenas se a curva será para esquerda ou direita
        curveDirection = Random.value < 0.5f ? -1f : 1f;
    }

    private void Update()
    {
        timer -= Time.deltaTime;

        // Não confia em activeLifetime já vir seguro (campo não-serializado — um
        // recompile de script em Play Mode zera ele mesmo num popup já em voo,
        // sem passar de novo por Setup). Divisão por zero aqui vira Infinity/NaN
        // e trava a posição do popup pro resto da vida dele.
        float safeLifetime = activeLifetime > 0f ? activeLifetime : 1f;
        float progress = 1f - (timer / safeLifetime);

        // -------------------------
        // Movimento vertical
        // -------------------------
        float speed = Mathf.Lerp(activeStartSpeed, activeEndSpeed, progress);

        transform.position += Vector3.up * speed * Time.deltaTime;

        // -------------------------
        // Curva suave
        // -------------------------
        float curve =
            Mathf.Sin(progress * Mathf.PI) *
            activeMaxCurve *
            curveDirection;

        transform.position += Vector3.right * curve * Time.deltaTime;

        // -------------------------
        // Escala
        // -------------------------
        float scale = Mathf.Lerp(
            1f,
            activeMaxScale,
            Mathf.Sin(progress * Mathf.PI)
        );

        transform.localScale = Vector3.one * scale;

        // -------------------------
        // Fade apenas no final
        // -------------------------
        Color color = textMesh.color;

        float fadeStart = safeLifetime * 0.4f;

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