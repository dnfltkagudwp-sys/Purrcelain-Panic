using System;
using System.Collections.Generic;
using UnityEngine;

namespace PurrcelainPanic
{
    /// <summary>
    /// 식탁 위 SpawnPoint 격자(열 × 줄) 중 빈 자리에 패턴대로 도자기를 놓는다.
    /// 완전한 무작위 좌표가 아니라 격자 칸 단위로 고르고, 깨진 도자기는 다시 꺼내 쓴다.
    /// </summary>
    public class VaseSpawner : MonoBehaviour
    {
        [SerializeField] GameTuning tuning;
        [SerializeField] BreakableVase[] prefabs = new BreakableVase[3];   // VaseKind 순서
        [SerializeField] LayerMask vaseMask;
        [SerializeField] Transform cat;
        [SerializeField] PawSwipe paws;

        /// <summary>도자기를 놓을 때마다 (패턴, 놓은 개수).</summary>
        public event Action<SpawnPattern, int> Spawned;

        // 능력 카드(6단계)가 바꾸는 값
        public float LineWeightBonus { get; set; }      // Domino Display: 앞뒤 2개 패턴 가중치 추가
        public float Line3Weight { get; set; }          // Domino Display Lv 4+: 앞뒤 3개 패턴
        public float ExtraVaseChance { get; set; }      // Full Set: 한 개 더 놓을 확률
        public float PackedChance { get; set; }         // Packed Display: 기존 도자기 옆 자리를 우선할 확률

