using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Espelha ShopWindowCanvasBuilder/SkillBarCanvasBuilder/QuestWindowCanvasBuilder:
// mesma paleta/Outline/fonte, tudo construído em código (sem prefab). Reconstrói
// a janela inteira (Equipamento + Grade de itens) que hoje vive num único
// "InventoryCanvas" hand-authored com a paleta antiga preto/branco translúcido.
// InventoryManager/ItemSlot/EquipmentManager/EquippedSlot não mudam — só a
// hierarquia visual é reconstruída, e as referências que o código não consegue
// recriar sozinho (toggleInventory, worldItemPrefab, sprite do preview de
// personagem) são capturadas do GameObject antigo antes de destruí-lo.
//
// Todo painel que participa de um LayoutGroup com childControlWidth/Height
// false precisa do sizeDelta setado EXPLICITAMENTE, além do LayoutElement —
// o LayoutElement sozinho só entra no cálculo de tamanho preferido do pai,
// não redimensiona o próprio RectTransform quando childControl está off (é
// assim que SkillBarCanvasBuilder faz pra cada slot; aqui aplicamos o mesmo
// em todo painel/contêiner, não só nas células de slot).
public static class InventoryCanvasBuilder
{
    private const string CanvasName = "InventoryCanvas";
    private const string PreviewPath = "Window Panel/Player Equipment/Equipment Panel/Center Panel/Preview";

    private const float SlotSize = 76f;
    private const float SlotSpacing = 8f;
    private const int ItemColumns = 4;
    private const int ItemSlotCount = 20;
    private const float HeaderHeight = 28f;

    private static readonly Color PanelBackground = new(0.04f, 0.05f, 0.07f, 0.9f);
    private static readonly Color SlotBackground = new(0.12f, 0.14f, 0.18f, 0.95f);
    private static readonly Color OutlineColor = new(0.55f, 0.6f, 0.7f, 0.9f);
    private static readonly Vector2 OutlineDistance = new(2f, -2f);
    private static readonly Color GoldAccent = new(1f, 0.86f, 0.35f, 1f);
    private static readonly Color ButtonColor = new(0.12f, 0.14f, 0.18f, 0.95f);

    private static readonly (string label, ItemSO.ItemType type)[] EquipmentSlots =
    {
        ("Head", ItemSO.ItemType.Head),
        ("Body", ItemSO.ItemType.Body),
        ("Legs", ItemSO.ItemType.Legs),
        ("Feet", ItemSO.ItemType.Feet),
        ("Main Hand", ItemSO.ItemType.MainHand),
        ("Off Hand", ItemSO.ItemType.OffHand),
        ("Necklace", ItemSO.ItemType.Necklace),
        ("Ring", ItemSO.ItemType.Ring),
    };

    private static Sprite runtimeSprite;
    private static TMP_FontAsset bangersFont;

