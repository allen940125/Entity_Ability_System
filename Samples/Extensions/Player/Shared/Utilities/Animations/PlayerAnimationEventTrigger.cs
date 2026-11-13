using UnityEngine;

namespace GameFramework.Actors
{
    public class PlayerAnimationEventTrigger : MonoBehaviour
    {
        private AbilityRunner _runner;
        private Animator _animator;

        private void Awake()
        {
            // 自動往父物件找 Runner
            _runner = GetComponentInParent<AbilityRunner>();
            _animator = GetComponent<Animator>();
        }

        private bool IsInAnimationTransition(int layerIndex = 0)
        {
            return _animator.IsInTransition(layerIndex);
        }

        // ==========================================
        // 戰鬥相關事件 (通報給 Runner，讓當前的技能 SO 去處理)
        // ==========================================
        // public void TriggerAttackWindupFinishedEvent() 
        // {
        //     if (IsInAnimationTransition()) return;
        //     _runner.TriggerAnimationEvent("Windup");
        // }
        //
        // public void TriggerAttackSwingFinishedEvent() 
        // {
        //     if (IsInAnimationTransition()) return;
        //     _runner.TriggerAnimationEvent("Swing");
        // }
        //
        // public void TriggerComboWindowOverEvent() 
        // {
        //     if (IsInAnimationTransition()) return;
        //     _runner.TriggerAnimationEvent("Combo");
        // }
    }
}