        // 패턴 모양: (열 오프셋, 줄 오프셋). 줄 0 = 고양이 쪽
        static readonly Dictionary<SpawnPattern, Vector2Int[]> Shapes = new Dictionary<SpawnPattern, Vector2Int[]>
        {
            { SpawnPattern.Single, new[] { new Vector2Int(0, 0) } },
            { SpawnPattern.Pair, new[] { new Vector2Int(0, 0), new Vector2Int(1, 0) } },
            { SpawnPattern.Line2, new[] { new Vector2Int(0, 0), new Vector2Int(0, 1) } },
            { SpawnPattern.Line3, new[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2) } },
            { SpawnPattern.Cluster, new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1) } },
        };
        static readonly SpawnPattern[] Patterns = (SpawnPattern[])Enum.GetValues(typeof(SpawnPattern));

        readonly List<BreakableVase>[] pools = { new List<BreakableVase>(), new List<BreakableVase>(), new List<BreakableVase>() };
        readonly List<Vector2Int> candidates = new List<Vector2Int>();
        readonly Collider[] nearby = new Collider[32];
        readonly float[] patternWeights = new float[5];
        readonly float[] kindWeights = new float[3];
        StageSettings stage;
        float timer;
        float refillTimer;

        public int ActiveCount
        {
            get
            {
                int n = 0;
                foreach (var pool in pools)
                    foreach (var v in pool)
                        if (v.gameObject.activeSelf)
                            n++;
                return n;
            }
        }

        void Awake()
        {
            SetStage(0);
            timer = refillTimer = tuning.firstSpawnDelay;
        }

        public void SetStage(int index)
        {
            stage = tuning.stages[Mathf.Clamp(index, 0, tuning.stages.Length - 1)];
            timer = Mathf.Min(timer, stage.interval);
        }

        void FixedUpdate() => Tick(Time.fixedDeltaTime);

        public void Tick(float dt)
        {
            // 식탁이 비어 가면 간격을 기다리지 않고 채운다 (쉬는 구간 방지)
            refillTimer -= dt;
            if (refillTimer <= 0f && OnTableCount < stage.minOnTable)
            {
                refillTimer = tuning.refillInterval;
                if (SpawnOnce() > 0)
                    timer = Mathf.Max(timer, tuning.refillInterval);
            }

            timer -= dt;
            if (timer > 0f)
                return;
            // 자리가 없어 실패하면 다음 간격까지 기다리지 않고 곧 다시 시도
            timer = SpawnOnce() > 0 ? stage.interval : tuning.spawnRetryDelay;
        }

        /// <summary>식탁 위에 서 있거나 놓여 있는 도자기 수 (떨어지는 중인 것은 제외).</summary>
        public int OnTableCount
        {
            get
            {
                float minY = TableSurface.Instance.TopY - 0.1f;
                int n = 0;
                foreach (var pool in pools)
                    foreach (var v in pool)
                        if (v.gameObject.activeSelf && v.transform.position.y > minY)
                            n++;
                return n;
            }
        }

        /// <summary>패턴 하나를 고르고, 들어갈 자리가 없으면 단독으로, 그것도 없으면 이번 생성은 건너뛴다.</summary>
        public int SpawnOnce()
        {
            foreach (var p in Patterns)
                patternWeights[(int)p] = stage.PatternWeight(p);
            patternWeights[(int)SpawnPattern.Line2] += LineWeightBonus;
            patternWeights[(int)SpawnPattern.Line3] += Line3Weight;
            var pattern = Patterns[StageSettings.Pick(patternWeights)];

            int placed = TryPlace(pattern);
            if (placed == 0 && pattern != SpawnPattern.Single)
                placed = TryPlace(pattern = SpawnPattern.Single);
            if (placed > 0 && UnityEngine.Random.value < ExtraVaseChance)
                placed += TryPlace(SpawnPattern.Single);
            if (placed > 0)
                Spawned?.Invoke(pattern, placed);
            return placed;
        }

        int TryPlace(SpawnPattern pattern)
        {
            var shape = Shapes[pattern];
            candidates.Clear();
            int half = (stage.columns - 1) / 2, center = (tuning.columnCount - 1) / 2;
            for (int col = center - half; col <= center + half; col++)
                for (int row = 0; row < tuning.rowOffsets.Length; row++)
                    if (Fits(shape, col, row, center - half, center + half))
                        candidates.Add(new Vector2Int(col, row));
            if (candidates.Count == 0)
                return 0;

            Vector2Int anchor = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            if (PackedChance > 0f && UnityEngine.Random.value < PackedChance)
            {
                var packed = candidates.FindAll(a => HasNeighbor(shape, a));
                if (packed.Count > 0)
                    anchor = packed[UnityEngine.Random.Range(0, packed.Count)];
            }

            foreach (var o in shape)
                Place(anchor + o);
            Physics.SyncTransforms();   // 바로 이어지는 빈 자리 검사에 방금 놓은 도자기가 보이도록
            return shape.Length;
        }

        bool Fits(Vector2Int[] shape, int col, int row, int minCol, int maxCol)
        {
            int backRow = tuning.rowOffsets.Length - 1;
            foreach (var o in shape)
            {
                var c = new Vector2Int(col + o.x, row + o.y);
                if (c.x < minCol || c.x > maxCol || c.y > backRow)
                    return false;
                // 뒷줄은 앞발이 닿지 않으므로, 같은 열 바로 앞 칸도 함께 놓는 배열일 때만 허용
                if (c.y == backRow && Array.IndexOf(shape, new Vector2Int(o.x, o.y - 1)) < 0)
                    return false;
                if (!IsFree(c))
                    return false;
            }
            return true;
        }

        bool IsFree(Vector2Int cell) => !IsOccupied(cell) && !IsBlockedByPaws(cell) && !HasMovingVaseNear(cell);

        /// <summary>맞아서 미끄러지는 도자기가 근처에 있으면, 막 놓은 도자기를 쳐서 넘어뜨리므로 기다린다.</summary>
        bool HasMovingVaseNear(Vector2Int cell)
        {
            int n = Physics.OverlapSphereNonAlloc(CellPosition(cell) + Vector3.up * 0.15f, tuning.movingVaseClearance, nearby, vaseMask, QueryTriggerInteraction.Ignore);
            float minSpeedSq = tuning.movingVaseSpeed * tuning.movingVaseSpeed;
            for (int i = 0; i < n; i++)
            {
                var body = nearby[i].attachedRigidbody;
                if (body != null && body.linearVelocity.sqrMagnitude > minSpeedSq)
                    return true;
            }
            return false;
        }

        bool IsOccupied(Vector2Int cell) =>
            Physics.CheckSphere(CellPosition(cell) + Vector3.up * 0.15f, tuning.spawnCheckRadius, vaseMask, QueryTriggerInteraction.Ignore);

        /// <summary>앞발에는 충돌체가 없으므로, 앞발이 놓인 자리와 휘두르는 범위에는 직접 생성을 막는다.</summary>
        bool IsBlockedByPaws(Vector2Int cell)
        {
            if (paws == null)
                return false;
            Vector3 p = CellPosition(cell);
            // 휘두르는 중: 고양이 앞 넓은 구역(앞발이 닿는 줄)
            if (paws.IsBusy && cell.y < tuning.rowOffsets.Length - 1 && Mathf.Abs(p.x - cat.position.x) < tuning.pawExclusionHalfWidth)
                return true;
            // 쉬는 자세 포함 지금 앞발(팔뚝~손끝)이 있는 자리
            return paws.DistanceToPawsXZ(p) < tuning.spawnCheckRadius + tuning.pawRadius;
        }

        bool HasNeighbor(Vector2Int[] shape, Vector2Int anchor)
        {
            foreach (var o in shape)
            {
                var c = anchor + o;
                foreach (var d in new[] { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down })
                {
                    var n = c + d;
                    if (Array.IndexOf(shape, n - anchor) < 0 && n.y >= 0 && n.y < tuning.rowOffsets.Length && IsOccupied(n))
                        return true;
                }
            }
            return false;
        }

        public Vector3 CellPosition(Vector2Int cell)
        {
            var table = TableSurface.Instance;
            float center = (tuning.columnCount - 1) / 2f;
            return new Vector3(table.CenterX + (cell.x - center) * tuning.columnSpacing, table.TopY + 0.005f,
                table.NearZ + tuning.rowOffsets[cell.y]);
        }

        void Place(Vector2Int cell)
        {
            for (int k = 0; k < kindWeights.Length; k++)
                kindWeights[k] = stage.KindWeight((VaseKind)k);
            int kind = StageSettings.Pick(kindWeights);

            BreakableVase vase = null;
            foreach (var v in pools[kind])
                if (!v.gameObject.activeSelf) { vase = v; break; }
            if (vase == null)
            {
                vase = Instantiate(prefabs[kind], transform);
                vase.gameObject.SetActive(false);
                pools[kind].Add(vase);
            }

            var rot = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
            var pos = CellPosition(cell);
            vase.transform.SetPositionAndRotation(pos, rot);
            vase.gameObject.SetActive(true);
            // 비활성 Rigidbody에 준 속도는 반영되지 않는다. 켠 뒤에 초기화해야 깨질 때의 속도·회전이 남지 않는다
            var body = vase.GetComponent<Rigidbody>();
            body.position = pos;
            body.rotation = rot;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

    }
}
