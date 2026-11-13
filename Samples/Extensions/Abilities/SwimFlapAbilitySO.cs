using UnityEngine;

namespace GameFramework.Actors
{
    [CreateAssetMenu(fileName = "SwimFlapAbility", menuName = "Abilities/Swim Flap")]
    public class SwimFlapAbilitySO : AbilitySO
    {
        [Header("踩水設定")]
        public float flapVelocity = 4f;
        
        private void Reset()
        {
            // 瞬發技能，不需要 duration
            duration = 0f;
            requiredStates = EntityStateFlags.Swimming; // 必須在水裡
            blockStates = EntityStateFlags.Dead | EntityStateFlags.Stunned | EntityStateFlags.Rooted;
        }

        public override void Activate(AbilityRunner runner)
        {
            EntityMotor motor = runner.GetComponent<EntityMotor>();
            
            // 踩水時，直接覆蓋 Y 軸速度，確保連續按空白鍵時高度穩定上升
            motor.SetVerticalVelocity(flapVelocity);
            
            // 你可以在這裡順便呼叫動畫控制器播放踩水動畫
            // runner.GetComponent<EntityAnimationController>().PlaySwimFlap();
        }
    }
}