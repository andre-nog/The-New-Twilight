using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

// Ponto único pra montar um inimigo novo: pega um EnemyArchetypeSO já pronto +
// até 3 clipes de animação, duplica o prefab-template genérico, gera o Animator
// Override Controller e salva o prefab final — sem tocar em Inspector fora disso.
//
// Também é dono do bootstrap dos dois assets compartilhados que o template e
// todo override dependem (EnemyBase.controller e _EnemyTemplate.prefab):
// construídos via API do Editor (AnimatorController/PrefabUtility), não por YAML
// escrito à mão — evita o risco de um grafo de Animator mal-formado que só se
// descobre quebrado dentro do Editor.
public class EnemySetupTool : EditorWindow
{
    private const string BaseControllerPath = "Assets/Animations/EnemyBase.controller";
    private const string PlaceholderClipFolder = "Assets/Animations/EnemyBase";
    private const string TemplatePrefabPath = "Assets/Prefab/Enemies/_EnemyTemplate.prefab";
    private const string OverrideControllerFolder = "Assets/Animations/Overrides";
    private const string NewPrefabFolder = "Assets/Prefab/Enemies";

    private const int EnemyLayer = 6; // "Enemy" (ver ProjectSettings/TagManager.asset)
    private const string EnemyTag = "Enemy";
    private const string UILayerName = "UI";

    private string enemyName = "";
    private EnemyArchetypeSO archetype;
    private AnimationClip idleClip;
    private AnimationClip walkClip;
    private AnimationClip attackClip;
    private Sprite spriteOverride;

    [MenuItem("Tools/Enemies/Setup New Enemy")]
    private static void ShowWindow()
    {
        GetWindow<EnemySetupTool>("Setup New Enemy");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("1. Identidade", EditorStyles.boldLabel);
        enemyName = EditorGUILayout.TextField("Nome (vira o nome do arquivo)", enemyName);
        archetype = (EnemyArchetypeSO)EditorGUILayout.ObjectField(
            "Archetype", archetype, typeof(EnemyArchetypeSO), false);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("2. Animação (opcional)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Se os três clipes forem preenchidos, o tool gera um Animator Override Controller " +
            "automaticamente. Deixe em branco pra criar o prefab só com a base (sem animação " +
            "própria) e ajustar depois.",
            MessageType.None);
        idleClip = (AnimationClip)EditorGUILayout.ObjectField("Idle", idleClip, typeof(AnimationClip), false);
        walkClip = (AnimationClip)EditorGUILayout.ObjectField("Walk", walkClip, typeof(AnimationClip), false);
        attackClip = (AnimationClip)EditorGUILayout.ObjectField("Attack", attackClip, typeof(AnimationClip), false);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("3. Visual (opcional)", EditorStyles.boldLabel);
        spriteOverride = (Sprite)EditorGUILayout.ObjectField(
            "Sprite inicial (senão usa archetype.defaultSprite)", spriteOverride, typeof(Sprite), false);

        EditorGUILayout.Space();

        bool canCreate = !string.IsNullOrWhiteSpace(enemyName) && archetype != null;

        using (new EditorGUI.DisabledScope(!canCreate))
        {
            if (GUILayout.Button("Create Enemy", GUILayout.Height(32)))
                CreateEnemy();
        }

        if (!canCreate)
            EditorGUILayout.HelpBox("Preencha o nome e o Archetype pra habilitar a criação.", MessageType.Info);
    }

