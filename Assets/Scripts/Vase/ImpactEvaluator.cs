using UnityEngine;

namespace PurrcelainPanic
{
    public enum BreakGrade { Crack, Smash, Purrfect }

    public struct ImpactResult
    {
        public BreakGrade grade;
        public float quality;        // 0~1
        public float speedScore;
        public float distanceScore;
    }

    /// <summary>바닥 충돌 속도와 비행 거리로 파괴 등급을 정한다 (기획서 15장, 부록 B-5).</summary>
    public static class ImpactEvaluator
    {
        public static ImpactResult Evaluate(GameTuning t, VaseKind kind, float impactSpeed, float flightDistance)
        {
            float speed = t.SpeedScore(impactSpeed);
            float distance = t.DistanceScore(flightDistance);
            float quality = speed * t.speedWeight + distance * (1f - t.speedWeight);
            float purrfect = kind == VaseKind.Fragile ? t.fragilePurrfectThreshold : t.purrfectThreshold;
            var grade = quality >= purrfect ? BreakGrade.Purrfect : quality >= t.smashThreshold ? BreakGrade.Smash : BreakGrade.Crack;
            return new ImpactResult { grade = grade, quality = quality, speedScore = speed, distanceScore = distance };
        }
    }
}
