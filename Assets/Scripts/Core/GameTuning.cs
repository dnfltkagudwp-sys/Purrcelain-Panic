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

        /// <summary>1-based 프레임 번호를 클립 정규화 시간(0~1)으로 바꾼다.</summary>
        public float FrameToNormalized(int frame) => (frame - 1f) / (clipLastFrame - 1f);

        public float ImpulseFor(float charge01) => Mathf.Lerp(minImpulse, maxImpulse, Mathf.Clamp01(charge01));
    }
}
