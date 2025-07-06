using UnityEngine;

public class SpawningFood : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float spawnInterval = 2f;

    private float _timer;

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= spawnInterval)
        {
            SpawnProjectile();
            _timer = 0f;
        }
    }

    private void SpawnProjectile()
    {
        GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
    }
}