using System;
using UnityEngine;

namespace GameFramework.Actors
{
    [Serializable]
    public class PlayerGroundedData
    {
        // ==========================================
        // 基礎移動設定 (Movement)
        // ==========================================

        [Header("基礎移動設定")]

        [Tooltip("角色的底層基礎移動速度")]
        [Range(0f, 25f)]
        public float BaseMoveSpeed = 5f;

        [Tooltip("靜步移動速度倍率")]
        [Range(0f, 1f)]
        public float WalkSpeedMultiplier = 0.4f;

        [Tooltip("奔跑移動速度倍率")]
        [Range(1f, 3f)]
        public float SprintSpeedMultiplier = 1.5f;


        // ==========================================
        // 移動慣性設定 (Acceleration)
        // ==========================================

        [Header("移動慣性設定")]

        [Tooltip("從靜止加速至目標速度的快慢，數值越大加速越快")]
        [Range(0.1f, 50f)]
        public float Acceleration = 10f;

        [Tooltip("放開方向鍵後的減速力度，數值越大急停越快")]
        [Range(0.1f, 50f)]
        public float Deceleration = 15f;


        // ==========================================
        // 斜坡設定 (Slope)
        // ==========================================

        [Header("斜坡設定")]

        [Tooltip("根據斜坡角度調整移動速度的曲線。X軸=角度，Y軸=速度倍率")]
        public AnimationCurve SlopeSpeedAngles = new AnimationCurve(
            new Keyframe(0f, 1f),      // 0度 (平地)：維持 1 倍速
            new Keyframe(30f, 0.9f),   // 30度 (緩坡)：降為 0.9 倍速
            new Keyframe(45f, 0.7f),   // 45度 (陡坡)：降為 0.7 倍速
            new Keyframe(60f, 0.3f)    // 60度 (極限坡)：降為 0.3 倍速
        );


        // ==========================================
        // 角色轉向設定 (Rotation)
        // ==========================================

        [Header("角色轉向設定")]

        public PlayerRotationData RotationData;
        
        // ==========================================
        // 落地與急停設定 (Events & Animation)
        // ==========================================
        [Header("落地判定與動畫")]
        public float HeavyLandSpeedThreshold = -22f;
        public string HeavyLandAnim = "Land_Heavy";
        [Tooltip("重落地的定身硬直時間 (秒)")]
        public float HeavyLandLockDuration = 0.6f;
        
        public float MediumLandSpeedThreshold = -12f;
        public string MediumLandAnim = "Land_Medium";
        
        public float LightLandSpeedThreshold = -2f;
        public string LightLandAnim = "Land_Light";
        
        public string HeavyRollAnim = "Roll_Forward"; // 重落地翻滾動畫
        public float HeavyRollLockDuration = 0.8f;    // 翻滾的硬直時間 (由 Root Motion 驅動)

        [Header("急停判定與動畫 (走路直接由 BlendTree 回 Idle)")]
        [Tooltip("衝刺急停門檻 (通常對應 Sprint 速度)")]
        public float HeavyStopSpeedThreshold = 7.5f;
        public string HeavyStopAnim = "Stop_Heavy";
        [Tooltip("衝刺急停的滑行硬直時間 (秒)")]
        public float HeavyStopLockDuration = 0.35f;
        [Tooltip("衝刺急停的摩擦力")]
        public float HeavyStopDeceleration = 15f;
        
        [Tooltip("常規跑步急停門檻 (通常對應一般 Run 速度)")]
        public float MediumStopSpeedThreshold = 4.0f;
        public string MediumStopAnim = "Stop_Medium";
        
        [Header("一般移動")]
        public string LocomotionAnim = "Locomotion";
    }
}