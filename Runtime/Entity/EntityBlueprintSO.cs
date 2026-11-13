using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "NewEntityData", menuName = "Game/Runtime Data/Entity Blueprint")]
public class EntityBlueprintSO : ScriptableObject
{
    [Header("戰鬥生成設定")]
    [Tooltip("進入戰鬥場景時，要生成的實際 Boss Prefab")]
    public GameObject battlePrefab;
    
    [Header("★ 絕對核心數據 (所有生物共用)")]
    public int maxHealth = 100;
    public int attackPower = 20;
    public int defense = 3;
    [Range(0f, 1f)] public float critRate = 0.2f;
    public float critMultiplier = 1.5f;

    [Header("★ 基礎移動數據")]
    public float baseMoveSpeed = 5f;

    [Header("受击行为")]
    public float damageInvincibilityDuration = 0.5f;
    
    [Header("★ 動態特徵插槽 (由企劃自由組合)")]
    [SerializeReference]
    public List<EntityTrait> traits = new List<EntityTrait>();
}

public enum AttributeType
{
    None,
    MaxHealth,
    AttackPower,
    Defense,
    CritRate,
    CritMultiplier,
    MoveSpeed
}

[Serializable]
public class EntityRuntime
{
    [Header("資料來源 (唯讀藍圖)")]
    public EntityBlueprintSO Blueprint;

    // ==========================================
    // 動態屬性容器
    // ==========================================
    public EntityAttribute MaxHealth;
    public EntityAttribute AttackPower;
    public EntityAttribute Defense;
    public EntityAttribute CritRate;
    public EntityAttribute CritMultiplier;
    public EntityAttribute MoveSpeed;
    public EntityAttribute DamageInvincibilityDuration;
    
    [Header("動態狀態 (執行期可見)")]
    [SerializeField] private float _currentHealth; 
    public float CurrentHealth => _currentHealth;

    [SerializeReference] private List<EntityTrait> _runtimeTraits = new List<EntityTrait>();
    public event Action<float> OnHealthRatioChanged;

    // ==========================================
    // 標籤系統 (整合了計數器，防止狀態衝突)
    // ==========================================
    public EntityStateFlags CurrentState = EntityStateFlags.None;
    
    private class TemporaryState
    {
        public EntityStateFlags Tag;
        public float RemainingTime;
    }
    
    [NonSerialized] private List<TemporaryState> _tempStates = new List<TemporaryState>();
    private Dictionary<EntityStateFlags, int> _stateCounts = new Dictionary<EntityStateFlags, int>();
    
    // 只要擁有傳入標籤中的「任何一個」就回傳 true (用於 BlockedTags 攔截)
    public bool HasAnyState(EntityStateFlags state) => (CurrentState & state) != 0;
    // 必須擁有傳入標籤的「全部」才回傳 true (用於 RequiredTags 條件檢查)
    public bool HasAllStates(EntityStateFlags state) => (CurrentState & state) == state;
    public void AddState(EntityStateFlags state) => CurrentState |= state;
    public void RemoveState(EntityStateFlags state) => CurrentState &= ~state;

    // 快取所有的列舉值，避免 Enum.GetValues 產生記憶體垃圾
    private static readonly EntityStateFlags[] AllFlags = (EntityStateFlags[])Enum.GetValues(typeof(EntityStateFlags));
    
    // ★ 核心修復：統一的狀態計數器 (Buff 和 TempState 共用)
    public void AddStateWithCount(EntityStateFlags compositeTags)
    {
        if (compositeTags == EntityStateFlags.None) return;

        // 走訪所有可能的標籤
        for (int i = 0; i < AllFlags.Length; i++)
        {
            EntityStateFlags singleFlag = AllFlags[i];
            if (singleFlag == EntityStateFlags.None) continue;

            // 如果傳入的複合標籤中，包含這個單一標籤，就對「該單一標籤」進行計數
            if ((compositeTags & singleFlag) == singleFlag)
            {
                AddState(singleFlag); // 給予標籤
                _stateCounts[singleFlag] = _stateCounts.TryGetValue(singleFlag, out var count) ? count + 1 : 1;
            }
        }
    }

    public void RemoveStateWithCount(EntityStateFlags compositeTags)
    {
        if (compositeTags == EntityStateFlags.None) return;

        for (int i = 0; i < AllFlags.Length; i++)
        {
            EntityStateFlags singleFlag = AllFlags[i];
            if (singleFlag == EntityStateFlags.None) continue;

            if ((compositeTags & singleFlag) == singleFlag)
            {
                if (_stateCounts.TryGetValue(singleFlag, out var count))
                {
                    if (count <= 1)
                    {
                        _stateCounts.Remove(singleFlag);
                        RemoveState(singleFlag); // 真正拔除
                    }
                    else
                    {
                        _stateCounts[singleFlag] = count - 1;
                    }
                }
            }
        }
    }

