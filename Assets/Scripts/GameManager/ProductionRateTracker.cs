using System.Collections.Generic;
using UnityEngine;

public static class ProductionRateTracker
{
    private static readonly Dictionary<ItemSO, Queue<float>> producedItemTimes = new Dictionary<ItemSO, Queue<float>>();
    private static readonly Dictionary<ItemSO, Queue<float>> deliveredItemTimes = new Dictionary<ItemSO, Queue<float>>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlayModeStart()
    {
        ResetTracking();
    }

    public static void ResetTracking()
    {
        producedItemTimes.Clear();
        deliveredItemTimes.Clear();
    }

    public static void RecordProduced(ItemSO item)
    {
        Record(item, producedItemTimes);
    }

    public static void RecordDelivery(ItemSO item)
    {
        Record(item, deliveredItemTimes);
    }

    private static void Record(ItemSO item, Dictionary<ItemSO, Queue<float>> dict)
    {
        if (item == null)
        {
            return;
        }

        if (!dict.TryGetValue(item, out Queue<float> timestamps))
        {
            timestamps = new Queue<float>();
            dict[item] = timestamps;
        }

        timestamps.Enqueue(Time.time);
    }

    public static float GetItemsPerSecond(ItemSO item, float sampleWindowSeconds)
    {
        return GetRate(item, producedItemTimes, sampleWindowSeconds);
    }

    public static float GetDeliveryRatePerSecond(ItemSO item, float sampleWindowSeconds)
    {
        return GetRate(item, deliveredItemTimes, sampleWindowSeconds);
    }

    private static float GetRate(ItemSO item, Dictionary<ItemSO, Queue<float>> dict, float sampleWindowSeconds)
    {
        if (item == null || sampleWindowSeconds <= 0f)
        {
            return 0f;
        }

        if (!dict.TryGetValue(item, out Queue<float> timestamps))
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
        float currentTime = Time.time;
        float cutoffTime = currentTime - sampleWindowSeconds;
        while (timestamps.Count > 0 && (timestamps.Peek() < cutoffTime || timestamps.Peek() > currentTime))
        {
            timestamps.Dequeue();
        }
    }
}
