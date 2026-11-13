using UnityEngine;

namespace GameFramework.Actors
{
    public class PlayerAirborneState : PlayerStateBase
    {
        private bool _hasPlayedAirborneAnim;
        private bool _wasGliding;
        
        public PlayerAirborneState(PlayerStateMachine sm) : base(sm) { }

        public override void OnEnter()
        {
            base.OnEnter();
            Core.RuntimeData.AddState(EntityStateFlags.Airborne);
            _hasPlayedAirborneAnim = false;
            _wasGliding = false;

            // ==========================================
            // [動畫視覺過濾系統]
            // ==========================================
            // 如果角色的垂直速度大於 0.1f (代表他是主動按空白鍵「跳」起來的)
            // 這種情況毫無疑問，必須立刻播放空中動畫！
            // ★ 修改後：只看大腦裡有沒有 Jumping 標籤，完全解耦！
            // 把 0.1f 提高到 1.5f 或 2.0f
            // 這樣可以過濾掉斜坡底部「懸浮避震器」產生的微小向上推力，
            // 但依然能捕捉到「高速衝出懸崖」或是「被炸彈炸飛」那種明顯的向上力道！
            //bool isJumping = Motor.CurrentVelocity.y > 1.5f || Core.RuntimeData.HasAnyState(EntityStateFlags.Jumping);

            bool isJumping = Core.RuntimeData.HasAnyState(EntityStateFlags.Jumping);
            
            if (isJumping)
            {
                StateMachine.AnimController.CrossFade("Airborne", 0.05f); 
                _hasPlayedAirborneAnim = true;
            }
            // 如果他是小於等於 0 (代表他是「走下台階」或「跑出懸崖」)，我們在 OnEnter 先「按兵不動」。
            // 此時角色在半空中會繼續維持他前一刻的動作 (例如跑步)。
        }

        public override void OnExit()
        {
            base.OnExit();
            
            // 離開空中前，把最後一刻的下墜速度存進大腦
            Context.LastFallSpeed = Motor.FallVelocityAtLand; 
            
            Core.RuntimeData.RemoveState(EntityStateFlags.Airborne);
            Core.RuntimeData.RemoveState(EntityStateFlags.Gliding);
        }

        public override void OnLogic()
        {
            base.OnLogic();

            // ==========================================
            // [延遲下墜判定]
            // ==========================================
            // 如果還沒播過空中動畫，且角色已經往下掉，累積出了一定的下墜速度 (例如 -3f)
            // (重力 -20 的情況下，從 0 加速到 -3 大約需要 0.15 秒，剛好過濾掉所有小台階！)
            // ★ 滑翔動畫：只在 Tag 變化的那一幀切換
            bool isGliding = Core.RuntimeData.HasAnyState(EntityStateFlags.Gliding);
            if (isGliding != _wasGliding)
            {
                _wasGliding = isGliding;
                StateMachine.AnimController.CrossFade(isGliding ? "Glide" : "Airborne", 0.15f);
                _hasPlayedAirborneAnim = true;   // 擋掉下面的延遲下墜判定，避免蓋掉滑翔動畫
            }

            // 延遲下墜判定（原本的）
            if (!_hasPlayedAirborneAnim && Motor.CurrentVelocity.y < -3f)
            {
                StateMachine.AnimController.CrossFade("Airborne", 0.2f);
                _hasPlayedAirborneAnim = true;
            }
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();

            if (!CanMove) 
            {
                Motor.Move(Vector3.zero, 0f, 0f, 0f, true, 1.0f);
                return;
            }

            bool isGliding = Core.RuntimeData.HasAnyState(EntityStateFlags.Gliding);
            Vector3 moveDir = Context.MoveDirection;
            float currentSpeed = Core.RuntimeData.MoveSpeed.Value;
            float currentY = Motor.CurrentVelocity.y;

            var airborneData = StateMachine.movementData.AirborneData;

            if (isGliding)
            {
                var glideData = airborneData.GlideData;
                Motor.Move(moveDir, currentSpeed * glideData.SpeedModifier, glideData.Acceleration, glideData.Deceleration, true, 1.0f);

                if (currentY < -glideData.GlideFallSpeedLimit)
                {
                    Motor.SetVerticalVelocity(-glideData.GlideFallSpeedLimit);
                }
            }
            else
            {
                var fallData = airborneData.FallData;
                
                if (fallData.canMovementInTheAir)
                {
                    Motor.Move(moveDir, currentSpeed * fallData.SpeedModifier, airborneData.Acceleration, airborneData.Deceleration, true, 1.0f);
                }
                else
                {
                    Motor.Move(Vector3.zero, 0f, 0f, 0f, true, 1.0f);
                }

                if (currentY < 0f && currentY < -fallData.FallSpeedLimit)
                {
                    Motor.SetVerticalVelocity(-fallData.FallSpeedLimit);
                }
            }
        }
    }
}