using UnityEngine;

public class MovingActor : MonoBehaviour
{
    [Header("移动设置")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float directionChangeInterval = 2f;
    [SerializeField] private float screenPadding = 0.5f; // 距离屏幕边缘的缓冲距离

    private Vector2 _randomDirection;
    private float _timer;
    private Camera _mainCamera;
    private float _minX, _maxX, _minY, _maxY;

    void Start()
    {
        _mainCamera = Camera.main;
        CalculateScreenBounds();
        ChangeDirection();
        LockZPosition(); // 初始锁定Z轴
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= directionChangeInterval)
        {
            ChangeDirection();
            _timer = 0f;
        }

        MoveActor();
        CheckBounds();
        LockZPosition(); // 每帧确保Z轴为0
    }

    private void CalculateScreenBounds()
    {
        Vector3 bottomLeft = _mainCamera.ViewportToWorldPoint(new Vector3(0, 0, 0));
        Vector3 topRight = _mainCamera.ViewportToWorldPoint(new Vector3(1, 1, 0));

        // 考虑物体自身大小
        float halfWidth = transform.localScale.x / 2;
        float halfHeight = transform.localScale.y / 2;
        
        _minX = bottomLeft.x + halfWidth + screenPadding;
        _maxX = topRight.x - halfWidth - screenPadding;
        _minY = bottomLeft.y + halfHeight + screenPadding;
        _maxY = topRight.y - halfHeight - screenPadding;
    }

    private void MoveActor()
    {
        transform.Translate(
            new Vector3(_randomDirection.x, _randomDirection.y, 0) * 
            moveSpeed * Time.deltaTime, 
            Space.World
        );
    }

    private void CheckBounds()
    {
        Vector3 pos = transform.position;
        bool hitBoundary = false;

        // 水平边界检查与反弹
        if (pos.x < _minX || pos.x > _maxX)
        {
            _randomDirection.x *= -1;
            hitBoundary = true;
        }

        // 垂直边界检查与反弹
        if (pos.y < _minY || pos.y > _maxY)
        {
            _randomDirection.y *= -1;
            hitBoundary = true;
        }

        // 修正位置确保不会卡在边界外
        transform.position = new Vector3(
            Mathf.Clamp(pos.x, _minX, _maxX),
            Mathf.Clamp(pos.y, _minY, _maxY),
            0 // 强制Z轴为0
        );

        // 碰到边界时重新标准化方向向量
        if (hitBoundary)
        {
            _randomDirection.Normalize();
        }
    }

    private void ChangeDirection()
    {
        _randomDirection = new Vector2(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f)
        ).normalized;
    }

    private void LockZPosition()
    {
        // 强制锁定Z轴位置
        if (transform.position.z != 0)
        {
            transform.position = new Vector3(
                transform.position.x,
                transform.position.y,
                0
            );
        }
    }

    // 调试用：在Scene视图显示移动边界
    private void OnDrawGizmosSelected()
    {
        if (_mainCamera == null) return;
        
        Vector3 center = new Vector3(
            (_minX + _maxX) / 2,
            (_minY + _maxY) / 2,
            0
        );
        
        Vector3 size = new Vector3(
            _maxX - _minX,
            _maxY - _minY,
            0.1f
        );
        
        Gizmos.color = new Color(0, 1, 1, 0.3f);
        Gizmos.DrawCube(center, size);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(center, size);
    }
}