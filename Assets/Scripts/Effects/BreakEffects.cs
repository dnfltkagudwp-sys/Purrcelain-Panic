using UnityEngine;

namespace PurrcelainPanic
{
    /// <summary>
    /// 등급별 파괴 연출: 먼지·부스러기 입자와 카메라 흔들림. 파편 세기는 BreakableVase가 등급으로 정한다.
    /// 입자 시스템은 하나씩만 두고 깨진 자리에서 Emit 한다.
    /// </summary>
    public class BreakEffects : MonoBehaviour
    {
        [SerializeField] GameTuning tuning;
        [SerializeField] ScoreComboManager score;
        [SerializeField] CameraFollow cameraFollow;
        [SerializeField] ParticleSystem dust;
        [SerializeField] ParticleSystem crumbs;

        void OnEnable() => score.Scored += Play;
        void OnDisable() => score.Scored -= Play;

        void Play(ScoreEvent e)
        {
            int g = (int)e.grade;
            Vector3 p = e.source.point + Vector3.up * 0.03f;

            Emit(dust, p, tuning.dustCount[g]);
            float crumbScale = e.source.kind == VaseKind.Fragile ? tuning.fragileCrumbMultiplier : 1f;
            Emit(crumbs, p, Mathf.RoundToInt(tuning.crumbCount[g] * crumbScale));

            if (cameraFollow != null && tuning.shakeAmplitude[g] > 0f)
                cameraFollow.Shake(tuning.shakeAmplitude[g], tuning.shakeDuration);
        }

        static void Emit(ParticleSystem system, Vector3 position, int count)
        {
            if (system == null || count <= 0)
                return;
            system.transform.position = position;
            system.Emit(count);
        }
    }
}
