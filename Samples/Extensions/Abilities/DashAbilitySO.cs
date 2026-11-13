using UnityEngine;

namespace GameFramework.Actors
{
    [CreateAssetMenu(fileName = "DashAbility", menuName = "Abilities/Dash")]
    public class DashAbilitySO : AbilitySO
    {
        [Header("衝刺設定")]
        public float dashForce = 20f;
        
        [Header("動畫設定")]
        public string dashAnimationTrigger = "OnDash";

        private void Reset()
        {
            // Runner 會在 0.3 秒後自動呼叫 EndAbility 幫忙拔掉標籤
            duration = 0.3f; 
            requiredStates = EntityStateFlags.Grounded; 
            blockStates = EntityStateFlags.Dead | EntityStateFlags.Stunned | EntityStateFlags.Rooted;
        }
        
        public override void Activate(AbilityRunner runner)
        {
            var runtime = runner.Core.RuntimeData;

            // 1. 掛上狀態標籤 (無敵與衝刺)
            runtime.AddState(EntityStateFlags.Dashing);
            runtime.AddState(EntityStateFlags.Invincible);

            // 2. 呼叫動畫
            if (runner.Anim != null)
            {
                runner.Anim.SetTrigger(dashAnimationTrigger);
            }

            // 3. 呼叫 Motor 進行物理推動
            if (runner.TryGetComponent(out EntityMotor motor))
            {
                // 先把 Y 軸速度清空，避免在斜坡衝刺時飛上天
                motor.SetVerticalVelocity(0f);
                motor.ApplyImpulse(runner.transform.forward * dashForce, ForceMode.VelocityChange);
            }
        }

        public override void EndAbility(AbilityRunner runner)
        {
            // ★ 時間到，統一在這裡拔除標籤
            runner.Core.RuntimeData.RemoveState(EntityStateFlags.Dashing);
            runner.Core.RuntimeData.RemoveState(EntityStateFlags.Invincible);
        }
    }
}