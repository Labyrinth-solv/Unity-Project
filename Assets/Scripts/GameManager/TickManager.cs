using System.Collections.Generic;
using UnityEngine;

public class TickManager : MonoBehaviour
{
    public static TickManager Instance { get; private set; }

    private readonly List<ITickable> tickables = new List<ITickable>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateInstance()
    {
        EnsureInstance();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        for (int i = 0; i < tickables.Count; i++)
        {
            tickables[i].Tick(deltaTime);
        }
    }

    public static void Register(ITickable tickable)
    {
        if (tickable == null) return;
        EnsureInstance();
        Instance.RegisterInternal(tickable);
    }

    public static void Unregister(ITickable tickable)
    {
        if (tickable == null || Instance == null) return;
        Instance.tickables.Remove(tickable);
    }

    private static void EnsureInstance()
    {
        if (Instance != null) return;

        GameObject tickManagerObject = new GameObject("TickManager");
        DontDestroyOnLoad(tickManagerObject);
        tickManagerObject.AddComponent<TickManager>();
    }

    private void RegisterInternal(ITickable tickable)
    {
        if (tickables.Contains(tickable)) return;
        tickables.Add(tickable);
    }
}
