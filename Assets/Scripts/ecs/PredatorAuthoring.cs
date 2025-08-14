using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using UnityEngine;
using Random = Unity.Mathematics.Random;

// 组件定义
public struct FoodLocked : IComponentData
{
    public Entity LockedBy;
}

public struct PredatorCount : IComponentData
{
    public int Value;
}

public struct Predator : IComponentData
{
    public Entity TargetFood;
    public float MoveSpeed;
    public float EatRange;
    public int FoodEaten;
    public int FoodToReproduce;
    public float3 RandomDirection;
}

public struct ReproductionRequest : IComponentData
{
    public float3 SpawnPosition;
}

// Authoring组件
public class PredatorAuthoring : MonoBehaviour
{
    public float MoveSpeed = 3f;
    public float EatRange = 1f;
    public int FoodToReproduce = 1;
    
    class Baker : Baker<PredatorAuthoring>
    {
        public override void Bake(PredatorAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Predator
            {
                MoveSpeed = authoring.MoveSpeed,
                EatRange = authoring.EatRange,
                FoodToReproduce = authoring.FoodToReproduce,
                FoodEaten = 0,
                TargetFood = Entity.Null,
                RandomDirection = math.normalize(new float3(
                    UnityEngine.Random.Range(-1f, 1f),
                    UnityEngine.Random.Range(-1f, 1f),
                    0
                ))
            });
        }
    }
}

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct PredatorSystem : ISystem
{
    private EntityQuery _foodQuery;
    private EntityQuery _predatorQuery;
    private CameraBounds _cameraBounds;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        _foodQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<Food>()
            .WithAll<LocalTransform>()
            .Build(ref state);
            
        _predatorQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<Predator>()
            .Build(ref state);
        
        var countEntity = state.EntityManager.CreateEntity();
        state.EntityManager.AddComponent<PredatorCount>(countEntity);
        state.EntityManager.SetComponentData(countEntity, new PredatorCount { Value = 0 });
        
        _cameraBounds = new CameraBounds {
            Min = new float2(-10, -10),
            Max = new float2(10, 10)
        };
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        UpdateCameraBounds(ref state);
        
        var ecb = SystemAPI
            .GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged)
            .AsParallelWriter();

        float deltaTime = SystemAPI.Time.DeltaTime;
        
        // 获取食物数据
        var foodEntities = _foodQuery.ToEntityArray(Allocator.TempJob);
        var foodTransforms = _foodQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);
        var foodLockedLookup = SystemAPI.GetComponentLookup<FoodLocked>(false);

        // 使用基于时间的随机种子
        uint randomSeed = (uint)(SystemAPI.Time.ElapsedTime * 1000);
        
        // 第一阶段：寻找目标
        new FindTargetJob {
            FoodEntities = foodEntities,
            FoodTransforms = foodTransforms,
            FoodLockedLookup = foodLockedLookup,
            Ecb = ecb,
            RandomSeed = randomSeed
        }.ScheduleParallel();

        // 第二阶段：移动和进食
        new MoveAndEatJob
        {
            DeltaTime = deltaTime,
            FoodEntities = foodEntities,
            FoodTransforms = foodTransforms,
            FoodLockedLookup = foodLockedLookup,
            Bounds = _cameraBounds,
            Ecb = ecb,
            RandomSeed = randomSeed + 1 // 不同种子
        }.ScheduleParallel();

        // 第三阶段：繁殖
        int currentCount = _predatorQuery.CalculateEntityCount();
        if (currentCount < 10000)
        {
            new ReproduceJob {
                Ecb = ecb,
                RandomSeed = randomSeed + 2,
                CurrentCount = currentCount
            }.Schedule();
        }
        
        // 更新计数器
        state.EntityManager.SetComponentData(
            SystemAPI.GetSingletonEntity<PredatorCount>(),
            new PredatorCount { Value = currentCount }
        );
    }

    private void UpdateCameraBounds(ref SystemState state)
    {
        if (SystemAPI.TryGetSingleton<CameraBounds>(out var bounds))
        {
            _cameraBounds = bounds;
        }
    }

    [BurstCompile]
    [WithAll(typeof(Predator))]
    public partial struct FindTargetJob : IJobEntity
    {
        [ReadOnly] public NativeArray<Entity> FoodEntities;
        [ReadOnly] public NativeArray<LocalTransform> FoodTransforms;
        [ReadOnly] public ComponentLookup<FoodLocked> FoodLockedLookup;
        public EntityCommandBuffer.ParallelWriter Ecb;
        public uint RandomSeed;

        private void Execute(
            [EntityIndexInQuery] int index,
            Entity entity,
            ref Predator predator,
            in LocalTransform transform)
        {
            // 已有有效目标则跳过
            if (predator.TargetFood != Entity.Null && 
                FoodEntities.Contains(predator.TargetFood))
                return;

            // 创建线程安全的Random实例
            var random = Random.CreateFromIndex(RandomSeed + (uint)index);
            
            // 寻找最近食物
            float minDist = float.MaxValue;
            Entity closestFood = Entity.Null;
            
            for (int i = 0; i < FoodEntities.Length; i++)
            {
                if (FoodLockedLookup.HasComponent(FoodEntities[i])) 
                    continue;
                    
                // 修正：使用transform.Position而不是RandomDirection
                float dist = math.lengthsq(FoodTransforms[i].Position - transform.Position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closestFood = FoodEntities[i];
                }
            }

            if (closestFood != Entity.Null)
            {
                Ecb.AddComponent(index, closestFood, new FoodLocked { LockedBy = entity });
                predator.TargetFood = closestFood;
                
                // 更新随机方向（朝向目标）
                predator.RandomDirection = math.normalize(
                    FoodTransforms[FoodEntities.IndexOf(closestFood)].Position - transform.Position
                );
                
                Ecb.SetComponent(index, entity, predator);
            }
            else
            {
                // 没有找到食物时更新随机方向
                predator.RandomDirection = math.normalize(new float3(
                    random.NextFloat(-1f, 1f),
                    random.NextFloat(-1f, 1f),
                    0
                ));
                Ecb.SetComponent(index, entity, predator);
            }
        }
    }

    [BurstCompile]
    [WithAll(typeof(Predator))]
    public partial struct MoveAndEatJob : IJobEntity
    {
        [ReadOnly] public NativeArray<Entity> FoodEntities;
        [ReadOnly] public NativeArray<LocalTransform> FoodTransforms;
        [ReadOnly] public ComponentLookup<FoodLocked> FoodLockedLookup;
        public float DeltaTime;
        public CameraBounds Bounds;
        public EntityCommandBuffer.ParallelWriter Ecb;
        public uint RandomSeed;

        private void Execute(
            [EntityIndexInQuery] int index,
            Entity entity,
            ref Predator predator,
            ref LocalTransform transform)
        {
            var random = Random.CreateFromIndex(RandomSeed + (uint)index);
            
            // 追逐目标
            if (predator.TargetFood != Entity.Null && 
                FoodEntities.Contains(predator.TargetFood))
            {
                if (!FoodLockedLookup.TryGetComponent(predator.TargetFood, out var locked) || 
                    locked.LockedBy != entity)
                {
                    predator.TargetFood = Entity.Null;
                    return;
                }
                
                int targetIndex = FoodEntities.IndexOf(predator.TargetFood);
                if (targetIndex != -1) 
                {
                    float3 targetPos = FoodTransforms[targetIndex].Position;
                    float3 dir = math.normalize(targetPos - transform.Position);
                    transform.Position += dir * predator.MoveSpeed * DeltaTime;

                    if (math.distance(transform.Position, targetPos) <= predator.EatRange)
                    {
                        Ecb.RemoveComponent<FoodLocked>(index, predator.TargetFood);
                        Ecb.DestroyEntity(index, predator.TargetFood);
                        predator.FoodEaten++;
                        predator.TargetFood = Entity.Null;

                        if (predator.FoodEaten >= predator.FoodToReproduce)
                        {
                            Ecb.AddComponent(index, entity, 
                                new ReproductionRequest { 
                                    SpawnPosition = transform.Position 
                                });
                            predator.FoodEaten = 0;
                        }
                        Ecb.SetComponent(index, entity, predator);
                    }
                }
            }
            else // 随机移动
            {
                transform.Position += predator.RandomDirection * predator.MoveSpeed * DeltaTime;
                
                // 边界检查和方向修正
                bool hitBoundary = false;
                float3 newPos = transform.Position;
                
                if (newPos.x <= Bounds.Min.x || newPos.x >= Bounds.Max.x)
                {
                    predator.RandomDirection.x *= -1;
                    hitBoundary = true;
                    newPos.x = math.clamp(newPos.x, Bounds.Min.x, Bounds.Max.x);
                }
            
                if (newPos.y <= Bounds.Min.y || newPos.y >= Bounds.Max.y)
                {
                    predator.RandomDirection.y *= -1;
                    hitBoundary = true;
                    newPos.y = math.clamp(newPos.y, Bounds.Min.y, Bounds.Max.y);
                }
                
                if (hitBoundary)
                {
                    transform.Position = newPos;
                    Ecb.SetComponent(index, entity, predator);
                }
            }
        }
    }

    [BurstCompile]
    public partial struct ReproduceJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Ecb;
        public uint RandomSeed;
        public int CurrentCount;

        private void Execute(
            [EntityIndexInQuery] int index,
            Entity entity,
            in ReproductionRequest request,
            in Predator predator)
        {
            if (CurrentCount >= 10000) return;
            
            var random = Random.CreateFromIndex(RandomSeed + (uint)index);
            
            Ecb.RemoveComponent<ReproductionRequest>(index, entity);
            Entity newPredator = Ecb.Instantiate(index, entity);
            
            float3 offset = random.NextFloat3Direction() * 2f;
            offset.z = 0; // 确保Z轴为0
            
            Ecb.SetComponent(index, newPredator, 
                LocalTransform.FromPosition(request.SpawnPosition + offset));
                
            Ecb.SetComponent(index, newPredator, new Predator {
                MoveSpeed = predator.MoveSpeed,
                EatRange = predator.EatRange,
                FoodToReproduce = predator.FoodToReproduce,
                FoodEaten = 0,
                TargetFood = Entity.Null,
                RandomDirection = math.normalize(random.NextFloat3())
            });
        }
    }
}