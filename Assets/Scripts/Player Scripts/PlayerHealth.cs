using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    // Inimigos assinam isso pra desaggrar e voltar ao spawn assim que o player morre,
    // em vez de continuar perseguindo/atacando um alvo "morto" até o respawn.
    public static event Action OnPlayerDied;

    // Disparado sempre que um inimigo acerta um golpe no player (independente do dano
    // final) — usado por skills de canal (ex.: Recovery) pra cancelar ao tomar dano.
    public static event Action OnPlayerDamaged;

    public float respawnDelay = 3f;

    private bool isDead;
    private float respawnTimer;

    private PlayerMovement playerMovement;
    private Player_Combat playerCombat;
    private PlayerSkillManager skillManager;
    private PlayerTargeting playerTargeting;
    private Collider2D playerCollider;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    private GameObject deathOverlay;
    private TMP_Text deathText;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerCombat = GetComponent<Player_Combat>();
        skillManager = GetComponent<PlayerSkillManager>();
        playerTargeting = GetComponent<PlayerTargeting>();
        playerCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        if (GameManager.Instance != null)
            GameManager.Instance.RegisterPlayer(transform);
    }

    private void OnEnable()
    {
        if (StatsManager.Instance != null)
        {
            StatsManager.Instance.OnStatsChanged += RefreshUI;
            RefreshUI();
        }
        else
        {
            Debug.LogWarning("PlayerHealth: StatsManager.Instance era null no OnEnable.");
        }
    }

    private void OnDisable()
    {
        if (StatsManager.Instance != null)
            StatsManager.Instance.OnStatsChanged -= RefreshUI;
    }

    // deathOverlay é um GameObject raiz solto (não é filho do player) — sem isso,
    // destruir o player no respawn deixaria a tela de "Você morreu" travada pra sempre.
    private void OnDestroy()
    {
        if (deathOverlay != null)
            Destroy(deathOverlay);
    }

    private void Update()
    {
        if (!isDead)
            return;

        respawnTimer -= Time.deltaTime;
        UpdateDeathText();

        if (respawnTimer <= 0f)
            GameManager.Instance.RespawnPlayer();
    }

    // A vida em si mora no StatsManager (MaxHealth é derivado dos atributos) —
    // este componente é a "porta de entrada" de dano do lado do jogador.
    public float Armor => StatsManager.Instance != null ? StatsManager.Instance.Armor : 0f;
    public bool IsAlive => !isDead;

    public void TakeDamage(DamageResult result)
    {
        OnPlayerDamaged?.Invoke();

        StatsManager.Instance.ChangeHealth(-result.FinalDamage);

        // Laranja-avermelhado no crítico de inimigo, pra diferenciar do hit normal.
        Color popupColor = result.IsCritical
            ? new Color(1f, 0.45f, 0.1f)
            : Color.red;

        DamageManager.Instance.CreatePopup(
            transform.position + Vector3.up * 0.5f,
            result.FinalDamage,
            popupColor);
    }

    public void ChangeHealth(int amount)
    {
        StatsManager.Instance.ChangeHealth(amount);

        if (amount < 0)
        {
            DamageManager.Instance.CreatePopup(
                transform.position + Vector3.up * 0.5f,
                -amount,
                Color.red);
        }
    }

    private void RefreshUI()
    {
        if (StatsManager.Instance.currentHealth <= 0 && !isDead)
            Die();
    }

    private void Die()
    {
        isDead = true;
        respawnTimer = respawnDelay;

        SetControlEnabled(false);
        SetVisualEnabled(false);
        ZeroVelocity();
        ShowDeathOverlay();

        // Sem isso, o círculo de seleção do inimigo fica aceso enquanto o player está
        // morto — não bloqueia o respawn (a instância nova nasce com currentTarget nulo),
        // mas é um resíduo visual enganoso: parece que o alvo continua "selecionado".
        if (playerTargeting != null)
            playerTargeting.ClearTarget();

        OnPlayerDied?.Invoke();
    }

    // Sem isso o player continuaria deslizando após morrer — o Rigidbody2D dele não
    // tem damping e PlayerMovement (que normalmente zera a velocidade) fica desabilitado.
    private void ZeroVelocity()
    {
        if (playerMovement != null && playerMovement.rb != null)
            playerMovement.rb.linearVelocity = Vector2.zero;
    }

    private void SetControlEnabled(bool value)
    {
        if (playerMovement != null)
            playerMovement.enabled = value;

        if (playerCombat != null)
            playerCombat.enabled = value;

        if (skillManager != null)
            skillManager.enabled = value;

        // Desabilitar o collider também tira o player da detecção de inimigos
        // (Physics2D.OverlapCircle contra a playerLayer) enquanto está "morto".
        if (playerCollider != null)
            playerCollider.enabled = value;
    }

    // Some com o sprite em vez de destruir o GameObject — assim câmera, singletons e
    // outras referências ao player continuam válidas durante o cooldown de respawn.
    private void SetVisualEnabled(bool value)
    {
        if (spriteRenderer != null)
            spriteRenderer.enabled = value;

        if (animator != null)
            animator.enabled = value;
    }

    private void UpdateDeathText()
    {
        if (deathText == null)
            return;

        int secondsLeft = Mathf.CeilToInt(Mathf.Max(0f, respawnTimer));
        deathText.text = $"Você morreu\nRenascendo em {secondsLeft}s...";
    }

    private void ShowDeathOverlay()
    {
        if (deathOverlay == null)
            BuildDeathOverlay();

        deathOverlay.SetActive(true);
        UpdateDeathText();
    }

    private void BuildDeathOverlay()
    {
        GameObject canvasObject = new("Death Screen Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform background = new GameObject("Background", typeof(RectTransform)).GetComponent<RectTransform>();
        background.SetParent(canvasObject.transform, false);
        background.anchorMin = Vector2.zero;
        background.anchorMax = Vector2.one;
        background.offsetMin = Vector2.zero;
        background.offsetMax = Vector2.zero;

        Image backgroundImage = background.gameObject.AddComponent<Image>();
        backgroundImage.color = new Color(0f, 0f, 0f, 0.75f);

        RectTransform textRect = new GameObject("Death Text", typeof(RectTransform)).GetComponent<RectTransform>();
        textRect.SetParent(background, false);
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(900f, 200f);
        textRect.anchoredPosition = Vector2.zero;

        deathText = textRect.gameObject.AddComponent<TextMeshProUGUI>();
        deathText.alignment = TextAlignmentOptions.Center;
        deathText.fontSize = 48f;
        deathText.color = Color.white;

        deathOverlay = canvasObject;
        deathOverlay.SetActive(false);
    }
}
