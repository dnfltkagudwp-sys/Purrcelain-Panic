using UnityEngine;

namespace PurrcelainPanic
{
    /// <summary>
    /// 진단용 (임시): 카드 화면이 떠 있는 동안과 닫힌 뒤 3초 동안 시간 배율·고양이·카메라 위치·이동 입력을 콘솔에 남긴다.
    /// "카드가 뜨면 화면 전체가 왼쪽으로 밀린다" 문제의 원인을 잡으면 지운다.
    /// </summary>
    public class SlideProbe : MonoBehaviour
    {
        [SerializeField] ExperienceAbilityManager abilities;
        [SerializeField] CatController cat;
        [SerializeField] Transform cameraTransform;
        [SerializeField] float interval = 0.25f;

        float watchLeft;
        float next;
        float lastCatX, lastCamX;

        void OnEnable()
        {
            abilities.OffersShown += _ => { watchLeft = float.MaxValue; Log("카드 열림"); };
            abilities.OffersClosed += () => { watchLeft = 3f; Log("카드 닫힘"); };
        }

        void LateUpdate()
        {
            if (watchLeft <= 0f)
                return;
            float catX = cat.transform.position.x, camX = cameraTransform.position.x;
            // 멈춘 동안 무언가 움직이면 바로 경고
            if (Time.timeScale <= 0f && (Mathf.Abs(catX - lastCatX) > 1e-4f || Mathf.Abs(camX - lastCamX) > 1e-4f))
                Debug.LogWarning($"[SlideProbe] 멈춘 상태에서 이동! cat {lastCatX:F3}->{catX:F3}, cam {lastCamX:F3}->{camX:F3}, input {cat.RawMoveInput}");
            lastCatX = catX;
            lastCamX = camX;

            if (Time.unscaledTime >= next)
            {
                next = Time.unscaledTime + interval;
                Log("관찰");
            }
            if (watchLeft != float.MaxValue)
                watchLeft -= Time.unscaledDeltaTime;
        }

        void Log(string what)
        {
            Debug.Log($"[SlideProbe] {what}: timeScale {Time.timeScale}, cat x {cat.transform.position.x:F3} (v {cat.VelocityX:F2}, input {cat.RawMoveInput}), " +
                      $"cam {cameraTransform.position.ToString("F3")} rot {cameraTransform.eulerAngles.ToString("F1")}, screen {Screen.width}x{Screen.height}");
        }
    }
}
