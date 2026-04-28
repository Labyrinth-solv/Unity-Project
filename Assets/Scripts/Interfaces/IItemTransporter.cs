
using UnityEngine;

public interface IItemTransporter
{
    bool HasItem();
    Item PeekItem();
    void RemoveItem();

    float GetProgress();          // 0 → 1 (item đã đi được bao xa)
    void SetProgress(float value);

    Vector3 GetDirection();       // hướng di chuyển
}