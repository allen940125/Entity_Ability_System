using UnityEngine;
using UnityHFSM;

namespace GameFramework.Actors
{
    [RequireComponent(typeof(EntityCore))]
    [RequireComponent(typeof(EntityMotor))]
    [RequireComponent(typeof(AbilityRunner))]
    [RequireComponent(typeof(PlayerInputReceiver))]
    public class PlayerStateMachine : MonoBehaviour
    {
        [Header("設定檔")]
        public PlayerSO movementData; 
        
        [Header("★ 主動技能配置 (GAS)")]
        public AbilitySO jumpAbility;
        public AbilitySO dashAbility;
        public AbilitySO attackAbility;

        [Header("★ 空中專屬技能 (GAS)")] 
        public AbilitySO glideAbility;
        
        [Header("★ 水中專屬技能 (GAS)")]
        public AbilitySO swimFlapAbility;
        public AbilitySO waterDashAbility;
        
        // ★ 注意：這裡已經把 mainCamera 徹底刪除了！狀態機不需要認識相機！
        
        // ==========================================
        // 核心器官 (公開給 State 讀取)
        // ==========================================
        public EntityCore Core { get; private set; }
        public EntityMotor Motor { get; private set; }
        public AbilityRunner Runner { get; private set; }
        public PlayerInputReceiver Input { get; private set; }
        public PlayerContext Context { get; private set; }

        public EntityAnimationController AnimController { get; private set; }
        
        private StateMachine _fsm;

        private void Awake()
        {
            InitializeComponents();
            InitializeStateMachine();
        }
        
        private void Start() 
        {
            if (_fsm != null)
            {
                // 等到 Start 階段，所有的 Awake 都一定執行完了，
                // EntityMotor 的 _rb 百分之百已經綁定好了，這時候 Init 就絕對安全！
                _fsm.Init(); 
            }
        }

        private void Update()
        {
            // ★ 直接從 Input 黑板拿已經算好的 3D 世界方向！
            Context.MoveDirection = Input.WorldMoveDirection;
            
            // 1. 按下瞬間：觸發起跳
            if (Input.JumpTriggered) 
            {
                if (Motor.IsInWater)
                    Runner.TryCastAbility(swimFlapAbility);
                else if (!Motor.IsGrounded)
                    Runner.TryCastAbility(glideAbility);
                else
                    Runner.TryCastAbility(jumpAbility);
            }

            // ★ 2. 新增：只要玩家放開了空白鍵 (!JumpHeld)，就嘗試中斷 JumpAbility！
            // 如果玩家只按了 0.1 秒就放開，StopAbility 會立刻觸發 EndAbility 把上升速度砍半，形成完美小跳！
            // 如果玩家死死按住不放，0.5 秒後角色到達最高點 (y <= 0)，技能自然結束，形成完美大跳！
            if (!Input.JumpHeld)
            {
                Runner.StopAbility(jumpAbility);
            }

            if (Input.DashTriggered) 
            {
                if (Motor.IsInWater)
                    Runner.TryCastAbility(waterDashAbility);
                else
                    Runner.TryCastAbility(dashAbility);
            }

            if (Input.AttackTriggered) 
            {
                Runner.TryCastAbility(attackAbility);
            }

            _fsm.OnLogic(); 
        }

        private void FixedUpdate()
        {
            if (_fsm == null) return;

            if (_fsm.ActiveState is PlayerStateBase currentState)
            {
                currentState.PhysicsUpdate();
            }
        }

        private void InitializeComponents()
        {
            Core = GetComponent<EntityCore>();
            Motor = GetComponent<EntityMotor>();
            Runner = GetComponent<AbilityRunner>();
            Input = GetComponent<PlayerInputReceiver>();
            AnimController = GetComponent<EntityAnimationController>(); // ★ 抓取
            Context = new PlayerContext();
        }

        private void InitializeStateMachine()
        {
            _fsm = new StateMachine();

            _fsm.AddState("Grounded", new PlayerGroundedState(this));
            _fsm.AddState("Airborne", new PlayerAirborneState(this));
            _fsm.AddState("Swimming", new PlayerSwimmingState(this));

            _fsm.AddTransition("Grounded", "Airborne", t => !Motor.IsGrounded && !Motor.IsInWater);
            _fsm.AddTransition("Airborne", "Grounded", t => Motor.IsGrounded && !Motor.IsInWater);
            
            _fsm.AddTransitionFromAny(new Transition("", "Swimming", t => Motor.IsInWater));
            _fsm.AddTransition("Swimming", "Grounded", t => !Motor.IsInWater && Motor.IsGrounded);
            _fsm.AddTransition("Swimming", "Airborne", t => !Motor.IsInWater && !Motor.IsGrounded);

            _fsm.SetStartState("Grounded");
            //_fsm.Init();
        }
        
        // ★ 原本的 GetCameraRelativeMoveDirection() 已經刪除！
    }
}