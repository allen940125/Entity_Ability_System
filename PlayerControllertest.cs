// using UnityEngine;
// using UnityEngine.InputSystem;
//
// [RequireComponent(typeof(AbilityRunner))]
// public class PlayerControllertest : MonoBehaviour
// {
//     [Header("核心依賴 (自動抓取)")]
//     public AbilityRunner runner;
//
//     [Header("攝影機設定")]
//     public Transform cameraTransform;
//
//     [Header("裝備的技能 (企劃配置)")]
//     public AbilitySO primaryAttackAbility;
//     public AbilitySO dashAbility;
//
//     private InputAction _moveAction;
//
//     private void Awake()
//     {
//         if (runner == null) runner = GetComponent<AbilityRunner>();
//         if (cameraTransform == null && Camera.main != null)
//             cameraTransform = Camera.main.transform;
//
//         // 從預設 Input Actions Asset 找到 "Move"
//         _moveAction = InputSystem.actions.FindAction("Move");
//     }
//
//     private void Update()
//     {
//         HandleAbilities();
//     }
//
//     private void FixedUpdate()
//     {
//         HandleMovement();
//     }
//
//     private void HandleAbilities()
//     {
//         if (runner.Core.RuntimeData.HasState(
//             EntityStateFlags.Dead | EntityStateFlags.Stunned)) return;
//
//         // 左鍵攻擊
//         if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
//         {
//             runner.TryCastAbility(primaryAttackAbility);
//         }
//
//         // 左 Shift 衝刺
//         if (Keyboard.current != null && Keyboard.current.leftShiftKey.wasPressedThisFrame)
//         {
//             runner.TryCastAbility(dashAbility);
//         }
//     }
//
//     private void HandleMovement()
//     {
//         if (_moveAction == null) return;
//
//         // 直接拿到 Vector2，不需要再手動拼 GetAxisRaw
//         Vector2 inputDir = _moveAction.ReadValue<Vector2>();
//
//         if (inputDir.sqrMagnitude >= 0.01f)   // 避免 tiny 抖動，比 magnitude 省一個 sqrt
//         {
//             Vector3 moveDir = new Vector3(inputDir.x, 0f, inputDir.y).normalized;
//
//             float targetAngle = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg
//                                 + cameraTransform.eulerAngles.y;
//             Vector3 worldDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
//
//             // motor.Move(worldDir);
//             RotateCharacter(worldDir);
//         }
//         else
//         {
//             // motor.Move(Vector3.zero);
//         }
//     }
//
//     private void RotateCharacter(Vector3 moveDirection)
//     {
//         float rotationSpeed = 10f;
//         Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
//         transform.rotation = Quaternion.Slerp(
//             transform.rotation, targetRotation,
//             rotationSpeed * Time.fixedDeltaTime);
//     }
// }