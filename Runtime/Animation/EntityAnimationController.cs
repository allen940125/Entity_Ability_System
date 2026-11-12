using UnityEngine;

namespace GameFramework.Actors
{
    [RequireComponent(typeof(EntityCore))]
    [RequireComponent(typeof(EntityMotor))]
    public class EntityAnimationController : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        private EntityMotor _motor;

        private readonly int _animHorizontalSpeed = Animator.StringToHash("HorizontalSpeed");
        private readonly int _animVerticalSpeed = Animator.StringToHash("VerticalSpeed");
        
        public float speedDampTime = 0.1f;

        // ★ 開關：方便你隨時在 Inspector 追蹤動畫呼叫來源
        [Header("除錯")]
        public bool logAnimationCalls = false;

        private void Awake()
        {
            _motor = GetComponent<EntityMotor>();
            if (_animator == null) _animator = GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            if (_animator == null || _motor == null) return;

            Vector3 horizontalVelocity = new Vector3(_motor.CurrentVelocity.x, 0f, _motor.CurrentVelocity.z);
            _animator.SetFloat(_animHorizontalSpeed, horizontalVelocity.magnitude, speedDampTime, Time.deltaTime);
            _animator.SetFloat(_animVerticalSpeed, _motor.CurrentVelocity.y);
            
            //_animator.SetFloat(_animVerticalSpeed, _motor.AnimationVerticalSpeed);
        }

        // ==========================================
        // ★ 封裝動畫呼叫介面 (字串版，方便可讀性)
        // ==========================================
        public void CrossFade(string stateName, float transitionDuration = 0.1f)
        {
            if (_animator == null) return;
            
            if (logAnimationCalls)
                Debug.Log($"[AnimController] 播放動畫: {stateName} | 來源: {new System.Diagnostics.StackTrace().GetFrame(1).GetMethod().Name}");

            _animator.CrossFadeInFixedTime(stateName, transitionDuration);
        }

        // ==========================================
        // ★ 封裝動畫呼叫介面 (Hash版，效能最好，給 SO 使用)
        // ==========================================
        public void CrossFade(int stateHash, float transitionDuration = 0.1f)
        {
            if (_animator == null) return;
            _animator.CrossFadeInFixedTime(stateHash, transitionDuration);
        }
        
        // ==========================================
        // ★ 新增：檢查特定動畫是否快要播完了 (給 State 判斷何時自然切回 Idle 用)
        // ==========================================
        public bool IsNonLoopingAnimationFinished(string animName, int layerIndex = 0)
        {
            if (_animator == null) return true;

            AnimatorStateInfo currentInfo = _animator.GetCurrentAnimatorStateInfo(layerIndex);
            AnimatorStateInfo nextInfo = _animator.GetNextAnimatorStateInfo(layerIndex);
            bool isInTransition = _animator.IsInTransition(layerIndex);

            // 1. 如果正在「過渡到」這個動畫，代表它才剛開始，絕對還沒播完
            if (isInTransition && nextInfo.IsName(animName))
            {
                return false;
            }

            // 2. 如果目前正在播這個動畫，檢查進度是否 >= 95% (不要用 == 1.0f，浮點數會吃鱉)
            if (currentInfo.IsName(animName))
            {
                return currentInfo.normalizedTime >= 0.95f;
            }

            // 3. 終極防卡死：如果現在根本沒在過渡，且當前動畫根本不是 animName
            // 代表動畫早就被切走，或者名字拼錯。直接回傳 true 放行，避免角色永久定身！
            if (!isInTransition && !currentInfo.IsName(animName))
            {
                return true; 
            }

            return false;
        }
    }
}