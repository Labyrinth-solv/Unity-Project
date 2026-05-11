using System.Collections.Generic;
using UnityEngine;

public static class ProductionRateTracker
{
    private static readonly Dictionary<ItemSO, Queue<float>> producedItemTimes = new Dictionary<ItemSO, Queue<float>>();

    public static void RecordProduced(ItemSO item)
    {
        if (item == null)
        {
            return;
        }

        if (!producedItemTimes.TryGetValue(item, out Queue<float> timestamps))
        {
            timestamps = new Queue<float>();
            producedItemTimes[item] = timestamps;
        }

        timestamps.Enqueue(Time.time);
    }

    public static float GetItemsPerSecond(ItemSO item, float sampleWindowSeconds)
    {
        if (item == null || sampleWindowSeconds <= 0f)
        {
            return 0f;
        }

        if (!producedItemTimes.TryGetValue(item, out Queue<float> timestamps))
        {
            return 0f;
        }

        CleanupOldSamples(timestamps, sampleWindowSeconds);
        if (timestamps.Count == 0)
        {
            return 0f;
        }

        return timestamps.Count / sampleWindowSeconds;
    }

    private static void CleanupOldSamples(Queue<float> timestamps, float sampleWindowSeconds)
    {
        float cutoffTime = Time.time - sampleWindowSeconds;
        while (timestamps.Count > 0 && timestamps.Peek() < cutoffTime)
        {
            timestamps.Dequeue();
        }
    }
}
