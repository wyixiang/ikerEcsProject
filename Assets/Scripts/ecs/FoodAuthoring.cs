using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

// 组件定义
public struct Food : IComponentData {} // 仅作为标记使用

// 食物Authoring（仅标记）
public class FoodAuthoring : MonoBehaviour
{
    class Baker : Baker<FoodAuthoring>
    {
        public override void Bake(FoodAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<Food>(entity);
        }
    }
}