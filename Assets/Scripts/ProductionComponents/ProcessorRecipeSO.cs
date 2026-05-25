using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Factory/Processor Recipe")]
public class ProcessorRecipeSO : ScriptableObject
{
    [System.Serializable]
    public struct Ingredient
    {
        public ItemSO item;
        public int count;
    }

    public List<Ingredient> ingredients;
    public ItemSO outputItem;
    public float processingTime = 2f;
}
