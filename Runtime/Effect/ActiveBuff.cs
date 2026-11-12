using System;
using System.Collections.Generic;

[Serializable]
public class ActiveBuff
{
    public BuffBlueprintSO Blueprint;
    public float RemainingTime;
    public float TickTimer;
    public EntityCore CasterCore; 
    
    public int CurrentStacks = 1; // ★ 當前層數

    // ★ 改為 List，因為一個 Buff 可能上了好幾個屬性修飾
    public List<AttributeModifier> appliedModifiers = new List<AttributeModifier>(); 

    public ActiveBuff(BuffBlueprintSO blueprint, EntityCore caster)
    {
        Blueprint = blueprint;
        CasterCore = caster;
        RemainingTime = blueprint.duration;
        TickTimer = 0f;
        CurrentStacks = 1;
    }
}