    [MenuItem("Tools/Inventory/Build Inventory Canvas")]
    private static void Build()
    {
        Capture capture = CaptureAndDestroyExisting();

        GameObject canvasObject = new(CanvasName);
        Undo.RegisterCreatedObjectUndo(canvasObject, "Create Inventory Canvas");

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        CanvasGroup canvasGroup = canvasObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        // ---- Tamanhos pré-calculados (mesma abordagem de SkillBarCanvasBuilder.barWidth) ----
        int itemRows = Mathf.CeilToInt(ItemSlotCount / (float)ItemColumns);
        float itemGridWidth = SlotSize * ItemColumns + SlotSpacing * (ItemColumns - 1);
        float itemGridHeight = SlotSize * itemRows + SlotSpacing * (itemRows - 1);
        const float itemPanelPadding = 12f;
        const float itemHeaderSpacing = 8f;
        float itemPanelWidth = itemGridWidth + itemPanelPadding * 2f;
        float itemPanelHeight = HeaderHeight + itemHeaderSpacing + itemGridHeight + itemPanelPadding * 2f;

        // Colunas de equipamento são de 1 coluna x 4 linhas (verticais) — a
        // linha N da esquerda alinha com a linha N da direita automaticamente
        // por terem a mesma altura.
        //
        // A seção de equipamento agora usa o MESMO tamanho externo do Item
        // Panel (pedido do usuário) — o conteúdo interno (Equipment Panel e o
        // retrato central) é derivado de trás pra frente pra caber exatamente
        // nesse espaço, em vez de pré-calculado e potencialmente maior/menor
        // que a grade de inventário.
        float sideColumnHeight = SlotSize * 4f + SlotSpacing * 3f;
        const float equipInnerSpacing = 12f;
        const float equipInnerPadding = 12f;
        const float outerSpacing = 8f;
        const float outerPadding = 12f;

        float playerEquipmentWidth = itemPanelWidth;
        float playerEquipmentHeight = itemPanelHeight;

        float equipmentPanelWidth = playerEquipmentWidth - outerPadding * 2f;
        float equipmentPanelHeight = playerEquipmentHeight - HeaderHeight - outerSpacing - outerPadding * 2f;

        float centerWidth = equipmentPanelWidth - equipInnerPadding * 2f - SlotSize * 2f - equipInnerSpacing * 2f;
        // Mesma altura das colunas laterais (não o espaço interno inteiro do
        // Equipment Panel) — assim as bordas de cima/baixo dos 3 painéis
        // (Left/Center/Right) ficam alinhadas em vez do Center Panel esticar
        // além do Left/Right, que a linha centralizada (MiddleCenter) já
        // reparte igualmente o espaço sobrando acima e abaixo dos três.
        float centerHeight = sideColumnHeight;

        const float windowSpacing = 16f;
        const float windowPadding = 16f;
        // Espaço extra só no topo — dá uma faixa de respiro pro botão "X" em vez
        // dele ficar espremido em cima do Outline do canto (era o problema
        // reportado com o padding simétrico de 16 em todos os lados).
        const float titleBarExtra = 32f;
        float windowWidth = playerEquipmentWidth + windowSpacing + itemPanelWidth + windowPadding * 2f;
        float windowHeight = Mathf.Max(playerEquipmentHeight, itemPanelHeight) + windowPadding * 2f + titleBarExtra;

        RectTransform window = CreateUIObject("Window Panel", canvasObject.transform);
        window.anchorMin = new Vector2(0.5f, 0.5f);
        window.anchorMax = new Vector2(0.5f, 0.5f);
        window.pivot = new Vector2(0.5f, 0.5f);
        window.anchoredPosition = Vector2.zero;
        window.sizeDelta = new Vector2(windowWidth, windowHeight);

        Image windowBackground = window.gameObject.AddComponent<Image>();
        windowBackground.sprite = GetRuntimeSprite();
        windowBackground.color = PanelBackground;

        Outline windowOutline = window.gameObject.AddComponent<Outline>();
        windowOutline.effectColor = OutlineColor;
        windowOutline.effectDistance = OutlineDistance;

        HorizontalLayoutGroup windowLayout = window.gameObject.AddComponent<HorizontalLayoutGroup>();
        windowLayout.padding = new RectOffset(16, 16, (int)(16f + titleBarExtra), 16);
        windowLayout.spacing = windowSpacing;
        windowLayout.childAlignment = TextAnchor.UpperLeft;
        windowLayout.childControlWidth = false;
        windowLayout.childControlHeight = false;
        windowLayout.childForceExpandWidth = false;
        windowLayout.childForceExpandHeight = false;

        EquippedSlot[] equippedSlots = BuildEquipmentColumn(
            window, playerEquipmentWidth, playerEquipmentHeight, equipmentPanelWidth, equipmentPanelHeight,
            sideColumnHeight, centerWidth, centerHeight, capture, out RectTransform equipmentPanelTransform);

        ItemSlot[] itemSlots = BuildItemPanel(
            window, itemPanelWidth, itemPanelHeight, itemGridWidth, itemGridHeight, out RectTransform itemPanelTransform);

        BuildCloseButton(window);

        InventoryManager inventoryManager = canvasObject.AddComponent<InventoryManager>();
        EquipmentManager equipmentManager = canvasObject.AddComponent<EquipmentManager>();

        inventoryManager.inventoryCanvas = canvasGroup;
        inventoryManager.toggleInventory = capture.toggleInventory;

        SerializedObject serializedInventory = new(inventoryManager);
        serializedInventory.FindProperty("itemSlot").ReplaceArray(itemSlots);
        serializedInventory.FindProperty("worldItemPrefab").objectReferenceValue = capture.worldItemPrefab;
        // "Modo loja" (ver InventoryManager.EnterShopMode/ExitShopMode) —
        // referências que não existiam antes desta janela precisar esconder o
        // equipamento e encolher em torno da grade de itens.
        serializedInventory.FindProperty("equipmentPanel").objectReferenceValue = equipmentPanelTransform.gameObject;
        serializedInventory.FindProperty("windowPanel").objectReferenceValue = window;
        serializedInventory.FindProperty("itemPanelRect").objectReferenceValue = itemPanelTransform;
        serializedInventory.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject serializedEquipment = new(equipmentManager);
        serializedEquipment.FindProperty("equippedSlots").ReplaceArray(equippedSlots);
        serializedEquipment.ApplyModifiedPropertiesWithoutUndo();

        if (capture.toggleInventory == null)
            Debug.LogWarning("InventoryCanvasBuilder: nenhuma InventoryCanvas anterior encontrada — atribua \"Toggle Inventory\" no InventoryManager manualmente.");

        Selection.activeGameObject = canvasObject;
    }

