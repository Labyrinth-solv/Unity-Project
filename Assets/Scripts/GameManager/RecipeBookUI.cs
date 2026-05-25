using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

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

        // Gather all recipes in the project
        List<ProcessorRecipeSO> allRecipes = new List<ProcessorRecipeSO>();
#if UNITY_EDITOR
        string[] guids = AssetDatabase.FindAssets("t:ProcessorRecipeSO");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ProcessorRecipeSO recipe = AssetDatabase.LoadAssetAtPath<ProcessorRecipeSO>(path);
            if (recipe != null) allRecipes.Add(recipe);
        }
#endif

        foreach (var r in allRecipes)
        {
            CreateRecipeRow("Processor", r);
        }

        // Gather all Generator configurations from PlacedObjectTypeSOs
        // (Assuming Generators are defined in the PlacedObjectTypeSO list in GridBuildingSystem)
        if (GridBuildingSystem.Instance != null)
        {
            // We need to access the list of building types. 
            // Since it's private in GridBuildingSystem, we'll try to find any Generator prefabs/instances.
            Generator[] sceneGenerators = Object.FindObjectsByType<Generator>(FindObjectsSortMode.None);
            HashSet<ItemSO> seenGenerators = new HashSet<ItemSO>();
            foreach (var g in sceneGenerators)
            {
                ItemSO output = g.GetOutputItem();
                if (output == null || seenGenerators.Contains(output)) continue;
                seenGenerators.Add(output);
                CreateGeneratorRow(g.GetMachineName(), output);
            }
            
            // If no scene generators, maybe check if we can find them in project
#if UNITY_EDITOR
            string[] buildingGuids = AssetDatabase.FindAssets("t:PlacedObjectTypeSO");
            foreach (string guid in buildingGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                PlacedObjectTypeSO type = AssetDatabase.LoadAssetAtPath<PlacedObjectTypeSO>(path);
                if (type != null && type.prefab != null)
                {
                    Generator g = type.prefab.GetComponent<Generator>();
                    if (g != null)
                    {
                        ItemSO output = g.GetOutputItem();
                        if (output != null && !seenGenerators.Contains(output))
                        {
                            seenGenerators.Add(output);
                            CreateGeneratorRow(g.GetMachineName(), output);
                        }
                    }
                }
            }
#endif
        }
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