    private void CreateEnemy()
    {
        EnsureBaseAssets();

        RuntimeAnimatorController controllerToAssign = archetype.animatorOverride != null
            ? archetype.animatorOverride
            : LoadBaseController();

        if (idleClip != null && walkClip != null && attackClip != null)
        {
            AnimatorOverrideController generated = BuildOverrideController(enemyName, idleClip, walkClip, attackClip);
            controllerToAssign = generated;

            // Grava de volta no archetype — é ele a fonte única de verdade que
            // EnemyStats.AutoConfigureFromArchetype() lê; sem isso, um prefab futuro
            // reconfigurado a partir deste mesmo archetype não veria o override novo.
            archetype.animatorOverride = generated;
            EditorUtility.SetDirty(archetype);
            AssetDatabase.SaveAssets();
        }

        GameObject root = PrefabUtility.LoadPrefabContents(TemplatePrefabPath);

        try
        {
            EnemyStats stats = root.GetComponent<EnemyStats>();
            SerializedObject statsObject = new(stats);
            statsObject.FindProperty("archetype").objectReferenceValue = archetype;
            statsObject.ApplyModifiedPropertiesWithoutUndo();

            Animator animator = root.GetComponent<Animator>();
            animator.runtimeAnimatorController = controllerToAssign;

            SpriteRenderer spriteRenderer = root.GetComponent<SpriteRenderer>();
            Sprite spriteToUse = spriteOverride != null ? spriteOverride : archetype.defaultSprite;
            if (spriteToUse != null)
                spriteRenderer.sprite = spriteToUse;

            root.name = enemyName;

            string prefabPath = $"{NewPrefabFolder}/{enemyName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);

            Debug.Log($"EnemySetupTool: '{enemyName}' criado em {prefabPath}.");

            Object savedAsset = AssetDatabase.LoadAssetAtPath<Object>(prefabPath);
            Selection.activeObject = savedAsset;
            EditorGUIUtility.PingObject(savedAsset);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // ------------------------------------------------------------------
    // Bootstrap dos assets compartilhados (idempotente — só cria o que faltar)
    // ------------------------------------------------------------------

    [MenuItem("Tools/Enemies/Bootstrap Base Assets")]
    private static void EnsureBaseAssetsMenuItem() => EnsureBaseAssets();

    private static void EnsureBaseAssets()
    {
        AnimatorController baseController = LoadBaseController();
        if (baseController == null)
            baseController = BuildBaseController();

        if (AssetDatabase.LoadAssetAtPath<GameObject>(TemplatePrefabPath) == null)
            BuildTemplatePrefab(baseController);
    }

    private static AnimatorController LoadBaseController()
    {
        return AssetDatabase.LoadAssetAtPath<AnimatorController>(BaseControllerPath);
    }

    // Contrato de parâmetros/estados confirmado em Enemy_Movement.cs: bools
    // isIdle/isChasing/isAttacking + trigger Attack, estados Idle/Walking/Attack.
    // Os "clipes" desta base são placeholders puros (sem conteúdo real) — servem
    // só de chave pro Animator Override Controller de cada inimigo substituir;
    // a base nunca é usada diretamente por um prefab real.
    private static AnimatorController BuildBaseController()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(BaseControllerPath)!);

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(BaseControllerPath);
        controller.AddParameter("isIdle", AnimatorControllerParameterType.Bool);
        controller.AddParameter("isChasing", AnimatorControllerParameterType.Bool);
        controller.AddParameter("isAttacking", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

        AnimationClip idlePlaceholder = CreatePlaceholderClip("EnemyBase_IdlePlaceholder");
        AnimationClip walkPlaceholder = CreatePlaceholderClip("EnemyBase_WalkPlaceholder");
        AnimationClip attackPlaceholder = CreatePlaceholderClip("EnemyBase_AttackPlaceholder");

        AnimatorState idleState = stateMachine.AddState("Enemy_Idle");
        idleState.motion = idlePlaceholder;

        AnimatorState walkState = stateMachine.AddState("Enemy_Walking");
        walkState.motion = walkPlaceholder;

        AnimatorState attackState = stateMachine.AddState("Enemy_Attack");
        attackState.motion = attackPlaceholder;

        stateMachine.defaultState = idleState;

        AddBoolTransition(idleState, walkState, "isChasing", true);
        AddBoolTransition(walkState, idleState, "isIdle", true);
        AddBoolTransition(idleState, attackState, "isAttacking", true);
        AddBoolTransition(walkState, attackState, "isAttacking", true);
        AddBoolTransition(attackState, idleState, "isIdle", true);
        AddBoolTransition(attackState, walkState, "isChasing", true);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        return controller;
    }

    // duration 0.15s (era 0.05s): 0.05 crossfadeava rápido demais pra essas
    // animações simples de sprite — lido como um "pulo" entre poses em vez de uma
    // transição, especialmente perceptível na borda do alcance de ataque quando
    // Idle/Chasing alternam seguido. 0.15 já suaviza sem deixar a resposta do
    // Attack largada.
    private static void AddBoolTransition(AnimatorState from, AnimatorState to, string parameter, bool value)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.hasFixedDuration = true;
        transition.duration = 0.15f;
        transition.AddCondition(
            value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot,
            0f,
            parameter);
    }

