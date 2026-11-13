using UnityEngine;

namespace GameFramework.Actors
{
    public class PlayerSwimmingState : PlayerStateBase
    {
        private bool _wasUnderwaterLastFrame; 

        public PlayerSwimmingState(PlayerStateMachine sm) : base(sm) { }

        public override void OnEnter()
        {
            base.OnEnter();
            Core.RuntimeData.AddState(EntityStateFlags.Swimming);
            
            // ★ 入水防護：關閉重力、關閉地面吸附射線、縮小碰撞體
            Motor.SetGravity(false);
            Motor.EnableHover = false; 
            Motor.ColliderUtility.Resize(1f, false);
            
            // 消除掉落進水裡時殘留的巨大下墜慣性
            Motor.SetVerticalVelocity(0f); 

            var swimData = StateMachine.movementData.SwimmingData;
            _wasUnderwaterLastFrame = Motor.IsUnderwater; 
            
            if (_wasUnderwaterLastFrame)
                StateMachine.AnimController.CrossFade(swimData.UnderwaterSwimAnim, 0.2f);
            else
                StateMachine.AnimController.CrossFade(swimData.SurfaceSwimAnim, 0.2f);
        }

        public override void OnExit()
        {
            base.OnExit();
            Core.RuntimeData.RemoveState(EntityStateFlags.Swimming);
            Core.RuntimeData.RemoveState(EntityStateFlags.Moving);
            Core.RuntimeData.RemoveState(EntityStateFlags.Sprinting);
            
            // ★ 洗掉水下頻繁按 Ctrl 造成的 Toggle 汙染，確保上岸時不會莫名其妙慢走
            StateMachine.Input.ResetCrouchToggle();
            
            // ★ 上岸防護：打開重力、恢復地面吸附、恢復碰撞體
            Motor.SetGravity(true);
            Motor.EnableHover = true;
            Motor.ColliderUtility.Resize(1f, true);
        }

        public override void OnLogic()
        {
            base.OnLogic();

            var swimData = StateMachine.movementData.SwimmingData;
            bool isUnderwaterNow = Motor.IsUnderwater;

            // 動態動畫切換：跨越水面時的過渡
            if (isUnderwaterNow != _wasUnderwaterLastFrame)
            {
                if (isUnderwaterNow)
                    StateMachine.AnimController.CrossFade(swimData.UnderwaterSwimAnim, 0.2f);
                else
                    StateMachine.AnimController.CrossFade(swimData.SurfaceSwimAnim, 0.2f);
                
                _wasUnderwaterLastFrame = isUnderwaterNow;
            }

            // 常規移動標籤 (判斷是否有水平移動)
            if (!IsTryingToMove || !CanMove)
            {
                Core.RuntimeData.RemoveState(EntityStateFlags.Moving);
                Core.RuntimeData.RemoveState(EntityStateFlags.Sprinting);
                return;
            }

            Core.RuntimeData.AddState(EntityStateFlags.Moving);

            if (StateMachine.Input.SprintHeld)
                Core.RuntimeData.AddState(EntityStateFlags.Sprinting);
            else
                Core.RuntimeData.RemoveState(EntityStateFlags.Sprinting);
        }

        public override void PhysicsUpdate()
        {
            if (Core.RuntimeData.HasAnyState(EntityStateFlags.Dashing | EntityStateFlags.KnockedBack)) return;

            var swimData = StateMachine.movementData.SwimmingData;
            float baseSpeed = Core.RuntimeData.MoveSpeed.Value;
            float targetSpeed = baseSpeed * (Core.RuntimeData.HasAnyState(EntityStateFlags.Sprinting) ? swimData.SprintSpeedModifier : swimData.SpeedModifier);

            // ==========================================
            // 1. 水平移動處理
            // ==========================================
            Motor.Move(
                Context.MoveDirection, 
                targetSpeed, 
                swimData.Acceleration, 
                swimData.Deceleration, 
                true, 
                1.0f 
            );

            // ==========================================
            // 2. 垂直移動處理 (Space上浮, Ctrl下潛, 懸停)
            // ==========================================
            
            // ★ 讀取玩家的主動意圖
            bool isTryingToAscend = CanMove && StateMachine.Input.JumpHeld;
            // 暫時用 IsWalkToggled 頂替下潛意圖 (之後可以換回 CrouchHeld)
            bool isTryingToSink = CanMove && StateMachine.Input.CrouchHeld; 

            float currentY = Motor.CurrentVelocity.y;
            float targetVerticalSpeed = 0f;

            if (isTryingToAscend)
            {
                // 主動上浮
                targetVerticalSpeed = targetSpeed * 0.8f; 
                
                // 防呆：如果已經浮出水面 (頭出水了)，就不允許繼續用游的往上飛
                if (!Motor.IsUnderwater) targetVerticalSpeed = 0f;
            }
            else if (isTryingToSink)
            {
                // 主動下潛
                targetVerticalSpeed = -targetSpeed * 0.8f; 
            }
            else
            {
                // 玩家沒按鍵：自然浮沈系統 (懸停)
                if (Motor.IsUnderwater)
                {
                    // 在水下：吃設定檔的浮力 (你設定為 0，角色就會懸停不動)
                    targetVerticalSpeed = swimData.BuoyancyForce; 
                }
                else
                {
                    // 在水面：鎖死高度，完美拖住角色
                    targetVerticalSpeed = 0f; 
                    if (!Motor.IsInWater) targetVerticalSpeed = -1f; // 避免飛天
                }
            }

            // 平滑過渡垂直速度 (水阻力)
            float newY = Mathf.Lerp(currentY, targetVerticalSpeed, Time.fixedDeltaTime * swimData.Acceleration);
            
            if (newY < -swimData.MaxSinkSpeed) newY = -swimData.MaxSinkSpeed;

            Motor.SetVerticalVelocity(newY);
        }
    }
}