    private struct Capture
    {
        public InputActionReference toggleInventory;
        public GameObject worldItemPrefab;
        public Sprite previewSprite;
    }

    private static Capture CaptureAndDestroyExisting()
    {
        Capture capture = default;

        GameObject existing = GameObject.Find(CanvasName);

        if (existing == null)
            return capture;

        InventoryManager oldManager = existing.GetComponent<InventoryManager>();

        if (oldManager != null)
        {
            capture.toggleInventory = oldManager.toggleInventory;

            SerializedObject serializedOld = new(oldManager);
            capture.worldItemPrefab = serializedOld.FindProperty("worldItemPrefab").objectReferenceValue as GameObject;
        }

        // Preserva o sprite do retrato do personagem entre reconstruções — nada
        // além do Inspector alimenta esse campo, então sem isso cada rebuild
        // apagaria a arte já atribuída.
        Transform oldPreview = existing.transform.Find(PreviewPath);

        if (oldPreview != null)
        {
            Image oldPreviewImage = oldPreview.GetComponent<Image>();

            if (oldPreviewImage != null)
                capture.previewSprite = oldPreviewImage.sprite;
        }

        Undo.DestroyObjectImmediate(existing);
        return capture;
    }

    private static EquippedSlot[] BuildEquipmentColumn(
        Transform parent,
        float outerWidth,
        float outerHeight,
        float innerWidth,
        float innerHeight,
        float columnHeight,
        float centerWidth,
        float centerHeight,
        Capture capture,
        out RectTransform equipmentPanelTransform)
    {
        RectTransform outer = CreateUIObject("Player Equipment", parent);
        outer.sizeDelta = new Vector2(outerWidth, outerHeight);
        equipmentPanelTransform = outer;

        LayoutElement outerLayoutElement = outer.gameObject.AddComponent<LayoutElement>();
        outerLayoutElement.preferredWidth = outerWidth;
        outerLayoutElement.preferredHeight = outerHeight;

        Image outerBackground = outer.gameObject.AddComponent<Image>();
        outerBackground.sprite = GetRuntimeSprite();
        outerBackground.color = SlotBackground;

        Outline outerOutline = outer.gameObject.AddComponent<Outline>();
        outerOutline.effectColor = OutlineColor;
        outerOutline.effectDistance = OutlineDistance;

        VerticalLayoutGroup outerLayoutGroup = outer.gameObject.AddComponent<VerticalLayoutGroup>();
        outerLayoutGroup.padding = new RectOffset(12, 12, 12, 12);
        outerLayoutGroup.spacing = 8f;
        outerLayoutGroup.childAlignment = TextAnchor.UpperCenter;
        outerLayoutGroup.childControlWidth = false;
        outerLayoutGroup.childControlHeight = false;
        outerLayoutGroup.childForceExpandWidth = false;
        outerLayoutGroup.childForceExpandHeight = false;

        TMP_Text header = CreateText("Header", outer, "Equipment", 21f, TextAlignmentOptions.MidlineLeft);
        header.rectTransform.sizeDelta = new Vector2(innerWidth, HeaderHeight);
        LayoutElement headerLayout = header.gameObject.AddComponent<LayoutElement>();
        headerLayout.preferredWidth = innerWidth;
        headerLayout.preferredHeight = HeaderHeight;
        header.fontStyle = FontStyles.Bold;
        header.color = GoldAccent;

        RectTransform equipmentPanel = CreateUIObject("Equipment Panel", outer);
        equipmentPanel.sizeDelta = new Vector2(innerWidth, innerHeight);

        LayoutElement equipmentLayout = equipmentPanel.gameObject.AddComponent<LayoutElement>();
        equipmentLayout.preferredWidth = innerWidth;
        equipmentLayout.preferredHeight = innerHeight;

        Image equipmentBackground = equipmentPanel.gameObject.AddComponent<Image>();
        equipmentBackground.sprite = GetRuntimeSprite();
        equipmentBackground.color = SlotBackground;

        HorizontalLayoutGroup equipmentLayoutGroup = equipmentPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
        equipmentLayoutGroup.padding = new RectOffset(12, 12, 12, 12);
        equipmentLayoutGroup.spacing = 12f;
        equipmentLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
        equipmentLayoutGroup.childControlWidth = false;
        equipmentLayoutGroup.childControlHeight = false;
        equipmentLayoutGroup.childForceExpandWidth = false;
        equipmentLayoutGroup.childForceExpandHeight = false;

        EquippedSlot[] slots = new EquippedSlot[EquipmentSlots.Length];

        RectTransform leftPanel = BuildEquipmentGrid(equipmentPanel, "Left Panel", SlotSize, columnHeight);
        for (int i = 0; i < 4; i++)
            slots[i] = BuildEquipmentSlot(leftPanel, EquipmentSlots[i].label, EquipmentSlots[i].type);

        RectTransform centerPanel = CreateUIObject("Center Panel", equipmentPanel);
        centerPanel.sizeDelta = new Vector2(centerWidth, centerHeight);

        LayoutElement centerLayout = centerPanel.gameObject.AddComponent<LayoutElement>();
        centerLayout.preferredWidth = centerWidth;
        centerLayout.preferredHeight = centerHeight;

        Image centerBackground = centerPanel.gameObject.AddComponent<Image>();
        centerBackground.sprite = GetRuntimeSprite();
        centerBackground.color = SlotBackground;

        Outline centerOutline = centerPanel.gameObject.AddComponent<Outline>();
        centerOutline.effectColor = OutlineColor;
        centerOutline.effectDistance = OutlineDistance;

        // Preview do personagem — sprite atribuído no Inspector (ver PreviewPath
        // acima, que preserva esse valor entre reconstruções). Só fica
        // habilitado se já houver um sprite, senão fica um quadro vazio.
        Image preview = CreateImage("Preview", centerPanel, Color.white);
        SetStretch(preview.rectTransform, 8f);
        preview.preserveAspect = true;
        preview.sprite = capture.previewSprite;
        preview.enabled = capture.previewSprite != null;

        RectTransform rightPanel = BuildEquipmentGrid(equipmentPanel, "Right Panel", SlotSize, columnHeight);
        for (int i = 0; i < 4; i++)
            slots[4 + i] = BuildEquipmentSlot(rightPanel, EquipmentSlots[4 + i].label, EquipmentSlots[4 + i].type);

        return slots;
    }

