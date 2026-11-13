using UnityEngine;

// ★ 只有具體的子類別才開放 CreateAssetMenu 給企劃使用
[CreateAssetMenu(fileName = "New ProjectileDataSO", menuName = "Combat/ProjectileDataSO")]
public class ProjectileDataSO : AttackDataSO
{
    [Header("★ 肉體 (空殼)")]
    [Tooltip("通常只是一個掛了 ProjectileDamageCaster 的泛用 Prefab")]
    public GameObject projectilePrefab;
    
    [Header("★ 飛行物理設定 (BulletDataSO 專屬)")]
    public float baseSpeed = 10f;
    
    [Tooltip("子彈存活時間 (秒)")]
    public float lifeTime = 5f;

    [Header("★ 反彈機制")]
    public bool canBounce = false;
    public int maxBounces = 3;
    
    [Tooltip("用來偵測牆壁的形狀 (通常比傷害判定盒小，避免穿模)")]
    public CollisionShapeConfig bounceShape;
    public LayerMask bounceLayer;

    [Header("★ 反彈擾動 (Jitter)")]
    [Range(0f, 60f)] public float maxBounceAngleJitter = 0f;
    public bool jitterFirstBounceOnly = false;
}