using UnityEngine;

namespace GameFramework.Actors
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(EntityCore))]
    [RequireComponent(typeof(ResizableCapsuleCollider))]
    public class EntityMotor : MonoBehaviour
    {
        private Rigidbody _rb;
        private EntityCore _core;
        public ResizableCapsuleCollider ColliderUtility { get; private set; }

        [Header("環境圖層 (★請務必勾選地板 Layer，且不要包含角色自身 Layer)")]
        
        public LayerMask groundMask;

        public Vector3 GroundNormal { get; private set; } = Vector3.up;
        public bool IsOnSteepSlope { get; private set; }

        [Header("陡坡滑動")]
        public float steepSlideAcceleration = 15f;
        
        // ==========================================
        // ★ 擴充的水體偵測 (Water Check)
        // ==========================================
        [Header("水體偵測 (Water Check)")]
        
        [Tooltip("水體圖層")]
        public LayerMask waterMask;
        
        [Tooltip("檢測球半徑")]
        public float waterDistance = 0.3f;

        [Tooltip("胸部/腰部位置：只要碰到水，就算作 IsInWater (觸發游泳狀態)")]
        public Transform waterCheckPoint;
        
        [Tooltip("頭頂位置：只有當這點也進入水中，才算作 IsUnderwater (切換潛游動畫)")]
        public Transform headCheckPoint;

        [Header("旋轉設定 (Rotation)")]
        public float defaultTurnSmoothTime = 0.1f;
        private float _turnSmoothVelocity;
        public float CurrentGroundAngle { get; private set; }
        
        [Header("狀態開關")]
        public bool EnableHover = true; // ★ 新增：允許大腦控制是否開啟懸浮吸附

        [Header("Root Motion 控制")]
        public bool EnableRootMotion = false; // 給狀態機或技能隨時開關
        
        [Header("除錯顯示 (Debug Gizmos)")]
        public bool showDebugRay = true;

        // 對外公開的物理真理
        public bool IsGrounded { get; private set; }
        public bool IsInWater { get; private set; }
        public bool IsUnderwater { get; private set; }
        public Vector3 CurrentVelocity => _rb.linearVelocity;

        public float FallVelocityAtLand { get; private set; } // ★ 新增：純粹記錄落地瞬間的原始下墜速度
        private bool _wasGroundedLastFixedUpdate;            // 記錄前一幀物理狀態
        
        private float _jumpCooldown;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _core = GetComponent<EntityCore>();
            ColliderUtility = GetComponent<ResizableCapsuleCollider>();
            ColliderUtility.Initialize();
        }

        private void FixedUpdate()
        {
            _wasGroundedLastFixedUpdate = IsGrounded;
            IsGrounded = false;
            
            // ★ 修改：只有在開啟懸浮時，才執行地形偵測與吸附
            if (EnableHover)
            {
                FloatAndCheckGround();
            }
            
            CheckWater();
        }

        // ★ 取得精準的膠囊中心世界座標 (不依賴容易延遲的 bounds.center)
        private Vector3 GetCapsuleWorldCenter()
        {
            var col = ColliderUtility.CapsuleColliderData.Collider;
            return col.transform.TransformPoint(col.center);
        }

        // ★ 計算從「削減後的膠囊中心」到「理想腳底板(0,0,0)」的正確高度
        private float GetTargetCenterHeight()
        {
            var col = ColliderUtility.CapsuleColliderData.Collider;
            // 直接讀取當前膠囊設定的 Center.y (例如 1.125m)，乘上縮放倍率
            return col.center.y * transform.localScale.y;
        }

        private void FloatAndCheckGround()
        {
            if (_jumpCooldown > 0f)
            {
                _jumpCooldown -= Time.fixedDeltaTime;
                return;
            }

            if (ColliderUtility?.CapsuleColliderData?.Collider == null) return;

            SlopeData slope = ColliderUtility.SlopeData;
            Vector3 origin = GetCapsuleWorldCenter();
            float targetH = GetTargetCenterHeight();

            IsOnSteepSlope = false;
            GroundNormal = Vector3.up;
            CurrentGroundAngle = 0f;

            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit,
                    slope.FloatRayDistance, groundMask, QueryTriggerInteraction.Ignore))
                return;

            // 離地太遠：視為空中
            if (hit.distance > targetH + 0.3f) return;

            GroundNormal = hit.normal;
            CurrentGroundAngle = Vector3.Angle(hit.normal, Vector3.up);
            bool walkable = CurrentGroundAngle <= slope.MaxSlopeAngle;

            Vector3 v = _rb.linearVelocity;

            // ---------- 前饋：沿坡面移動所需的 y 速度 ----------
            float slopeVy = 0f;
            if (hit.normal.y > 0.3f)
                slopeVy = -(hit.normal.x * v.x + hit.normal.z * v.z) / hit.normal.y;

            // ---------- 彈簧：不論陡不陡都維持高度，腳才不會沉進坡 ----------
            float error = targetH - hit.distance;             // 正 = 太低
            bool rising = v.y > 0.05f && error < 0f;          // 正在上升且偏高 → 不拉回
            if (!rising)
            {
                float reach = Mathf.Clamp(slope.StepReachForce, 0f, 1f / Time.fixedDeltaTime);
                float desiredVy = slopeVy + error * reach;
                float delta = desiredVy - v.y;
                delta = delta > 0f ? Mathf.Min(delta, 2f) : Mathf.Max(delta, -20f);
                _rb.AddForce(0f, delta, 0f, ForceMode.VelocityChange);
            }

            if (walkable)
            {
                if (!_wasGroundedLastFixedUpdate) FallVelocityAtLand = v.y;
                IsGrounded = true;
            }
            else
            {
                // 陡坡：不算接地，改為滑行
                IsOnSteepSlope = true;
                Vector3 slideDir = Vector3.ProjectOnPlane(Vector3.down, hit.normal).normalized;
                _rb.AddForce(slideDir * steepSlideAcceleration, ForceMode.Acceleration);
            }
        }

        private void CheckWater()
        {
            // 1. 判斷是否在水中 (胸部浸水即算)
            if (waterCheckPoint != null)
            {
                IsInWater = Physics.CheckSphere(waterCheckPoint.position, waterDistance, waterMask, QueryTriggerInteraction.Collide);
            }

            // 2. ★ 判斷是否完全潛入水下 (頭頂也浸水)
            if (headCheckPoint != null)
            {
                // 只有當胸部在水中，且頭頂也在水中時，才算完全潛水
                IsUnderwater = IsInWater && Physics.CheckSphere(headCheckPoint.position, waterDistance, waterMask, QueryTriggerInteraction.Collide);
            }
            else
            {
                // 防呆：如果企劃忘記拉頭頂點，預設不潛水
                IsUnderwater = false; 
            }
        }

        // 取得「水平方向的上坡方向」
        private Vector3 GetUphillDir()
        {
            Vector3 up = Vector3.ProjectOnPlane(Vector3.up, GroundNormal);
            up.y = 0f;
            return up.sqrMagnitude > 0.0001f ? up.normalized : Vector3.zero;
        }

        // 移除向量中「往上坡」的分量
        private Vector3 RemoveUphill(Vector3 horizontal)
        {
            Vector3 uphill = GetUphillDir();
            float into = Vector3.Dot(horizontal, uphill);
            return into > 0f ? horizontal - uphill * into : horizontal;
        }
        
        // ==========================================
        // 核心對外接口
        // ==========================================
        public void Move(Vector3 moveDirection, float targetSpeed, float acceleration, float deceleration, bool faceDirection = true, float turnMultiplier = 1.0f)
        {
            if (_core.RuntimeData.HasAnyState(EntityStateFlags.Rooted | EntityStateFlags.Stunned | EntityStateFlags.Dead))
            {
                Vector3 hVel = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z);
                hVel = Vector3.Lerp(hVel, Vector3.zero, Time.fixedDeltaTime * 15f);
                _rb.linearVelocity = new Vector3(hVel.x, _rb.linearVelocity.y, hVel.z);
                return;
            }

            if (faceDirection && moveDirection.magnitude >= 0.1f && turnMultiplier > 0.01f)
            {
                float targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
                float smoothTime = defaultTurnSmoothTime / turnMultiplier;
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, smoothTime);
                _rb.MoveRotation(Quaternion.Euler(0f, angle, 0f));
            }

            Vector3 currentHorizontalVel = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z);

            if (moveDirection.magnitude >= 0.1f)
            {
                Vector3 targetVelocity = moveDirection * targetSpeed;

                // ==========================================
                // ★ 新增：防陡坡「硬上」過濾器 (Wall Deflection)
                // ==========================================
                if (ColliderUtility != null && ColliderUtility.CapsuleColliderData?.Collider != null)
                {
                    float radius = ColliderUtility.CapsuleColliderData.Collider.radius;
                    Vector3 center = GetCapsuleWorldCenter();
                    
                    // 在小腿的高度發射探測 (稍微抬高一點，避免掃到正常的地板)
                    Vector3 bottomPos = center - new Vector3(0, GetTargetCenterHeight() - radius - 0.1f, 0);

                    // 往前探測 0.3 公尺，看有沒有撞到陡坡或牆壁
                    if (Physics.SphereCast(bottomPos, radius * 0.9f, moveDirection.normalized, out RaycastHit wallHit, 0.3f, groundMask, QueryTriggerInteraction.Ignore))
                    {
                        float wallAngle = Vector3.Angle(wallHit.normal, Vector3.up);
                        
                        // 如果撞到的坡度大於我們能爬的最大角度
                        if (wallAngle > ColliderUtility.SlopeData.MaxSlopeAngle)
                        {
                            // 把目標速度沿著牆壁法線投影 (抹除掉往牆裡擠的力道，只保留沿著牆壁滑行的力道)
                            targetVelocity = Vector3.ProjectOnPlane(targetVelocity, wallHit.normal);
                            
                            // 確保投影後不會產生向上的速度 (避免變成爬牆)
                            targetVelocity.y = 0f;
                        }
                    }
                }

                currentHorizontalVel = Vector3.Lerp(currentHorizontalVel, targetVelocity, Time.fixedDeltaTime * acceleration);
            }
            else
            {
                currentHorizontalVel = Vector3.Lerp(currentHorizontalVel, Vector3.zero, Time.fixedDeltaTime * deceleration);
            }

            _rb.linearVelocity = new Vector3(currentHorizontalVel.x, _rb.linearVelocity.y, currentHorizontalVel.z);
        }

        public void Jump(Vector3 jumpForce)
        {
            if (!IsGrounded && !IsInWater) return; 
            if (_core.RuntimeData.HasAnyState(EntityStateFlags.Rooted)) return; 

            _jumpCooldown = 0.2f;
            IsGrounded = false;

            Vector3 currentVel = _rb.linearVelocity;
            currentVel.y = 0f; 
            _rb.linearVelocity = currentVel;

            _rb.AddForce(jumpForce, ForceMode.VelocityChange);
        }
        
        public void SetVerticalVelocity(float yVelocity)
        {
            _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, yVelocity, _rb.linearVelocity.z);
        }
        
        public void AddVerticalForce(float force, ForceMode mode = ForceMode.Acceleration)
        {
            _rb.AddForce(new Vector3(0, force, 0), mode);
        }

        public void SetCrouchState(bool isCrouching)
        {
            ColliderUtility.Resize(isCrouching ? 0.5f : 1f);
        }
        
        // ==========================================
        // 供狀態機操作的設定介面
        // ==========================================
        public void SetGravity(bool useGravity)
        {
            if (_rb != null)
            {
                _rb.useGravity = useGravity;
            }
        }
        
        // ==========================================
        // 供 GAS (Ability) 呼叫的瞬發動作接口
        // ==========================================
        public void ApplyImpulse(Vector3 forceVector, ForceMode mode = ForceMode.VelocityChange)
        {
            _rb.AddForce(forceVector, mode);
        }
        
        // ★ 新增：無視一切，直接覆蓋絕對速度 (給 Dash 專用)
        public void OverwriteVelocity(Vector3 newVelocity)
        {
            _rb.linearVelocity = newVelocity;
        }
        
        // ★ 新增：接收來自子物件的動畫位移，並推動剛體
        public void ApplyRootMotion(Vector3 deltaPosition, Quaternion deltaRotation)
        {
            if (_rb == null) return;

            // 使用 MovePosition 而不是直接改 transform，才能正確觸發物理碰撞！
            _rb.MovePosition(_rb.position + deltaPosition);
            
            // 如果你的遊戲允許動畫自帶轉向，也可以加上這行
            _rb.MoveRotation(_rb.rotation * deltaRotation);
        }

        // ==========================================
        // ★ 射線視覺化 (Scene 視窗直接看，不用開 Play)
        // ==========================================
        private void OnDrawGizmos()
        {
            if (!showDebugRay) return;

            if (ColliderUtility == null) ColliderUtility = GetComponent<ResizableCapsuleCollider>();
            if (ColliderUtility == null) return;

            ColliderUtility.Initialize();
            var col = ColliderUtility.CapsuleColliderData?.Collider;
            if (col == null) return;

            Vector3 rayOrigin = GetCapsuleWorldCenter();
            float rayLength = ColliderUtility.SlopeData.FloatRayDistance;
            float targetHeight = GetTargetCenterHeight();

            // 1. 畫出「最大偵測射線」 (灰色底線：代表 FloatRayDistance 最遠能探測到哪裡)
            Gizmos.color = Color.grey;
            Gizmos.DrawLine(rayOrigin, rayOrigin + Vector3.down * rayLength);

            // 2. 畫出「理想懸浮高度線」 (黃色線 + 黃色小圓盤：這顆球的位置就是角色腳底板 (0,0,0) 應該踩在地板上的位置！)
            Vector3 idealFootPosition = rayOrigin + Vector3.down * targetHeight;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(rayOrigin, idealFootPosition);
            Gizmos.DrawWireSphere(idealFootPosition, 0.08f);

            // 3. 畫出射線起點 (青色小點：削減後的膠囊正中心)
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(rayOrigin, 0.05f);

            // 4. 如果射線實際有打到地板 (绿色點：代表實際打到的地面點)
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayLength, groundMask, QueryTriggerInteraction.Ignore))
            {
                float groundAngle = Vector3.Angle(hit.normal, Vector3.up);
                Gizmos.color = (groundAngle <= ColliderUtility.SlopeData.MaxSlopeAngle) ? Color.green : Color.red;
                Gizmos.DrawSphere(hit.point, 0.08f);
            }
            
            // ★ 新增：畫出水體檢測球，方便企劃調整位置
            if (waterCheckPoint != null)
            {
                Gizmos.color = IsInWater ? Color.blue : Color.cyan;
                Gizmos.DrawWireSphere(waterCheckPoint.position, waterDistance);
            }

            if (headCheckPoint != null)
            {
                Gizmos.color = IsUnderwater ? new Color(0, 0, 0.8f) : Color.cyan; // 深藍色代表完全沒入水中
                Gizmos.DrawWireSphere(headCheckPoint.position, waterDistance);
            }
        }
    }
}