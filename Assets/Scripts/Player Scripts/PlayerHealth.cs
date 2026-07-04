using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    // Inimigos assinam isso pra desaggrar e voltar ao spawn assim que o player morre,
    // em vez de continuar perseguindo/atacando um alvo "morto" até o respawn.
    public static event Action OnPlayerDied;

    public TMP_Text healthText;
    public Transform respawnPoint;
    public float respawnDelay = 3f;

    private bool isDead;
    private float respawnTimer;

    private PlayerMovement playerMovement;
    private Player_Combat playerCombat;
    private PlayerSkillManager skillManager;
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
        playerCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
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

    private void Update()
    {
        if (!isDead)
            return;

        respawnTimer -= Time.deltaTime;
        UpdateDeathText();

        if (respawnTimer <= 0f)
            Respawn();
    }

    public void ChangeHealth(int amount)
    {
        StatsManager.Instance.ChangeHealth(amount);

        // Mesmo popup de dano flutuante que Enemy_Health já usa — só a cor muda
        // (sempre vermelho, já que ataque de inimigo não tem crítico hoje).
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
        healthText.text = "HP: " + StatsManager.Instance.currentHealth + " / " + StatsManager.Instance.MaxHealth;

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

        OnPlayerDied?.Invoke();
    }

    private void Respawn()
    {
        isDead = false;

        if (respawnPoint != null)
            transform.position = respawnPoint.position;

        ZeroVelocity();
        StatsManager.Instance.FullHeal();
        StatsManager.Instance.RestoreFullMana();

        SetControlEnabled(true);
        SetVisualEnabled(true);
        HideDeathOverlay();
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

    private void HideDeathOverlay()
    {
        if (deathOverlay != null)
            deathOverlay.SetActive(false);
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
