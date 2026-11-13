using System;
using UnityEngine;

namespace GameFramework.Actors
{
    [Serializable]
    public class PlayerGlideData
    {
        [Tooltip("滑翔時的最大下落速度 (數值越小，滑得越平緩)")]
        [Range(0f, 10f)] public float GlideFallSpeedLimit = 2f;
        
        [Tooltip("滑翔時的水平速度倍率 (通常比一般空中移動快)")]
        [Range(0f, 3f)] public float SpeedModifier = 1.2f;
        
        [Tooltip("滑翔時的加速度 (操控靈敏度)")]
        [Range(0f, 20f)] public float Acceleration = 8f;
        
        [Tooltip("滑翔時的減速度")]
        [Range(0f, 10f)] public float Deceleration = 2f;
    }

    [Serializable]
    public class PlayerAirborneData
    {
        [Header("空中微調 (Air Control)")]
        [Range(0f, 20f)] public float Acceleration = 5f;
        [Range(0f, 10f)] public float Deceleration = 1f;
        
        [field: SerializeField] public PlayerJumpData JumpData { get; private set; }
        [field: SerializeField] public PlayerFallData FallData { get; private set; }
        
        // ★ 新增滑翔參數
        [field: SerializeField] public PlayerGlideData GlideData { get; private set; } 
    }
}