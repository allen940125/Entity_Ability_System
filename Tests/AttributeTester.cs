using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Sirenix.OdinInspector;

// ★ 改名為統一的 Debug Tool
public class EntityDebugTool : MonoBehaviour
{
    [Header("目標依賴")]
    public EntityCore targetCore;
    public AbilityRunner targetRunner; 

    private void Awake()
    {
        if (targetCore == null) targetCore = GetComponent<EntityCore>();
        if (targetRunner == null) targetRunner = GetComponent<AbilityRunner>();
    }

    // ==========================================
    // 區塊 1：動態屬性修改
    // ==========================================
    [FoldoutGroup("1. 屬性修改 (Attributes)")]
    [ValueDropdown("GetAttributeNames")] 
    public string selectedAttribute = "AttackPower"; 

    [FoldoutGroup("1. 屬性修改 (Attributes)")]
    public float modValue = 0.5f;
    [FoldoutGroup("1. 屬性修改 (Attributes)")]
    public ModifierType modType = ModifierType.MultiplyBase;

    [FoldoutGroup("1. 屬性修改 (Attributes)")]
    [Button("給予單一屬性 Modifier", ButtonSizes.Medium), GUIColor(0.2f, 0.8f, 0.2f)]
    public void ApplyAttributeMod()
    {
        var attr = GetSelectedAttribute();
        if (attr == null) return;
        attr.AddModifier(new AttributeModifier(modValue, modType, this, "Debug Tool"));
        Debug.Log($"[{selectedAttribute}] 修改後數值：{attr.Value}");
    }

    [FoldoutGroup("1. 屬性修改 (Attributes)")]
    [Button("移除我給的 Modifier", ButtonSizes.Medium), GUIColor(0.8f, 0.2f, 0.2f)]
    public void RemoveAttributeMod()
    {
        var attr = GetSelectedAttribute();
        if (attr == null) return;
        attr.RemoveAllModifiersFromSource(this);
        Debug.Log($"[{selectedAttribute}] 恢復為：{attr.Value}");
    }

    // ==========================================
    // 區塊 2：全新 Buff 系統測試
    // ==========================================
    [FoldoutGroup("2. 狀態與 Buff (Gameplay Effects)")]
    public BuffBlueprintSO buffToTest;

    [FoldoutGroup("2. 狀態與 Buff (Gameplay Effects)")]
    [Button("掛載 Buff", ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1f)]
    public void ApplyBuff()
    {
        if (targetCore == null || buffToTest == null) return;
        
        // ★ 呼叫 EntityRuntime 去掛載 Buff (請確保你在 Runtime 寫了 ApplyBuff 方法)
        targetCore.RuntimeData.ApplyBuff(buffToTest, targetCore);
        Debug.Log($"已給予 Buff：{buffToTest.buffName}");
    }

    [FoldoutGroup("2. 狀態與 Buff (Gameplay Effects)")]
    [Button("一鍵清除所有 Buff", ButtonSizes.Medium), GUIColor(1f, 0.4f, 0.4f)]
    public void ClearAllBuffs()
    {
        if (targetCore == null) return;
        // ★ 建議在 EntityRuntime 補上一個迴圈清除所有 Buff 的方法
        targetCore.RuntimeData.ClearAllBuffs(); 
        Debug.Log("已清空所有 Buff！");
    }

    // ==========================================
    // 區塊 3：技能測試
    // ==========================================
    [FoldoutGroup("3. 技能施放 (Abilities)")]
    public AbilitySO abilityToTest; 
    
    [FoldoutGroup("3. 技能施放 (Abilities)")]
    [Button("發動測試技能", ButtonSizes.Large), GUIColor(0, 1, 0)]
    public void CastTestAbility()
    {
        if (targetRunner == null || abilityToTest == null) return;
        targetRunner.TryCastAbility(abilityToTest);
    }

    // ==========================================
    // 內部反射工具
    // ==========================================
    private IEnumerable<string> GetAttributeNames()
    {
        List<string> names = new List<string>();
        FieldInfo[] fields = typeof(EntityRuntime).GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (var field in fields)
        {
            if (field.FieldType == typeof(EntityAttribute)) names.Add(field.Name);
        }
        return names;
    }

    private EntityAttribute GetSelectedAttribute()
    {
        if (targetCore == null || targetCore.RuntimeData == null) return null;
        FieldInfo field = typeof(EntityRuntime).GetField(selectedAttribute, BindingFlags.Public | BindingFlags.Instance);
        return field?.GetValue(targetCore.RuntimeData) as EntityAttribute;
    }
}