    // Clipe vazio de 1 frame — existe só pra dar ao Animator Override Controller
    // uma chave de origem pra substituir. Nunca deve aparecer em um prefab real
    // (todo enemy criado pelo tool recebe um override com os 3 clipes de verdade).
    private static AnimationClip CreatePlaceholderClip(string name)
    {
        string path = $"{PlaceholderClipFolder}/{name}.anim";

        if (!AssetDatabase.IsValidFolder(PlaceholderClipFolder))
        {
            Directory.CreateDirectory(PlaceholderClipFolder);
            AssetDatabase.Refresh();
        }

        AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (existing != null)
            return existing;

        AnimationClip clip = new() { name = name };
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static AnimatorOverrideController BuildOverrideController(
        string name, AnimationClip idle, AnimationClip walk, AnimationClip attack)
    {
        if (!AssetDatabase.IsValidFolder(OverrideControllerFolder))
        {
            Directory.CreateDirectory(OverrideControllerFolder);
            AssetDatabase.Refresh();
        }

        AnimatorController baseController = LoadBaseController();
        AnimatorOverrideController overrideController = new(baseController) { name = name };

        List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new();
        overrideController.GetOverrides(overrides);

        for (int i = 0; i < overrides.Count; i++)
        {
            AnimationClip original = overrides[i].Key;

            if (original.name.Contains("Idle"))
                overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(original, idle);
            else if (original.name.Contains("Walk"))
                overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(original, walk);
            else if (original.name.Contains("Attack"))
                overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(original, attack);
        }

        overrideController.ApplyOverrides(overrides);

        string path = $"{OverrideControllerFolder}/{name}.overrideController";
        AssetDatabase.CreateAsset(overrideController, path);
        AssetDatabase.SaveAssets();

        return overrideController;
    }

    // Monta _EnemyTemplate.prefab do zero via API (não YAML à mão): todo componente
    // reusável que hoje existe em Goblin_Melee/Orc, sem nenhum archetype nem tuning
    // — isso é o que EnemyStats.AutoConfigureFromArchetype()/Enemy_Movement.Start()
    // preenchem em runtime a partir do archetype atribuído em cada duplicata.
    private static void BuildTemplatePrefab(AnimatorController baseController)
    {
        GameObject root = new("_EnemyTemplate");

        try
        {
            root.layer = EnemyLayer;
            root.tag = EnemyTag;

            SpriteRenderer spriteRenderer = root.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 0;

            Animator animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = baseController;

            Rigidbody2D rb = root.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            CapsuleCollider2D collider = root.AddComponent<CapsuleCollider2D>();
            collider.size = new Vector2(0.7f, 1.2f);

            root.AddComponent<EnemyStats>();
            root.AddComponent<Enemy_Health>();
            root.AddComponent<Enemy_Movement>();
            root.AddComponent<Enemy_Combat>();
            root.AddComponent<SpriteYSorter>();
            root.AddComponent<AudioSource>().playOnAwake = false;

            NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
            agent.radius = 0.2f;
            agent.height = 0.2f;
            agent.baseOffset = 0f;

            BuildHealthBarHierarchy(root.transform);
            BuildProjectileSpawnPoint(root.transform);

            Directory.CreateDirectory(Path.GetDirectoryName(TemplatePrefabPath)!);
            PrefabUtility.SaveAsPrefabAsset(root, TemplatePrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    // Réplica da hierarquia Canvas > HealthBar > background/fill já usada por
    // Goblin_Melee/Orc — mesmos números (escala 0.01, resolução de referência
    // 800x600) pra bater visualmente com o que já existe.
    private static void BuildHealthBarHierarchy(Transform parent)
    {
        GameObject canvasObject = new("Canvas");
        canvasObject.layer = LayerMask.NameToLayer(UILayerName);
        canvasObject.transform.SetParent(parent, false);
        canvasObject.transform.localScale = new Vector3(0.01f, 0.01f, 1f);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.referenceResolution = new Vector2(800, 600);

        canvasObject.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(1920, 1080);
        canvasRect.anchoredPosition = new Vector2(0f, 0.637f); // sobrescrito em runtime por archetype.healthBarOffset

        GameObject healthBarObject = new("HealthBar");
        healthBarObject.layer = LayerMask.NameToLayer(UILayerName);
        healthBarObject.transform.SetParent(canvasObject.transform, false);

        RectTransform healthBarRect = healthBarObject.AddComponent<RectTransform>();
        healthBarRect.anchorMin = new Vector2(0.5f, 0.5f);
        healthBarRect.anchorMax = new Vector2(0.5f, 0.5f);
        healthBarRect.sizeDelta = new Vector2(82.0122f, 9.6475f);

        Slider slider = healthBarObject.AddComponent<Slider>();
        slider.interactable = false;
        slider.transition = Selectable.Transition.ColorTint;
        slider.minValue = 0f;
        slider.maxValue = 1f;

        GameObject background = new("background");
        background.layer = LayerMask.NameToLayer(UILayerName);
        background.transform.SetParent(healthBarObject.transform, false);
        RectTransform backgroundRect = background.AddComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.sizeDelta = Vector2.zero;
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = new Color(0.0849f, 0.0805f, 0.0805f, 1f);

        GameObject fill = new("fill");
        fill.layer = LayerMask.NameToLayer(UILayerName);
        fill.transform.SetParent(healthBarObject.transform, false);
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.zero;
        fillRect.sizeDelta = Vector2.zero;
        // Type.Simple (padrão) — o Slider redimensiona o próprio fillRect via
        // anchorMax pra mostrar a vida, não via Image.fillAmount. Mesma configuração
        // já usada em Goblin_Melee/Orc.
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.8774f, 0.1614f, 0.1614f, 1f);

        slider.fillRect = fillRect;
        slider.targetGraphic = null;
    }

    // Ponto de disparo do ataque básico à distância (Enemy_RangedBasicAttack) —
    // um child Transform simples com um marcador (ProjectileSpawnPoint) que
    // desenha um gizmo, pra ser arrastado na Scene view em vez de calibrado só
    // por número. Posição inicial é um chute razoável ("altura da mão"); o
    // objetivo é que o usuário ajuste olhando o personagem, não que já nasça certo.
    private static void BuildProjectileSpawnPoint(Transform parent)
    {
        GameObject spawnPoint = new("Projectile Spawn Point");
        spawnPoint.layer = EnemyLayer;
        spawnPoint.tag = EnemyTag;
        spawnPoint.transform.SetParent(parent, false);
        spawnPoint.transform.localPosition = new Vector3(0.3f, 0.2f, 0f);
        spawnPoint.AddComponent<ProjectileSpawnPoint>();
    }
}
