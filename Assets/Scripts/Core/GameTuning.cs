using UnityEngine;

namespace PurrcelainPanic
{
    /// <summary>
    /// 게임 수치를 한 곳에 모은다 (기획서 부록 B). 단계별로 필요한 항목을 추가한다.
    /// </summary>
    [CreateAssetMenu(menuName = "Purrcelain Panic/Game Tuning", fileName = "GameTuning")]
    public class GameTuning : ScriptableObject
    {
        [Header("고양이 이동 (B-1)")]
        public float moveSpeed = 2.2f;              // m/s
        public float moveAccelTime = 0.08f;         // 최고 속도까지 걸리는 시간, 초
        public float chargingMoveMultiplier = 0.6f; // 충전 중 이동 속도 배율
        public float moveRangeX = 1.3f;             // 고양이 원점이 움직일 수 있는 X 범위 (±), 스테이지 구현 전 임시

        [Header("Swipe 힘 (B-3)")]
        public float minImpulse = 1.5f;             // ForceMode.Impulse, N·s
        public float maxImpulse = 4.5f;
        public float maxChargeTime = 1.0f;          // 초
        public float cooldown = 0.6f;               // Swipe를 놓은 시점부터, 초
        [Range(0f, 1f)] public float handDirectionWeight = 0.6f; // 나머지는 고양이 전방
        public float upwardBias = 0.15f;            // 정규화 전에 더하는 위쪽 성분

        [Header("Swipe 판정 (B-3)")]
        public float castRadius = 0.08f;            // m
        public float pawTipOffset = 0.10f;          // 손 본(손목)에서 판정 중심까지, m. 손목~손끝 약 0.12 m
        public float shockwaveRange = 0.18f;        // 손끝에서 앞발이 향한 방향으로 더 닿는 거리 (발톱 바람), m
        [Range(0f, 1f)] public float shockwaveFalloff = 0.7f;  // 충격파 끝에서의 힘 비율 (손끝은 100%)

        [Header("Swipe 클립 (Swipe_L/R, 30fps, 1~16프레임)")]
        public int clipLastFrame = 16;
        public int windupFrame = 4;                 // 충전 중 멈춰 있는 준비 자세
        public int hitStartFrame = 5;
        public int hitEndFrame = 9;
        public float layerBlendTime = 0.06f;        // 앞발 레이어가 켜지고 꺼지는 시간, 초

        [Header("이동 중 앞발 당기기 (Push_L/R 클립의 준비 자세)")]
        public int tuckFrame = 4;                   // Push 클립에서 팔을 몸쪽으로 당긴 프레임
        public float tuckBlendTime = 0.1f;          // 당기고 내려놓는 데 걸리는 시간, 초
        public float tuckMoveSpeed = 0.05f;         // 이 속도 이상 움직이면 앞발을 당김, m/s

        [Header("도자기 연쇄 (B-4)")]
        [Range(0f, 1f)] public float chainTransferRate = 0.6f;  // Domino Effect 기본값
        public float chainMinSpeed = 0.5f;          // 친 쪽 도자기가 이 속도 이상일 때만 전달, m/s
        [Range(0f, 1f)] public float chainCarrierKeep = 0.4f;   // 전달 후 친 쪽에 남는 속도 비율
        [Range(0f, 1f)] public float chainCenterWeight = 0.7f;  // 전달 방향: 두 중심을 잇는 방향 비중 (나머지는 친 쪽 진행 방향)

        [Header("파괴 판정 (B-5)")]
        public float impactSpeedMin = 3f;           // SpeedScore 0, m/s
        public float impactSpeedMax = 6f;           // SpeedScore 1
        public float flightDistanceMax = 1.5f;      // DistanceScore 1, m
        [Range(0f, 1f)] public float speedWeight = 0.65f;      // ImpactQuality = SpeedScore × 0.65 + DistanceScore × 0.35
        public float smashThreshold = 0.35f;        // ImpactQuality 이상이면 SMASH
        public float purrfectThreshold = 0.70f;     // 이상이면 PURRFECT
        public float fragilePurrfectThreshold = 0.60f;  // Fragile Batch는 PURRFECT가 쉬움

        [Header("점수·경험치 (B-5, B-7)")]
        public int[] gradeScore = { 100, 160, 250 };     // CRACK, SMASH, PURRFECT
        public int[] gradeXp = { 1, 2, 3 };
        public float collectorScoreMultiplier = 2f;
        public int collectorBonusXp = 2;
        public float comboWindow = 2f;              // 마지막 파괴부터 이 시간 안에 다음 파괴가 오면 콤보 유지, 초
        public float comboWindowReductionPerTier = 0.3f; // 10콤보마다 기본 유지시간 감소, 초
        public float comboWindowMinimum = 1.1f;     // 강화 보너스 적용 전 최소 유지시간, 초
        public float comboStep = 0.25f;             // 콤보 배율 = 1 + (콤보 단계 - 1) × 0.25
        public float comboMaxMultiplier = 3f;

        [Header("파편 (B-6)")]
        public int shardPoolSize = 12;              // 도자기 종류별 미리 만들어 둘 파편 세트 수
        public float[] shardImpulse = { 0.3f, 0.6f, 1.0f };  // 조각당, CRACK / SMASH / PURRFECT

