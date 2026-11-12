using UnityEngine;

[CreateAssetMenu(fileName = "ProjectileAbility", menuName = "Abilities/Projectile")]
public class ProjectileAbilitySO : AbilitySO
{
    [Header("子彈發射設定")]
    public ProjectileDataSO projectileData; // 你之前做好的子彈規格
    
    [Header("發射點設定 (相對位置)")]
    [Tooltip("子彈生成位置相對於 Runner 的偏移")]
    public Vector3 spawnOffset = new Vector3(0, 1f, 1f); 

    [Header("動畫設定")]
    public string fireAnimationTrigger = "OnFire";

    public override void Activate(AbilityRunner runner)
    {
        var runtime = runner.Core.RuntimeData;

        // 1. 扣資源
        if (staminaCost > 0f && runtime.TryGetTrait(out StaminaTrait stamina))
        {
            stamina.ConsumeStamina(staminaCost);
        }

        // 2. 播動畫
        if (runner.Anim != null)
        {
            runner.Anim.SetTrigger(fireAnimationTrigger);
        }

        // 3. 防呆檢查
        if (projectileData == null || projectileData.projectilePrefab == null)
        {
            Debug.LogError($"[{abilityName}] 缺少 ProjectileDataSO 或 Prefab！");
            return;
        }

        // 4. 計算發射位置與方向 (基於 Runner 的 Transform)
        Vector3 spawnPos = runner.transform.position + runner.transform.TransformDirection(spawnOffset);
        Vector3 fireDir = runner.transform.forward;

        // 5. 生成肉體與注入靈魂 (我們之前的邏輯)
        GameObject bulletObj = Instantiate(projectileData.projectilePrefab, spawnPos, Quaternion.LookRotation(fireDir));
        ProjectileDamageCaster caster = bulletObj.GetComponent<ProjectileDamageCaster>();
        
        if (caster != null)
        {
            caster.Initialize(runner.Core, projectileData);
            caster.SetTrajectory(fireDir, projectileData.baseSpeed);
        }
    }
}