using UnityEngine;

namespace GameFramework.Actors
{
    [RequireComponent(typeof(Animator))]
    public class EntityAnimationEventHandler : MonoBehaviour
    {
        [Header("父物件核心依賴")]
        private Animator _animator;
        private EntityCore _core;
        private EntityMotor _motor;
        private AbilityRunner _runner;
        private PlayerStateMachine _stateMachine; // 如果需要直接通知狀態機也可以抓

        private void Awake()
        {
            // ★ 在 Awake 時，主動往上尋找父物件的大腦與技能執行器
            _animator = GetComponent<Animator>();
            _core = GetComponentInParent<EntityCore>();
            _motor = GetComponentInParent<EntityMotor>();
            _runner = GetComponentInParent<AbilityRunner>();
            _stateMachine = GetComponentInParent<PlayerStateMachine>();

            if (_core == null)
            {
                Debug.LogError("動畫事件接收器找不到父物件的 EntityCore！");
            }
        }

        // ==========================================
        // ★ 1. 狀態標籤清理事件 (給落地、急停使用)
        // ==========================================
        
        /// <summary>
        /// 給 Animator Event 呼叫：移除硬直/定身狀態
        /// </summary>
        public void RemoveRootedState()
        {
            if (_core != null)
            {
                _core.RuntimeData.RemoveState(EntityStateFlags.Rooted);
            }
        }

        /// <summary>
        /// 給 Animator Event 呼叫：手動切換回地面狀態 (如急停提早結束)
        /// </summary>
        public void ForceReturnToGrounded()
        {
            if (_stateMachine != null)
            {
                //_stateMachine.RequestStateChange("Grounded");
            }
        }

        // ==========================================
        // ★ 2. GAS 戰鬥技能專用事件 (為下一個階段鋪路)
        // ==========================================

        public void OnDamageWindowOpen()
        {
            // 通知 AbilityRunner 開啟武器傷害判定 (Hitbox)
        }

        public void OnDamageWindowClose()
        {
            // 通知 AbilityRunner 關閉武器傷害判定
        }

        public void OnComboWindowOpen()
        {
            // 通知 AbilityRunner 現在允許玩家按下一次攻擊鍵來連擊
        }

        public void OnAbilityAnimationEnd()
        {
            // 動畫徹底播完，通知 Runner 結束當前技能 (拔除 Attacking 標籤)
        }
        
        // ==========================================
        // ★ Root Motion 轉接核心
        // ==========================================
        private void OnAnimatorMove()
        {
            if (_animator == null || _motor == null) return;

            // 如果父物件允許使用 Root Motion，就把這幀的動畫位移傳上去
            if (_motor.EnableRootMotion)
            {
                _motor.ApplyRootMotion(_animator.deltaPosition, _animator.deltaRotation);
            }
            // 如果不允許，Unity 依然會把 _animator.deltaPosition 歸零 (因為我們寫了這個函數)
            // 這樣可以完美防止子物件因為誤開 Root Motion 而自己亂飄離父物件！
        }
    }
}