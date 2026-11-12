using System;

[Flags]
public enum EntityStateFlags
{
    None = 0,
    
    // --- 物理與環境狀態 (Environment - 由狀態機管理) ---
    Grounded = 1 << 0,    // 在陸地上
    Airborne = 1 << 1,    // 在空中
    Gliding = 1 << 2,     // 滑翔中
    Swimming = 1 << 3,    // 在水中 (預留)
    
    // --- 基礎位移狀態 (Locomotion - 給動畫機讀取) ---
    Moving = 1 << 4,      
    Walking = 1 << 17,
    Sprinting = 1 << 5,   
    
    // --- 動作技能狀態 (Action - 由 Ability 給予) ---
    Jumping = 1 << 18,
    Dashing = 1 << 6,     
    Sliding = 1 << 7,     
    Rolling = 1 << 8,     
    Attacking = 1 << 9,   
    Blocking = 1 << 10,    
    
    // --- 權限與異常狀態 (Status/Debuff) ---
    Invincible = 1 << 11, 
    Stunned = 1 << 12,    
    Rooted = 1 << 13,     
    Silenced = 1 << 14,   
    KnockedBack = 1 << 15,
    Dead = 1 << 16        
}