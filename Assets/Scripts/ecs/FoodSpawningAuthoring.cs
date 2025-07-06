using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;
using UnityEngine;
using Random = Unity.Mathematics.Random;
using Unity.Mathematics;

// 食物标记组件

// 2. 生成器配置组件
public struct FoodSpawner : IComponentData
{
    public Entity Prefab;
    public float SpawnInterval;
    public float NextSpawnTime;
}

// 3. Authoring组件（编辑器配置）
public class FoodSpawningAuthoring : MonoBehaviour
{
    public GameObject FoodPrefab;
    public float SpawnInterval = 2f;

    class Baker : Baker<FoodSpawningAuthoring>
    {
        public override void Bake(FoodSpawningAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            var prefabEntity = GetEntity(authoring.FoodPrefab, TransformUsageFlags.Dynamic);
            AddComponent(entity, new FoodSpawner
            {
                Prefab = prefabEntity,
                SpawnInterval = authoring.SpawnInterval,
                NextSpawnTime = 0
            });
        }
    }
}

// 4. 食物生成系统（修正继承语法）
public partial struct FoodSpawningSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI
            .GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged)
            .AsParallelWriter();
        
        new SpawnJob
        {
            Ecb = ecb,
            CurrentTime = (float)SystemAPI.Time.ElapsedTime,
            Seed = (uint)SystemAPI.Time.ElapsedTime + 1
        }.ScheduleParallel();
    }

    public partial struct SpawnJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Ecb;
        public float CurrentTime;
        public uint Seed;

        private void Execute(
            [EntityIndexInQuery] int entityInQueryIndex,
            ref FoodSpawner spawner,
            in LocalTransform transform)
        {
            if (CurrentTime >= spawner.NextSpawnTime)
            {
                // 使用构造函数初始化Random
                Random random = new Random(Seed + (uint)entityInQueryIndex);
                
                spawner.NextSpawnTime = CurrentTime + spawner.SpawnInterval;

                Entity food = Ecb.Instantiate(entityInQueryIndex, spawner.Prefab);
                
                // 生成随机方向（单位向量）
                float3 randomDirection = math.normalize(random.NextFloat3Direction());
                
                //Ecb.SetComponent(entityInQueryIndex, food, new Food());
                Ecb.SetComponent(entityInQueryIndex, food, 
                    LocalTransform.FromPositionRotation(
                        transform.Position + randomDirection * 2f,
                        transform.Rotation
                    ));
            }
        }
    }
}