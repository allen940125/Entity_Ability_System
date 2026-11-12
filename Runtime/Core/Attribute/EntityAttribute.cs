using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class EntityAttribute
{
    [SerializeField, LabelText("基礎數值 (Base)")]
    private float _baseValue;
    
    [SerializeField, ReadOnly, TableList(AlwaysExpanded = true), LabelText("身上 Buff 清單")]
    private List<AttributeModifier> _modifiers = new List<AttributeModifier>();
    
    private bool _isDirty = true;
    private float _cachedValue;

    // ★ 新增：專門給 Inspector 看的快取變數 (不需要序列化存檔)
    private float _cachedAddTotal;
    private float _cachedMultiplyBase;
    private float _cachedMultiplyTotal;

    public EntityAttribute(float baseValue)
    {
        _baseValue = baseValue;
    }

    [ShowInInspector, LabelText("★ 最終結算數值"), DisplayAsString(true), PropertyOrder(-1)]
    public float Value
    {
        get
        {
            if (_isDirty)
            {
                _cachedValue = CalculateFinalValue(); // 呼叫時會更新所有 _cached 變數
                _isDirty = false;
            }
            return _cachedValue;
        }
    }

    // ==========================================
    // ★ Odin 神奇魔法：讓這些隱藏的加總數值顯示在面板上
    // ==========================================
    [ShowInInspector, LabelText("固定加總 (Add Total)"), DisplayAsString, PropertyOrder(1)]
    private float DisplayAddTotal => _cachedAddTotal;

    [ShowInInspector, LabelText("基礎乘算總和 (Multiply Base)"), DisplayAsString, PropertyOrder(2)]
    private float DisplayMultiplyBase => _cachedMultiplyBase;

    [ShowInInspector, LabelText("最終乘算總和 (Multiply Total)"), DisplayAsString, PropertyOrder(3)]
    private float DisplayMultiplyTotal => _cachedMultiplyTotal;

    public void AddModifier(AttributeModifier modifier)
    {
        _modifiers.Add(modifier);
        _isDirty = true;
    }

    public void RemoveModifier(AttributeModifier modifier)
    {
        _modifiers.Remove(modifier);
        _isDirty = true;
    }

    // 根據 Source (來源) 一次移除所有相關 Buff (例如：脫下裝備時)
    public void RemoveAllModifiersFromSource(object source)
    {
        int removedCount = _modifiers.RemoveAll(mod => mod.Source == source);
        if (removedCount > 0) _isDirty = true;
    }

    private float CalculateFinalValue()
    {
        // ★ 修正：把數值清零並存入類別層級的 _cached 變數中，而不是區域變數
        _cachedAddTotal = 0f;
        _cachedMultiplyBase = 0f;
        _cachedMultiplyTotal = 0f;

        foreach (var mod in _modifiers)
        {
            switch (mod.Type)
            {
                case ModifierType.Add: _cachedAddTotal += mod.Value; break;
                case ModifierType.MultiplyBase: _cachedMultiplyBase += mod.Value; break;
                case ModifierType.MultiplyTotal: _cachedMultiplyTotal += mod.Value; break;
            }
        }

        // ★ 計算時使用 _cached 變數
        float finalValue = (_baseValue * (1f + _cachedMultiplyBase) + _cachedAddTotal) * (1f + _cachedMultiplyTotal);
        return Mathf.Max(0f, finalValue);
    }
}