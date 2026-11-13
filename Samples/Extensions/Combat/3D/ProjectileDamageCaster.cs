using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ProjectileDamageCaster : DamageCasterBase
{
    private ProjectileDataSO _projectileData;
    private Vector3 _currentDirection;
    private float _currentSpeed;
    private Rigidbody _rb;
    private bool _isInitialized = false;
    private readonly RaycastHit[] _hitBuffer = new RaycastHit[16];

    public override void Initialize(EntityCore caster, AttackDataSO attackData, int fallbackDamage = 10)
    {
        base.Initialize(caster, attackData, fallbackDamage);
        _projectileData = attackData as ProjectileDataSO;

        _rb = GetComponent<Rigidbody>();
        _rb.isKinematic = true;
        _rb.useGravity = false;
        _rb.constraints = RigidbodyConstraints.FreezeRotation; 
    }

    public void SetTrajectory(Vector3 direction, float speed)
    {
        _currentSpeed = speed;
        _currentDirection = direction.normalized;
        
        Destroy(gameObject, _projectileData.lifeTime);
        UpdateRotation();
        _isInitialized = true; 
    }

    private void FixedUpdate()
    {
        if (!_isInitialized) return;
        MoveAndCollide();
    }

    private void MoveAndCollide()
    {
        if (_projectileData == null) return;

        float remainingDistance = _currentSpeed * Time.fixedDeltaTime;
        Vector3 currentPos = transform.position;

        int bounceHitCount = ShapeCastUtility.PerformCast(
            currentPos, _currentDirection, remainingDistance,
            _hitBuffer, _projectileData.bounceShape, _projectileData.bounceLayer, enableDebug); // 將開關傳給底層畫射線
            
        float travelDistance = remainingDistance;
        bool hasWallHit = false;

        if (bounceHitCount > 0)
        {
            hasWallHit = true;
            travelDistance = _hitBuffer[0].distance;
            if (enableDebug) Debug.Log($"<color=yellow>[Projectile]</color> 撞到牆面: {_hitBuffer[0].collider.name}");
        }

        int damageHitCount = ShapeCastUtility.PerformCast(
            currentPos, _currentDirection, remainingDistance,
            _hitBuffer, _attackData.damageShape, _attackData.damageLayer, enableDebug); // 將開關傳給底層畫射線

        for (int i = 0; i < damageHitCount; i++)
        {
            if (_hitBuffer[i].collider == null) continue;

            if (_hitBuffer[i].distance <= travelDistance + 0.01f)
            {
                if (enableDebug) Debug.Log($"<color=orange>[Projectile]</color> 命中目標: {_hitBuffer[i].collider.name}，準備結算傷害！");
                TryApplyDamage(_hitBuffer[i].collider);
            }
        }

        transform.position = currentPos + _currentDirection * travelDistance;
        
        if (hasWallHit)
        {
            if (enableDebug) Debug.Log("<color=red>[Projectile]</color> 撞牆銷毀。");
            Destroy(gameObject);
        }
    }

    private void UpdateRotation()
    {
        if (_currentDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(_currentDirection);
        }
    }
}