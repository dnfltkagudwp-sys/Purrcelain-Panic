using System;
using UnityEngine;

namespace PurrcelainPanic
{
    public enum VaseKind { Normal, Fragile, Collector }

    /// <summary>바닥에 닿아 깨진 도자기 정보. 등급·점수 단계에서 사용한다.</summary>
    public struct VaseBreak
    {
        public BreakableVase vase;
        public VaseKind kind;
        public Vector3 point;
        public float impactSpeed;     // 바닥에 닿기 직전 속도, m/s
        public float flightDistance;  // 식탁을 벗어난 지점부터 바닥 충돌 지점까지 수평 거리, m
        public int swipeId;           // 0이면 앞발과 관계없이 떨어짐
        public int chainStep;         // 0 = 앞발이 직접 맞힘, 1 = 그 도자기에 맞음, ...
        public ImpactResult impact;   // 파괴 등급과 품질
    }

    /// <summary>
    /// 도자기 한 개. 앞발·연쇄로 받은 Swipe 정보를 기억하고, 다른 도자기에 연쇄 힘을 한 번만 전달하며,
    /// 식탁을 벗어난 위치를 기록했다가 바닥에 처음 닿으면 파편으로 바뀐다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class BreakableVase : MonoBehaviour
    {
        [SerializeField] GameTuning tuning;
        [SerializeField] VaseKind kind;
        [SerializeField] ShardSet shardPrefab;
        [SerializeField] LayerMask floorMask;

        public static event Action<VaseBreak> Broken;

        /// <summary>Domino Effect(6단계)가 더하는 연쇄 힘 전달률. 합계는 최대 95%.</summary>
        public static float ChainTransferBonus { get; set; }
        float ChainTransferRate => Mathf.Min(tuning.chainTransferRate + ChainTransferBonus, 0.95f);

        Rigidbody body;
        Vector3 lastVelocity;   // 이번 물리 스텝 직전 속도 = 충돌 직전 속도
        bool broken;
        bool exitedTable;
        Vector3 exitPoint;

        public VaseKind Kind => kind;
        public int SwipeId { get; private set; }
        public int ChainStep { get; private set; }
        public float ReceivedSpeed { get; private set; }
        public bool HasTransferred { get; private set; }

        void Awake()
        {
            body = GetComponent<Rigidbody>();
        }

        void OnEnable()
        {
            broken = exitedTable = HasTransferred = false;
            SwipeId = ChainStep = 0;
            ReceivedSpeed = 0f;
            lastVelocity = Vector3.zero;
        }

        void FixedUpdate() => PhysicsStep();

        /// <summary>물리 스텝 직전 처리. 직접 물리를 진행하는 테스트에서는 Physics.Simulate 전에 호출한다.</summary>
        public void PhysicsStep()
        {
            lastVelocity = body.linearVelocity;
            if (body.position.y < -1f)   // 바닥 밖으로 빠진 경우 (파괴 없이 정리)
            {
                gameObject.SetActive(false);
                return;
            }
            if (!exitedTable && TableSurface.Instance != null && !TableSurface.Instance.ContainsXZ(body.worldCenterOfMass))
            {
                exitedTable = true;
                exitPoint = body.worldCenterOfMass;
            }
        }

        /// <summary>앞발이 직접 맞혔을 때 (PawSwipe가 힘을 준 직후).</summary>
        public void OnPawHit(in PawHit hit)
        {
            SwipeId = hit.swipeId;
            ChainStep = 0;
            HasTransferred = false;
            ReceivedSpeed = hit.impulse.magnitude / body.mass;
        }

        void OnCollisionEnter(Collision c) => HandleCollision(c);
        // 처음부터 붙어 있던 도자기는 Enter가 오지 않으므로 Stay에서도 연쇄를 확인한다
        void OnCollisionStay(Collision c) => HandleCollision(c);

        void HandleCollision(Collision c)
        {
            if (broken)
                return;
            if ((floorMask.value & (1 << c.gameObject.layer)) != 0)
            {
                Break(c.GetContact(0).point);
                return;
            }
            if (c.rigidbody != null && c.rigidbody.TryGetComponent(out BreakableVase other))
                TryTransfer(other);
        }

        void TryTransfer(BreakableVase other)
        {
            if (SwipeId == 0 || HasTransferred || other.broken || other.SwipeId == SwipeId)
                return;
            float speed = lastVelocity.magnitude;
            if (speed < tuning.chainMinSpeed)
                return;

            Vector3 toOther = Vector3.ProjectOnPlane(other.body.worldCenterOfMass - body.worldCenterOfMass, Vector3.up).normalized;
            Vector3 travel = Vector3.ProjectOnPlane(lastVelocity, Vector3.up).normalized;
            Vector3 dir = (toOther * tuning.chainCenterWeight + travel * (1f - tuning.chainCenterWeight)).normalized;

            other.ReceiveChain(SwipeId, ChainStep + 1, dir * speed * ChainTransferRate);
            body.linearVelocity = lastVelocity * tuning.chainCarrierKeep;
            HasTransferred = true;
        }

        void ReceiveChain(int swipeId, int chainStep, Vector3 velocity)
        {
            SwipeId = swipeId;
            ChainStep = chainStep;
            HasTransferred = false;
            ReceivedSpeed = velocity.magnitude;
            body.linearVelocity = new Vector3(velocity.x, Mathf.Max(body.linearVelocity.y, 0f), velocity.z);
        }

        void Break(Vector3 point)
        {
            broken = true;
            float impactSpeed = lastVelocity.magnitude;
            float distance = exitedTable ? Vector2.Distance(new Vector2(exitPoint.x, exitPoint.z), new Vector2(point.x, point.z)) : 0f;

            var impact = ImpactEvaluator.Evaluate(tuning, kind, impactSpeed, distance);
            if (shardPrefab != null && ShardPool.Instance != null)
                ShardPool.Instance.Spawn(shardPrefab, transform.position, transform.rotation,
                    lastVelocity * tuning.shardInheritVelocity, tuning.shardImpulse[(int)impact.grade]);

            Broken?.Invoke(new VaseBreak
            {
                vase = this, kind = kind, point = point, impactSpeed = impactSpeed, flightDistance = distance,
                swipeId = SwipeId, chainStep = ChainStep, impact = impact,
            });
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            gameObject.SetActive(false);   // 생성 시스템(4단계)이 다시 꺼내 쓴다
        }
    }
}
