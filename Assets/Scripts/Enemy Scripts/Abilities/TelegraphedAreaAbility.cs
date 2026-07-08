using System.Collections;
using UnityEngine;

// Habilidade especial telegrafada: marca uma área no chão, espera, arremessa
// um efeito visual até lá, e só então aplica dano em quem ainda estiver dentro
// da área nesse instante (ex.: o ataque à distância do Orc) — o círculo fica
// congelado onde foi marcado, mas a checagem final usa a posição ATUAL do
// alvo; é essa diferença que permite desviar saindo da área a tempo.
[CreateAssetMenu(fileName = "New Telegraphed Area Ability", menuName = "Enemies/Abilities/Telegraphed Area")]
public class TelegraphedAreaAbility : EnemyAbility
{
    [Header("Dano")]
    public float damage = 10f;

    [Header("Timing")]
    public float windup = 1f;
    [Tooltip("Tempo de voo puramente visual entre o fim do windup e o dano resolver de fato.")]
    public float travelTime = 0.2f;

    [Header("Área")]
    public float areaRadius = 1.5f;
    [Tooltip("Deslocamento do transform do jogador até o ponto dos \"pés\" (centro da caixinha de contato).")]
    public Vector2 playerFeetOffset = new(0f, -0.48f);
    [Tooltip("Tamanho da caixinha de contato do jogador — o dano só é aplicado quando ela toca o círculo do ataque.")]
    public Vector2 playerHitboxSize = new(0.3f, 0.05f);

    [Header("Visuals (opcional)")]
    public GameObject telegraphPrefab;
    public GameObject projectilePrefab;

    public override IEnumerator Execute(EnemyStats casterStats, Transform caster, Transform target)
    {
        Vector3 groundPosition = GetFeetPosition(target);
        int sortingLayerID = ResolveSortingLayer(caster, target);

        GameObject telegraph = SpawnVisual(
            telegraphPrefab, groundPosition, areaRadius * 2f,
            new Color(1f, 0.15f, 0.1f, 0.55f), -1000000, sortingLayerID);

        if (windup > 0f)
            yield return new WaitForSeconds(windup);

        if (telegraph != null)
            Destroy(telegraph);

        Vector3 throwOrigin = caster.position;
        GameObject projectile = SpawnVisual(
            projectilePrefab, throwOrigin, 0.3f,
            new Color(0.25f, 0.15f, 0.1f, 1f), 1000000, sortingLayerID);

        float elapsed = 0f;

        while (elapsed < travelTime)
        {
            elapsed += Time.deltaTime;

            if (projectile != null)
                projectile.transform.position = Vector3.Lerp(
                    throwOrigin, groundPosition, travelTime > 0f ? elapsed / travelTime : 1f);

            yield return null;
        }

        if (projectile != null)
            Destroy(projectile);

        ResolveDamage(casterStats, target, groundPosition);
    }

    private void ResolveDamage(EnemyStats casterStats, Transform target, Vector3 groundPosition)
    {
        if (target == null)
            return;

        Vector2 liveFeet = GetFeetPosition(target);

        if (!CircleIntersectsBox(groundPosition, areaRadius, liveFeet, playerHitboxSize))
            return;

        IDamageable damageable = target.GetComponent<IDamageable>();
        EnemyDamage.TryDealDamage(damageable, damage, casterStats.CriticalChance, casterStats.CriticalDamage);
    }

    private Vector2 GetFeetPosition(Transform target)
    {
        return (Vector2)target.position + playerFeetOffset;
    }

    // Sorting Layer do alvo (o jogador pode estar numa Layer diferente da do
    // inimigo) — cai pro caster se o alvo não tiver SpriteRenderer.
    private static int ResolveSortingLayer(Transform caster, Transform target)
    {
        SpriteRenderer targetSprite = target != null ? target.GetComponent<SpriteRenderer>() : null;

        if (targetSprite != null)
            return targetSprite.sortingLayerID;

        SpriteRenderer casterSprite = caster != null ? caster.GetComponent<SpriteRenderer>() : null;
        return casterSprite != null ? casterSprite.sortingLayerID : 0;
    }

    // Instancia por uso (mesma tolerância a alocação por cast que EnemyProjectile
    // já tem) — sem prefab, cai num círculo placeholder colorido.
    private static GameObject SpawnVisual(
        GameObject prefab, Vector3 position, float placeholderDiameter,
        Color placeholderColor, int placeholderSortingOrder, int sortingLayerID)
    {
        if (prefab != null)
            return Instantiate(prefab, position, Quaternion.identity);

        GameObject instance = new("Ability Visual");
        instance.transform.position = position;
        instance.transform.localScale = new Vector3(placeholderDiameter, placeholderDiameter, 1f);

        SpriteRenderer sprite = instance.AddComponent<SpriteRenderer>();
        sprite.sprite = PlaceholderSpriteFactory.GetCircleSprite();
        sprite.color = placeholderColor;
        sprite.sortingOrder = placeholderSortingOrder;
        sprite.sortingLayerID = sortingLayerID;

        return instance;
    }

    private static bool CircleIntersectsBox(Vector2 circleCenter, float radius, Vector2 boxCenter, Vector2 boxSize)
    {
        Vector2 halfSize = boxSize * 0.5f;
        Vector2 min = boxCenter - halfSize;
        Vector2 max = boxCenter + halfSize;

        Vector2 closestPoint = new(
            Mathf.Clamp(circleCenter.x, min.x, max.x),
            Mathf.Clamp(circleCenter.y, min.y, max.y));

        return Vector2.Distance(circleCenter, closestPoint) <= radius;
    }
}
