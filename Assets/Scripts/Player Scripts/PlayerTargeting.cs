using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerTargeting : MonoBehaviour
{
    public InputActionReference selectTarget;
    public InputActionReference targetNext;
    public GameObject currentTarget;
    private Color originalColor;

    private void Update()
    {
        if (selectTarget.action.WasPressedThisFrame())
        {
            CheckEnemyClick();
        }

        if (targetNext.action.WasPressedThisFrame())
        {
            SelectNextEnemy();
        }
    }

    [SerializeField] private LayerMask enemyLayerMask;

    private void CheckEnemyClick()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(
            Mouse.current.position.ReadValue());

        // ALTERADO: OverlapPoint devolve só 1 collider e a escolha entre
        // sobrepostos não tem nenhuma relação com sortingOrder (física e
        // rendering são sistemas independentes). OverlapPointAll pega TODOS
        // os colliders no ponto, daí escolhemos manualmente o de cima.
        Collider2D[] hits = Physics2D.OverlapPointAll(mousePos, enemyLayerMask); // ALTERADO

        if (hits.Length == 0) // ALTERADO
            return;

        GameObject bestTarget = null; // NOVO
        int bestSortingOrder = int.MinValue; // NOVO

        foreach (Collider2D hit in hits) // NOVO
        {
            GameObject hitObject = hit.attachedRigidbody != null
                ? hit.attachedRigidbody.gameObject
                : hit.gameObject;

            if (!hitObject.CompareTag("Enemy"))
                continue; // NOVO

            SpriteRenderer sr = hitObject.GetComponent<SpriteRenderer>(); // NOVO
            int sortingOrder = sr != null ? sr.sortingOrder : int.MinValue; // NOVO

            // NOVO: maior sortingOrder = desenhado por cima = visualmente na frente
            if (sortingOrder > bestSortingOrder) // NOVO
            {
                bestSortingOrder = sortingOrder; // NOVO
                bestTarget = hitObject; // NOVO
            }
        }

        if (bestTarget != null) // ALTERADO
        {
            SelectTarget(bestTarget); // ALTERADO
        }
    }

    private void SelectTarget(GameObject target)
    {
        if (target == currentTarget)
            return;

        if (currentTarget != null)
        {
            SpriteRenderer oldSr = currentTarget.GetComponent<SpriteRenderer>();

            if (oldSr != null)
            {
                oldSr.color = originalColor;
            }
        }

        currentTarget = target;

        SpriteRenderer sr = currentTarget.GetComponent<SpriteRenderer>();

        if (sr != null)
        {
            originalColor = sr.color;
            sr.color = Color.red;
        }

        Debug.Log("Inimigo selecionado: " + target.name);
    }

    private void SelectNextEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        if (enemies.Length == 0)
            return;

        List<GameObject> visibleEnemies = new();

        foreach (GameObject enemy in enemies)
        {
            Vector3 viewportPos =
                Camera.main.WorldToViewportPoint(enemy.transform.position);

            bool isVisible =
                viewportPos.x >= 0 &&
                viewportPos.x <= 1 &&
                viewportPos.y >= 0 &&
                viewportPos.y <= 1 &&
                viewportPos.z > 0;

            if (isVisible)
            {
                visibleEnemies.Add(enemy);
            }
        }

        if (visibleEnemies.Count == 0)
            return;

        visibleEnemies.Sort((a, b) =>
            Vector2.Distance(transform.position, a.transform.position)
            .CompareTo(
                Vector2.Distance(transform.position, b.transform.position)));

        if (currentTarget == null)
        {
            SelectTarget(visibleEnemies[0]);
            return;
        }

        int currentIndex = visibleEnemies.IndexOf(currentTarget);

        if (currentIndex == -1)
        {
            SelectTarget(visibleEnemies[0]);
            return;
        }

        int nextIndex = (currentIndex + 1) % visibleEnemies.Count;

        SelectTarget(visibleEnemies[nextIndex]);
    }
}