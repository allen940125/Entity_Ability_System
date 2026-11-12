using System;
using UnityEngine;

namespace GameFramework.Actors
{
    [Serializable]
    public class PlayerSwimmingData
    {
        // ==========================================
        // 水中移動設定 (Movement)
        // ==========================================

        [Header("水中移動設定")]

        [Tooltip("水中一般移動速度倍率，乘上角色目前的 MoveSpeed")]
        [SerializeField]
        [Range(0f, 1f)]
        private float _speedModifier = 0.5f;

        public float SpeedModifier => _speedModifier;

        [Tooltip("水中快游速度倍率")]
        [Range(0f, 2f)]
        public float SprintSpeedModifier = 0.8f;


        // ==========================================
        // 水中慣性設定 (Acceleration)
        // ==========================================

        [Header("水中慣性設定")]

        [Tooltip("水中起步加速度，數值越大越快達到目標速度")]
        [Range(0f, 20f)]
        public float Acceleration = 3f;

        [Tooltip("水中停止移動時的減速力度，數值越大煞車越快")]
        [Range(0f, 50f)]
        public float Deceleration = 20f;


        // ==========================================
        // 水中物理設定 (Physics)
        // ==========================================

        [Header("水中物理設定")]

        [Tooltip("水中最大下沉速度")]
        [SerializeField]
        [Range(0f, 5f)]
        private float _maxSinkSpeed = 2f;

        public float MaxSinkSpeed => _maxSinkSpeed;

        [Tooltip("角色在水中不操作時的預設浮力，正值向上，負值向下")]
        [SerializeField]
        private float _buoyancyForce = 1f;

        public float BuoyancyForce => _buoyancyForce;
        
        // ==========================================
        // ★ 動畫名稱設定 (Animation Mapping)
        // ==========================================
        
        [Header("動畫設定")]

        [Tooltip("水面游泳 Blend Tree 名稱 (仰著頭踩水、水面游)")]
        public string SurfaceSwimAnim = "Swim_Surface";
        
        [Tooltip("水下潛泳 Blend Tree 名稱 (潛入水面下、水中游)")]
        public string UnderwaterSwimAnim = "Swim_Underwater";
    }
}