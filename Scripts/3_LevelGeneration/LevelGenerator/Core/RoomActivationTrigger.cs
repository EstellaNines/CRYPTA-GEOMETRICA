using UnityEngine;
using CryptaGeometrica.LevelGeneration.SmallRoomV2;
using CryptaGeometrica.PlayerSystem;

namespace CryptaGeometrica.LevelGeneration.MultiRoom
{
    public class RoomActivationTrigger : MonoBehaviour
    {
        public SpawnPointManager spawnManager;
        public bool clearOnExit = false;

        private bool _hasSpawned = false;

        private void Start()
        {
            // 若触发器创建时玩家已经处于房间内，进行一次补偿检测
            TrySpawnIfPlayerInside();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_hasSpawned) return;
            bool isPlayer = other.CompareTag("Player") || other.GetComponent<PlayerController>() != null;
            if (!isPlayer) return;
            if (spawnManager == null) return;

            Debug.Log($"[RoomActivationTrigger] 玩家进入 {gameObject.name}，触发刷怪: {spawnManager.SpawnPoints.Count} 个点");
            // 立即生成，避免延迟造成的误判
            spawnManager.SpawnAllEnemiesImmediate();
            _hasSpawned = true;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            bool isPlayer = other.CompareTag("Player") || other.GetComponent<PlayerController>() != null;
            if (!isPlayer) return;
            if (!clearOnExit) return;
            if (spawnManager == null) return;

            spawnManager.ClearAllSpawnedEnemies();
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            // 防止生成点在创建时玩家已经处于触发器内部而未触发 Enter 的情况
            if (_hasSpawned) return;
            bool isPlayer = other.CompareTag("Player") || other.GetComponent<PlayerController>() != null;
            if (!isPlayer) return;
            if (spawnManager == null) return;

            Debug.Log($"[RoomActivationTrigger] OnTriggerStay2D 检测到玩家在 {gameObject.name} 内，补偿触发刷怪");
            spawnManager.SpawnAllEnemiesImmediate();
            _hasSpawned = true;
        }

        private void TrySpawnIfPlayerInside()
        {
            if (_hasSpawned || spawnManager == null) return;
            var box = GetComponent<BoxCollider2D>();
            if (box == null) return;

            // 使用 OverlapBoxAll 检查当前是否已有玩家在房间区域内
            var hits = Physics2D.OverlapBoxAll(box.bounds.center, box.bounds.size, 0f);
            foreach (var h in hits)
            {
                if (h == null) continue;
                bool isPlayer = h.CompareTag("Player") || h.GetComponent<PlayerController>() != null;
                if (isPlayer)
                {
                    Debug.Log($"[RoomActivationTrigger] 初始检测到玩家在 {gameObject.name} 内，立即刷怪");
                    spawnManager.SpawnAllEnemiesImmediate();
                    _hasSpawned = true;
                    break;
                }
            }
        }
    }
}
