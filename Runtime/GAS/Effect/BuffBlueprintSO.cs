using System;
using System.Collections.Generic;
using UnityEngine;

public enum TickScaleMode
{
    Flat,               // 固定數值 (早期遊戲做法)
    CasterAttack,       // 依賴施法者的攻擊力乘區 (常見於燃燒、流血)
    TargetMaxHealth     // 依賴目標的最大生命值乘區 (常見於百分比毒藥、百分比回血)
}

public enum BuffStackBehavior
{
    Independent, // 獨立共存：中兩次毒，身上就有兩個毒各自跳血
    Refresh,     // 刷新秒數：中兩次毒，身上還是一個毒，但持續時間補滿
    AddStack     // 疊加層數：刷新秒數，且層數 +1（傷害或效果倍增）
}

// ★ 定義單一屬性修飾包裝
[Serializable]
public struct BuffAttributeMod
{
    public AttributeType targetAttribute;
    public float modifierValue;
    public ModifierType modifierType;
}

[CreateAssetMenu(fileName = "NewBuff", menuName = "Combat/Buff Blueprint")]
public class BuffBlueprintSO : ScriptableObject
{
    public string buffName;
    public float duration = 5f;
    public bool isDebuff = false;

    [Header("★ 疊加規則")]
    public BuffStackBehavior stackBehavior = BuffStackBehavior.Refresh;
    public int maxStacks = 1; // 如果是 AddStack 模式，最高疊幾層？

    [Header("★ 屬性修飾 (支援多種屬性同時加成)")]
    public bool modifyAttribute = false;
    public List<BuffAttributeMod> attributeMods = new List<BuffAttributeMod>();

    [Header("★ 狀態標籤")]
    public EntityStateFlags stateTagToApply = EntityStateFlags.None;

    [Header("★ 週期性影響 (動態結算)")]
    public bool isPeriodic = false;
    public float tickInterval = 1f; 
    
    [Tooltip("決定 Tick 數值的計算基準")]
    public TickScaleMode tickScaleMode = TickScaleMode.Flat;
    
    [Tooltip("倍率。若為 Flat 就是絕對值；若是 CasterAttack，0.5 代表施法者 50% 攻擊力")]
    public float tickMultiplier = 5f; 
    
    [Tooltip("打勾代表扣血，取消打勾代表回血")]
    public bool isDamage = true;
}