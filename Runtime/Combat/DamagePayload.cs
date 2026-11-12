using UnityEngine;

// 1. 定義明確的介面
public interface IDamageable
{
    void TakeDamage(DamagePayload payload);
}

public interface IHealable
{
    void ReceiveHeal(HealPayload payload);
}

// 2. 定義純粹的封包
public struct DamagePayload
{
    public int Damage;
    public bool IsCrit;
    public GameObject Source;     // 攻擊來源
    public EntityCore CasterCore; // ★ 新增：如果需要讀取攻擊者的屬性(如吸血)，必須傳入大腦

    // ★ 新架構核心：傷害標籤 (用來判定弱點、抗性或特殊效果)
    // 例如：Damage.Fire, Damage.Heavy, Damage.Piercing
    public EntityStateFlags DamageTags; 
    
    public bool IgnoreInvincibility;
}

public struct HealPayload
{
    public int HealAmount;
    public GameObject Source;
    // 未來可以擴充：public bool IsOverHealAllowed; 等
}