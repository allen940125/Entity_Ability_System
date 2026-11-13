using UnityEngine;

public class EntityCore : MonoBehaviour
{
    [Header("資料來源 (唯讀藍圖)")]
    [SerializeField] private EntityBlueprintSO _blueprint;

    [SerializeField] private EntityRuntime _runtimeData; 
    public EntityRuntime RuntimeData => _runtimeData;

    // ★ 快取所有依賴此大腦的組件
    private IEntityRuntimeDependent[] _dependents;

    private void Awake()
    {
        _dependents = GetComponentsInChildren<IEntityRuntimeDependent>(true);

        if (_blueprint == null)
        {
            Debug.LogError($"[EntityCore] {name} 沒有指定 Blueprint！", this);
            return;
        }
        _runtimeData = new EntityRuntime();
        _runtimeData.Initialize(_blueprint);
        BroadcastDataChange();
    }
    
    private void Update()
    {
        if (_runtimeData != null)
        {
            float dt = Time.deltaTime; // 快取 deltaTime，避免重複呼叫底層 C++ API

            // 1. 驅動短暫狀態 (無敵幀、暈眩等)
            _runtimeData.TickTemporaryStates(dt);

            // 2. 驅動 Buff 系統 (週期性扣血、屬性增益倒數)
            _runtimeData.TickBuffs(dt);

            // 3. 驅動 特徵系統 (Stamina 體力回復等)
            foreach (var trait in _runtimeData.GetAllTraits()) 
            {
                if (trait is ITickableTrait tickable)
                {
                    tickable.Tick(dt);
                }
            }
        }
    }

    public void InjectRuntimeData(EntityRuntime existingData)
    {
        if (existingData == null) return;
        
        _runtimeData = existingData;
        BroadcastDataChange(); // 替換完成，發送廣播
    }

    // 統一的推播中心
    private void BroadcastDataChange()
    {
        foreach (var dep in _dependents)
        {
            dep.OnRuntimeDataChanged(_runtimeData);
        }
    }
}