    // Coluna de 1 slot de largura x 4 de altura (empilhados verticalmente) —
    // esquerda e direita usam a mesma altura, então a linha N de uma alinha
    // com a linha N da outra automaticamente.
    private static RectTransform BuildEquipmentGrid(Transform parent, string name, float width, float height)
    {
        RectTransform grid = CreateUIObject(name, parent);
        grid.sizeDelta = new Vector2(width, height);

        LayoutElement layoutElement = grid.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = width;
        layoutElement.preferredHeight = height;

        GridLayoutGroup gridLayout = grid.gameObject.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(SlotSize, SlotSize);
        gridLayout.spacing = new Vector2(SlotSpacing, SlotSpacing);
        gridLayout.childAlignment = TextAnchor.UpperLeft;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 1;

        return grid;
    }

    private static EquippedSlot BuildEquipmentSlot(Transform parent, string label, ItemSO.ItemType acceptedType)
    {
        RectTransform slot = CreateUIObject(label, parent);
        slot.sizeDelta = new Vector2(SlotSize, SlotSize);

        Image background = slot.gameObject.AddComponent<Image>();
        background.sprite = GetRuntimeSprite();
        background.color = SlotBackground;

        Outline outline = slot.gameObject.AddComponent<Outline>();
        outline.effectColor = OutlineColor;
        outline.effectDistance = OutlineDistance;

        Image icon = CreateImage("Icon", slot, Color.white);
        SetStretch(icon.rectTransform, 8f);
        icon.preserveAspect = true;
        icon.enabled = false;

        TMP_Text placeholderText = CreateText("Placeholder", slot, label, 15f, TextAlignmentOptions.Center);
        SetStretch(placeholderText.rectTransform, 4f);
        placeholderText.color = new Color(0.6f, 0.65f, 0.72f, 0.7f);
        placeholderText.textWrappingMode = TextWrappingModes.Normal;

        EquippedSlot equippedSlot = slot.gameObject.AddComponent<EquippedSlot>();
        equippedSlot.acceptedType = acceptedType;

        SerializedObject serialized = new(equippedSlot);
        serialized.FindProperty("itemImage").objectReferenceValue = icon;
        serialized.FindProperty("placeholder").objectReferenceValue = placeholderText.gameObject;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return equippedSlot;
    }

