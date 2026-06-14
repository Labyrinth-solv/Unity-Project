using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Factory/Processor Recipe List")]
public class ProcessorRecipeListSO : ScriptableObject
{
    [SerializeField] private List<ProcessorRecipeSO> recipes = new List<ProcessorRecipeSO>();

    public List<ProcessorRecipeSO> Recipes => recipes;
}
