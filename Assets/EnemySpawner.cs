using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public float spawnInterval = 3f;

    public NoiseVoxelMap voxelMap; // 지형 참조

    private float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            // 맵 범위 내에서 랜덤 좌표 선택
            int spawnX = Random.Range(0, voxelMap.width);
            int spawnZ = Random.Range(0, voxelMap.depth);

            // 지형 높이 계산
            float nx = (spawnX + voxelMap.offsetX) / voxelMap.noiseScale;
            float nz = (spawnZ + voxelMap.offsetZ) / voxelMap.noiseScale;
            float noise = Mathf.PerlinNoise(nx, nz);
            int height = Mathf.FloorToInt(noise * voxelMap.maxHeight);

            Vector3 spawnPos = new Vector3(spawnX, height + 1, spawnZ);
            Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

            timer = 0f;
        }
    }
}