    private static ItemSlot[] BuildItemPanel(
        Transform parent, float outerWidth, float outerHeight, float gridWidth, float gridHeight,
        out RectTransform itemPanelTransform)
    {
        RectTransform panel = CreateUIObject("Item Panel", parent);
        panel.sizeDelta = new Vector2(outerWidth, outerHeight);
        itemPanelTransform = panel;

        LayoutElement layoutElement = panel.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = outerWidth;
        layoutElement.preferredHeight = outerHeight;

        Image background = panel.gameObject.AddComponent<Image>();
        background.sprite = GetRuntimeSprite();
        background.color = SlotBackground;

        Outline outline = panel.gameObject.AddComponent<Outline>();
        outline.effectColor = OutlineColor;
        outline.effectDistance = OutlineDistance;

        VerticalLayoutGroup panelLayout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        panelLayout.padding = new RectOffset(12, 12, 12, 12);
        panelLayout.spacing = 8f;
        panelLayout.childAlignment = TextAnchor.UpperLeft;
        panelLayout.childControlWidth = false;
        panelLayout.childControlHeight = false;
        panelLayout.childForceExpandWidth = false;
        panelLayout.childForceExpandHeight = false;

        BuildItemPanelHeader(panel, gridWidth);

        RectTransform grid = CreateUIObject("Grid", panel);
        grid.sizeDelta = new Vector2(gridWidth, gridHeight);

        LayoutElement gridLayoutElement = grid.gameObject.AddComponent<LayoutElement>();
        gridLayoutElement.preferredWidth = gridWidth;
        gridLayoutElement.preferredHeight = gridHeight;

        GridLayoutGroup gridLayout = grid.gameObject.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(SlotSize, SlotSize);
        gridLayout.spacing = new Vector2(SlotSpacing, SlotSpacing);
        gridLayout.childAlignment = TextAnchor.UpperLeft;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = ItemColumns;

        ItemSlot[] slots = new ItemSlot[ItemSlotCount];

        for (int i = 0; i < ItemSlotCount; i++)
            slots[i] = BuildItemSlot(grid, i);

        return slots;
    }

