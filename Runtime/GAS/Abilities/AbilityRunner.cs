using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EntityCore))]
public class AbilityRunner : MonoBehaviour
{
    [Header("核心依賴 (必填)")]
    public EntityCore Core;
    public Animator Anim;
    public Rigidbody RB;

    // 記錄這個角色每個技能的「下一次可用時間」(冷卻)
    private Dictionary<AbilitySO, float> _nextAvailableTimes = new Dictionary<AbilitySO, float>();
    
    // ★ 新增：記錄「正在執行中」的技能，以及它的「結束時間」
    private Dictionary<AbilitySO, float> _activeAbilities = new Dictionary<AbilitySO, float>();
    
    // 用來暫存要結束的技能 (避免在 foreach 迴圈裡直接修改 Dictionary 導致報錯)
    private List<AbilitySO> _abilitiesToEnd = new List<AbilitySO>();

    // ★ 新增：專門用來跑迴圈的「快照名單」(避免 foreach 字典時字典被修改)
    private List<AbilitySO> _currentActiveKeys = new List<AbilitySO>();
    
    private void Awake()
    {
        if (Core == null) Core = GetComponent<EntityCore>();
        if (Anim == null) Anim = GetComponentInChildren<Animator>();
        if (RB == null) RB = GetComponent<Rigidbody>();
    }

    // ★ 新增：在 Update 巡邏，幫有 Duration 的技能倒數計時
    private void Update()
    {
        if (_activeAbilities.Count == 0) return;

        _abilitiesToEnd.Clear();
        _currentActiveKeys.Clear();

        // ★ 1. 把現在這一幀所有的 Ability 鑰匙，複製一份到快照名單裡
        _currentActiveKeys.AddRange(_activeAbilities.Keys);

        // ★ 2. 對著「快照名單」跑迴圈，這樣就算 _activeAbilities 被修改也不會報錯了！
        foreach (var ability in _currentActiveKeys)
        {
            // 防呆：如果這個技能在 Tick 之前，就已經被其他因素移除了，就跳過
            if (!_activeAbilities.TryGetValue(ability, out float endTime)) continue;

            // 讓技能 Tick (裡面如果呼叫 StopAbility 修改了 _activeAbilities，也是安全的)
            ability.OnAbilityTick(this);

            // ★ 再次檢查：如果 Tick 完之後，它沒有呼叫 StopAbility (還在字典裡)
            if (_activeAbilities.TryGetValue(ability, out endTime))
            {
                // 檢查是否超時
                if (endTime > 0f && Time.time >= endTime)
                {
                    _abilitiesToEnd.Add(ability);
                }
            }
        }

        // 3. 處理因為時間到而需要自動結束的技能
        foreach (var ability in _abilitiesToEnd)
        {
            StopAbility(ability);
        }
    }

    public void TryCastAbility(AbilitySO ability)
    {
        if (ability == null) return;
        if (_activeAbilities.ContainsKey(ability)) return;
        if (IsOnCooldown(ability)) return;

        if (ability.CanActivate(this))
        {
            ability.Activate(this);

            if (ability.cooldownTime > 0f)
            {
                _nextAvailableTimes[ability] = Time.time + ability.cooldownTime;
            }

            // ★ 修改：duration > 0 記錄結束時間；duration < 0 記錄為 -1f (無限期執行中)
            if (ability.duration > 0f)
            {
                _activeAbilities[ability] = Time.time + ability.duration;
            }
            else if (ability.duration < 0f)
            {
                _activeAbilities[ability] = -1f; 
            }
        }
    }

    // ★ 新增：強制結束某個技能 (可以由 Update 時間到自動呼叫，也可以由動畫事件手動呼叫)
    public void StopAbility(AbilitySO ability)
    {
        if (ability == null) return;

        // 確保它真的在執行中
        if (_activeAbilities.ContainsKey(ability))
        {
            ability.EndAbility(this);            // 呼叫 SO 裡面的清理邏輯 (如拔掉 Tag)
            _activeAbilities.Remove(ability);    // 從執行中名單剔除
        }
    }

    public bool IsOnCooldown(AbilitySO ability)
    {
        if (_nextAvailableTimes.TryGetValue(ability, out float nextTime))
        {
            return Time.time < nextTime;
        }
        return false;
    }

    public float GetRemainingCooldown(AbilitySO ability)
    {
        if (_nextAvailableTimes.TryGetValue(ability, out float nextTime))
        {
            float remaining = nextTime - Time.time;
            return remaining > 0f ? remaining : 0f;
        }
        return 0f;
    }
}