    public void AddTemporaryState(EntityStateFlags tag, float duration)
    {
        AddStateWithCount(tag);
        _tempStates.Add(new TemporaryState { Tag = tag, RemainingTime = duration });
    }

    public void TickTemporaryStates(float deltaTime)
    {
        for (int i = _tempStates.Count - 1; i >= 0; i--)
        {
            var s = _tempStates[i];
            s.RemainingTime -= deltaTime;
            if (s.RemainingTime <= 0f)
            {
                _tempStates.RemoveAt(i);
                RemoveStateWithCount(s.Tag); // ★ 改用計數器移除
            }
        }
    }

    // ==========================================
    // Buff 系統
    // ==========================================
    [ShowInInspector, ReadOnly, TableList(IsReadOnly = true, AlwaysExpanded = true)]
    private List<ActiveBuff> _activeBuffs = new List<ActiveBuff>();

    public void TickBuffs(float deltaTime)
    {
        for (int i = _activeBuffs.Count - 1; i >= 0; i--)
        {
            var buff = _activeBuffs[i];
            
            if (buff.Blueprint.isPeriodic)
            {
                buff.TickTimer += deltaTime;
                if (buff.TickTimer >= buff.Blueprint.tickInterval)
                {
                    buff.TickTimer -= buff.Blueprint.tickInterval;
                    
                    // ★ 高端做法：每次跳 Tick 時，動態計算數值
                    float tickValue = CalculateTickValue(buff);
                    
                    // 根據是傷害還是治療，決定正負號
                    float finalDelta = buff.Blueprint.isDamage ? -tickValue : tickValue;
                    
                    ModifyHealth(finalDelta); 
                }
            }

            if (buff.Blueprint.duration > 0) 
            {
                buff.RemainingTime -= deltaTime;
                if (buff.RemainingTime <= 0f)
                {
                    RemoveBuff(buff);
                }
            }
        }
    }
    
    // ==========================================
    // EntityRuntime 內的 Buff 系統
    // ==========================================

    public void ApplyBuff(BuffBlueprintSO buffSO, EntityCore casterCore)
    {
        // 1. 檢查疊加規則 (尋找身上是否已經有同一個 SO 的 Buff)
        ActiveBuff existingBuff = _activeBuffs.Find(b => b.Blueprint == buffSO);

        if (existingBuff != null && buffSO.stackBehavior != BuffStackBehavior.Independent)
        {
            // 如果規則是 Refresh 或 AddStack，我們不產生新 Buff，而是操作舊的
            
            // 刷新持續時間
            existingBuff.RemainingTime = buffSO.duration; 
            
            // 更新施法者 (如果是新的敵人放的同一個毒，傷害依據轉移給新敵人)
            existingBuff.CasterCore = casterCore; 

            // 如果是疊加層數模式，增加層數
            if (buffSO.stackBehavior == BuffStackBehavior.AddStack)
            {
                if (existingBuff.CurrentStacks < buffSO.maxStacks)
                {
                    existingBuff.CurrentStacks++;
                    
                    // ★ 屬性重算邏輯 (拔掉舊的，根據新層數掛新的)
                    RefreshBuffAttributeModifiers(existingBuff);
                }
            }
            return; // 疊加處理完畢，直接結束
        }

        // 2. 如果是 Independent，或是身上沒有這個 Buff，則建立全新的
        ActiveBuff newBuff = new ActiveBuff(buffSO, casterCore);

        // 3. 多屬性修飾掛載
        if (buffSO.modifyAttribute && buffSO.attributeMods != null)
        {
            ApplyBuffAttributeModifiers(newBuff);
        }

        AddStateWithCount(buffSO.stateTagToApply);
        _activeBuffs.Add(newBuff);
    }

    private void RemoveBuff(ActiveBuff buff)
    {
        // 將該 Buff 掛在所有屬性上的 Modifier 全部拔除
        if (buff.Blueprint.modifyAttribute)
        {
            foreach (var mod in buff.Blueprint.attributeMods)
            {
                EntityAttribute targetAttr = GetAttribute(mod.targetAttribute);
                if (targetAttr != null)
                {
                    targetAttr.RemoveAllModifiersFromSource(buff);
                }
            }
        }

        RemoveStateWithCount(buff.Blueprint.stateTagToApply);
        _activeBuffs.Remove(buff);
    }

    // ==========================================
    // 輔助方法：處理多重屬性的掛載與重算
    // ==========================================

