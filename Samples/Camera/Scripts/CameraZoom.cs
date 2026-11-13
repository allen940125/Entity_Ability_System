using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace GameFramework.Actors
{
    public class CameraZoom : MonoBehaviour
    {
        [Header("依賴注入 (雙軌制)")]
        [Tooltip("【正規架構】統一從玩家黑板讀取輸入。如果留空，會在 Awake 自動尋找。")]
        [SerializeField] private PlayerInputReceiver playerInput;
        
        [Tooltip("【快捷開發】直接拖曳 Input Action。如果有填這個，會優先使用它，無視 PlayerInput！")]
        [SerializeField] private InputActionReference zoomInputAction;

        [Header("Distance Settings")]
        [SerializeField] private float defaultDistance = 6f;
        [SerializeField] private float minimumDistance = 1f;
        [SerializeField] private float maximumDistance = 12f;

        [Header("Zoom Settings")]
        [SerializeField] private float smoothing = 4f;
        [SerializeField] private float scrollSensitivity = 1f; 
        [SerializeField] private float touchSensitivity = 0.01f; 
        [SerializeField] private bool invertScroll = false;

        private CinemachinePositionComposer cinemachinePositionComposer;
        private float currentTargetDistance;

        private void Awake()
        {
            cinemachinePositionComposer = GetComponent<CinemachinePositionComposer>();
            
            if (cinemachinePositionComposer != null)
            {
                currentTargetDistance = cinemachinePositionComposer.CameraDistance;
            }
            else
            {
                Debug.LogError("找不到 CinemachinePositionComposer！請檢查組件。");
                enabled = false;
            }

            // 動態尋找黑板 (如果快捷 InputAction 沒填，且 playerInput 沒拉，就自動找)
            if (zoomInputAction == null && playerInput == null)
            {
                playerInput = FindFirstObjectByType<PlayerInputReceiver>();
            }
        }

        private void OnEnable()
        {
            if (zoomInputAction != null && zoomInputAction.action != null)
                zoomInputAction.action.Enable();
            
            EnhancedTouchSupport.Enable();
        }

        private void OnDisable()
        {
            if (zoomInputAction != null && zoomInputAction.action != null)
                zoomInputAction.action.Disable();
                
            EnhancedTouchSupport.Disable();
        }

        private void Update()
        {
            HandleInput();
            ApplyZoom();
        }

        private void HandleInput()
        {
            float zoomDelta = 0f;

            // --- 手機雙指 ---
            if (Touch.activeTouches.Count >= 2)
            {
                var touch0 = Touch.activeTouches[0];
                var touch1 = Touch.activeTouches[1];

                float prevMagnitude = ((touch0.screenPosition - touch0.delta) - (touch1.screenPosition - touch1.delta)).magnitude;
                float currentMagnitude = (touch0.screenPosition - touch1.screenPosition).magnitude;

                float difference = currentMagnitude - prevMagnitude;
                zoomDelta = difference * touchSensitivity;
            }
            // --- PC 滾輪 (雙軌制核心邏輯) ---
            else 
            {
                float scrollValue = 0f;

                // 優先級 1：快捷開發 (Inspector 有拉 Input Action 就直接用)
                if (zoomInputAction != null && zoomInputAction.action != null)
                {
                    scrollValue = zoomInputAction.action.ReadValue<float>();
                }
                // 優先級 2：正規架構 (從 PlayerInputReceiver 拿)
                else if (playerInput != null)
                {
                    scrollValue = playerInput.ZoomInput;
                }

                // 計算縮放量
                if (Mathf.Abs(scrollValue) > 0)
                {
                    float direction = invertScroll ? -1f : 1f;
                    zoomDelta = scrollValue * scrollSensitivity * direction;
                }
            }

            // --- 應用縮放 ---
            if (Mathf.Abs(zoomDelta) > 0)
            {
                currentTargetDistance -= zoomDelta; 
                currentTargetDistance = Mathf.Clamp(currentTargetDistance, minimumDistance, maximumDistance);
            }
        }

        private void ApplyZoom()
        {
            if (cinemachinePositionComposer == null) return;

            float currentDistance = cinemachinePositionComposer.CameraDistance;

            if (Mathf.Abs(currentDistance - currentTargetDistance) > 0.001f)
            {
                float lerpedZoomValue = Mathf.Lerp(currentDistance, currentTargetDistance, smoothing * Time.deltaTime);
                cinemachinePositionComposer.CameraDistance = lerpedZoomValue;
            }
        }
    }
}