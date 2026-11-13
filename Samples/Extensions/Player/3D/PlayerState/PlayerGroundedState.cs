using UnityEngine;

namespace GameFramework.Actors
{
    public class PlayerGroundedState : PlayerStateBase
    {
        // ★ 用明確的狀態機子階段，取代原本曖昧的布林值
        private enum GroundPhase
        {
            None,      
            Landing,   
            Stopping   
        }

        private GroundPhase _phase;
        private string _phaseAnim;        
        private float _lastSpeedForStop;
        private bool _wasRootedLastFrame;

        public PlayerGroundedState(PlayerStateMachine sm) : base(sm) { }

        public override void OnEnter()
        {
            base.OnEnter();
            Core.RuntimeData.AddState(EntityStateFlags.Grounded);

            _wasRootedLastFrame = false;
            _phase              = GroundPhase.None;
            _phaseAnim          = null;

            var groundData = StateMachine.movementData.GroundedData;
            float fallSpeed = Context.LastFallSpeed;

            Vector3 hVel = new Vector3(Motor.CurrentVelocity.x, 0f, Motor.CurrentVelocity.z);
            float currentHorizontalSpeed = hVel.magnitude;
            bool isTryingToMove = Context.MoveDirection.sqrMagnitude > 0.01f;

            _lastSpeedForStop = currentHorizontalSpeed;

            // ==========================================
            // [落地與急停 權重分配系統] (Land > Stop)
            // ==========================================
            bool isHeavyFall = fallSpeed < groundData.HeavyLandSpeedThreshold;
            bool isMediumFall = fallSpeed < groundData.MediumLandSpeedThreshold;
            bool isLightFall = fallSpeed < groundData.LightLandSpeedThreshold;

            if (isHeavyFall)
            {
                // ★ 翻滾判定：如果有一定水平速度，或是玩家有推搖桿
                bool wantsToRoll = isTryingToMove || currentHorizontalSpeed > groundData.MediumStopSpeedThreshold;
                
                // ★ 物理過濾：判斷輸入方向是否與慣性完全反向 (大於 135 度)
                float angleDiff = isTryingToMove && currentHorizontalSpeed > 1f ? Vector3.Angle(hVel, Context.MoveDirection) : 0f;
                bool isReversing = angleDiff > 135f;

                if (wantsToRoll && !isReversing)
                {
                    // 1. 觸發受身翻滾
                    Core.RuntimeData.AddTemporaryState(EntityStateFlags.Rooted, groundData.HeavyRollLockDuration);
                    Motor.EnableRootMotion = true;
                    
                    // 確保角色面向正確的翻滾方向 (推搖桿就順著搖桿，沒推搖桿就順著慣性)
                    Vector3 rollDir = isTryingToMove ? Context.MoveDirection : hVel.normalized;
                    if (rollDir.sqrMagnitude > 0.01f)
                    {
                        Motor.transform.forward = rollDir; 
                    }

                    StartPhase(GroundPhase.Landing, groundData.HeavyRollAnim, 0.1f);
                }
                else
                {
                    // 2. 原地砸地 (如果沒有動能，或是玩家試圖在往前飛時往後翻滾，強制沒收翻滾)
                    Core.RuntimeData.AddTemporaryState(EntityStateFlags.Rooted, groundData.HeavyLandLockDuration);
                    StartPhase(GroundPhase.Landing, groundData.HeavyLandAnim, 0.1f);
                }
            }
            else if (isMediumFall)
            {
                Core.RuntimeData.AddTemporaryState(EntityStateFlags.Rooted, 0.12f);
                StartPhase(GroundPhase.Landing, groundData.MediumLandAnim, 0.1f);
            }
            // 空中放開搖桿的著陸急停
            else if (!isTryingToMove && currentHorizontalSpeed > groundData.MediumStopSpeedThreshold)
            {
                if (currentHorizontalSpeed > groundData.HeavyStopSpeedThreshold)
                {
                    Core.RuntimeData.AddTemporaryState(EntityStateFlags.Rooted, groundData.HeavyStopLockDuration);
                    Motor.EnableRootMotion = true;
                    StartPhase(GroundPhase.Stopping, groundData.HeavyStopAnim, 0.1f);
                }
                else
                {
                    StartPhase(GroundPhase.Stopping, groundData.MediumStopAnim, 0.1f);
                }
                _lastSpeedForStop = 0f; 
            }
            // 空中推著搖桿的無縫跑酷
            else if (isTryingToMove && currentHorizontalSpeed > 0.5f)
            {
                StartPhase(GroundPhase.None, groundData.LocomotionAnim, 0.15f);
            }
            else if (isLightFall)
            {
                Core.RuntimeData.AddTemporaryState(EntityStateFlags.Rooted, 0.06f);
                StartPhase(GroundPhase.Landing, groundData.LightLandAnim, 0.1f);
            }
            else
            {
                StartPhase(GroundPhase.None, groundData.LocomotionAnim, 0.1f);
            }
        }

        public override void OnExit()
        {
            base.OnExit();
            Core.RuntimeData.RemoveState(EntityStateFlags.Grounded);
            Core.RuntimeData.RemoveState(EntityStateFlags.Moving);
            Core.RuntimeData.RemoveState(EntityStateFlags.Sprinting);
            Core.RuntimeData.RemoveState(EntityStateFlags.Walking);

            _phase     = GroundPhase.None;
            _phaseAnim = null;
            Motor.EnableRootMotion = false;
        }

