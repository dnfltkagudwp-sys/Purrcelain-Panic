using System;
using System.Collections.Generic;
using UnityEngine;

namespace PurrcelainPanic
{
    public enum Paw { Left, Right }

    /// <summary>앞발 한 번이 도자기 하나에 준 타격 정보. 연쇄·점수 단계에서 사용한다.</summary>
    public struct PawHit
    {
        public Paw paw;
        public int swipeId;
        public Rigidbody body;
        public Vector3 point;
        public Vector3 impulse;
        public float charge01;
    }

    /// <summary>
    /// 양손 Swipe. 손마다 충전량과 재사용 대기시간이 따로 있고, Animator 레이어(PawL/PawR)도 따로 쓴다.
    /// 손에 Collider를 붙이지 않고, 타격 구간 동안 손끝이 이동한 경로를 Sphere Cast로 검사한다.
    /// 클립 시간은 이 스크립트가 직접 진행시키고 Animator에는 Play(정규화 시간)로 넘긴다
    /// (충전 중 준비 자세 유지, Quick Swipe 재생 속도 조절, 편집 모드 시뮬레이션을 위해).
    /// </summary>
    public class PawSwipe : MonoBehaviour
    {
        const float ClipFps = 30f;
        static readonly string[] LayerNames = { "PawL", "PawR" };
        static readonly int SwipeState = Animator.StringToHash("Swipe");
        static int nextSwipeId = 1;

        [SerializeField] GameTuning tuning;
        [SerializeField] LayerMask vaseMask;

        public event Action<PawHit> VaseHit;

        enum Phase { Ready, Charging, Swiping }

        class PawState
        {
            public Paw paw;
            public int layer;
            public Transform hand;
            public Transform forearm;
            public Phase phase;
            public bool held;
            public bool pressedLatch;     // Update에서 눌렀다 뗀 짧은 클릭을 FixedUpdate까지 기억
            public float chargeTime;
            public float charge01;
            public float clipTime;        // 정규화 시간 0~1 (Animator에 넘긴 값)
            public float playbackSpeed;
            public float cooldownLeft;
            public float layerWeight;
            public int swipeId;
            public Vector3 prevTip;
            public bool hasPrevTip;
            public readonly HashSet<Rigidbody> hitBodies = new HashSet<Rigidbody>();
        }

        Animator animator;
        readonly PawState[] paws = new PawState[2];
        readonly RaycastHit[] hitBuffer = new RaycastHit[16];
        readonly Collider[] overlapBuffer = new Collider[16];

        public bool IsCharging => paws[0].phase == Phase.Charging || paws[1].phase == Phase.Charging;
        public float ChargeOf(Paw paw) => paws[(int)paw].charge01;
        public float CooldownLeft(Paw paw) => paws[(int)paw].cooldownLeft;

        float ClipDuration => (tuning.clipLastFrame - 1) / ClipFps;

        void Awake() => Initialize();

        /// <summary>Animator 레이어와 손 본을 찾는다. 편집 모드 시뮬레이션에서는 직접 호출한다.</summary>
        public void Initialize()
        {
            animator = GetComponent<Animator>();
            paws[0] = CreatePaw(Paw.Left, HumanBodyBones.LeftHand, HumanBodyBones.LeftLowerArm);
            paws[1] = CreatePaw(Paw.Right, HumanBodyBones.RightHand, HumanBodyBones.RightLowerArm);
        }

        PawState CreatePaw(Paw paw, HumanBodyBones hand, HumanBodyBones forearm)
        {
            var s = new PawState
            {
                paw = paw,
                layer = animator.GetLayerIndex(LayerNames[(int)paw]),
                hand = animator.GetBoneTransform(hand),
                forearm = animator.GetBoneTransform(forearm),
            };
            animator.SetLayerWeight(s.layer, 0f);
            return s;
        }

        /// <summary>버튼 상태. 매 프레임 호출한다.</summary>
        public void SetHeld(Paw paw, bool held)
        {
            var s = paws[(int)paw];
            if (held && !s.held)
                s.pressedLatch = true;
            s.held = held;
        }

        /// <summary>진행 중인 충전을 취소한다 (일시정지, 레벨업 화면).</summary>
        public void CancelCharge()
        {
            foreach (var s in paws)
            {
                s.held = s.pressedLatch = false;
                if (s.phase == Phase.Charging)
                    s.phase = Phase.Ready;
            }
        }

        void FixedUpdate() => Step(Time.fixedDeltaTime);

        /// <summary>한 물리 스텝. Animator는 AnimatePhysics라 이 직후에 포즈를 계산한다.</summary>
        public void Step(float dt)
        {
            foreach (var s in paws)
                StepPaw(s, dt);
        }

