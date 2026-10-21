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
        static readonly string[] TuckLayerNames = { "TuckL", "TuckR" };
        static readonly int SwipeState = Animator.StringToHash("Swipe");
        static readonly int TuckState = Animator.StringToHash("Tuck");
        static int nextSwipeId = 1;

        [SerializeField] GameTuning tuning;
        [SerializeField] LayerMask vaseMask;
        [SerializeField] ParticleSystem gust;   // 발톱 바람 줄기 (선택)

        public event Action<PawHit> VaseHit;
        public event Action<float> SwipeReleased;

        enum Phase { Ready, Charging, Swiping }

        class PawState
        {
            public Paw paw;
            public int layer;
            public int tuckLayer;
            public float tuckWeight;
            public Vector3 restWrist;     // 쉬는 자세의 손목·손끝 (고양이 기준 좌표)
            public Vector3 restTip;
            public bool hasRest;
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
            public float releaseTime;
            public float alternatingBoost;  // 이번 Swipe에 붙은 Alternating Paws 보너스
            public Vector3 prevTip;
            public bool hasPrevTip;
            public readonly HashSet<Rigidbody> hitBodies = new HashSet<Rigidbody>();
        }

        Animator animator;
        readonly PawState[] paws = new PawState[2];
        PawState lastSwiper;
        float clock;
        readonly RaycastHit[] hitBuffer = new RaycastHit[16];
        readonly Collider[] overlapBuffer = new Collider[16];

        public bool IsCharging => paws[0].phase == Phase.Charging || paws[1].phase == Phase.Charging;
        /// <summary>앞발이 쉬는 자세로 완전히 돌아와 있는지 (충전·Swipe·레이어 복귀 중이 아님).</summary>
        public bool IsIdle(Paw paw) => paws[(int)paw].phase == Phase.Ready && paws[(int)paw].layerWeight <= 0f;
        public bool IsBusy => paws[0].phase != Phase.Ready || paws[1].phase != Phase.Ready;
        public float ChargeOf(Paw paw) => paws[(int)paw].charge01;
        public bool IsPawCharging(Paw paw) => paws[(int)paw].phase == Phase.Charging;
        public Vector3 ChargePosition(Paw paw) => paws[(int)paw].hand.position;
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
                tuckLayer = animator.GetLayerIndex(TuckLayerNames[(int)paw]),
                hand = animator.GetBoneTransform(hand),
                forearm = animator.GetBoneTransform(forearm),
            };
            animator.SetLayerWeight(s.layer, 0f);
            animator.SetLayerWeight(s.tuckLayer, 0f);
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

        /// <summary>고양이가 좌우로 움직이는 중인지. 움직이는 동안 쉬는 앞발을 몸쪽으로 당겨 도자기와 겹치지 않게 한다.</summary>
        public bool Moving { get; set; }

        // 능력 카드(6단계)가 바꾸는 값
        public float ImpulseMultiplier { get; set; } = 1f;     // Iron Paw
        public float CooldownMultiplier { get; set; } = 1f;    // Quick Swipe
        public float ChargeTimeMultiplier { get; set; } = 1f;  // Quick Charge
        public float CastRadiusMultiplier { get; set; } = 1f;  // Wide Arc
        public float AlternatingBonus { get; set; }            // Alternating Paws: 교대로 맞히면 다음 Swipe 힘 추가 비율
        public float ShockwaveRangeBonus { get; set; }         // 발톱 바람: 충격파 사거리 추가, m

        /// <summary>마지막으로 놓은 Swipe가 교대 보너스를 받았는지.</summary>
        public bool LastSwipeAlternating { get; private set; }

        float Cooldown => tuning.cooldown * CooldownMultiplier;
        float CastRadius => tuning.castRadius * CastRadiusMultiplier;
        float ShockwaveRange => tuning.shockwaveRange + ShockwaveRangeBonus;

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
            clock += dt;
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
                    s.charge01 = Mathf.Clamp01(s.chargeTime / (tuning.maxChargeTime * ChargeTimeMultiplier));
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

            // 앞발 당기기: 움직이는 중이거나, 쉬는 자리에 도자기가 있으면 몸쪽으로 당긴다.
            // Swipe 레이어 아래(팔만 덮는 마스크)에 있어 Swipe가 시작되면 자연스럽게 덮인다
            if (s.phase == Phase.Ready && s.layerWeight <= 0f && s.tuckWeight <= 0f)
                RememberRestPose(s, tip);
            float tuckTarget = s.phase == Phase.Ready && (Moving || RestSpotBlocked(s)) ? 1f : 0f;
            s.tuckWeight = Mathf.MoveTowards(s.tuckWeight, tuckTarget, dt / Mathf.Max(tuning.tuckBlendTime, 0.001f));
            animator.SetLayerWeight(s.tuckLayer, s.tuckWeight);
            if (s.tuckWeight > 0f)
                animator.Play(TuckState, s.tuckLayer, tuning.FrameToNormalized(tuning.tuckFrame));
        }

        void Release(PawState s)
        {
            // Alternating Paws: 직전 Swipe가 반대 손이고, 무언가 맞혔고, 짧은 시간 안이면 이번 Swipe가 강해진다.
            // 같은 손을 연속으로 쓰거나 빗나가면 보너스 없음
            bool alternating = lastSwiper != null && lastSwiper != s && lastSwiper.hitBodies.Count > 0
                && clock - lastSwiper.releaseTime <= tuning.alternatingWindow;
            s.alternatingBoost = alternating ? AlternatingBonus : 0f;
            LastSwipeAlternating = alternating;
            lastSwiper = s;
            s.releaseTime = clock;

            s.phase = Phase.Swiping;
            s.swipeId = nextSwipeId++;
            s.hitBodies.Clear();
            s.cooldownLeft = Cooldown;
            // 준비 자세 이후 남은 동작이 쿨다운보다 길면 빨리 재생 (Quick Swipe)
            float remaining = (1f - s.clipTime) * ClipDuration;
            s.playbackSpeed = Mathf.Max(1f, remaining / Mathf.Max(Cooldown, 0.01f));
            SwipeReleased?.Invoke(s.charge01);
        }

        bool InHitWindow(float clipTime) =>
            clipTime >= tuning.FrameToNormalized(tuning.hitStartFrame) &&
            clipTime <= tuning.FrameToNormalized(tuning.hitEndFrame) + 1e-4f;

        void RememberRestPose(PawState s, Vector3 tip)
        {
            s.restWrist = transform.InverseTransformPoint(s.hand.position);
            s.restTip = transform.InverseTransformPoint(tip);
            s.hasRest = true;
        }

        /// <summary>쉬는 자세로 내려놓을 자리에 도자기가 있는지 (있으면 앞발을 당긴 채 둔다).</summary>
        bool RestSpotBlocked(PawState s) =>
            s.hasRest && Physics.CheckCapsule(transform.TransformPoint(s.restWrist), transform.TransformPoint(s.restTip),
                tuning.pawRadius, vaseMask, QueryTriggerInteraction.Ignore);

        /// <summary>위에서 봤을 때 점에서 가장 가까운 앞발(팔뚝 ~ 손끝)까지의 거리.</summary>
        public float DistanceToPawsXZ(Vector3 p)
        {
            float best = float.MaxValue;
            foreach (var s in paws)
            {
                Vector2 a = new Vector2(s.forearm.position.x, s.forearm.position.z);
                Vector3 tip = PawTip(s);
                Vector2 b = new Vector2(tip.x, tip.z);
                Vector2 q = new Vector2(p.x, p.z);
                Vector2 ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(q - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
                best = Mathf.Min(best, Vector2.Distance(q, a + ab * t));
            }
            return best;
        }

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
                count = Physics.SphereCastNonAlloc(from, CastRadius, delta / dist, hitBuffer, dist, vaseMask, QueryTriggerInteraction.Ignore);
            }
            else
            {
                int n = Physics.OverlapSphereNonAlloc(to, CastRadius, overlapBuffer, vaseMask, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < n; i++)
                    TryHit(s, overlapBuffer[i].attachedRigidbody, overlapBuffer[i].ClosestPoint(to), Vector3.zero);
                Shockwave(s, to, Vector3.zero);
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
            Shockwave(s, to, handDir);
        }

        /// <summary>
        /// 발톱 바람: 손끝에서 앞발이 향한 방향으로 판정을 더 뻗는다. 팔이 짧아 닿지 않는 도자기를 위한 판정으로,
        /// 처음 닿은 도자기 하나에서 막히고, 멀수록 힘이 약해진다.
        /// </summary>
        void Shockwave(PawState s, Vector3 tip, Vector3 handDir)
        {
            float range = ShockwaveRange;
            Vector3 dir = Vector3.ProjectOnPlane(tip - s.forearm.position, Vector3.up);
            if (range <= 0f || dir.sqrMagnitude < 1e-6f)
                return;
            dir.Normalize();
            EmitGust(tip, dir, range);

            int n = Physics.SphereCastNonAlloc(tip, CastRadius, dir, hitBuffer, range, vaseMask, QueryTriggerInteraction.Ignore);
            int nearest = -1;
            for (int i = 0; i < n; i++)
                if (hitBuffer[i].rigidbody != null && (nearest < 0 || hitBuffer[i].distance < hitBuffer[nearest].distance))
                    nearest = i;
            if (nearest < 0)
                return;
            var h = hitBuffer[nearest];
            Vector3 point = h.distance <= 0f && h.point == Vector3.zero ? h.collider.ClosestPoint(tip) : h.point;
            TryHit(s, h.rigidbody, point, handDir, Mathf.Lerp(1f, tuning.shockwaveFalloff, h.distance / range));
        }

        void EmitGust(Vector3 tip, Vector3 dir, float range)
        {
            if (gust == null)
                return;
            var p = new ParticleSystem.EmitParams { applyShapeToPosition = false };
            for (int i = 0; i < 3; i++)
            {
                p.position = tip + dir * (range * (i + 0.5f) / 3f) + Vector3.up * 0.02f;
                p.velocity = dir * 1.5f;
                gust.Emit(p, 1);
            }
        }

        void TryHit(PawState s, Rigidbody body, Vector3 point, Vector3 handDir, float forceScale = 1f)
        {
            if (body == null || !s.hitBodies.Add(body))
                return;

            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            Vector3 hand = handDir.sqrMagnitude > 1e-8f ? handDir.normalized : forward;
            Vector3 dir = hand * tuning.handDirectionWeight + forward * (1f - tuning.handDirectionWeight);
            dir = dir.normalized + Vector3.up * tuning.upwardBias;
            Vector3 impulse = dir.normalized * tuning.ImpulseFor(s.charge01) * ImpulseMultiplier * (1f + s.alternatingBoost) * forceScale;

            body.AddForceAtPosition(impulse, point, ForceMode.Impulse);
            var hit = new PawHit
            {
                paw = s.paw, swipeId = s.swipeId, body = body, point = point, impulse = impulse, charge01 = s.charge01,
            };
            if (body.TryGetComponent(out BreakableVase vase))
                vase.OnPawHit(hit);
            VaseHit?.Invoke(hit);
        }
    }
}
