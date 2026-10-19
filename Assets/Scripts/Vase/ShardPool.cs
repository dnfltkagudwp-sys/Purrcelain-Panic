using System.Collections.Generic;
using UnityEngine;

namespace PurrcelainPanic
{
    /// <summary>파편 세트를 종류별로 미리 만들어 두고 돌려 쓴다.</summary>
    public class ShardPool : MonoBehaviour
    {
        [SerializeField] GameTuning tuning;
        [SerializeField] ShardSet[] prefabs;

        public static ShardPool Instance { get; private set; }

        readonly Dictionary<ShardSet, Stack<ShardSet>> idle = new Dictionary<ShardSet, Stack<ShardSet>>();

        void Awake()
        {
            Instance = this;
            foreach (var prefab in prefabs)
                for (int i = 0; i < tuning.shardPoolSize; i++)
                    Return(Create(prefab));
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public ShardSet Spawn(ShardSet prefab, Vector3 position, Quaternion rotation, Vector3 inheritedVelocity, float impulsePerPiece)
        {
            var set = idle.TryGetValue(prefab, out var stack) && stack.Count > 0 ? stack.Pop() : Create(prefab);
            set.Launch(position, rotation, inheritedVelocity, impulsePerPiece);
            return set;
        }

        public void Return(ShardSet set)
        {
            set.gameObject.SetActive(false);
            if (!idle.TryGetValue(set.SourcePrefab, out var stack))
                idle[set.SourcePrefab] = stack = new Stack<ShardSet>();
            stack.Push(set);
        }

        ShardSet Create(ShardSet prefab)
        {
            var set = Instantiate(prefab, transform);
            set.SourcePrefab = prefab;
            set.gameObject.SetActive(false);
            return set;
        }
    }
}
