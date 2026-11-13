using System;
using UnityEngine;
using UnityEngine.Events;

public class EntityHealthComponent : MonoBehaviour, IEntityRuntimeDependent, IDamageable, IHealable
{
    private EntityRuntime _entityData;
    
    [Header("偵錯設定")]
    [Tooltip("開啟後，印出無敵判定、防禦抵銷與剩餘血量的深度計算過程")]
    [SerializeField] private bool enableDebug = false;

    [Header("視覺廣播")]
    public UnityEvent onTakeDamageVisuals;
    
    public event Action OnDeath;
    public event Action<float> OnDamageTaken;

    // ★ 全局受擊廣播：給 Buff、天賦、動畫狀態機 (Motion) 監聽用
    public event Action<DamagePayload> OnTakeDamagePayload;
    
    public void OnRuntimeDataChanged(EntityRuntime newData)
    {
        _entityData = newData;
        if (enableDebug)
        {
            Debug.Log($"<color=cyan>[Health]</color> {gameObject.name} 綁定新的 RuntimeData: {(newData != null ? "成功" : "空值")}");
        }

        if (_entityData != null && _entityData.TryGetTrait(out RuntimeAnchorTrait anchor))
        {
            anchor.RegisterEntity(this.gameObject);
        }
    }

    public void TakeDamage(DamagePayload payload)
    {
        if (_entityData == null)
        {
            if (enableDebug) Debug.LogWarning($"<color=yellow>[Health]</color> {gameObject.name} 收到傷害，但 _entityData 為空，拒絕結算！");
            return;
        }

        if (_entityData.CurrentHealth <= 0)
        {
            if (enableDebug) Debug.LogWarning($"<color=grey>[Health]</color> {gameObject.name} 已經處於死亡狀態 (HP <= 0)，忽略傷害。");
            return;
        }

        string sourceName = payload.Source != null ? payload.Source.name : "未知/環境來源";
        if (enableDebug)
        {
            Debug.Log($"<color=cyan>[Health]</color> === {gameObject.name} 進入受擊結算 ===\n" +
                      $"- 攻擊來源: {sourceName}\n" +
                      $"- 原始傷害: {payload.Damage}\n" +
                      $"- 無視無敵標籤: {payload.IgnoreInvincibility}\n" +
                      $"- 當前無敵狀態: {_entityData.HasAnyState(EntityStateFlags.Invincible)}");
        }
        
        // 1. Tag 攔截層 (無敵判定)
        // 改成 HasAnyState，語意上代表：「只要身上有『無敵』這個標籤的任何一種來源就擋傷害」
        if (!payload.IgnoreInvincibility && _entityData.HasAnyState(EntityStateFlags.Invincible))
        {
            if (enableDebug) Debug.Log($"<color=yellow>[Health]</color> {gameObject.name} 命中被無敵幀阻擋 (Invincible)，結算中斷。");
            return; 
        }

        // 2. 數值結算層 (動態讀取 TotalDefense)
        float defense = _entityData.Defense != null ? _entityData.Defense.Value : 0f;
        float finalDamage = Mathf.Max(1, payload.Damage - defense);

        if (enableDebug)
        {
            Debug.Log($"<color=cyan>[Health]</color> 數值結算: 原始 {payload.Damage} - 防禦 {defense} = 最終承受 {finalDamage} 點 (最低保底 1 點)");
        }
        
        // 3. 執行扣血
        _entityData.ModifyHealth(-finalDamage);

        if (enableDebug)
        {
            Debug.Log($"<color=cyan>[Health]</color> {gameObject.name} 剩餘生命: {_entityData.CurrentHealth} / {_entityData.MaxHealth.Value}");
        }
        
        // 4. 廣播表現層 (粒子特效、UI 扣血條)
        onTakeDamageVisuals?.Invoke();
        OnDamageTaken?.Invoke(finalDamage);

        OnTakeDamagePayload?.Invoke(payload);

        if (_entityData.CurrentHealth <= 0)
        {
            if (enableDebug) Debug.Log($"<color=red>[Health]</color> {gameObject.name} 生命歸零，觸發 OnDeath 事件！");
            
            // 1. 給大腦掛上死亡標籤 (這會讓 StateMachine 自動切換到 PlayerDeadState)
            _entityData.AddState(EntityStateFlags.Dead); 
            
            // 2. 廣播給 UI、GameManager 等外部系統
            OnDeath?.Invoke();
        }
        else if (!payload.IgnoreInvincibility && _entityData.DamageInvincibilityDuration != null && _entityData.DamageInvincibilityDuration.Value > 0f)
        {
            float duration = _entityData.DamageInvincibilityDuration.Value;
            if (enableDebug) Debug.Log($"<color=yellow>[Health]</color> {gameObject.name} 掛載受擊無敵幀，持續時間: {duration} 秒");
            _entityData.AddTemporaryState(EntityStateFlags.Invincible, duration);
        }
    }

    public void ReceiveHeal(HealPayload payload)
    {
        if (_entityData == null || _entityData.CurrentHealth <= 0)
        {
            if (enableDebug) Debug.LogWarning($"<color=yellow>[Health]</color> {gameObject.name} 無法接受治療 (_entityData 為空或已死亡)");
            return;
        }
        
        if (enableDebug)
        {
            string sourceName = payload.Source != null ? payload.Source.name : "未知來源";
            Debug.Log($"<color=green>[Health]</color> {gameObject.name} 收到來自 {sourceName} 的治療: {payload.HealAmount} 點，治療前 HP: {_entityData.CurrentHealth}");
        }

        _entityData.ModifyHealth(payload.HealAmount);

        if (enableDebug)
        {
            Debug.Log($"<color=green>[Health]</color> {gameObject.name} 治療後 HP: {_entityData.CurrentHealth} / {_entityData.MaxHealth.Value}");
        }
    }
}