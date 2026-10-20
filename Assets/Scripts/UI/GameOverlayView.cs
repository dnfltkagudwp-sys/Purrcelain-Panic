using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PurrcelainPanic
{
    /// <summary>
    /// Owner ETA 시계, 스테이지 전환 안내, 일시정지 화면, 결과 화면.
    /// 그레이박스 단계라 UI를 코드로 만든다.
    /// </summary>
    public class GameOverlayView : MonoBehaviour
    {
        [SerializeField] GameTuning tuning;
        [SerializeField] GameManager game;
        [SerializeField] StageDirector stages;
        [SerializeField] ScoreComboManager score;
        [SerializeField] ExperienceAbilityManager abilities;

        static readonly string[] StageMessages =
        {
            "스테이지 1",
            "스테이지 2  —  식탁이 넓어지고 얇은 도자기가 나와요",
            "스테이지 3  —  식탁 전체 개방, 금장 도자기 등장!",
        };

        TMP_Text etaText;
        TMP_Text bannerText;
        float bannerLeft;
        GameObject pausePanel;
        GameObject resultPanel;
        TMP_Text resultText;

        void Awake()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            etaText = CreateText(transform, "", 48, new Vector2(0.5f, 1f), new Vector2(0, -60), new Vector2(800, 70));
            bannerText = CreateText(transform, "", 56, new Vector2(0.5f, 0.5f), new Vector2(0, 260), new Vector2(1600, 90));
            bannerText.color = new Color(1f, 0.88f, 0.45f);

            pausePanel = CreatePanel("Pause", new Color(0f, 0f, 0f, 0.55f));
            CreateText(pausePanel.transform, "일시정지\n\nESC로 계속하기", 64, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 400));
            pausePanel.SetActive(false);

            resultPanel = CreatePanel("Result", new Color(0.05f, 0.03f, 0.02f, 0.82f));
            resultText = CreateText(resultPanel.transform, "", 52, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200, 700));
            resultPanel.SetActive(false);
        }

        void OnEnable()
        {
            game.StateChanged += OnStateChanged;
            stages.StageChanged += OnStageChanged;
        }

        void OnDisable()
        {
            game.StateChanged -= OnStateChanged;
            stages.StageChanged -= OnStageChanged;
        }

        void Update()
        {
            int sec = Mathf.CeilToInt(game.EtaLeft);
            etaText.text = $"집주인 도착까지  {sec / 60}:{sec % 60:00}";
            bool hurry = game.EtaLeft <= 10f && game.State == GameState.Playing;
            etaText.color = hurry ? Color.Lerp(new Color(1f, 0.35f, 0.3f), Color.white, Mathf.PingPong(Time.unscaledTime * 2f, 1f)) : Color.white;

            bannerText.enabled = !abilities.IsChoosing;   // 카드 화면 위에 겹치지 않게
            if (bannerLeft > 0f)
            {
                bannerLeft -= Time.unscaledDeltaTime;
                var c = bannerText.color;
                c.a = Mathf.Clamp01(bannerLeft / 0.5f);
                bannerText.color = c;
                if (bannerLeft <= 0f)
                    bannerText.text = "";
            }
        }

        void OnStageChanged(int stage)
        {
            bannerText.text = StageMessages[Mathf.Clamp(stage, 0, StageMessages.Length - 1)];
            bannerLeft = tuning.stageBannerTime;
        }

        void OnStateChanged(GameState s)
        {
            pausePanel.SetActive(s == GameState.Paused);
            resultPanel.SetActive(s == GameState.Over);
            if (s == GameState.Over)
            {
                resultText.text =
                    "집주인이 돌아왔다!\n\n" +
                    $"최종 점수   {score.Score:N0}\n" +
                    $"최고 콤보   {score.MaxCombo}\n" +
                    $"PURRFECT   {score.PurrfectCount}회\n" +
                    $"도달 레벨   {abilities.Level}\n\n" +
                    "R  다시 하기";
            }
        }

        GameObject CreatePanel(string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return go;
        }

        static TMP_Text CreateText(Transform parent, string text, int size, Vector2 anchor, Vector2 position, Vector2 box)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = box;
            var t = UiText.Add(go, size, TextAlignmentOptions.Center);
            t.text = text;
            return t;
        }
    }
}
