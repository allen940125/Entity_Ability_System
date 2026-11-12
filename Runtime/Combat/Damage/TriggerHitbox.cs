using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TriggerHitbox : DamageCasterBase
{
    private void OnTriggerEnter(Collider other)
    {
        // 直接把碰到的 Collider 丟給基底處理
        TryApplyDamage(other);
    }
}