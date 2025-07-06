using UnityEngine;
using System.Collections.Generic;

public class PredatorActor : MonoBehaviour
{
    [SerializeField] private GameObject actorPrefab;
    // 捕猎者参数
    [SerializeField] public float moveSpeed = 3f;
    [SerializeField] public int eatCountToReplicate = 3;
    [SerializeField] public float eatRange = 1f;
    [SerializeField] public float chaseRange = 10f;
    
    // 移动变量
    private int _eatenCount;
    private Prey _currentTarget;
    private Vector3 _randomDirection;
    private float _directionTimer;

    private static int predatorActorCount = 0;

    void Start()
    {
        // 初始随机方向
        ChangeRandomDirection();
    }

    void Update()
    {
        // 1. 检查是否有目标
        if (_currentTarget != null && _currentTarget.IsTargeted)
        {
            // 2. 追逐目标
            ChaseTarget();
            
            // 3. 检查是否在捕食范围内
            float distance = Vector3.Distance(transform.position, _currentTarget.transform.position);
            if (distance <= eatRange)
            {
                EatTarget();
            }
            return;
        }
        
        // 4. 没有目标或目标被吃掉，寻找新目标
        FindAndAssignPrey();
        
        // 5. 如果还没找到目标，随机移动
        if (_currentTarget == null)
        {
            RandomMove();
        }
    }

    // 追逐目标
    private void ChaseTarget()
    {
        Vector3 direction = (_currentTarget.transform.position - transform.position).normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;
    }

    // 随机移动
    private void RandomMove()
    {
        _directionTimer += Time.deltaTime;
        if (_directionTimer >= 3f) // 每3秒改变方向
        {
            ChangeRandomDirection();
            _directionTimer = 0f;
        }
        transform.position += _randomDirection * moveSpeed * 0.7f * Time.deltaTime;
    }

    // 改变随机方向
    private void ChangeRandomDirection()
    {
        _randomDirection = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            0
        ).normalized;
    }

    // 寻找并分配猎物
    private void FindAndAssignPrey()
    {
        Prey closest = null;
        float minDistance = float.MaxValue;
        
        // 遍历所有猎物
        foreach (var prey in Prey.AllPrey)
        {
            if (prey == null || prey.IsTargeted) continue;
            
            float distance = Vector3.Distance(transform.position, prey.transform.position);
            if (distance < minDistance && distance <= chaseRange)
            {
                minDistance = distance;
                closest = prey;
            }
        }
        
        // 标记猎物已被锁定
        if (closest != null)
        {
            _currentTarget = closest;
            closest.IsTargeted = true;
        }
    }

    // 吃掉目标
    private void EatTarget()
    {
        if (_currentTarget == null) return;
        
        // 销毁猎物
        Destroy(_currentTarget.gameObject);
        _eatenCount++;
        _currentTarget = null;
        
        // 检查是否需要分裂
        if (_eatenCount >= eatCountToReplicate)
        {
            Replicate();
            _eatenCount = 0;
        }
    }

    // 分裂新捕猎者
    private void Replicate()
    {
        if (predatorActorCount > 100)
        {
            return;
        }
        predatorActorCount += 1;
        // 简单创建新捕猎者
        GameObject newPredator = Instantiate(actorPrefab);
        newPredator.name = $"Dog_{predatorActorCount}";
        newPredator.tag = "Dog";
        
        // 随机位置偏移
        Vector3 offset = new Vector3(
            Random.Range(-2f, 2f),
            Random.Range(-2f, 2f),
            0
        );
        newPredator.transform.position = transform.position + offset;
    }
}