using UnityEngine;

namespace PurrcelainPanic
{
    /// <summary>
    /// 도자기 한 개분의 파편 9조각. 실시간 절단 없이 미리 잘라 둔 조각을 같은 자리에 꺼내 흩뿌린다.
    /// 수명이 끝나면 줄어들며 사라지고 풀로 돌아간다.
    /// </summary>
    public class ShardSet : MonoBehaviour
    {
        [SerializeField] GameTuning tuning;

        Rigidbody[] pieces;
        Vector3[] localPositions;
        Quaternion[] localRotations;
        float age;

        public ShardSet SourcePrefab { get; set; }

        void Awake()
        {
            pieces = GetComponentsInChildren<Rigidbody>(true);
            localPositions = new Vector3[pieces.Length];
            localRotations = new Quaternion[pieces.Length];
            for (int i = 0; i < pieces.Length; i++)
            {
                localPositions[i] = pieces[i].transform.localPosition;
                localRotations[i] = pieces[i].transform.localRotation;
            }
        }

        /// <summary>도자기 자리에서 조각을 원래 모양대로 맞춘 뒤 바깥·위쪽으로 흩뿌린다.</summary>
        public void Launch(Vector3 position, Quaternion rotation, Vector3 inheritedVelocity, float impulsePerPiece)
        {
            transform.SetPositionAndRotation(position, rotation);
            Vector3 center = position + rotation * Vector3.up * 0.15f;
            age = 0f;
            gameObject.SetActive(true);

            for (int i = 0; i < pieces.Length; i++)
            {
                var piece = pieces[i];
                piece.transform.SetLocalPositionAndRotation(localPositions[i], localRotations[i]);
                piece.transform.localScale = Vector3.one;
                piece.position = piece.transform.position;
                piece.rotation = piece.transform.rotation;
                piece.linearVelocity = inheritedVelocity;
                piece.angularVelocity = Vector3.zero;

                Vector3 outward = Vector3.ProjectOnPlane(piece.worldCenterOfMass - center, Vector3.up).normalized;
                piece.AddForce((outward + Vector3.up * 0.5f).normalized * impulsePerPiece, ForceMode.Impulse);
                piece.AddTorque(Random.insideUnitSphere * impulsePerPiece * 0.02f, ForceMode.Impulse);
            }
        }

        void FixedUpdate() => Step(Time.fixedDeltaTime);

        public void Step(float dt)
        {
            age += dt;
            float fadeStart = tuning.shardLifetime - tuning.shardShrinkTime;
            if (age > fadeStart)
            {
                float s = 1f - Mathf.Clamp01((age - fadeStart) / tuning.shardShrinkTime);
                foreach (var piece in pieces)
                    piece.transform.localScale = Vector3.one * s;
            }
            if (age >= tuning.shardLifetime)
                ShardPool.Instance.Return(this);
        }
    }
}
