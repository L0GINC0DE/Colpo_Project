using UnityEngine;

public class DebuffSpawn : MonoBehaviour
{
    [SerializeField] private GameObject debuffPrefab;   // 스폰할 프리팹
    [SerializeField] private float spawnInterval = 3f;  // 스폰 간격(초)

    [Header("스폰 범위 (자기 자신 위치 기준)")]
    [SerializeField] private Vector3 spawnRange = new Vector3(5f, 0f, 5f); // X, Y, Z 각 방향 최대 거리

    private float timer;

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            timer = 0f;
            Spawn();
        }
    }

    private void Spawn()
    {
        Vector3 randomOffset = new Vector3(
            Random.Range(-spawnRange.x, spawnRange.x),
            Random.Range(-spawnRange.y, spawnRange.y),
            Random.Range(-spawnRange.z, spawnRange.z)
        );

        Vector3 spawnPos = transform.position + randomOffset;

        Instantiate(debuffPrefab, spawnPos, Quaternion.identity);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, spawnRange * 2f);
    }
}
