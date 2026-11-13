using UnityEngine;
using UnityEngine.InputSystem;

namespace GameFramework.Actors
{
    public class PlayerInputReceiver : MonoBehaviour, GameInputActions.IPlayerActions
    {
        [Header("相機參考 (若留空會自動抓取 MainCamera)")]
        public Transform cameraTransform;

        private GameInputActions _inputActions;

        // ==========================================
        // 公開屬性 (純資料黑板)
        // ==========================================
        public Vector2 RawMoveInput { get; private set; }
        public bool SprintHeld { get; private set; }
        
        // ★ 將原本的 IsWalkToggled 替換為通用的 Crouch 意圖
        public bool CrouchHeld { get; private set; } 
        public bool CrouchToggled { get; private set; } 
        
        public Vector3 WorldMoveDirection { get; private set; }
        
        public bool JumpHeld { get; private set; }
        public bool JumpTriggered { get; private set; }
        public bool AttackTriggered { get; private set; }
        public bool DashTriggered { get; private set; }

        public bool CameraAdjustHeld { get; private set; }
        public bool CursorUnlockHeld { get; private set; }
        public bool MenuToggleTriggered { get; private set; }
        public float ZoomInput { get; private set; }

        private void Awake()
        {
            _inputActions = new GameInputActions();
            _inputActions.Player.SetCallbacks(this);

            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
        }

        private void OnEnable() => _inputActions.Enable();
        private void OnDisable() => _inputActions.Disable();

        private void Update()
        {
            CalculateCameraRelativeDirection();
        }

        private void LateUpdate()
        {
            JumpTriggered = false;
            AttackTriggered = false;
            DashTriggered = false;
            MenuToggleTriggered = false;
        }

        private void CalculateCameraRelativeDirection()
        {
            if (RawMoveInput.sqrMagnitude < 0.01f)
            {
                WorldMoveDirection = Vector3.zero;
                return;
            }

            if (cameraTransform == null)
            {
                if (Camera.main != null) cameraTransform = Camera.main.transform;
                else
                {
                    WorldMoveDirection = new Vector3(RawMoveInput.x, 0f, RawMoveInput.y).normalized;
                    return;
                }
            }

            Vector3 camForward = cameraTransform.forward;
            camForward.y = 0f;
            camForward.Normalize();

            Vector3 camRight = cameraTransform.right;
            camRight.y = 0f;
            camRight.Normalize();

            WorldMoveDirection = (camForward * RawMoveInput.y + camRight * RawMoveInput.x).normalized;
        }

        // ==========================================
        // 實作 IPlayerActions
        // ==========================================
        public void OnAxes(InputAction.CallbackContext context)
        {
            RawMoveInput = context.ReadValue<Vector2>();
        }

        public void OnRun(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Performed) SprintHeld = true;
            else if (context.phase == InputActionPhase.Canceled) SprintHeld = false;
            
            // 奔跑時自動解除蹲下/慢走切換狀態
            if (SprintHeld) CrouchToggled = false;
        }

        // ★ 修改：實作通用的降低身姿/向下意圖 (對應陸地慢走/蹲下，水中下潛)
        // 請記得去 Unity 編輯器把 InputActions 裡的 Action 名字改成 Crouch！
        public void OnCrouch(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Performed)
            {
                CrouchHeld = true;
                CrouchToggled = !CrouchToggled; // 按下的瞬間切換 Toggle 狀態
            }
            else if (context.phase == InputActionPhase.Canceled)
            {
                CrouchHeld = false;
            }
        }
        
        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Performed)
            {
                JumpHeld = true;       
                JumpTriggered = true;  
            }
            else if (context.phase == InputActionPhase.Canceled)
            {
                JumpHeld = false;      
            }
        }

        public void OnDash(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Performed) DashTriggered = true;
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Performed) AttackTriggered = true;
        }

        public void OnAdjust(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Performed) CameraAdjustHeld = true;
            else if (context.phase == InputActionPhase.Canceled) CameraAdjustHeld = false;
        }

        public void OnCursorToggle(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Performed) CursorUnlockHeld = true;
            else if (context.phase == InputActionPhase.Canceled) CursorUnlockHeld = false;
        }
        
        public void OnEscape(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Performed) MenuToggleTriggered = true;
        }

        public void OnZoom(InputAction.CallbackContext context)
        {
            ZoomInput = context.ReadValue<float>();
        }
        
        public void OnRoll(InputAction.CallbackContext context) { }
        public void OnChargedAttack(InputAction.CallbackContext context) { }
        public void OnLook(InputAction.CallbackContext context) { }
        public void OnLockEnemyToggle(InputAction.CallbackContext context) { }
        
        // ==========================================
        // 供狀態機在跨越環境時，重置輸入意圖
        // ==========================================
        public void ResetCrouchToggle()
        {
            CrouchToggled = false;
        }
    }
}