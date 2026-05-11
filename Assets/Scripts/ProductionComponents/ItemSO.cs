using UnityEngine;

[CreateAssetMenu(menuName = "Factory/Item")]
public class ItemSO : ScriptableObject
{
    public string id;
    public string displayName;
    public GameObject visualPrefab;
}
