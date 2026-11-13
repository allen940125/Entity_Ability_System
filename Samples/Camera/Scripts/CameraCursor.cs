using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch; 
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch; 

namespace GameFramework.Actors
{
    public enum CameraControlMode
    {
        TPS_FreeLook,   // 越肩/射擊視角：滑鼠移動就轉視角
        RTS_ARPG,       // 戰略/俯視角 (如 AGE4)：必須按住特定快捷鍵 (中鍵) 才能轉視角
        Disabled        // 劇情/鎖定：完全沒收玩家控制權
    }

    public class CameraCursor : MonoBehaviour
    {
        [Header("依賴注入 (雙軌制)")]
        [SerializeField] private PlayerInputReceiver playerInput; 
        [SerializeField] private InputActionReference cameraAdjustAction; 
        [SerializeField] private InputActionReference cameraToggleAction; // ★ 這裡綁定 Alt 鍵

        [Header("核心組件")]
        [SerializeField] private CinemachineInputAxisController cinemachineInput;

        [Header("遊戲相機模式")]
        public CameraControlMode controlMode = CameraControlMode.RTS_ARPG;
        
        [Header("手機觸控設定")]
        [SerializeField] private bool onlyRotateOnRightSide = true;
        [Range(0.1f, 0.9f)] [SerializeField] private float rightSideRatio = 0.5f;

        // 內部狀態
        public bool IsMenuOpen { get; private set; }        // 由 UIManager 控制的「硬解鎖」(如 Esc 打開背包)
        public bool IsAltMenuHeld { get; private set; }     // 由玩家按住 Alt 控制的「軟解鎖」
        
        private bool _isTouchCameraAdjusting = false;
        private bool _isQuickAdjustHeld = false; 

        private void Awake()
        {
            if (cinemachineInput == null) cinemachineInput = GetComponent<CinemachineInputAxisController>();

            // 註冊快捷鍵事件 (按住中鍵轉視角)
            if (cameraAdjustAction != null && cameraAdjustAction.action != null)
            {
                cameraAdjustAction.action.started += ctx => _isQuickAdjustHeld = true;
                cameraAdjustAction.action.canceled += ctx => _isQuickAdjustHeld = false;
            }
            
            // ★ 註冊快捷鍵事件 (按住 Alt 呼叫游標)
            if (cameraToggleAction != null && cameraToggleAction.action != null)
            {
                cameraToggleAction.action.started += ctx => IsAltMenuHeld = true;
                cameraToggleAction.action.canceled += ctx => IsAltMenuHeld = false;
            }
        }

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            if (cameraAdjustAction != null && cameraAdjustAction.action != null) cameraAdjustAction.action.Enable();
            if (cameraToggleAction != null && cameraToggleAction.action != null) cameraToggleAction.action.Enable();
        }

        private void OnDisable()
        {
            EnhancedTouchSupport.Disable();
            if (cameraAdjustAction != null && cameraAdjustAction.action != null) cameraAdjustAction.action.Disable();
            if (cameraToggleAction != null && cameraToggleAction.action != null) cameraToggleAction.action.Disable();
        }

        // ====================================================================
        // ★ 外部控制接口
        // ====================================================================
        public void BindPlayer(PlayerInputReceiver newPlayer)
        {
            playerInput = newPlayer;
        }

        public void SetControlMode(CameraControlMode newMode)
        {
            controlMode = newMode;
            UpdateCinemachineState(); 
        }

        // 由 UIManager 呼叫：硬性打開/關閉 UI 介面 (如背包)
        public void SetCursorState(bool isMenuOpen)
        {
            IsMenuOpen = isMenuOpen;
            // 這裡不再直接設定 Cursor，交給 Update 統一處理
        }

        // ====================================================================

        private void Update()
        {
            // 動態尋找玩家
            if (cameraAdjustAction == null && playerInput == null)
            {
                playerInput = FindFirstObjectByType<PlayerInputReceiver>();
                if (playerInput == null) return; 
            }

            // ★ 1. 讀取正規架構下的 Alt 按住狀態 (如果沒用快捷開發)
            if (cameraToggleAction == null && playerInput != null)
            {
                IsAltMenuHeld = playerInput.CursorUnlockHeld;
            }

            // ★ 2. 統合游標狀態：只要 (UIManager 打開了介面) 或是 (玩家按住了 Alt)，就顯示游標
            bool shouldShowCursor = IsMenuOpen || IsAltMenuHeld;
            Cursor.visible = shouldShowCursor;
            Cursor.lockState = shouldShowCursor ? CursorLockMode.None : CursorLockMode.Locked;

            CheckMobileTouchInput();
            UpdateCinemachineState();
        }

        private void CheckMobileTouchInput()
        {
            // 觸控防呆：如果游標已經被釋放，或是處於鎖定模式，就不理會觸控轉視角
            if (controlMode == CameraControlMode.Disabled || IsMenuOpen || IsAltMenuHeld) 
            {
                _isTouchCameraAdjusting = false;
                return;
            }

            _isTouchCameraAdjusting = false;

            foreach (var touch in Touch.activeTouches)
            {
                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began || 
                    touch.phase == UnityEngine.InputSystem.TouchPhase.Moved || 
                    touch.phase == UnityEngine.InputSystem.TouchPhase.Stationary)
                {
                    if (onlyRotateOnRightSide)
                    {
                        float splitX = Screen.width * (1.0f - rightSideRatio);
                        if (touch.screenPosition.x > splitX)
                        {
                            _isTouchCameraAdjusting = true;
                            break;
                        }
                    }
                    else
                    {
                        _isTouchCameraAdjusting = true;
                        break;
                    }
                }
            }
        }

        private void UpdateCinemachineState()
        {
            if (cinemachineInput == null) return;

            // ★ 核心防護：只要打開了 UI 菜單，或是「按住了 Alt 呼叫游標」，一律不准轉視角！
            if (IsMenuOpen || IsAltMenuHeld)
            {
                cinemachineInput.enabled = false;
                return;
            }

            // 讀取玩家是否有按住「旋轉熱鍵」
            bool isHoldingRotateButton = false;
            if (cameraAdjustAction != null && cameraAdjustAction.action != null)
                isHoldingRotateButton = _isQuickAdjustHeld;
            else if (playerInput != null)
                isHoldingRotateButton = playerInput.CameraAdjustHeld;

            isHoldingRotateButton = isHoldingRotateButton || _isTouchCameraAdjusting;

            // 根據遊戲模式決定操作手感
            switch (controlMode)
            {
                case CameraControlMode.TPS_FreeLook:
                    cinemachineInput.enabled = true;
                    break;

                case CameraControlMode.RTS_ARPG:
                    cinemachineInput.enabled = isHoldingRotateButton;
                    break;

                case CameraControlMode.Disabled:
                    cinemachineInput.enabled = false;
                    break;
            }
        }
    }
}