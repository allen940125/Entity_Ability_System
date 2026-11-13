using UnityEngine;

namespace GameFramework.Actors
{
    public class PlayerContext
    {
        [Header("輸入情報")]
        public Vector3 MoveDirection;
        public float LastFallSpeed;      // 落地前一幀的 Y 軸速度
        public float LastHorizontalSpeed; // 煞車前一幀的 XZ 軸速度
        
        public bool WantsToJump;

        // 攻擊輸入緩衝機制 (保留你的優良設計)
        private float _lastAttackInputTime = -999f;
        private float _inputBufferWindow = 0.5f;

        public void SignalAttack() => _lastAttackInputTime = Time.time;
        public void ConsumeAttackInput() => _lastAttackInputTime = -999f;
        public bool HasAttackBuffer() => Time.time - _lastAttackInputTime <= _inputBufferWindow;

        // ==========================================
        // ★ 補上這個統一清除所有輸入狀態的方法
        // ==========================================
        public void ClearBuffers()
        {
            MoveDirection = Vector2.zero;
            WantsToJump = false;
            ConsumeAttackInput(); 
        }
        
        [Header("轉向與視角")]
        public RotationMode CurrentRotationMode = RotationMode.OrientToMovement;
        public Vector3 MouseWorldPosition; 
    }

    public enum RotationMode
    {
        OrientToMovement,
        OrientToCursor
    }
}