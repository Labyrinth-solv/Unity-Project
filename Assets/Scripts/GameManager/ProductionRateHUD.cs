using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class ProductionRateHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text outputText;
    [SerializeField] private string title = "Production Rate";
    [SerializeField] private List<ItemSO> trackedItems = new List<ItemSO>();
    [SerializeField] private float sampleWindowSeconds = 5f;
    [SerializeField] private float refreshInterval = 0.2f;

    private readonly StringBuilder stringBuilder = new StringBuilder();
    private float nextRefreshTime;

    private void OnEnable()
    {
        RefreshText();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefreshTime)
        {
            return;
        }

        RefreshText();
    }

    private void RefreshText()
    {
        nextRefreshTime = Time.unscaledTime + refreshInterval;

        if (outputText == null)
        {
            return;
        }

        stringBuilder.Clear();
        stringBuilder.AppendLine(title);

        foreach (ItemSO item in trackedItems)
        {
            if (item == null)
            {
                continue;
            }

            float rate = ProductionRateTracker.GetItemsPerSecond(item, sampleWindowSeconds);
            if (rate < 0.005f)
            {
                rate = 0f;
            }

            string itemName = string.IsNullOrWhiteSpace(item.displayName) ? item.name : item.displayName;
            stringBuilder.Append(itemName);
            stringBuilder.Append(": ");
            stringBuilder.Append(rate.ToString("0.##"));
            stringBuilder.AppendLine("/s");
        }

        outputText.text = stringBuilder.ToString().TrimEnd();
    }
}
