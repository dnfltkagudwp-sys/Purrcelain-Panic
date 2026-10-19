using UnityEngine;

namespace PurrcelainPanic
{
    /// <summary>
    /// 고양이 머리 바로 위에서 식탁을 거의 수직으로 내려다보는 카메라. 고양이의 X축 이동만 부드럽게 따라간다.
    /// 머리가 크고 앞발이 얼굴 바로 아래에 있어 45° 뒤쪽 구도에서는 앞발이 가려지므로 72°로 정했다 (부록 A).
    /// 편집 모드에서도 위치를 맞춰 Scene/Game 뷰에서 구도를 바로 확인할 수 있다.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] Transform target;

        [Header("구도 (부록 B-2)")]
        [SerializeField] float height = 2.6f;           // 바닥 기준 높이, m
        [SerializeField] float backOffset = -0.15f;     // 고양이 원점에서 뒤로, m (음수 = 앞쪽, 머리 바로 위)
        [SerializeField, Range(30f, 85f)] float pitch = 72f;
        [SerializeField, Range(40f, 70f)] float fieldOfView = 50f;

        [Header("추적")]
        [SerializeField] float followSmoothTime = 0.12f;

        [Header("흔들림")]
        [SerializeField] float shakeFrequency = 28f;

        Camera cam;
        float velocityX;
        float shakeAmplitude;
        float shakeTimeLeft;
        float shakeDuration;

        void OnEnable()
        {
            cam = GetComponent<Camera>();
        }

        /// <summary>파괴 연출용 흔들림. amplitude는 m 단위 (SMASH 0.04, PURRFECT 0.08).</summary>
        public void Shake(float amplitude, float duration = 0.15f)
        {
            if (amplitude < shakeAmplitude && shakeTimeLeft > 0f)
                return;
            shakeAmplitude = amplitude;
            shakeDuration = shakeTimeLeft = duration;
        }

        void LateUpdate()
        {
            if (target == null)
                return;

            cam.fieldOfView = fieldOfView;
            transform.rotation = Quaternion.Euler(pitch, 0f, 0f);

            float x = transform.position.x;
            if (Application.isPlaying)
                x = Mathf.SmoothDamp(x, target.position.x, ref velocityX, followSmoothTime);
            else
                x = target.position.x;

            Vector3 position = new Vector3(x, height, target.position.z - backOffset);

            if (Application.isPlaying && shakeTimeLeft > 0f)
            {
                shakeTimeLeft -= Time.deltaTime;
                float fade = Mathf.Clamp01(shakeTimeLeft / shakeDuration);
                float t = Time.time * shakeFrequency;
                Vector2 noise = new Vector2(Mathf.PerlinNoise(t, 0f), Mathf.PerlinNoise(0f, t)) * 2f - Vector2.one;
                position += (transform.right * noise.x + transform.up * noise.y) * shakeAmplitude * fade;
            }

            transform.position = position;
        }
    }
}
