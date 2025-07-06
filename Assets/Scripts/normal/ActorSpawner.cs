using UnityEngine;

public class ActorSpawner : MonoBehaviour
{
    [Header("生成设置")]
    [SerializeField] private GameObject actorPrefab;
    [SerializeField] private int spawnCount = 10;
    [SerializeField] private Vector2 spawnAreaSize = new Vector2(10f, 10f); // 仅使用X和Y
    
    [Header("捕食者设置")] 
    [SerializeField] private GameObject predatorPrefab;

    void Start()
    {
        SpawnActors();
    }

    // 生成Actor
    public void SpawnActors()
    {
        // 生成普通Actor
        for (int i = 0; i < spawnCount; i++)
        {
            Vector2 randomPos = (Vector2)transform.position + new Vector2(
                Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
                Random.Range(-spawnAreaSize.y / 2, spawnAreaSize.y / 2)
            );
            
            Instantiate(actorPrefab, randomPos, Quaternion.identity).name = $"Cat_{i}";
        }
        
        // 生成初始捕食者（居中）
        Instantiate(predatorPrefab, Vector3.zero, Quaternion.identity);
    }

    // 可视化生成区域（仅在编辑器可见）
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, new Vector3(spawnAreaSize.x, spawnAreaSize.y, 0));
    }
}