using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProductionMachineUI : MonoBehaviour
{
    private class ItemButtonView
    {
        public ItemSO Item;
        public GameObject RootObject;
        public Button Button;
        public TMP_Text TmpText;
        public Text LegacyText;
        public Image Background;
    }

    [Header("UI Prefabs Or Scene References")]
    [SerializeField] private Canvas canvasPrefab;
    [SerializeField] private GameObject menuPrefab;
    [SerializeField] private GameObject itemButtonPrefab;

    [Header("UI References (Assigned from Prefab or Manual)")]
    [SerializeField] private RectTransform menuRoot;
    [SerializeField] private Image productionProgressFill;
    [SerializeField] private TMP_Text productionProgressTMP;
    [SerializeField] private TMP_Text inputStatusTMP;
    [SerializeField] private TMP_Text countersTMP;
    [SerializeField] private Button closeButton;
    [SerializeField] private Transform itemButtonContainer;

    [Header("Items")]
    [SerializeField] private List<ItemSO> availableItems = new List<ItemSO>();

    public IReadOnlyList<ItemSO> GetAvailableItems()
    {
        return availableItems;
    }

    [Header("Screen UI Layout (Legacy/Fallback)")]
    [SerializeField] private Vector2 canvasSize = new Vector2(460f, 320f);
    [SerializeField] private Vector2 menuSize = new Vector2(440f, 400f);
    [SerializeField] private Vector2 menuScreenPosition = new Vector2(40f, -40f);
    [SerializeField] private Color selectedButtonColor = new Color(1f, 0.93f, 0.55f, 1f);
    [SerializeField] private Color normalButtonColor = Color.white;
    [SerializeField] private Color selectedTextColor = Color.black;
    [SerializeField] private Color normalTextColor = Color.black;

    private static ProductionMachineUI openInstance;

    private IProductionMachine machine;
    private Canvas runtimeCanvas;
    private RectTransform runtimeMenuRoot;
    private readonly List<GameObject> spawnedButtons = new List<GameObject>();
    private readonly List<ItemButtonView> itemButtonViews = new List<ItemButtonView>();

    private void Start()
    {
        if (IsGhostPreviewInstance())
        {
            enabled = false;
            return;
        }

        machine = FindProductionMachine();
        if (machine == null)
        {
            Debug.LogError($"ProductionMachineUI could not find a component implementing IProductionMachine near {name}.");
            enabled = false;
            return;
        }

        InitializeUI();
        CreateItemButtons();
        CloseMenu();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            HandleOpenShortcut();
        }

        RefreshProductionProgressVisual();
    }

    private void OnDestroy()
    {
        if (openInstance == this)
        {
            openInstance = null;
        }

        if (runtimeCanvas != null)
        {
            Destroy(runtimeCanvas.gameObject);
        }
    }

    private IProductionMachine FindProductionMachine()
    {
        MonoBehaviour[] localComponents = GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour component in localComponents)
        {
            if (component is IProductionMachine localMachine)
            {
                return localMachine;
            }
        }

        MonoBehaviour[] childComponents = GetComponentsInChildren<MonoBehaviour>(true);
        foreach (MonoBehaviour component in childComponents)
        {
            if (component is IProductionMachine childMachine)
            {
                return childMachine;
            }
        }

        MonoBehaviour[] parentComponents = GetComponentsInParent<MonoBehaviour>(true);
        foreach (MonoBehaviour component in parentComponents)
        {
            if (component is IProductionMachine parentMachine)
            {
                return parentMachine;
            }
        }

        return null;
    }

    private void InitializeUI()
    {
        // 1. Setup Canvas
        if (canvasPrefab != null)
        {
            runtimeCanvas = Instantiate(canvasPrefab);
        }
        else
        {
            GameObject canvasObj = new GameObject("ProductionMachineCanvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            runtimeCanvas = canvasObj.GetComponent<Canvas>();
            runtimeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            runtimeCanvas.sortingOrder = 100;
            
            RectTransform canvasRT = canvasObj.GetComponent<RectTransform>();
            canvasRT.sizeDelta = canvasSize;
        }

        // 2. Setup Menu Panel
        if (menuPrefab != null)
        {
            GameObject menuObj = Instantiate(menuPrefab, runtimeCanvas.transform, false);
            runtimeMenuRoot = menuObj.GetComponent<RectTransform>();
        }
        else if (menuRoot != null)
        {
            runtimeMenuRoot = menuRoot;
            if (runtimeMenuRoot.transform.parent != runtimeCanvas.transform)
                runtimeMenuRoot.SetParent(runtimeCanvas.transform, false);
        }
        else
        {
            GameObject panelObj = new GameObject("GeneratorMenuPanel", typeof(RectTransform), typeof(Image));
            panelObj.transform.SetParent(runtimeCanvas.transform, false);
            runtimeMenuRoot = panelObj.GetComponent<RectTransform>();
            runtimeMenuRoot.sizeDelta = menuSize;
        }

        ConfigureMenuRoot(runtimeMenuRoot);

        // 3. Resolve References from Prefab
        ResolveInternalReferences();

        // 4. Wire up Close Button
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseMenu);
        }
    }

    private void ResolveInternalReferences()
    {
        if (productionProgressFill == null) productionProgressFill = FindComponentByName<Image>(runtimeMenuRoot, "Fill");
        if (productionProgressTMP == null) productionProgressTMP = FindComponentByName<TMP_Text>(runtimeMenuRoot, "Label");
        if (inputStatusTMP == null) inputStatusTMP = FindComponentByName<TMP_Text>(runtimeMenuRoot, "InputStatus");
        if (countersTMP == null) countersTMP = FindComponentByName<TMP_Text>(runtimeMenuRoot, "Counters");
        if (closeButton == null) closeButton = FindComponentByName<Button>(runtimeMenuRoot, "CloseButton");
        
        if (itemButtonContainer == null)
        {
            ScrollRect scroll = runtimeMenuRoot.GetComponentInChildren<ScrollRect>(true);
            if (scroll != null) itemButtonContainer = scroll.content;
            else itemButtonContainer = runtimeMenuRoot;
        }
    }

    private T FindComponentByName<T>(Transform root, string name) where T : Component
    {
        T[] components = root.GetComponentsInChildren<T>(true);
        foreach (T c in components)
        {
            if (c.name == name) return c;
        }
        return null;
    }

    private void ConfigureMenuRoot(RectTransform rect)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = menuScreenPosition;
        rect.localScale = Vector3.one;
    }

    private void HandleOpenShortcut()
    {
        if (TryGetHoveredMachineUI(out ProductionMachineUI hoveredMachineUI))
        {
            if (hoveredMachineUI == this)
            {
                if (openInstance == this)
                {
                    CloseMenu();
                }
                else
                {
                    OpenMenu();
                }
            }
            else if (openInstance == this)
            {
                CloseMenu();
            }
        }
        else if (openInstance == this)
        {
            CloseMenu();
        }
    }

    private static bool TryGetHoveredMachineUI(out ProductionMachineUI hoveredMachineUI)
    {
        hoveredMachineUI = null;

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return false;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            return false;
        }

        hoveredMachineUI = hit.transform.GetComponentInParent<ProductionMachineUI>();
        return hoveredMachineUI != null;
    }

    private struct MenuEntry
    {
        public ItemSO Item;
        public string Label;
    }

    private void CreateItemButtons()
    {
        foreach (GameObject buttonObject in spawnedButtons)
        {
            if (buttonObject != null)
            {
                Destroy(buttonObject);
            }
        }
        spawnedButtons.Clear();
        itemButtonViews.Clear();

        if (itemButtonContainer == null || itemButtonPrefab == null)
        {
            return;
        }

        foreach (MenuEntry entry in GetMenuEntries())
        {
            if (entry.Item == null) continue;

            GameObject buttonObject = Instantiate(itemButtonPrefab, itemButtonContainer);
            buttonObject.name = $"Item_{entry.Label}";
            spawnedButtons.Add(buttonObject);

            RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0f, 1f);
                rectTransform.anchorMax = new Vector2(1f, 1f);
                rectTransform.pivot = new Vector2(0.5f, 1f);
                rectTransform.sizeDelta = new Vector2(0f, rectTransform.sizeDelta.y);
                rectTransform.localScale = Vector3.one;
            }

            SetButtonLabel(buttonObject, entry.Label);

            Button button = buttonObject.GetComponent<Button>();
            if (button != null)
            {
                ItemSO selectedItem = entry.Item;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => SelectItem(selectedItem));
            }

            itemButtonViews.Add(new ItemButtonView
            {
                Item = entry.Item,
                RootObject = buttonObject,
                Button = button,
                TmpText = buttonObject.GetComponentInChildren<TMP_Text>(true),
                LegacyText = buttonObject.GetComponentInChildren<Text>(true),
                Background = buttonObject.GetComponent<Image>(),
            });
        }

        RefreshSelectedItemVisuals();
    }

    public void OpenMenu()
    {
        if (runtimeMenuRoot == null) return;

        if (openInstance != null && openInstance != this)
        {
            openInstance.CloseMenu();
        }

        RefreshSelectedItemVisuals();
        runtimeMenuRoot.gameObject.SetActive(true);
        openInstance = this;
    }

    public void CloseMenu()
    {
        if (runtimeMenuRoot != null)
        {
            runtimeMenuRoot.gameObject.SetActive(false);
        }

        if (openInstance == this)
        {
            openInstance = null;
        }
    }

    private void SelectItem(ItemSO selectedItem)
    {
        if (machine != null)
        {
            machine.SetOutputItem(selectedItem);
        }

        RefreshSelectedItemVisuals();
    }

    private void SetButtonLabel(GameObject buttonObject, string label)
    {
        if (string.IsNullOrEmpty(label)) label = "Unknown";

        // Try TextMeshProUGUI first (specific)
        var tmp = buttonObject.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null)
        {
            tmp.text = label;
            return;
        }

        // Try TMP_Text base class
        var tmpBase = buttonObject.GetComponentInChildren<TMP_Text>(true);
        if (tmpBase != null)
        {
            tmpBase.text = label;
            return;
        }

        // Try Legacy Text
        var text = buttonObject.GetComponentInChildren<Text>(true);
        if (text != null)
        {
            text.text = label;
        }
    }

    private void RefreshSelectedItemVisuals()
    {
        ItemSO selectedItem = machine != null ? machine.GetOutputItem() : null;

        foreach (ItemButtonView buttonView in itemButtonViews)
        {
            bool isSelected = buttonView.Item == selectedItem;

            if (buttonView.Background != null)
            {
                buttonView.Background.color = isSelected ? selectedButtonColor : normalButtonColor;
            }

            if (buttonView.TmpText != null)
            {
                buttonView.TmpText.fontStyle = isSelected ? FontStyles.Bold : FontStyles.Normal;
                buttonView.TmpText.color = isSelected ? selectedTextColor : normalTextColor;
            }

            if (buttonView.LegacyText != null)
            {
                buttonView.LegacyText.fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal;
                buttonView.LegacyText.color = isSelected ? selectedTextColor : normalTextColor;
            }
        }
    }

    private void RefreshProductionProgressVisual()
    {
        if (machine == null || productionProgressFill == null || productionProgressTMP == null)
        {
            return;
        }

        ItemSO selectedItem = machine.GetOutputItem();
        float duration = machine.GetProductionDuration();
        float elapsed = machine.GetProductionElapsedTime();
        float progress = machine.GetProductionProgressNormalized();

        if (selectedItem == null)
        {
            productionProgressFill.fillAmount = 0f;
            productionProgressTMP.text = "Select output item";
            if (inputStatusTMP != null) inputStatusTMP.text = "";
            if (countersTMP != null) countersTMP.text = "";
            return;
        }

        productionProgressFill.fillAmount = Mathf.Clamp01(progress);

        if (duration <= 0f)
        {
            productionProgressTMP.text = $"{selectedItem.displayName}: ready";
        }
        else
        {
            productionProgressTMP.text = $"{selectedItem.displayName}: {elapsed:0.0}s / {duration:0.0}s";
        }

        if (machine is Processor processor)
        {
            if (inputStatusTMP != null) inputStatusTMP.text = processor.GetInputBufferStatus();
            if (countersTMP != null) countersTMP.text = $"In: {processor.GetTotalReceived()} | Out: {processor.GetTotalProduced()}";
        }
    }

    private IEnumerable<MenuEntry> GetMenuEntries()
    {
        List<MenuEntry> entries = new List<MenuEntry>();

        if (machine is Processor processor)
        {
            var recipes = processor.GetRecipes();
            if (recipes != null && recipes.Count > 0)
            {
                foreach (var r in recipes)
                {
                    if (r != null && r.outputItem != null)
                    {
                        // Use Recipe Name for Processors
                        entries.Add(new MenuEntry { Item = r.outputItem, Label = "Recipe: " + r.name });
                    }
                }
                return entries;
            }
        }

        if (availableItems.Count > 0)
        {
            foreach (var item in availableItems)
            {
                if (item == null) continue;
                entries.Add(new MenuEntry { Item = item, Label = item.displayName });
            }
            return entries;
        }

        if (machine is Generator generator)
        {
            ItemSO item = generator.GetOutputItem();
            if (item != null)
            {
                entries.Add(new MenuEntry { Item = item, Label = item.displayName });
            }
        }

        return entries;
    }

    private bool IsGhostPreviewInstance()
    {
        return GetComponentInParent<BuildingGhost>() != null;
    }
}
