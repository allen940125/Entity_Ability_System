using System;
using System.Collections.Generic;
using UnityEngine;

public enum ModifierType
{
    Add,            // 固定加值 (例如：攻擊力 +50)
    MultiplyBase,   // 基礎乘算 (例如：裝備給的 攻擊力 +20%)
    MultiplyTotal   // 最終乘算 (例如：狂暴 Buff 最終總傷害 +50%)
}

[Serializable] // ★ 加上這個，讓 Unity Inspector 能夠顯示
public class AttributeModifier
{
    public float Value;
    public ModifierType Type;
    
    // 原本的 Source 無法被序列化顯示在 Inspector，我們多加一個字串來 Debug
    public string DebugSource; 
    public object Source { get; private set; }

    public AttributeModifier(float value, ModifierType type, object source = null, string debugName = "Unknown")
    {
        Value = value;
        Type = type;
        Source = source;
        DebugSource = debugName;
    }
}