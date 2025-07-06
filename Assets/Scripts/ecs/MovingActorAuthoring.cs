using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using UnityEngine;
using Random = Unity.Mathematics.Random;
using Unity.Burst;

// 移动组件数据（仅2D）
public struct MovingActorData : IComponentData
{
    public float MoveSpeed;
    public float DirectionChangeInterval;
    public float2 RandomDirection; // 改为float2
    public float Timer;
}

// 摄像机边界（仅2D）
public struct CameraBounds : IComponentData
{
    public float2 Min; // 最小边界(x,y)
    public float2 Max; // 最大边界(x,y)
}

// 移动Actor标签
public struct MovingActorTag : IComponentData {}

// Authoring组件（用于编辑器配置）
public class MovingActorAuthoring : MonoBehaviour
{
    public float moveSpeed = 3f;
    public float directionChangeInterval = 2f;

    class Baker : Baker<MovingActorAuthoring>
    {
        public override void Bake(MovingActorAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            
            // 初始化随机方向（仅XY平面）
            var random = Random.CreateFromIndex((uint)entity.Index);
            float2 initialDirection = math.normalize(new float2(
                random.NextFloat(-1f, 1f),
                random.NextFloat(-1f, 1f)
            ));

            AddComponent(entity, new MovingActorData
            {
                MoveSpeed = authoring.moveSpeed,
                DirectionChangeInterval = authoring.directionChangeInterval,
                RandomDirection = initialDirection,
                Timer = 0f
            });
            AddComponent<MovingActorTag>(entity);
        }
    }
}

// 移动系统（仅2D）
[BurstCompile]
[UpdateInGroup(typeof(TransformSystemGroup))]
public partial struct MovingActorSystem : ISystem
{
    private Random _random;
    private CameraBounds _cameraBounds;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        _random = Random.CreateFromIndex((uint)233);
        
        // 初始化默认边界
        _cameraBounds = new CameraBounds {
            Min = new float2(-10, -10),
            Max = new float2(10, 10)
        };
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        UpdateCameraBounds(ref state);
        float deltaTime = SystemAPI.Time.DeltaTime;

        new MoveJob
        {
            DeltaTime = deltaTime,
            Random = _random,
            Bounds = _cameraBounds
        }.ScheduleParallel();
    }

    // 更新摄像机边界
    private void UpdateCameraBounds(ref SystemState state)
    {
        if (SystemAPI.TryGetSingleton<CameraBounds>(out var bounds))
        {
            _cameraBounds = bounds;
        }
    }

    [BurstCompile]
    [WithAll(typeof(MovingActorTag))]
    public partial struct MoveJob : IJobEntity
    {
        public float DeltaTime;
        public Random Random;
        public CameraBounds Bounds;

        private void Execute(ref LocalTransform transform, ref MovingActorData movingData)
        {
            // 更新计时器和方向
            movingData.Timer += DeltaTime;
            if (movingData.Timer >= movingData.DirectionChangeInterval)
            {
                movingData.RandomDirection = math.normalize(new float2(
                    Random.NextFloat(-1f, 1f),
                    Random.NextFloat(-1f, 1f)
                ));
                movingData.Timer = 0f;
            }

            // 计算新位置（Z轴固定为0）
            float3 currentPos = transform.Position;
            float3 newPos = transform.Position + 
                new float3(movingData.RandomDirection.x, movingData.RandomDirection.y, 0) * 
                movingData.MoveSpeed * DeltaTime;

            // 边界检查和方向修正
            bool hitBoundary = false;
            
            if (newPos.x <= Bounds.Min.x || newPos.x >= Bounds.Max.x)
            {
                movingData.RandomDirection.x *= -1; // X轴反向
                hitBoundary = true;
            }
            
            if (newPos.y <= Bounds.Min.y || newPos.y >= Bounds.Max.y)
            {
                movingData.RandomDirection.y *= -1; // Y轴反向
                hitBoundary = true;
            }

            // 如果碰到边界或者需要改变方向，重新计算方向
            if (hitBoundary)
            {
                movingData.RandomDirection = GetNewDirection(currentPos, Bounds);
                movingData.Timer = 0f;
                newPos = transform.Position + 
                                new float3(movingData.RandomDirection.x, movingData.RandomDirection.y, 0) * 
                                movingData.MoveSpeed * DeltaTime;
            }
            
            newPos.x = math.clamp(newPos.x, Bounds.Min.x, Bounds.Max.x);
            newPos.y = math.clamp(newPos.y, Bounds.Min.y, Bounds.Max.y);
            // 确保Z轴为0并应用新位置
            newPos.z = 0;

            transform.Position = newPos;
        }

        private float2 GetNewDirection(float3 currentPos, CameraBounds bounds)
        {
            return math.normalize(Random.NextFloat2Direction());
            // 计算指向屏幕中心的方向（更自然的转向）
            float2 center = (bounds.Min + bounds.Max) * 0.5f;
            float2 toCenter = math.normalize(center - new float2(currentPos.x, currentPos.y));
            
            // 混合随机方向和中心方向（50%概率朝向中心）
            if (Random.NextFloat() > 0.5f)
            {
                return math.normalize(toCenter + Random.NextFloat2Direction() * 0.5f);
            }
            return math.normalize(Random.NextFloat2Direction());
            
            
        }
    }
}