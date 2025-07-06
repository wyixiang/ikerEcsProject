using UnityEngine;
using TMPro;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;

public class GameStatsUIEcs : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI statsText;
    public float updateInterval = 0.5f;

    private float _lastUpdateTime;
    private World _world;
    private EntityQuery _actorQuery;
    private EntityQuery _predatorQuery;
    private EntityQuery _foodQuery;

    void Start()
    {
        // 获取默认的ECS World
        _world = World.DefaultGameObjectInjectionWorld;
        
        // 创建EntityQuery
        var entityManager = _world.EntityManager;
        _actorQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<MovingActorData, LocalTransform>()
            .Build(entityManager);
            
        _predatorQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<Predator, LocalTransform>()
            .Build(entityManager);
            
        _foodQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<Food, LocalTransform>()
            .Build(entityManager);
    }

    void Update()
    {
        if (Time.time - _lastUpdateTime < updateInterval)
            return;

        // 获取实体数量
        int actorCount = _actorQuery.CalculateEntityCount();
        int predatorCount = _predatorQuery.CalculateEntityCount();
        int foodCount = _foodQuery.CalculateEntityCount();
        
        // 更新UI文本
        statsText.text = $"Cat: {actorCount} | Dog: {predatorCount} | Food: {foodCount}";
        
        _lastUpdateTime = Time.time;
    }

    void OnDestroy()
    {
        // 清理EntityQuery
        if (_world != null && _world.IsCreated)
        {
            _actorQuery.Dispose();
            _predatorQuery.Dispose();
            _foodQuery.Dispose();
        }
    }
}