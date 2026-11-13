using UnityEngine;

public class MeleeDamageCaster : DamageCasterBase
{
    [Header("揮砍狀態 (可從 Animator 動態控制)")]
    [Tooltip("開啟後，物件每一幀只要有產生位移，就會在位移軌跡上產生傷害判定")]
    [SerializeField] private bool _isCasting = false;

    private Vector3 _lastPosition;
    private readonly RaycastHit[] _hitBuffer = new RaycastHit[16];

    private void Start()
    {
        _lastPosition = transform.position;
    }

    // ==========================================
    // ★ 提供給外部 Animator 事件 (Animation Event) 呼叫
    // ==========================================
    
    // 開始揮刀：啟動判定，並清空上一刀的命中記憶，允許再次對相同目標造成傷害
    public void BeginCast()
    {
        _isCasting = true;
        _lastPosition = transform.position; // 重置起點，避免算出從上一次結束到這一次開始的幽靈軌跡
        
        // ★ 呼叫基類的清理方法，而不是自己拿 HashSet 亂清
        ClearHitMemory(); 
        
        if (enableDebug) Debug.Log("<color=magenta>[Melee]</color> 開始揮砍，記憶已重置！");
    }

    // 結束揮刀：停止判定
    public void EndCast()
    {
        _isCasting = false;
        if (enableDebug) Debug.Log("<color=magenta>[Melee]</color> 結束揮砍。");
    }

    // ==========================================
    // ★ 物理運算層
    // ==========================================
    private void FixedUpdate()
    {
        if (!_isCasting) 
        {
            // 如果沒在揮砍，依然要追蹤武器當前位置，否則下次 BeginCast 的瞬間會產生殘影判定
            _lastPosition = transform.position;
            return;
        }

        Vector3 currentPos = transform.position;
        Vector3 movementDelta = currentPos - _lastPosition; // ★ 修正你的編譯錯誤
        float distance = movementDelta.magnitude;

        // 只有產生實質位移時才進行判定 (避免原地不動卻狂打射線浪費效能)
        if (distance > 0.001f)
        {
            Vector3 direction = movementDelta.normalized;

            int hitCount = ShapeCastUtility.PerformCast(
                _lastPosition, direction, distance,
                _hitBuffer, _attackData.damageShape, _attackData.damageLayer, enableDebug);

            for (int i = 0; i < hitCount; i++)
            {
                Collider targetCol = _hitBuffer[i].collider;
                if (targetCol == null) continue;

                // ★ 這裡直接把命中的目標全部丟給基類，不用自己寫 if 過濾了。
                // 如果 _attackData 沒有開啟 useDamageTick，基類自然會幫你擋下這刀的重複傷害。
                // 如果開啟了（例如電鋸），基類就會幫你計算冷卻間隔。
                TryApplyDamage(targetCol);
            }
        }

        _lastPosition = currentPos; // 更新上一幀位置，供下一幀結算位移
    }
}