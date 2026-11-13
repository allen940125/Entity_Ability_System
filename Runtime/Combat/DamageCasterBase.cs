using System.Collections.Generic;
using UnityEngine;

public abstract class DamageCasterBase : MonoBehaviour
{
    protected EntityCore _casterCore;
    [SerializeField] protected AttackDataSO _attackData; 
    
    [Header("★ 偵錯設定")]
    [SerializeField] protected bool enableDebug = false;

    protected int _fallbackBaseDamage = 10; 

    // ==========================================
    // ★ 新增：跨越單發與持續的目標記憶系統
    // ==========================================
    // 記憶結構：紀錄每一個被打到的 Collider，以及它最後一次被打到的時間
    protected Dictionary<Collider, float> _hitMemory = new Dictionary<Collider, float>();

    public virtual void Initialize(EntityCore caster, AttackDataSO attackData, int fallbackDamage = 10)
    {
        _casterCore = caster;
        _attackData = attackData;
        _fallbackBaseDamage = fallbackDamage;
        
        // 初始化時清空記憶
        _hitMemory.Clear();
    }

    // 提供給子類別手動清空記憶 (例如：迴力鏢折返時、揮砍動作重置時)
    public virtual void ClearHitMemory()
    {
        _hitMemory.Clear();
        if (enableDebug) Debug.Log($"<color=white>[DamageCaster]</color> 命中記憶已清空。");
    }

    // ==========================================
    // ★ 核心升級：統一封裝傷害嘗試與 Tick 邏輯
    // ==========================================
    protected void TryApplyDamage(Collider targetCollider)
    {
        if (_attackData == null) return; 

        // --- 1. Tick 與記憶過濾層 ---
        if (_hitMemory.TryGetValue(targetCollider, out float lastHitTime))
        {
            if (_attackData.useDamageTick)
            {
                // 若設定為持續傷害，且距離上次傷害未超過間隔，則攔截
                if (Time.time < lastHitTime + _attackData.damageTickInterval)
                {
                    return; 
                }
            }
            else
            {
                // 若不是持續傷害，代表「打過一次就不准再打」，直接攔截
                return;
            }
        }

        // --- 2. 目標合法性檢查 ---
        var damageable = targetCollider.GetComponentInParent<IDamageable>();
        if (damageable == null)
        {
            if (enableDebug) Debug.LogWarning($"<color=grey>[DamageCaster]</color> 目標 {targetCollider.name} 沒有 IDamageable，無法造成傷害！");
            return;
        }

        if (_casterCore != null && targetCollider.gameObject == _casterCore.gameObject)
        {
            if (enableDebug) Debug.LogWarning($"<color=grey>[DamageCaster]</color> 打中施法者自己，取消傷害。");
            return;
        }

        // --- 3. 數值結算 ---
        int finalDamage = (_casterCore != null && _casterCore.RuntimeData != null) 
            ? Mathf.RoundToInt(_casterCore.RuntimeData.AttackPower.Value * _attackData.damageMultiplier) 
            : Mathf.RoundToInt(_fallbackBaseDamage * _attackData.damageMultiplier);

        DamagePayload payload = new DamagePayload
        {
            Damage = finalDamage,
            IsCrit = false, // 未來可擴充爆擊率
            Source = _casterCore != null ? _casterCore.gameObject : this.gameObject,
            CasterCore = _casterCore,
            IgnoreInvincibility = _attackData.ignoreInvincibility
        };

        if (enableDebug) Debug.Log($"<color=green>[DamageCaster]</color> 成功發送 {finalDamage} 點傷害給 {targetCollider.name}！");
        
        // --- 4. 執行扣血 ---
        damageable.TakeDamage(payload);

        // --- 5. 更新命中記憶 ---
        // 成功發送傷害後，記錄這個目標的時間
        _hitMemory[targetCollider] = Time.time;
    }
    
#if UNITY_EDITOR
    protected virtual void OnDrawGizmos()
    {
        if (_attackData == null) return;
        _attackData.damageShape.DrawGizmo(transform.position, transform.rotation, Color.red);
        
        if (_attackData is ProjectileDataSO projData)
        {
            projData.bounceShape.DrawGizmo(transform.position, transform.rotation, Color.yellow);
        }
    }
#endif
}