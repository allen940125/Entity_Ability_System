using UnityEngine;

namespace GameFramework.Actors
{
    [CreateAssetMenu(fileName = "ToggleGlideAbility", menuName = "Abilities/Toggle Glide")]
    public class ToggleGlideAbilitySO : AbilitySO
    {
        private void Reset()
        {
            duration = 0f; // 開關型技能，由自己手動管理標籤
            requiredStates = EntityStateFlags.Airborne; // ★ 必須在空中
            blockStates = EntityStateFlags.Dead | EntityStateFlags.Stunned | EntityStateFlags.Rooted;
        }
        
        public override void Activate(AbilityRunner runner)
        {
            var runtime = runner.Core.RuntimeData;
        
            // 如果已經在滑翔，就關掉；如果沒在滑翔，就打開
            if (runtime.HasAnyState(EntityStateFlags.Gliding))
            {
                runtime.RemoveState(EntityStateFlags.Gliding);
            }
            else
            {
                runtime.AddState(EntityStateFlags.Gliding);
            
                // 開傘瞬間可以給個微弱的向上浮力，手感更好
                if (runner.TryGetComponent(out GameFramework.Actors.EntityMotor motor))
                {
                    motor.SetVerticalVelocity(1f);
                }
            }
        }
    }
}