        void StepPaw(PawState s, float dt)
        {
            // 1. 지난 스텝에 넘긴 clipTime의 포즈가 지금 손 위치다 → 타격 판정
            Vector3 tip = PawTip(s);
            if (s.phase == Phase.Swiping && s.hasPrevTip && InHitWindow(s.clipTime))
                Sweep(s, s.prevTip, tip);
            s.prevTip = tip;
            s.hasPrevTip = true;

            // 2. 상태 진행
            s.cooldownLeft = Mathf.Max(0f, s.cooldownLeft - dt);
            bool pressed = s.held || s.pressedLatch;
            s.pressedLatch = false;

            switch (s.phase)
            {
                case Phase.Ready:
                    if (pressed && s.cooldownLeft <= 0f)
                    {
                        s.phase = Phase.Charging;
                        s.chargeTime = 0f;
                        s.charge01 = 0f;
                        s.clipTime = 0f;
                        if (!s.held)
                            Release(s);   // 한 프레임 안에 눌렀다 뗀 클릭
                    }
                    break;

                case Phase.Charging:
                    s.chargeTime += dt;
                    s.charge01 = Mathf.Clamp01(s.chargeTime / tuning.maxChargeTime);
                    s.clipTime = Mathf.Min(s.clipTime + dt / ClipDuration, tuning.FrameToNormalized(tuning.windupFrame));
                    if (!s.held)
                        Release(s);
                    break;

                case Phase.Swiping:
                    s.clipTime += dt * s.playbackSpeed / ClipDuration;
                    if (s.clipTime >= 1f)
                    {
                        s.clipTime = 1f;
                        s.phase = Phase.Ready;
                    }
                    break;
            }

            // 3. Animator 반영
            float targetWeight = s.phase == Phase.Ready ? 0f : 1f;
            s.layerWeight = Mathf.MoveTowards(s.layerWeight, targetWeight, dt / Mathf.Max(tuning.layerBlendTime, 0.001f));
            animator.SetLayerWeight(s.layer, s.layerWeight);
            if (s.phase != Phase.Ready || s.layerWeight > 0f)
                animator.Play(SwipeState, s.layer, s.clipTime);
        }

        void Release(PawState s)
        {
            s.phase = Phase.Swiping;
            s.swipeId = nextSwipeId++;
            s.hitBodies.Clear();
            s.cooldownLeft = tuning.cooldown;
            // 준비 자세 이후 남은 동작이 쿨다운보다 길면 빨리 재생 (Quick Swipe)
            float remaining = (1f - s.clipTime) * ClipDuration;
            s.playbackSpeed = Mathf.Max(1f, remaining / Mathf.Max(tuning.cooldown, 0.01f));
        }

        bool InHitWindow(float clipTime) =>
            clipTime >= tuning.FrameToNormalized(tuning.hitStartFrame) &&
            clipTime <= tuning.FrameToNormalized(tuning.hitEndFrame) + 1e-4f;

        Vector3 PawTip(PawState s)
        {
            Vector3 along = s.hand.position - s.forearm.position;
            return s.hand.position + along.normalized * tuning.pawTipOffset;
        }

        void Sweep(PawState s, Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            float dist = delta.magnitude;
            int count;
            if (dist > 1e-4f)
            {
                count = Physics.SphereCastNonAlloc(from, tuning.castRadius, delta / dist, hitBuffer, dist, vaseMask, QueryTriggerInteraction.Ignore);
            }
            else
            {
                int n = Physics.OverlapSphereNonAlloc(to, tuning.castRadius, overlapBuffer, vaseMask, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < n; i++)
                    TryHit(s, overlapBuffer[i].attachedRigidbody, overlapBuffer[i].ClosestPoint(to), Vector3.zero);
                return;
            }

            Vector3 handDir = Vector3.ProjectOnPlane(delta, Vector3.up);
            for (int i = 0; i < count; i++)
            {
                var h = hitBuffer[i];
                // 시작부터 겹쳐 있으면 point가 비어 있다
                Vector3 point = h.distance <= 0f && h.point == Vector3.zero ? h.collider.ClosestPoint(from) : h.point;
                TryHit(s, h.rigidbody, point, handDir);
            }
        }

        void TryHit(PawState s, Rigidbody body, Vector3 point, Vector3 handDir)
        {
            if (body == null || !s.hitBodies.Add(body))
                return;

            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            Vector3 hand = handDir.sqrMagnitude > 1e-8f ? handDir.normalized : forward;
            Vector3 dir = hand * tuning.handDirectionWeight + forward * (1f - tuning.handDirectionWeight);
            dir = dir.normalized + Vector3.up * tuning.upwardBias;
            Vector3 impulse = dir.normalized * tuning.ImpulseFor(s.charge01);

            body.AddForceAtPosition(impulse, point, ForceMode.Impulse);
            VaseHit?.Invoke(new PawHit
            {
                paw = s.paw, swipeId = s.swipeId, body = body, point = point, impulse = impulse, charge01 = s.charge01,
            });
        }
    }
}
