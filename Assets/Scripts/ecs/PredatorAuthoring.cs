using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using UnityEngine;
using Random = Unity.Mathematics.Random;

// 组件定义
// 新增组件
public struct FoodLocked : IComponentData
{
    public Entity LockedBy; // 锁定该食物的捕食者实体
}

// 新增全局计数器组件
public struct PredatorCount : IComponentData
{
    public int Value;
}

public struct Predator : IComponentData
{
    public Entity TargetFood;      // 当前目标食物
    public float MoveSpeed;       // 移动速度
    public float EatRange;        // 进食范围
    public int FoodEaten;         // 已吃食物数量
    public int FoodToReproduce;   // 繁殖所需食物量
    public float3 RandomDirection; // 随机移动方向
}

public struct ReproductionRequest : IComponentData
{
    public float3 SpawnPosition;
}

// Authoring组件（编辑器配置）
public class PredatorAuthoring : MonoBehaviour
{
    public float MoveSpeed = 3f;
    public float EatRange = 1f;
    public int FoodToReproduce = 3;
    
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



// 捕食者系统（完整逻辑）
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct PredatorSystem : ISystem
{
    private EntityQuery _foodQuery;
    private EntityQuery _predatorQuery;
    private Random _random;
    private CameraBounds _cameraBounds;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        _foodQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<Food>()                  // 必须包含Food组件
            .WithAll<LocalTransform>()        // 必须包含LocalTransform组件
            .Build(ref state);
        _random = Random.CreateFromIndex((uint)System.DateTime.Now.Ticks);
        _predatorQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<Predator>()
            .Build(ref state);
        
        state.EntityManager.CreateEntity(typeof(PredatorCount));
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
        
        // 获取所有食物实体和位置
        var foodEntities = _foodQuery.ToEntityArray(Allocator.TempJob);
        var foodTransforms = _foodQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);
        var foodLockedLookup = SystemAPI.GetComponentLookup<FoodLocked>(false);

        // 第一阶段：寻找目标
        new FindTargetJob {
            FoodEntities = foodEntities,
            FoodTransforms = foodTransforms,
            FoodLockedLookup = foodLockedLookup,
            Ecb = ecb
        }.ScheduleParallel();

        // 第二阶段：移动和进食
        new MoveAndEatJob
        {
            DeltaTime = deltaTime,
            FoodEntities = foodEntities,
            FoodTransforms = foodTransforms,
            FoodLockedLookup = foodLockedLookup,
            Bounds = _cameraBounds,
            Ecb = ecb
        }.ScheduleParallel();

        // 获取当前捕食者数量
        int currentCount = _predatorQuery.CalculateEntityCount();
        if (currentCount >= 100) return; // 达到上限直接返回
        
