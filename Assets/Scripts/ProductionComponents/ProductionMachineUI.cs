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
    [SerializeField] private Button openMenuButton;
    [SerializeField] private Canvas menuPanel;
    [SerializeField] private Transform itemButtonContainer;
    [SerializeField] private GameObject itemButtonPrefab;

    [Header("Items")]
    [SerializeField] private List<ItemSO> availableItems = new List<ItemSO>();

    [Header("Screen UI Layout")]
    [SerializeField] private Vector2 canvasSize = new Vector2(420f, 260f);
    [SerializeField] private Vector2 menuSize = new Vector2(260f, 240f);
    [SerializeField] private Vector2 menuScreenPosition = new Vector2(40f, -40f);
    [SerializeField] private Vector2 closeButtonPosition = new Vector2(130f, -10f);
    [SerializeField] private Vector2 buttonSize = new Vector2(120f, 30f);
    [SerializeField] private Color selectedButtonColor = new Color(1f, 0.93f, 0.55f, 1f);
    [SerializeField] private Color normalButtonColor = Color.white;
    [SerializeField] private Color selectedTextColor = Color.black;
    [SerializeField] private Color normalTextColor = new Color(0.19607843f, 0.19607843f, 0.19607843f, 1f);

    private static ProductionMachineUI openInstance;

    private IProductionMachine machine;
    private Canvas runtimeCanvas;
    private RectTransform runtimeCanvasRect;
    private RectTransform runtimeMenuRoot;
    private Transform runtimeItemContainer;
    private readonly List<GameObject> spawnedButtons = new List<GameObject>();
    private readonly List<ItemButtonView> itemButtonViews = new List<ItemButtonView>();

    private void Awake()
    {
        machine = FindProductionMachine();
        if (machine == null)
        {
            Debug.LogError($"ProductionMachineUI could not find a component implementing IProductionMachine near {name}.");
            enabled = false;
            return;
        }

        BuildUIFromPrefabs();
        CreateItemButtons();
        CloseMenu();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.O))
        {
            HandleOpenShortcut();
        }

        if (openInstance == this && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseMenu();
        }
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

    private void BuildUIFromPrefabs()
    {
        runtimeCanvas = ResolveCanvas();
        if (runtimeCanvas == null)
        {
            Debug.LogError("ProductionMachineUI could not resolve the Canvas prefab/reference.");
            enabled = false;
            return;
        }

        runtimeCanvasRect = runtimeCanvas.GetComponent<RectTransform>();
        ConfigureCanvas();

        runtimeMenuRoot = ResolveMenuRoot();
        if (runtimeMenuRoot == null)
        {
            Debug.LogError("ProductionMachineUI could not find GeneratorMenuPanel inside the Canvas prefab.");
            enabled = false;
            return;
        }
        ConfigureMenuRoot();

        Button closeButton = CreateRuntimeButton("CloseButton", runtimeMenuRoot, closeButtonPosition, buttonSize);
        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(CloseMenu);
        SetButtonLabel(closeButton.gameObject, "Close");

        Transform scrollContent = ResolveScrollContent();
        runtimeItemContainer = ResolveItemContainer(scrollContent);
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

    private Canvas ResolveCanvas()
    {
        if (IsSceneObject(menuPanel))
        {
            return menuPanel;
        }

        if (menuPanel != null)
        {
            return Instantiate(menuPanel);
        }

        return null;
    }

    private void ConfigureCanvas()
    {
        runtimeCanvas.transform.SetParent(null, false);
        runtimeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        runtimeCanvas.worldCamera = null;
        runtimeCanvas.sortingOrder = 100;

        if (runtimeCanvasRect != null)
        {
            runtimeCanvasRect.anchorMin = Vector2.zero;
            runtimeCanvasRect.anchorMax = Vector2.zero;
            runtimeCanvasRect.pivot = Vector2.zero;
            runtimeCanvasRect.sizeDelta = canvasSize;
            runtimeCanvasRect.anchoredPosition = Vector2.zero;
            runtimeCanvasRect.localScale = Vector3.one;
        }
    }

    private RectTransform ResolveMenuRoot()
    {
        Transform namedRoot = runtimeCanvas.transform.Find("GeneratorMenuPanel");
        if (namedRoot != null)
        {
            return namedRoot as RectTransform;
        }

        Image image = runtimeCanvas.GetComponentInChildren<Image>(true);
        return image != null ? image.GetComponent<RectTransform>() : null;
    }

    private void ConfigureMenuRoot()
    {
        runtimeMenuRoot.anchorMin = new Vector2(0f, 1f);
        runtimeMenuRoot.anchorMax = new Vector2(0f, 1f);
        runtimeMenuRoot.pivot = new Vector2(0f, 1f);
        runtimeMenuRoot.sizeDelta = menuSize;
        runtimeMenuRoot.anchoredPosition = menuScreenPosition;
        runtimeMenuRoot.localScale = Vector3.one;
    }

    private Transform ResolveScrollContent()
    {
        ScrollRect scrollRect = runtimeMenuRoot.GetComponentInChildren<ScrollRect>(true);
        if (scrollRect != null && scrollRect.content != null)
        {
            RectTransform contentRect = scrollRect.content;
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.localScale = Vector3.one;
            return contentRect;
        }

        return runtimeMenuRoot;
    }

    private Transform ResolveItemContainer(Transform parent)
    {
        Transform container;

        if (IsSceneObject(itemButtonContainer))
        {
            container = itemButtonContainer;
            container.SetParent(parent, false);
        }
        else if (itemButtonContainer != null)
        {
            container = Instantiate(itemButtonContainer, parent);
        }
        else
        {
            GameObject containerObject = new GameObject("ItemButtonContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
            containerObject.transform.SetParent(parent, false);
            container = containerObject.transform;
        }

        RectTransform rectTransform = container as RectTransform;
        if (rectTransform != null)
        {
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.localScale = Vector3.one;
        }

        VerticalLayoutGroup layout = container.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = container.gameObject.AddComponent<VerticalLayoutGroup>();
        }
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = container.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = container.gameObject.AddComponent<ContentSizeFitter>();
        }
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return container;
    }

    private Button CreateRuntimeButton(string objectName, Transform parent, Vector2 anchoredPosition, Vector2 size)
    {
        Button template = openMenuButton;
        Button button;

        if (template != null)
        {
            button = Instantiate(template, parent);
        }
        else
        {
            GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            button = buttonObject.GetComponent<Button>();
        }

        button.gameObject.name = objectName;
        ConfigureButtonRect(button.GetComponent<RectTransform>(), anchoredPosition, size);
        return button;
    }

    private void ConfigureButtonRect(RectTransform rectTransform, Vector2 anchoredPosition, Vector2 size)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;
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

        if (runtimeItemContainer == null)
        {
            Debug.LogWarning("ProductionMachineUI is missing an item button container.");
            return;
        }

        if (itemButtonPrefab == null)
        {
            Debug.LogWarning("ProductionMachineUI is missing the item button prefab.");
            return;
        }

        foreach (ItemSO item in GetMenuItems())
        {
            if (item == null)
            {
                continue;
            }

            GameObject buttonObject = Instantiate(itemButtonPrefab, runtimeItemContainer);
            buttonObject.name = $"Item_{item.displayName}";
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

            SetButtonLabel(buttonObject, item.displayName);

            Button button = buttonObject.GetComponent<Button>();
            if (button != null)
            {
                ItemSO selectedItem = item;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => SelectItem(selectedItem));
            }

            itemButtonViews.Add(new ItemButtonView
            {
                Item = item,
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
        if (runtimeMenuRoot == null)
        {
            return;
        }

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
        CloseMenu();
    }

    private void SetButtonLabel(GameObject buttonObject, string label)
    {
        TMP_Text tmpText = buttonObject.GetComponentInChildren<TMP_Text>(true);
        if (tmpText != null)
        {
            tmpText.text = label;
            return;
        }

        Text text = buttonObject.GetComponentInChildren<Text>(true);
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

    private IEnumerable<ItemSO> GetMenuItems()
    {
        if (availableItems.Count > 0)
        {
            return availableItems;
        }

        ItemSO currentItem = machine != null ? machine.GetOutputItem() : null;
        if (currentItem != null)
        {
            return new[] { currentItem };
        }

        return System.Array.Empty<ItemSO>();
    }

    private bool IsSceneObject(Component component)
    {
        return component != null && component.gameObject.scene.IsValid();
    }
}
