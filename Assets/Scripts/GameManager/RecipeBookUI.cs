using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text;
using TMPro;

public class RecipeBookUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Transform container;
    [SerializeField] private GameObject rowPrefab;

    private void Start()
    {
        if (panel != null) panel.SetActive(false);
        if (openButton != null) openButton.onClick.AddListener(TogglePanel);
        if (closeButton != null) closeButton.onClick.AddListener(TogglePanel);
    }

    public void TogglePanel()
    {
        if (panel == null) return;
        bool isActive = !panel.activeSelf;
        panel.SetActive(isActive);
        if (isActive)
        {
            RefreshUI();
        }
    }

    private void RefreshUI()
    {
        if (container == null) return;

        // Clear existing
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }

        HashSet<ProcessorRecipeSO> seenProcessorRecipes = new HashSet<ProcessorRecipeSO>();
        HashSet<ItemSO> seenGeneratorOutputs = new HashSet<ItemSO>();

        foreach (Processor processor in Object.FindObjectsByType<Processor>(FindObjectsSortMode.None))
        {
            AddProcessorRecipes(processor.GetMachineName(), processor.GetRecipes(), seenProcessorRecipes);
        }

        foreach (Generator generator in Object.FindObjectsByType<Generator>(FindObjectsSortMode.None))
        {
            AddGeneratorOutputs(generator.GetMachineName(), generator, generator.GetComponent<ProductionMachineUI>(), seenGeneratorOutputs);
        }

        if (GridBuildingSystem.Instance != null)
        {
            IReadOnlyList<PlacedObjectTypeSO> placedObjectTypes = GridBuildingSystem.Instance.GetPlacedObjectTypes();
            if (placedObjectTypes == null) return;

            foreach (PlacedObjectTypeSO type in placedObjectTypes)
            {
                AddMachineRecipesFromPrefab(type, seenProcessorRecipes, seenGeneratorOutputs);
            }
        }
    }

    private void AddMachineRecipesFromPrefab(PlacedObjectTypeSO type, HashSet<ProcessorRecipeSO> seenProcessorRecipes, HashSet<ItemSO> seenGeneratorOutputs)
    {
        if (type == null || type.prefab == null) return;

        Processor processor = type.prefab.GetComponent<Processor>();
        if (processor != null)
        {
            AddProcessorRecipes(processor.GetMachineName(), processor.GetRecipes(), seenProcessorRecipes);
        }

        Generator generator = type.prefab.GetComponent<Generator>();
        if (generator != null)
        {
            AddGeneratorOutputs(generator.GetMachineName(), generator, type.prefab.GetComponent<ProductionMachineUI>(), seenGeneratorOutputs);
        }
    }

    private void AddProcessorRecipes(string machineName, List<ProcessorRecipeSO> recipes, HashSet<ProcessorRecipeSO> seenRecipes)
    {
        if (recipes == null) return;

        foreach (ProcessorRecipeSO recipe in recipes)
        {
            if (recipe == null || seenRecipes.Contains(recipe)) continue;

            seenRecipes.Add(recipe);
            CreateRecipeRow(machineName, recipe);
        }
    }

    private void AddGeneratorOutputs(string machineName, Generator generator, ProductionMachineUI machineUI, HashSet<ItemSO> seenOutputs)
    {
        if (generator != null)
        {
            AddGeneratorOutput(machineName, generator.GetOutputItem(), seenOutputs);
        }

        IReadOnlyList<ItemSO> availableItems = machineUI != null ? machineUI.GetAvailableItems() : null;
        if (availableItems == null) return;

        foreach (ItemSO item in availableItems)
        {
            AddGeneratorOutput(machineName, item, seenOutputs);
        }
    }

    private void AddGeneratorOutput(string machineName, ItemSO output, HashSet<ItemSO> seenOutputs)
    {
        if (output == null || seenOutputs.Contains(output)) return;

        seenOutputs.Add(output);
        CreateGeneratorRow(machineName, output);
    }

    private void CreateRecipeRow(string machineName, ProcessorRecipeSO recipe)
    {
        if (rowPrefab == null) return;
        GameObject row = Instantiate(rowPrefab, container);
        row.SetActive(true);
        TMP_Text text = row.GetComponentInChildren<TMP_Text>();
        if (text != null)
        {
            StringBuilder sb = new StringBuilder();
            if (recipe.ingredients != null)
            {
                for (int i = 0; i < recipe.ingredients.Count; i++)
                {
                    if (recipe.ingredients[i].item != null)
                    {
                        sb.Append(recipe.ingredients[i].item.displayName);
                        if (recipe.ingredients[i].count > 1) sb.Append(" x").Append(recipe.ingredients[i].count);
                        if (i < recipe.ingredients.Count - 1) sb.Append(" + ");
                    }
                }
            }
            sb.Append(" -> ").Append(machineName).Append(" -> ").Append(recipe.outputItem != null ? recipe.outputItem.displayName : "Unknown");
            text.text = sb.ToString();
        }
    }

    private void CreateGeneratorRow(string machineName, ItemSO output)
    {
        if (rowPrefab == null) return;
        GameObject row = Instantiate(rowPrefab, container);
        row.SetActive(true);
        TMP_Text text = row.GetComponentInChildren<TMP_Text>();
        if (text != null)
        {
            text.text = "None -> " + machineName + " -> " + (output != null ? output.displayName : "Unknown");
        }
    }
}
