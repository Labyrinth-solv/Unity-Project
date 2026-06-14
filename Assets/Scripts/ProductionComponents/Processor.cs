using UnityEngine;
using System.Collections.Generic;

public class Processor : MonoBehaviour, ITickable, IProductionMachine, IItemEndpoint
{
    [SerializeField] private string machineName = "Processor";
    [SerializeField] private ProcessorRecipeListSO recipeList;
    [SerializeField, HideInInspector] private List<ProcessorRecipeSO> recipes;
    
    public string GetMachineName() => machineName;
    public List<ProcessorRecipeSO> GetRecipes()
    {
        if (recipeList != null)
        {
            return recipeList.Recipes;
        }

        return recipes;
    }

    [SerializeField] private ProcessorRecipeSO currentRecipe;

    [System.Serializable]
    private class BufferedItem
    {
        public ItemSO item;
        public int count;
    }

    [SerializeField] private List<BufferedItem> inputBuffer = new List<BufferedItem>();
    [SerializeField] private ItemSO outputBuffer;

    private float timer;
    private bool isProcessing;
    private int totalItemsReceived;
    private int totalItemsProduced;

    private void OnEnable()
    {
        ValidateCurrentRecipe();
        TickManager.Register(this);
    }

    private void OnDisable()
    {
        TickManager.Unregister(this);
    }

    public void Tick(float deltaTime)
    {
        if (isProcessing)
        {
            timer += deltaTime;
            if (timer >= currentRecipe.processingTime)
            {
                FinishProcessing();
            }
        }
        else
        {
            if (CanStartProcessing())
            {
                StartProcessing();
            }
        }
    }

    private bool CanStartProcessing()
    {
        if (currentRecipe == null || outputBuffer != null) return false;

        foreach (var ingredient in currentRecipe.ingredients)
        {
            if (GetBufferedCount(ingredient.item) < ingredient.count)
                return false;
        }
        return true;
    }

    private void StartProcessing()
    {
        isProcessing = true;
        timer = 0f;
    }

    private void FinishProcessing()
    {
        isProcessing = false;
        timer = 0f;

        // Consume ingredients
        foreach (var ingredient in currentRecipe.ingredients)
        {
            RemoveBufferedCount(ingredient.item, ingredient.count);
        }

        outputBuffer = currentRecipe.outputItem;
        ProductionRateTracker.RecordProduced(outputBuffer);
    }

    private int GetBufferedCount(ItemSO item)
    {
        var buffered = inputBuffer.Find(b => b.item == item);
        return buffered != null ? buffered.count : 0;
    }

    private void AddBufferedCount(ItemSO item, int amount)
    {
        var buffered = inputBuffer.Find(b => b.item == item);
        if (buffered == null)
        {
            buffered = new BufferedItem { item = item, count = 0 };
            inputBuffer.Add(buffered);
        }
        buffered.count += amount;
    }

    private void RemoveBufferedCount(ItemSO item, int amount)
    {
        var buffered = inputBuffer.Find(b => b.item == item);
        if (buffered != null)
        {
            buffered.count -= amount;
            if (buffered.count <= 0)
            {
                inputBuffer.Remove(buffered);
            }
        }
    }

    // IProductionMachine Implementation
    public void SetOutputItem(ItemSO item)
    {
        List<ProcessorRecipeSO> availableRecipes = GetRecipes();
        ProcessorRecipeSO newRecipe = (availableRecipes != null) ? availableRecipes.Find(r => r != null && r.outputItem == item) : null;
        if (newRecipe != currentRecipe)
        {
            currentRecipe = newRecipe;
            // Reset processing if recipe changes to avoid undefined state
            isProcessing = false;
            timer = 0f;
        }
    }

    public ItemSO GetOutputItem() => currentRecipe?.outputItem;
    public float GetProductionProgressNormalized() => currentRecipe == null || currentRecipe.processingTime <= 0 ? 0 : timer / currentRecipe.processingTime;
    public float GetProductionElapsedTime() => timer;
    public float GetProductionDuration() => currentRecipe?.processingTime ?? 0f;

    // IItemEndpoint Implementation
    public bool CanReceive()
    {
        if (currentRecipe == null) return false;
        // Can receive if any ingredient is still needed for the current recipe
        foreach (var ingredient in currentRecipe.ingredients)
        {
            if (GetBufferedCount(ingredient.item) < ingredient.count) return true;
        }
        return false;
    }

    public bool TryReceive(ItemSO item)
    {
        if (currentRecipe == null) return false;

        // Check if item is an ingredient for the current recipe
        var ingredient = currentRecipe.ingredients.Find(i => i.item == item);
        if (ingredient.item == null) return false;

        if (GetBufferedCount(item) < ingredient.count)
        {
            AddBufferedCount(item, 1);
            totalItemsReceived++;
            return true;
        }
        return false;
    }

    public int GetTotalReceived() => totalItemsReceived;
    public int GetTotalProduced() => totalItemsProduced;

    public string GetInputBufferStatus()
    {
        if (currentRecipe == null) return "No recipe selected";
        
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append("Input: ");
        for (int i = 0; i < currentRecipe.ingredients.Count; i++)
        {
            var ing = currentRecipe.ingredients[i];
            int current = GetBufferedCount(ing.item);
            sb.Append($"{ing.item.displayName} ({current}/{ing.count})");
            if (i < currentRecipe.ingredients.Count - 1) sb.Append(", ");
        }
        return sb.ToString();
    }

    public bool CanProvide() => outputBuffer != null;
    public ItemSO Peek() => outputBuffer;
    public void Consume()
    {
        outputBuffer = null;
    }

    private void ValidateCurrentRecipe()
    {
        if (currentRecipe == null) return;

        List<ProcessorRecipeSO> availableRecipes = GetRecipes();
        if (availableRecipes == null || !availableRecipes.Contains(currentRecipe))
        {
            currentRecipe = null;
            isProcessing = false;
            timer = 0f;
        }
    }
}