        private void StartPhase(GroundPhase phase, string animName, float fade)
        {
            _phase     = phase;
            _phaseAnim = animName;
            StateMachine.AnimController.CrossFade(animName, fade);
        }

        public override void OnLogic()
        {
            var groundData  = StateMachine.movementData.GroundedData;
            bool isRootedNow = Core.RuntimeData.HasAnyState(EntityStateFlags.Rooted);
            bool isTryingToMove = Context.MoveDirection.sqrMagnitude > 0.01f;
            bool canMove = !Core.RuntimeData.HasAnyState(EntityStateFlags.Stunned | EntityStateFlags.Rooted | EntityStateFlags.Dead);

            // 1. Rooted 結束：關閉 RootMotion
            if (_wasRootedLastFrame && !isRootedNow)
            {
                Motor.EnableRootMotion = false;
            }
            _wasRootedLastFrame = isRootedNow;

            // 2. 過渡動畫處理：Landing / Stopping
            if (_phase != GroundPhase.None && canMove)
            {
                if (isTryingToMove)
                {
                    StartPhase(GroundPhase.None, groundData.LocomotionAnim, 0.15f);
                }
                else if (StateMachine.AnimController.IsNonLoopingAnimationFinished(_phaseAnim))
                {
                    StartPhase(GroundPhase.None, groundData.LocomotionAnim, 0.2f);
                }
            }

            // 3. 平地急停系統 (守門：只有無過渡時才觸發)
            Vector3 hVel = new Vector3(Motor.CurrentVelocity.x, 0f, Motor.CurrentVelocity.z);
            float currentSpeed = hVel.magnitude;

            bool isStopping  = !isTryingToMove;
            float angleDiff  = isTryingToMove ? Vector3.Angle(hVel, Context.MoveDirection) : 0f;
            bool isReversing = isTryingToMove && angleDiff > 135f;

            if (canMove && _phase == GroundPhase.None && (isStopping || isReversing) && _lastSpeedForStop > groundData.MediumStopSpeedThreshold)
            {
                float stopRefSpeed = _lastSpeedForStop;
                _lastSpeedForStop  = 0f;           
                Context.LastHorizontalSpeed = stopRefSpeed;

                Core.RuntimeData.RemoveState(EntityStateFlags.Moving);
                Core.RuntimeData.RemoveState(EntityStateFlags.Sprinting);
                Core.RuntimeData.RemoveState(EntityStateFlags.Walking);

                if (stopRefSpeed > groundData.HeavyStopSpeedThreshold)
                {
                    Core.RuntimeData.AddTemporaryState(EntityStateFlags.Rooted, groundData.HeavyStopLockDuration);
                    Motor.EnableRootMotion = true;
                    StartPhase(GroundPhase.Stopping, groundData.HeavyStopAnim, 0.1f);
                }
                else
                {
                    StartPhase(GroundPhase.Stopping, groundData.MediumStopAnim, 0.1f);
                }
                return;
            }

            if (isTryingToMove && canMove)
            {
                _lastSpeedForStop = currentSpeed;
            }

            // 4. 常規移動標籤管理
            if (!isTryingToMove || !canMove)
            {
                Core.RuntimeData.RemoveState(EntityStateFlags.Moving);
                Core.RuntimeData.RemoveState(EntityStateFlags.Sprinting);
                Core.RuntimeData.RemoveState(EntityStateFlags.Walking);
                return;
            }

            Core.RuntimeData.AddState(EntityStateFlags.Moving);

            if (StateMachine.Input.SprintHeld)
            {
                Core.RuntimeData.AddState(EntityStateFlags.Sprinting);
                Core.RuntimeData.RemoveState(EntityStateFlags.Walking);
            }
            else if (StateMachine.Input.CrouchToggled)
            {
                Core.RuntimeData.AddState(EntityStateFlags.Walking);
                Core.RuntimeData.RemoveState(EntityStateFlags.Sprinting);
            }
            else
            {
                Core.RuntimeData.RemoveState(EntityStateFlags.Sprinting);
                Core.RuntimeData.RemoveState(EntityStateFlags.Walking);
            }
        }

        public override void PhysicsUpdate()
        {
            var groundData = StateMachine.movementData.GroundedData;

            if (Core.RuntimeData.HasAnyState(EntityStateFlags.Rooted))
            {
                Motor.Move(Vector3.zero, 0f, 0f, groundData.HeavyStopDeceleration, false);
                return;
            }

            if (Core.RuntimeData.HasAnyState(EntityStateFlags.Dashing | EntityStateFlags.KnockedBack)) return;

            Vector3 moveDir = Context.MoveDirection;
            float baseSpeed = Core.RuntimeData.MoveSpeed.Value;
            float targetSpeed = baseSpeed;

            if (Core.RuntimeData.HasAnyState(EntityStateFlags.Sprinting))
                targetSpeed = baseSpeed * groundData.SprintSpeedMultiplier;
            else if (Core.RuntimeData.HasAnyState(EntityStateFlags.Walking))
                targetSpeed = baseSpeed * groundData.WalkSpeedMultiplier;

            float slopeAngle = Motor.CurrentGroundAngle;
            targetSpeed *= groundData.SlopeSpeedAngles.Evaluate(slopeAngle);

            Motor.Move(moveDir, targetSpeed, groundData.Acceleration, groundData.Deceleration, true, groundData.RotationData.TargetRotationReachTime.y);
        }
    }
}