        // 第三阶段：繁殖
        new ReproduceJob
        {
            Ecb = ecb,
            Random = _random,
            CurrentCount = currentCount
        }.Schedule();
    }

    private void UpdateCameraBounds(ref SystemState state)
    {
        if (Camera.main == null) return;
        
        Vector3 min = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, 0));
        Vector3 max = Camera.main.ViewportToWorldPoint(new Vector3(1, 1, 0));
        
        _cameraBounds = new CameraBounds {
            Min = new float2(min.x, min.y),
            Max = new float2(max.x, max.y)
        };
    }

    [BurstCompile]
    [WithAll(typeof(Predator))]
    public partial struct FindTargetJob : IJobEntity
    {
        [ReadOnly] public NativeArray<Entity> FoodEntities;
        [ReadOnly] public NativeArray<LocalTransform> FoodTransforms;
        [ReadOnly] public ComponentLookup<FoodLocked> FoodLockedLookup;
        public EntityCommandBuffer.ParallelWriter Ecb;

        private void Execute(
            [EntityIndexInQuery] int index,
            Entity entity,
            ref Predator predator)
        {
            // 已有有效目标则跳过
            if (predator.TargetFood != Entity.Null && 
                FoodEntities.Contains(predator.TargetFood))
                return;

            // 寻找最近食物
            float minDist = float.MaxValue;
            Entity closestFood = Entity.Null;
            
            for (int i = 0; i < FoodEntities.Length; i++)
            {
                if (FoodLockedLookup.HasComponent(FoodEntities[i])) 
                    continue; // 跳过已锁定食物
                float dist = math.lengthsq(FoodTransforms[i].Position - predator.RandomDirection);
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

        private void Execute(
            [EntityIndexInQuery] int index,
            Entity entity,
            ref Predator predator,
            ref LocalTransform transform)
        {
            // 追逐目标
            if (predator.TargetFood != Entity.Null && 
                FoodEntities.Contains(predator.TargetFood))
            {
                // 检查目标是否仍被自己锁定
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

                    // 检查进食距离
                    if (math.distance(transform.Position, targetPos) <= predator.EatRange)
                    {
                        // 进食时释放锁定
                        Ecb.RemoveComponent<FoodLocked>(index, predator.TargetFood);
                        Ecb.DestroyEntity(index, predator.TargetFood);
                        predator.FoodEaten++;
                        predator.TargetFood = Entity.Null;

                        // 检查繁殖条件
                        if (predator.FoodEaten == predator.FoodToReproduce)
                        {
                            Ecb.AddComponent(index, entity, 
                                new ReproductionRequest { SpawnPosition = transform.Position });
                            predator.FoodEaten = 0;
                            Ecb.SetComponent(index, entity, predator);
                        }
                    }
                }
                
            }
            else // 随机移动
            {
                transform.Position += predator.RandomDirection * predator.MoveSpeed * 0.5f * DeltaTime;
                // 计算新位置（Z轴固定为0）
                float3 currentPos = transform.Position;
                float3 newPos = transform.Position + 
                                new float3(predator.RandomDirection.x, predator.RandomDirection.y, 0) * 
                                predator.MoveSpeed * DeltaTime;
                // 边界检查和方向修正
                bool hitBoundary = false;
            
                if (newPos.x <= Bounds.Min.x || newPos.x >= Bounds.Max.x)
                {
                    predator.RandomDirection.x *= -1; // X轴反向
                    hitBoundary = true;
                    newPos.x = math.clamp(newPos.x, Bounds.Min.x, Bounds.Max.x);
                }
            
                if (newPos.y <= Bounds.Min.y || newPos.y >= Bounds.Max.y)
                {
                    predator.RandomDirection.y *= -1; // Y轴反向
                    hitBoundary = true;
                    newPos.y = math.clamp(newPos.y, Bounds.Min.y, Bounds.Max.y);
                }
            }
        }
    }

    [BurstCompile]
    public partial struct ReproduceJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Ecb;
        public Random Random;
        public int CurrentCount;

        private void Execute(
            [EntityIndexInQuery] int index,
            Entity entity,
            in ReproductionRequest request,
            in Predator predator)
        {
            if (CurrentCount >= 100) return; // 数量检查
            
            Ecb.RemoveComponent<ReproductionRequest>(index, entity);
            Entity newPredator = Ecb.Instantiate(index, entity);
            float3 offset = Random.NextFloat3Direction() * 2f;
            Ecb.SetComponent(index, newPredator, 
                LocalTransform.FromPosition(request.SpawnPosition + offset));
            Ecb.SetComponent(index, newPredator, new Predator {
                MoveSpeed = 3f, // 明确设置基础值
                EatRange = 1f,
                FoodToReproduce = 3, // 固定值（避免异常继承）
                FoodEaten = 0, // 必须重置
                TargetFood = Entity.Null,
                RandomDirection = math.normalize(Random.NextFloat3())
            });
        }
    }
}


// 计数器更新系统
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(PredatorSystem))]
public partial struct PredatorCountSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var predatorQuery = SystemAPI.QueryBuilder().WithAll<Predator>().Build();
        int count = predatorQuery.CalculateEntityCount();
        
        var counter = SystemAPI.GetSingletonRW<PredatorCount>();
        counter.ValueRW.Value = count;
    }
}