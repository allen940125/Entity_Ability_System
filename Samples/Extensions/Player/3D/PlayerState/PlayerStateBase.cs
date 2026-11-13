using UnityEngine;
using UnityHFSM;

namespace GameFramework.Actors
{
    public class PlayerStateBase : State
    {
        protected PlayerStateMachine StateMachine;
        protected EntityCore Core => StateMachine.Core;
        protected EntityMotor Motor => StateMachine.Motor;
        protected AbilityRunner Runner => StateMachine.Runner;
        protected PlayerInputReceiver Input => StateMachine.Input;
        protected PlayerContext Context => StateMachine.Context;

        // ==========================================
        // ★ 泛用判斷屬性 (所有子狀態都可以直接用，不用再寫一長串！)
        // ==========================================
        
        /// <summary> 是否擁有移動與操作權限 (沒被控場、沒死亡、沒硬直) </summary>
        protected bool CanMove => !Core.RuntimeData.HasAnyState(EntityStateFlags.Stunned | EntityStateFlags.Rooted | EntityStateFlags.Dead);
        
        /// <summary> 玩家是否有推動搖桿 (移動意圖) </summary>
        protected bool IsTryingToMove => Context.MoveDirection.sqrMagnitude > 0.01f;
        
        /// <summary> 當前的純水平速度 (忽略 Y 軸) </summary>
        protected float CurrentHorizontalSpeed => new Vector3(Motor.CurrentVelocity.x, 0f, Motor.CurrentVelocity.z).magnitude;

        /// <summary> 當前的純水平方向向量 </summary>
        protected Vector3 HorizontalVelocity => new Vector3(Motor.CurrentVelocity.x, 0f, Motor.CurrentVelocity.z);

        public PlayerStateBase(PlayerStateMachine stateMachine)
        {
            StateMachine = stateMachine;
        }
        
        public virtual void PhysicsUpdate() 
        { 
            // 預設什麼都不做，讓子類別覆寫
        }
    }
}