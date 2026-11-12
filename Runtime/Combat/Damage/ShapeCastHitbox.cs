using UnityEngine;

public class ShapeCastHitbox : DamageCasterBase
{
    // ★ 欄位全刪！不需要在 Inspector 裡面設定了
    // public CollisionShapeConfig shapeConfig; 
    // public LayerMask hitLayer; 
    
    // 判定距離可以由外部呼叫時傳入 (例如不同武器揮砍距離不同)
    private RaycastHit[] _hitResults = new RaycastHit[10]; 

    // ★ 改由外部 (動畫事件或 Ability) 傳入方向與距離
    public void PerformCast(Vector3 direction, float distance)
    {
        // 防呆：如果沒有注入資料，就不能掃描
        if (_attackData == null) 
        {
            Debug.LogError("ShapeCastHitbox 尚未初始化 AttackDataSO！");
            return;
        }

        // ★ 直接使用 _attackData 裡面的形狀與 Layer 進行判定
        int hitCount = ShapeCastUtility.PerformCast(
            transform.position, 
            direction, 
            distance, 
            _hitResults, 
            _attackData.damageShape, // 從 SO 拿
            _attackData.damageLayer, // 從 SO 拿
            true 
        );

        for (int i = 0; i < hitCount; i++)
        {
            TryApplyDamage(_hitResults[i].collider);
        }
    }
}