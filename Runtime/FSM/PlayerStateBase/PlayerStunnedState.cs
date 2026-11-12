// using UnityEngine;
//
// namespace GameFramework.Actors
// {
//     public class PlayerStunnedState : PlayerStateBase
//     {
//         public PlayerStunnedState(PlayerStateMachine sm) : base(sm) { }
//
//         public override void OnEnter()
//         {
//             base.OnEnter();
//             
//             // 1. 播放暈眩動畫 (透過 Runner 拿 Animator，或是你在 StateMachine 裡快取的 Anim)
//             if (StateMachine.Runner != null && StateMachine.Runner.Anim != null)
//             {
//                 StateMachine.Runner.Anim.SetBool("IsStunned", true);
//             }
//
//             // 2. 清空玩家目前的輸入指令，避免醒來後突然砍出一刀
//             StateMachine.Context.ClearBuffers();
//         }
//
//         public override void OnLogic()
//         {
//             base.OnLogic();
//             // 暈眩狀態下，我們完全不去讀取 Input，也不呼叫 AbilityRunner
//             // 這裡什麼都不做，就是最好的狀態控制
//         }
//
//         public override void PhysicsUpdate()
//         {
//             // 物理層面：強制把移動方向與速度設為 0，但保留重力 (讓空中暈眩時會掉下來)
//             StateMachine.Motor.Move(Vector3.zero, 0f, true);
//         }
//
//         public override void OnExit()
//         {
//             base.OnExit();
//             
//             // 離開暈眩時，解除動畫狀態
//             if (StateMachine.Runner != null && StateMachine.Runner.Anim != null)
//             {
//                 StateMachine.Runner.Anim.SetBool("IsStunned", false);
//             }
//         }
//     }
// }