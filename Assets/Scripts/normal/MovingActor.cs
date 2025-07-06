using UnityEngine;

public class MovingActor : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3f;          // 移动速度
    [SerializeField] private float directionChangeInterval = 2f; // 方向变化间隔
    [SerializeField] private bool useZAxis = false;          // 是否允许 Z 轴移动（比如飞行单位）

    private Vector3 _randomDirection;
    private float _timer;

    void Start()
    {
        ChangeDirection();
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= directionChangeInterval)
        {
            ChangeDirection();
            _timer = 0f;
        }

        // 移动
        transform.Translate(_randomDirection * moveSpeed * Time.deltaTime, Space.World);
    }

    private void ChangeDirection()
    {
        float z = useZAxis ? Random.Range(-1f, 1f) : 0f; // 控制是否启用 Y 轴移动
        _randomDirection = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            z
        ).normalized; // 确保方向向量长度为 1
    }
}