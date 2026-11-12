using UnityEngine;

namespace GameFramework.Actors
{
    public class PlayerDeadState : PlayerStateBase
    {
        public PlayerDeadState(PlayerStateMachine sm) : base(sm) { }

        public override void OnEnter()
        {
            base.OnEnter();

            // 1. 播放死亡動畫
            if (StateMachine.Runner != null && StateMachine.Runner.Anim != null)
            {
                StateMachine.Runner.Anim.SetTrigger("Die");
            }

            // 2. 徹底關閉碰撞與物理 (避免屍體擋路或被子彈鞭屍)
            // 根據你的 Motor 實作，可能是關閉 Collider 或修改 Layer
            if (StateMachine.Motor != null && StateMachine.Motor.TryGetComponent(out Collider col))
            {
                col.enabled = false;
            }
            
            if (StateMachine.Motor != null && StateMachine.Motor.TryGetComponent(out Rigidbody rb))
            {
                rb.isKinematic = true; 
                rb.linearVelocity = Vector3.zero;
            }

            // 3. 清空一切行為與 Buff (確保不會因為持續回血 Buff 突然復活)
            StateMachine.Core.RuntimeData.ClearAllBuffs();
            StateMachine.Context.ClearBuffers();
            
            Debug.Log("<color=red>[State]</color> 玩家進入死亡狀態，停止一切交互。");
        }

        public override void OnLogic()
        {
            // 死亡後不執行任何邏輯更新
        }

        public override void PhysicsUpdate()
        {
            // 死亡後不執行任何物理更新
        }

        public override void OnExit()
        {
            // 除非你有「復活」機制，否則正常情況下永遠不會執行到這裡
            base.OnExit();
        }
    }
}