    // Título "Inventory" à esquerda + ouro atual à direita, na mesma linha —
    // reaproveita o GoldDisplayUI já usado pela loja/placeholder de tela,
    // só que embutido aqui em vez de num canvas solto.
    private static void BuildItemPanelHeader(Transform parent, float width)
    {
        RectTransform row = CreateUIObject("Header Row", parent);
        row.sizeDelta = new Vector2(width, HeaderHeight);

        LayoutElement rowLayout = row.gameObject.AddComponent<LayoutElement>();
        rowLayout.preferredWidth = width;
        rowLayout.preferredHeight = HeaderHeight;

        HorizontalLayoutGroup rowGroup = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        rowGroup.childAlignment = TextAnchor.MiddleLeft;
        rowGroup.childControlWidth = true;
        rowGroup.childControlHeight = true;
        rowGroup.childForceExpandWidth = true;
        rowGroup.childForceExpandHeight = true;

        TMP_Text title = CreateText("Title", row, "Inventory", 21f, TextAlignmentOptions.MidlineLeft);
        title.fontStyle = FontStyles.Bold;
        title.color = GoldAccent;

        TMP_Text goldText = CreateText("Gold Text", row, "Gold: 0", 17f, TextAlignmentOptions.MidlineRight);
        goldText.color = GoldAccent;

        GoldDisplayUI goldDisplay = row.gameObject.AddComponent<GoldDisplayUI>();
        SerializedObject serialized = new(goldDisplay);
        serialized.FindProperty("goldText").objectReferenceValue = goldText;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    // "X" no canto superior direito da janela. Diferente de QuestWindow (que
    // não tem faixa de topo dedicada), aqui o Build() reserva um padding
    // superior extra (titleBarExtra) só pra este botão não ficar espremido em
    // cima do Outline do painel — o botão fica centralizado nessa faixa. Este
    // é o único "X" do par loja+inventário quando a loja está aberta — ver
    // InventoryWindowButton (a ShopWindow não tem mais o seu próprio).
    private static void BuildCloseButton(Transform panelParent)
    {
        RectTransform rect = CreateUIObject("Close Button", panelParent);
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-16f, -13f);
        rect.sizeDelta = new Vector2(22f, 22f);

        LayoutElement layoutElement = rect.gameObject.AddComponent<LayoutElement>();
        layoutElement.ignoreLayout = true;

        Image background = rect.gameObject.AddComponent<Image>();
        background.sprite = GetRuntimeSprite();
        background.color = ButtonColor;
        background.raycastTarget = true;

        Outline outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = OutlineColor;
        outline.effectDistance = OutlineDistance;

        TMP_Text text = CreateText("Label", rect, "X", 16f, TextAlignmentOptions.Center);
        SetStretch(text.rectTransform, 0f);
        text.fontStyle = FontStyles.Bold;

        rect.gameObject.AddComponent<InventoryWindowButton>();
    }

