using UnityEngine;
using GameFramework.Actors;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "JumpAbility", menuName = "Abilities/Jump")]
public class JumpAbilitySO : AbilitySO
{
    [Header("跳躍參數")]
    [Tooltip("最大起跳力道 (大跳)")]
    public Vector3 jumpForce = new Vector3(0f, 10f, 0f);

    [Tooltip("提早放開按鍵時，上升速度的保留比例 (0.5 代表瞬間砍半變成小跳)")]
    [Range(0.1f, 1f)]
    public float jumpCutMultiplier = 0.45f;

    // 追蹤按下時間與起跳高度
    private Dictionary<AbilityRunner, float> _startTimes = new Dictionary<AbilityRunner, float>();
    private Dictionary<AbilityRunner, float> _startHeights = new Dictionary<AbilityRunner, float>();

    private void Reset()
    {
        duration = 0.5f; 
        cooldownTime = 0.1f;
        requiredStates = EntityStateFlags.Grounded;
        blockStates = EntityStateFlags.Dead | EntityStateFlags.Stunned | EntityStateFlags.Rooted;
    }

    public override void Activate(AbilityRunner runner)
    {
        _startTimes[runner] = Time.time;
        _startHeights[runner] = runner.transform.position.y; // 記錄起跳瞬間的 Y 座標

        runner.Core.RuntimeData.AddState(EntityStateFlags.Jumping);
        
        if (runner.TryGetComponent(out EntityMotor motor))
        {
            motor.Jump(jumpForce);
        }
    }

    public override void OnAbilityTick(AbilityRunner runner)
    {
        // ★ 重大修正：防禦物理引擎延遲！
        // 必須等至少 0.05 秒，讓 FixedUpdate 有時間把剛體推上去，才能開始檢查是否下墜。
        if (_startTimes.TryGetValue(runner, out float startTime))
        {
            if (Time.time - startTime > 0.05f) 
            {
                if (runner.RB != null && runner.RB.linearVelocity.y <= 0f)
                {
                    runner.StopAbility(this);
                }
            }
        }
    }

    public override void EndAbility(AbilityRunner runner)
    {
        float elapsedTime = 0f;
        float jumpedHeight = 0f; // 從起跳點算起，目前爬升了多高

        if (_startTimes.TryGetValue(runner, out float startTime))
        {
            elapsedTime = Time.time - startTime;
            _startTimes.Remove(runner);
        }

        if (_startHeights.TryGetValue(runner, out float startY))
        {
            jumpedHeight = runner.transform.position.y - startY;
            _startHeights.Remove(runner);
        }

        runner.Core.RuntimeData.RemoveState(EntityStateFlags.Jumping);
        
        if (runner.TryGetComponent(out EntityMotor motor))
        {
            float beforeY = motor.CurrentVelocity.y;
            
            if (beforeY > 0f)
            {
                motor.SetVerticalVelocity(beforeY * jumpCutMultiplier);
                
                Debug.Log($"<color=green>[Jump Debug] 小跳觸發！</color>\n" +
                          $"按鍵持續: <b>{elapsedTime:F3} 秒</b> | 觸發小跳時的瞬間高度: <b>{jumpedHeight:F3} 公尺</b>\n" +
                          $"Y軸速度: {beforeY:F2} -> {motor.CurrentVelocity.y:F2}");
            }
            else
            {
                Debug.Log($"<color=orange>[Jump Debug] 技能自然結束 (未觸發小跳)。</color>\n" +
                          $"技能存活: <b>{elapsedTime:F3} 秒</b> | 抵達最高點的高度: <b>{jumpedHeight:F3} 公尺</b>\n" +
                          $"結束時 Y軸速度: {beforeY:F2}");
            }
        }
    }
}