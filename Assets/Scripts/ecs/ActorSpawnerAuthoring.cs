using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

// 组件定义
public struct ActorSpawnerData : IComponentData
{
    public Entity ActorPrefab;
    public Entity PredatorPrefab;
    public int SpawnCount;
    public float3 SpawnAreaSize;
    public float3 SpawnCenter;
}

// Authoring组件
public class ActorSpawnerAuthoring : MonoBehaviour
{
    public GameObject ActorPrefab;
    public int SpawnCount = 10;
    public Vector3 SpawnAreaSize = new Vector3(10f, 0f, 10f);
    public GameObject PredatorPrefab;
    
    class Baker : Baker<ActorSpawnerAuthoring>
    {
        public override void Bake(ActorSpawnerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new ActorSpawnerData
            {
                ActorPrefab = GetEntity(authoring.ActorPrefab, TransformUsageFlags.Dynamic),
                PredatorPrefab = GetEntity(authoring.PredatorPrefab, TransformUsageFlags.Dynamic),
                SpawnCount = authoring.SpawnCount,
                SpawnAreaSize = authoring.SpawnAreaSize,
                SpawnCenter = authoring.transform.position
            });
        }
    }
}

// 生成系统
[BurstCompile]
public partial struct ActorSpawnerSystem : ISystem
{
    private Random _random;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        _random = Random.CreateFromIndex((uint)233);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        
        if (!SystemAPI.TryGetSingleton<ActorSpawnerData>(out var spawner)) 
            return;
        
        // 生成普通Actor
        for (int i = 0; i < spawner.SpawnCount; i++)
        {
            float3 randomPosition = spawner.SpawnCenter + new float3(
                _random.NextFloat(-spawner.SpawnAreaSize.x / 2, spawner.SpawnAreaSize.x / 2),
                _random.NextFloat(-spawner.SpawnAreaSize.y / 2, spawner.SpawnAreaSize.y / 2),
                _random.NextFloat(-spawner.SpawnAreaSize.z / 2, spawner.SpawnAreaSize.z / 2)
            );
            
            Entity newActor = ecb.Instantiate(spawner.ActorPrefab);
            ecb.SetComponent(newActor, LocalTransform.FromPosition(randomPosition));
        }
        
        // 生成初始捕食者
        Entity predator = ecb.Instantiate(spawner.PredatorPrefab);
        ecb.SetComponent(predator, LocalTransform.FromPosition(float3.zero));
        
        // 禁用自身系统（只生成一次）
        state.Enabled = false;
    }
}