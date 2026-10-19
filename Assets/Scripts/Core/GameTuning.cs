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

        [Header("파편 (B-6)")]
        public int shardPoolSize = 12;              // 도자기 종류별 미리 만들어 둘 파편 세트 수
        public float shardImpulseMin = 0.3f;        // 조각당, 약한 충돌
        public float shardImpulseMax = 1.0f;        // 조각당, 강한 충돌 (등급 연출은 5단계)
        public float shardInheritVelocity = 0.3f;   // 도자기 속도 중 파편이 물려받는 비율
        public float shardLifetime = 4f;            // 초
        public float shardShrinkTime = 0.5f;        // 마지막에 줄어들며 사라지는 시간, 초

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

        [Header("스테이지 (B-8)")]
        public StageSettings[] stages =
        {
            new StageSettings { startTime = 0f, interval = 2.5f, columns = 5, single = 70, pair = 30, normal = 100 },
            new StageSettings { startTime = 60f, interval = 1.8f, columns = 7, single = 45, pair = 30, line2 = 25, normal = 75, fragile = 25 },
            new StageSettings { startTime = 120f, interval = 1.2f, columns = 11, single = 30, pair = 25, line2 = 25, cluster = 20, normal = 60, fragile = 25, collector = 15 },
        };

        public float SpeedScore(float impactSpeed) => Mathf.InverseLerp(impactSpeedMin, impactSpeedMax, impactSpeed);
        public float DistanceScore(float distance) => Mathf.Clamp01(distance / flightDistanceMax);

        /// <summary>1-based 프레임 번호를 클립 정규화 시간(0~1)으로 바꾼다.</summary>
        public float FrameToNormalized(int frame) => (frame - 1f) / (clipLastFrame - 1f);

        public float ImpulseFor(float charge01) => Mathf.Lerp(minImpulse, maxImpulse, Mathf.Clamp01(charge01));
    }
}
