using UnityEngine;

public abstract class AbilitySO : ScriptableObject
{
    [Header("技能基本設定")]
    public string abilityName = "New Ability";
    public float cooldownTime = 1.0f;
    public float staminaCost = 0f;
    
    [Tooltip("技能持續時間。大於 0 代表 Runner 會自動幫你倒數並呼叫 EndAbility；等於 0 代表瞬發技能或由動畫事件手動結束。")]
    public float duration = 0f; 

    [Header("標籤條件設定 (GAS 核心)")]
    public EntityStateFlags requiredStates = EntityStateFlags.None;
    public EntityStateFlags blockStates = EntityStateFlags.Dead | EntityStateFlags.Stunned;

    public virtual bool CanActivate(AbilityRunner runner)
    {
        var runtime = runner.Core.RuntimeData;

        if (runtime.HasAnyState(blockStates)) return false;

        if (requiredStates != EntityStateFlags.None && !runtime.HasAllStates(requiredStates)) 
            return false;

        if (staminaCost > 0f && runtime.TryGetTrait(out StaminaTrait stamina))
        {
            if (stamina.currentStamina < staminaCost) return false;
        }

        return true;
    }

    // 技能開始執行 (由 Runner 呼叫)
    public abstract void Activate(AbilityRunner runner);
    
    // ★ 新增：技能執行期間每幀呼叫 (給蓄力、持續施法、或狀態檢查使用)
    public virtual void OnAbilityTick(AbilityRunner runner) { }

    // ★ 新增：技能結束執行 (可以選擇性覆寫，用來清除 Tag 或特效)
    public virtual void EndAbility(AbilityRunner runner) 
    { 
        // 預設什麼都不做，繼承的技能可以覆寫它，例如 Dash 可以在這裡移除 Dashing Tag
    }
}