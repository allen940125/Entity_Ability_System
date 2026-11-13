using System;
using UnityEngine;

namespace GameFramework.Actors
{
    [Serializable]
    public class DefaultColliderData
    {
        [Tooltip("角色完整身高 (頭頂到腳底)")]
        [field: SerializeField] public float Height { get; private set; } = 1.8f;
        [Tooltip("預設中心點高度 (通常是身高的一半，確保腳底在 0)")]
        [field: SerializeField] public float CenterY { get; private set; } = 0.9f;
        [field: SerializeField] public float Radius { get; private set; } = 0.2f;
    }

    [Serializable]
    public class SlopeData
    {
        [Tooltip("小腿懸浮區佔總身高的比例 (0.25 代表底部 25% 懸空不碰撞)")]
        [field: SerializeField, Range(0f, 0.8f)] public float StepHeightPercentage { get; private set; } = 0.25f;
        [Tooltip("從膠囊中心往下打射線的最大偵測距離")]
        [field: SerializeField, Range(0f, 5f)] public float FloatRayDistance { get; private set; } = 2f;
        [Tooltip("懸浮校正的速度倍率 (數值越大，上下樓梯吸附越快)")]
        [field: SerializeField, Range(0f, 50f)] public float StepReachForce { get; private set; } = 25f;
        [Tooltip("最大可爬坡角度")]
        [field: SerializeField, Range(0f, 89f)] public float MaxSlopeAngle { get; private set; } = 45f;
    }

    public class CapsuleColliderData
    {
        public CapsuleCollider Collider { get; private set; }
        public Vector3 ColliderCenterInLocalSpace { get; private set; }
        public Vector3 ColliderVerticalExtents { get; private set; }

        public void Initialize(CapsuleCollider targetCollider)
        {
            if (targetCollider == null) return;
            Collider = targetCollider;
            UpdateColliderData();
        }

        public void UpdateColliderData()
        {
            if (Collider == null) return;
            ColliderCenterInLocalSpace = Collider.center;
            // ★ 修正原版 Bug：不要用 bounds.extents.y (在 OnValidate 或角色旋轉時會失準)，直接用 height 算最準
            ColliderVerticalExtents = new Vector3(0f, Collider.height * 0.5f, 0f);
        }
    }

    /// <summary>
    /// 專門負責管理與動態縮放 CapsuleCollider 的獨立組件
    /// </summary>
    public class ResizableCapsuleCollider : MonoBehaviour
    {
        [Header("綁定碰撞體 (若留空會自動往子物件找)")]
        [SerializeField] private CapsuleCollider targetCollider;

        public CapsuleColliderData CapsuleColliderData { get; private set; }
        [field: SerializeField] public DefaultColliderData DefaultColliderData { get; private set; } = new DefaultColliderData();
        [field: SerializeField] public SlopeData SlopeData { get; private set; } = new SlopeData();

        private void Awake()
        {
            Resize();
        }

        private void OnValidate()
        {
            Resize();
        }

        public void Resize(float heightMultiplier = 1f, bool enableHoverCut = true)
        {
            Initialize();
            if (CapsuleColliderData.Collider == null) return;

            CalculateCapsuleColliderDimensions(heightMultiplier, enableHoverCut);
        }

        public void Initialize()
        {
            if (targetCollider == null)
            {
                targetCollider = GetComponentInChildren<CapsuleCollider>();
            }

            if (CapsuleColliderData == null)
            {
                CapsuleColliderData = new CapsuleColliderData();
            }

            CapsuleColliderData.Initialize(targetCollider);
        }

        public void CalculateCapsuleColliderDimensions(float heightMultiplier = 1f, bool enableHoverCut = true)
        {
            SetCapsuleColliderRadius(DefaultColliderData.Radius);

            float targetHeight = DefaultColliderData.Height * heightMultiplier;
            
            // ★ 如果 enableHoverCut 是 false (例如在水底)，就不削掉 25% 的小腿，讓 StepPercentage = 0
            float currentStepPercentage = enableHoverCut ? SlopeData.StepHeightPercentage : 0f;
            SetCapsuleColliderHeight(targetHeight * (1f - currentStepPercentage));

            RecalculateCapsuleColliderCenter(targetHeight);
            RecalculateColliderRadius();

            CapsuleColliderData.UpdateColliderData();
        }

        public void SetCapsuleColliderRadius(float radius) => CapsuleColliderData.Collider.radius = radius;
        public void SetCapsuleColliderHeight(float height) => CapsuleColliderData.Collider.height = height;

        public void RecalculateCapsuleColliderCenter(float currentFullHeight)
        {
            // 計算「預設完整高度」與「當前削底後高度」的落差
            float colliderHeightDifference = DefaultColliderData.Height - CapsuleColliderData.Collider.height;

            // 如果有蹲下 (currentFullHeight < DefaultHeight)，還要把頂部降下來的高度扣掉，否則蹲下會浮在半空
            float crouchOffset = DefaultColliderData.Height - currentFullHeight;

            float newCenterY = DefaultColliderData.CenterY + (colliderHeightDifference * 0.5f) - (crouchOffset * 0.5f);
            CapsuleColliderData.Collider.center = new Vector3(0f, newCenterY, 0f);
        }

        public void RecalculateColliderRadius()
        {
            float halfColliderHeight = CapsuleColliderData.Collider.height * 0.5f;
            if (halfColliderHeight < CapsuleColliderData.Collider.radius)
            {
                SetCapsuleColliderRadius(halfColliderHeight);
            }
        }
    }
}