    private static ItemSlot BuildItemSlot(Transform parent, int index)
    {
        RectTransform slot = CreateUIObject($"Item Slot {index}", parent);
        slot.sizeDelta = new Vector2(SlotSize, SlotSize);

        Image background = slot.gameObject.AddComponent<Image>();
        background.sprite = GetRuntimeSprite();
        background.color = SlotBackground;

        Outline outline = slot.gameObject.AddComponent<Outline>();
        outline.effectColor = OutlineColor;
        outline.effectDistance = OutlineDistance;

        Image icon = CreateImage("Icon", slot, Color.white);
        SetStretch(icon.rectTransform, 8f);
        icon.preserveAspect = true;
        icon.enabled = false;

        // Overlay de seleção (ItemSlot.selectedShader) — translúcido, inativo
        // até o jogador clicar no slot.
        Image selectedPanel = CreateImage("SelectedPanel", slot, new Color(1f, 1f, 1f, 0.35f));
        SetStretch(selectedPanel.rectTransform, 0f);
        selectedPanel.raycastTarget = false;
        selectedPanel.gameObject.SetActive(false);

        TMP_Text quantityText = CreateText("Qtd Text", slot, "0", 15f, TextAlignmentOptions.BottomRight);
        SetStretch(quantityText.rectTransform, 4f);
        quantityText.fontStyle = FontStyles.Bold;
        quantityText.color = GoldAccent;
        quantityText.enabled = false;

        ItemSlot itemSlot = slot.gameObject.AddComponent<ItemSlot>();
        itemSlot.selectedShader = selectedPanel.gameObject;

        SerializedObject serialized = new(itemSlot);
        serialized.FindProperty("itemImage").objectReferenceValue = icon;
        serialized.FindProperty("quantityText").objectReferenceValue = quantityText;
        // ItemSlots.prefab sobrescrevia isso pra 9 (o default do código é 99) —
        // preserva o limite de stack real que já está em uso hoje.
        serialized.FindProperty("maxNumberOfItems").intValue = 9;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return itemSlot;
    }

    private static void ReplaceArray<T>(this SerializedProperty property, T[] values) where T : Object
    {
        property.arraySize = values.Length;

        for (int i = 0; i < values.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    private static RectTransform CreateUIObject(string objectName, Transform parent)
    {
        GameObject gameObject = new(objectName, typeof(RectTransform));
        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        rectTransform.localScale = Vector3.one;
        return rectTransform;
    }

    private static Image CreateImage(string objectName, Transform parent, Color color)
    {
        RectTransform rectTransform = CreateUIObject(objectName, parent);
        Image image = rectTransform.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text CreateText(
        string objectName,
        Transform parent,
        string value,
        float fontSize,
        TextAlignmentOptions alignment)
    {
        RectTransform rectTransform = CreateUIObject(objectName, parent);
        TextMeshProUGUI text = rectTransform.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        text.overflowMode = TextOverflowModes.Truncate;

        TMP_FontAsset font = GetBangersFont();

        if (font != null)
            text.font = font;

        return text;
    }

    private static TMP_FontAsset GetBangersFont()
    {
        if (bangersFont == null)
            bangersFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Bangers SDF");

        return bangersFont;
    }

    private static void SetStretch(RectTransform rectTransform, float margin)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = new Vector2(margin, margin);
        rectTransform.offsetMax = new Vector2(-margin, -margin);
    }

    private static Sprite GetRuntimeSprite()
    {
        if (runtimeSprite != null)
            return runtimeSprite;

        Texture2D texture = new(1, 1, TextureFormat.RGBA32, false);
        texture.name = "Inventory Canvas Runtime Texture";
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        runtimeSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f);
        runtimeSprite.name = "Inventory Canvas Runtime Sprite";
        return runtimeSprite;
    }
}