    private void ApplyBuffAttributeModifiers(ActiveBuff buff)
    {
        buff.appliedModifiers.Clear(); // 清空紀錄
        
        foreach (var modConf in buff.Blueprint.attributeMods)
        {
            EntityAttribute targetAttr = GetAttribute(modConf.targetAttribute);
            if (targetAttr != null)
            {
                // ★ 如果有層數，修飾值直接乘上層數 (例如 1層加10攻擊，2層加20攻擊)
                float finalValue = modConf.modifierValue * buff.CurrentStacks;
                
                var runtimeMod = new AttributeModifier(finalValue, modConf.modifierType, buff);
                buff.appliedModifiers.Add(runtimeMod);
                targetAttr.AddModifier(runtimeMod);
            }
        }
    }

    private void RefreshBuffAttributeModifiers(ActiveBuff existingBuff)
    {
        if (!existingBuff.Blueprint.modifyAttribute) return;

        // 1. 先把舊層數的 Modifier 全部拔掉
        foreach (var modConf in existingBuff.Blueprint.attributeMods)
        {
            EntityAttribute targetAttr = GetAttribute(modConf.targetAttribute);
            targetAttr?.RemoveAllModifiersFromSource(existingBuff);
        }

        // 2. 依據新層數，重新掛載
        ApplyBuffAttributeModifiers(existingBuff);
    }

    // ==========================================
    // 更新 Tick 時，把層數算進去
    // ==========================================
    private float CalculateTickValue(ActiveBuff buff)
    {
        float baseMultiplier = buff.Blueprint.tickMultiplier;
        float finalMultiplier = baseMultiplier * buff.CurrentStacks; // ★ 層數越高，Tick越痛

        switch (buff.Blueprint.tickScaleMode)
        {
            case TickScaleMode.CasterAttack:
                if (buff.CasterCore != null && buff.CasterCore.RuntimeData != null)
                    return buff.CasterCore.RuntimeData.AttackPower.Value * finalMultiplier;
                return 0f; 

            case TickScaleMode.TargetMaxHealth:
                return MaxHealth.Value * finalMultiplier;

            case TickScaleMode.Flat:
            default:
                return finalMultiplier;
        }
    }

    // ★ 新增：一鍵清除所有 Buff
    public void ClearAllBuffs()
    {
        // 必須從後往前迭代，因為 RemoveBuff 會操作 List
        for (int i = _activeBuffs.Count - 1; i >= 0; i--)
        {
            RemoveBuff(_activeBuffs[i]);
        }
        _activeBuffs.Clear();
    }

    private EntityAttribute GetAttribute(AttributeType type)
    {
        switch (type)
        {
            case AttributeType.MaxHealth: return MaxHealth;
            case AttributeType.AttackPower: return AttackPower;
            case AttributeType.Defense: return Defense;
            case AttributeType.CritRate: return CritRate;
            case AttributeType.CritMultiplier: return CritMultiplier;
            case AttributeType.MoveSpeed: return MoveSpeed;
            default: return null;
        }
    }

    // ==========================================
    // 初始化與結算
    // ==========================================
    public void Initialize(EntityBlueprintSO blueprint)
    {
        Blueprint = blueprint;
        
        MaxHealth = new EntityAttribute(blueprint.maxHealth);
        AttackPower = new EntityAttribute(blueprint.attackPower);
        Defense = new EntityAttribute(blueprint.defense);
        CritRate = new EntityAttribute(blueprint.critRate);
        CritMultiplier = new EntityAttribute(blueprint.critMultiplier);
        MoveSpeed = new EntityAttribute(blueprint.baseMoveSpeed);
        DamageInvincibilityDuration = new EntityAttribute(blueprint.damageInvincibilityDuration);
        
        _currentHealth = MaxHealth.Value; 

        // ★ 修復：確保每次初始化時，動態名單都是乾淨的
        _tempStates.Clear();
        _stateCounts.Clear();
        CurrentState = EntityStateFlags.None;
        
        if (_activeBuffs != null) ClearAllBuffs();
        else _activeBuffs = new List<ActiveBuff>();

        _runtimeTraits.Clear();
        foreach (var trait in blueprint.traits)
        {
            var instancedTrait = trait.Clone();
            instancedTrait.Initialize(this); 
            _runtimeTraits.Add(instancedTrait);
        }
    }

    public void ModifyHealth(float delta)
    {
        _currentHealth = Mathf.Clamp(_currentHealth + delta, 0, MaxHealth.Value);
        float ratio = MaxHealth.Value > 0 ? _currentHealth / MaxHealth.Value : 0f;
        OnHealthRatioChanged?.Invoke(ratio);
    }
    
    // ==========================================
    // 特徵查詢介面
    // ==========================================
    public IEnumerable<EntityTrait> GetAllTraits() => _runtimeTraits;
    public T GetTrait<T>() where T : EntityTrait
    {
        foreach (var trait in _runtimeTraits) if (trait is T match) return match;
        return null;
    }
    public bool TryGetTrait<T>(out T trait) where T : EntityTrait
    {
        trait = GetTrait<T>();
        return trait != null;
    }
}