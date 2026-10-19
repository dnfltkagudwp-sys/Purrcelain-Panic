using UnityEngine;

namespace PurrcelainPanic
{
    /// <summary>식탁 상판 범위. 도자기가 식탁을 벗어난 순간을 판단하는 기준이다.</summary>
    [RequireComponent(typeof(Collider))]
    public class TableSurface : MonoBehaviour
    {
        public static TableSurface Instance { get; private set; }

        Bounds bounds;

        public float TopY => bounds.max.y;
        public float NearZ => bounds.min.z;    // 고양이 쪽 가장자리 (고양이는 -Z에서 +Z를 본다)
        public float CenterX => bounds.center.x;

        void Awake()
        {
            Instance = this;
            bounds = GetComponent<Collider>().bounds;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>위에서 내려다봤을 때 점이 상판 안에 있는지.</summary>
        public bool ContainsXZ(Vector3 p) =>
            p.x >= bounds.min.x && p.x <= bounds.max.x && p.z >= bounds.min.z && p.z <= bounds.max.z;
    }
}
