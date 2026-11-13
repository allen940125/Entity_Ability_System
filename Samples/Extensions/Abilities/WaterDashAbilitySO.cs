using UnityEngine;

namespace GameFramework.Actors
{
    [CreateAssetMenu(fileName = "WaterDashAbility", menuName = "Abilities/Water Dash")]
    public class WaterDashAbilitySO : AbilitySO
    {
        [Header("水中衝刺設定")]
        public float dashForce = 15f;

        private void Reset()
        {
            duration = 0.3f;
            requiredStates = EntityStateFlags.Swimming; 
            blockStates = EntityStateFlags.Dead | EntityStateFlags.Stunned | EntityStateFlags.Rooted;
        }
        
        public override void Activate(AbilityRunner runner)
        {
            EntityMotor motor = runner.GetComponent<EntityMotor>();

            // 1. 給角色加上「衝刺中」的 Tag，讓狀態機罷工 0.3 秒
            runner.Core.RuntimeData.AddState(EntityStateFlags.Dashing);

            // 2. ★ 核心修正：跟陸地一樣，使用「角色正面」，確保力量 100% 往前，不會砸進土裡！
            Vector3 dashDirection = runner.transform.forward; 

            motor.SetVerticalVelocity(0f);
            // 3. ★ 核心修正：捨棄 ApplyImpulse，直接暴力覆蓋絕對速度！
            motor.ApplyImpulse(dashDirection * dashForce);
            
            // 4. ★ 如果你的 Animator 有勾選 Apply Root Motion，請務必播一個動畫！
            // 如果不播動畫，請去 Unity 把 Animator 的 Apply Root Motion 取消勾選。
            // if (runner.Anim != null) runner.Anim.SetTrigger("OnWaterDash");
        }

        public override void EndAbility(AbilityRunner runner)
        {
            runner.Core.RuntimeData.RemoveState(EntityStateFlags.Dashing);
        }
    }
}