        [Header("등급 연출")]
        public float[] shakeAmplitude = { 0f, 0.04f, 0.08f };  // 카메라 흔들림, m
        public float shakeDuration = 0.15f;
        public int[] crumbCount = { 0, 12, 30 };    // 부스러기 입자 수 (Fragile은 ×1.5)
        public int[] dustCount = { 4, 10, 16 };     // 먼지 입자 수
        public float fragileCrumbMultiplier = 1.5f;
        public float shardInheritVelocity = 0.3f;   // 도자기 속도 중 파편이 물려받는 비율
        public float shardLifetime = 4f;            // 초
        public float shardShrinkTime = 0.5f;        // 마지막에 줄어들며 사라지는 시간, 초

        [Header("게임 (3장)")]
        public float ownerEta = 180f;               // 집주인이 돌아올 때까지, 초. 늘리거나 줄이는 효과는 없음
        public float stageBannerTime = 2f;          // 스테이지 전환 안내 표시 시간, 초

        [Header("생성 격자 (B-1)")]
        public int columnCount = 11;
        public float columnSpacing = 0.25f;         // m, 가운데 열이 x = 0. 가장 굵은 Collector 지름 0.223 m보다 여유 있게
        public float[] rowOffsets = { 0.19f, 0.44f, 0.69f };  // 고양이 쪽 가장자리에서, m. 마지막 줄 = 뒷줄 (간격 0.25 m). 첫 줄은 쉬는 앞발(약 0.2 m) 바로 앞
        public float spawnCheckRadius = 0.12f;      // 이 반경 안에 도자기가 있으면 빈 자리가 아님, m
        public float pawExclusionHalfWidth = 0.65f; // 앞발을 쓰는 중이면 고양이 중심 ±이 거리, 앞 2줄에는 생성 안 함 (휘두르는 폭 약 ±0.55 m)
        public float pawRadius = 0.06f;             // 앞발 두께(반지름). 생성 금지 판정과 앞발 충돌체, m
        public float movingVaseClearance = 0.45f;   // 이 거리 안에 움직이는 도자기가 있으면 그 칸에는 생성 안 함, m
        public float movingVaseSpeed = 0.15f;       // 이 속도 이상이면 움직이는 도자기로 봄, m/s
        public float firstSpawnDelay = 0.5f;        // 초
        public float refillInterval = 0.35f;        // 최소 개수보다 적을 때 채우는 간격, 초
        public float spawnRetryDelay = 0.25f;       // 자리가 없어 생성에 실패하면 이만큼 뒤 다시 시도, 초

        [Header("스테이지 (B-8)")]
        public StageSettings[] stages =
        {
            new StageSettings { startTime = 0f, interval = 2.5f, columns = 5, minOnTable = 3, single = 70, pair = 30, normal = 100 },
            new StageSettings { startTime = 60f, interval = 1.8f, columns = 7, minOnTable = 5, single = 45, pair = 30, line2 = 25, normal = 75, fragile = 25 },
            new StageSettings { startTime = 120f, interval = 1.2f, columns = 11, minOnTable = 7, single = 30, pair = 25, line2 = 25, cluster = 20, normal = 60, fragile = 25, collector = 15 },
        };

        [Header("경험치·카드 (B-9)")]
        public int xpRequiredBase = 4;              // 레벨업 요구량 = 4 + 5 × (현재 레벨 - 1)
        public int xpRequiredStep = 5;              // 도자기가 끊기지 않게 채우면서 경험치가 두 배가 되어 3 → 5
        public int maxAbilitySlots = 6;
        public int maxAbilityLevel = 5;
        public int cardsPerLevelUp = 3;
        public int emptyLevelUpBonus = 500;         // 고를 카드가 없을 때 대신 주는 점수

        [Header("능력 수치 (B-10, 레벨당)")]
        public float ironPawPerLevel = 0.20f;       // 밀기 힘 +20%
        public float dominoEffectPerLevel = 0.07f;  // 힘 전달률 +7%p (최대 95%)
        public float quickSwipePerLevel = 0.12f;    // 쿨다운 -12%
        public float quickChargePerLevel = 0.12f;   // 최대 충전시간 -12%
        public float wideArcPerLevel = 0.20f;       // Sphere Cast 반경 +20%
        public float alternatingPerLevel = 0.15f;   // 교대 Swipe 힘 +15%
        public float alternatingWindow = 1.5f;      // 직전 Swipe(반대 손, 적중)부터 이 시간 안이어야 교대로 인정, 초
        public float comboKeeperPerLevel = 0.40f;   // 콤보 유지시간 +0.4초
        public float dominoDisplayPerLevel = 15f;   // 앞뒤 2개 패턴 가중치 +15
        public int dominoDisplayLine3Level = 4;     // 이 레벨부터 앞뒤 3개 패턴 등장
        public float dominoDisplayLine3Weight = 10f;
        public float fullSetPerLevel = 0.15f;       // 한 개 더 놓을 확률 +15%
        public float packedDisplayPerLevel = 0.20f; // 기존 도자기 옆 자리 우선 확률 +20%
        public float clawGustPerLevel = 0.08f;      // 충격파 사거리 +0.08 m

        public int XpRequired(int level) => xpRequiredBase + xpRequiredStep * (level - 1);

        public float SpeedScore(float impactSpeed) => Mathf.InverseLerp(impactSpeedMin, impactSpeedMax, impactSpeed);
        public float DistanceScore(float distance) => Mathf.Clamp01(distance / flightDistanceMax);

        /// <summary>1-based 프레임 번호를 클립 정규화 시간(0~1)으로 바꾼다.</summary>
        public float FrameToNormalized(int frame) => (frame - 1f) / (clipLastFrame - 1f);

        public float ImpulseFor(float charge01) => Mathf.Lerp(minImpulse, maxImpulse, Mathf.Clamp01(charge01));
    }
}
