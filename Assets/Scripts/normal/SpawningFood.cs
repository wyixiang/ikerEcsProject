using UnityEngine;

public class SpawningFood : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float spawnInterval = 2f;

    private float _timer;
    static private int foodIndex = 1;

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
        GameObject newActor = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        newActor.name = $"Food_{foodIndex}";
        newActor.tag = "Food";
        foodIndex += 1;
    }
}