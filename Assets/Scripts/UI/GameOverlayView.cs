using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
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
        string resultSummary;
        bool rankingBusy;
        bool rankingFailed;
        GameObject titlePanel, modal, transition;
        RawImage background;
        TMP_Text resultHeading, rankText, nicknameText, modalMessage;
        TMP_InputField nicknameInput;
        Button retryButton;
        bool startAfterName;
        Texture seated, fleeing;
        int modalVersion;

        void Awake()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();
            if (EventSystem.current == null)
                new GameObject("Menu EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            seated = Resources.Load<Texture>("MenuArt/CatSeated");
            fleeing = Resources.Load<Texture>("MenuArt/CatFleeing");
            AudioListener.volume = PlayerPrefs.GetFloat("Settings.Volume", 0.8f);
            BuildMenu();

            etaText = CreateText(transform, "", 48, new Vector2(0.5f, 1f), new Vector2(0, -60), new Vector2(800, 70));
            bannerText = CreateText(transform, "", 56, new Vector2(0.5f, 0.5f), new Vector2(0, 260), new Vector2(1600, 90));
            bannerText.color = new Color(1f, 0.88f, 0.45f);

            pausePanel = CreatePanel("Pause", new Color(0f, 0f, 0f, 0.55f));
            CreateText(pausePanel.transform, "일시정지\n\nESC로 계속하기", 64, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 400));
            pausePanel.SetActive(false);

            resultPanel = CreatePanel("Result", MenuWidgets.Cream);
            background = MenuWidgets.Full("Cat background", resultPanel.transform).gameObject.AddComponent<RawImage>();
            background.texture = seated;
            FitIllustration(background);
            var sheet = MenuWidgets.Rect("Score sheet", resultPanel.transform, new Vector2(-450, 0), new Vector2(700, 860));
            MenuWidgets.Paint(sheet, new Color(1f, 0.965f, 0.89f, 0.97f), true);
            resultHeading = MenuWidgets.Text(sheet, "", 50, new Vector2(0, 325), new Vector2(630, 80));
            rankText = MenuWidgets.Text(sheet, "", 29, new Vector2(0, 245), new Vector2(630, 80));
            resultText = MenuWidgets.Text(sheet, "", 34, new Vector2(0, 30), new Vector2(590, 320), TextAlignmentOptions.Left);
            retryButton = MenuWidgets.Button(sheet, "순위 다시 확인", new Vector2(0, -205), new Vector2(570, 65), SubmitRanking);
            MenuWidgets.Button(sheet, "다시 하기", new Vector2(-150, -320), new Vector2(280, 80), game.Restart, true);
            MenuWidgets.Button(sheet, "처음으로", new Vector2(150, -320), new Vector2(280, 80), game.ReturnToTitle);
            resultPanel.SetActive(false);
            BuildTransition();
        }

        void Start() => OnStateChanged(game.State);

        static void FitIllustration(RawImage image)
        {
            var fit = image.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = 16f / 9f;
        }

        void OnDestroy()
        {
            modalVersion++;
        }

        void BuildMenu()
        {
            titlePanel = CreatePanel("Title", MenuWidgets.Cream);
            var art = MenuWidgets.Full("Title illustration", titlePanel.transform).gameObject.AddComponent<RawImage>();
            art.texture = Resources.Load<Texture>("MenuArt/CatTitle");
            FitIllustration(art);
            MenuWidgets.Text(titlePanel.transform, "PURRCELAIN\nPANIC", 105, new Vector2(-480, 245), new Vector2(760, 260), TextAlignmentOptions.Left);
            MenuWidgets.Text(titlePanel.transform, "집사가 없는 사이, 한바탕 와장창!", 32,
                new Vector2(-480, 55), new Vector2(760, 60), TextAlignmentOptions.Left);
            MenuWidgets.Button(titlePanel.transform, "시작하기", new Vector2(-515, -90), new Vector2(630, 100), StartClicked, true);
            MenuWidgets.Button(titlePanel.transform, "설정", new Vector2(-680, -210), new Vector2(300, 85), OpenSettings);
            MenuWidgets.Button(titlePanel.transform, "랭킹", new Vector2(-350, -210), new Vector2(300, 85), OpenRanking);
            nicknameText = MenuWidgets.Text(titlePanel.transform, "", 27, new Vector2(-540, -320), new Vector2(580, 55));
            MenuWidgets.Button(titlePanel.transform, "이름 변경", new Vector2(-515, -405), new Vector2(300, 65), () => OpenName(false));
            RefreshNickname();
        }

        void RefreshNickname() => nicknameText.text = OnlineRanking.ValidName(OnlineRanking.PlayerName)
            ? $"랭킹 이름  ·  {OnlineRanking.PlayerName}" : "아직 이름이 없는 고양이";

        void StartClicked()
        {
            if (!OnlineRanking.ValidName(OnlineRanking.PlayerName))
                OpenName(true);
            else
                game.StartRun();
        }

        RectTransform OpenModal(string heading)
        {
            CloseModal();
            modal = CreatePanel("Modal", new Color(0.18f, 0.12f, 0.08f, 0.65f));
            modal.GetComponent<Image>().raycastTarget = true;
            modal.transform.SetAsLastSibling();
            var card = MenuWidgets.Rect("Dialog", modal.transform, Vector2.zero, new Vector2(970, 890));
            MenuWidgets.Paint(card, MenuWidgets.Cream, true);
            MenuWidgets.Text(card, heading, 54, new Vector2(0, 345), new Vector2(850, 85));
            MenuWidgets.Button(card, "닫기", new Vector2(0, -355), new Vector2(300, 70), CloseModal);
            return card;
        }

        void CloseModal()
        {
            modalVersion++;
            if (modal == null)
                return;
            modal.SetActive(false);
            Destroy(modal);
            modal = null;
        }

        void OpenName(bool andStart)
        {
            startAfterName = andStart;
            var card = OpenModal("어떤 이름으로 사고 칠까요?");
            MenuWidgets.Text(card, "이 이름은 온라인 랭킹에 공개됩니다.\n한글·영문·숫자·_·-로 1~16자", 30,
                new Vector2(0, 205), new Vector2(800, 110));
            var field = MenuWidgets.Rect("Nickname", card, new Vector2(0, 50), new Vector2(760, 95));
            var image = MenuWidgets.Paint(field, Color.white, true);
            nicknameInput = field.gameObject.AddComponent<TMP_InputField>();
            nicknameInput.targetGraphic = image;
            var viewport = MenuWidgets.Rect("Viewport", field, Vector2.zero, new Vector2(700, 80));
            viewport.gameObject.AddComponent<RectMask2D>();
            var inputText = MenuWidgets.Text(viewport, "", 36, Vector2.zero, new Vector2(690, 75), TextAlignmentOptions.Left);
            nicknameInput.textViewport = viewport;
            nicknameInput.textComponent = (TextMeshProUGUI)inputText;
            nicknameInput.richText = false;
            nicknameInput.characterLimit = 16;
            nicknameInput.lineType = TMP_InputField.LineType.SingleLine;
            nicknameInput.text = OnlineRanking.PlayerName;
            modalMessage = MenuWidgets.Text(card, "", 27, new Vector2(0, -65), new Vector2(820, 95));
            MenuWidgets.Button(card, andStart ? "이 이름으로 시작" : "이름 저장", new Vector2(0, -205),
                new Vector2(600, 85), SaveName, true);
            nicknameInput.ActivateInputField();
        }

        void SaveName()
        {
            string value = nicknameInput.text.Trim();
            if (!OnlineRanking.ValidName(value))
            {
                modalMessage.text = "한글·영문·숫자·_·-로 1~16자 입력해 주세요.";
                return;
            }
            OnlineRanking.SaveName(value);
            RefreshNickname();
            CloseModal();
            if (startAfterName)
                game.StartRun();
        }

        void OpenSettings()
        {
            var card = OpenModal("설정");
            var volumeText = MenuWidgets.Text(card, $"전체 음량  {AudioListener.volume:P0}", 38,
                new Vector2(0, 200), new Vector2(700, 70));
            void Volume(float delta)
            {
                AudioListener.volume = Mathf.Clamp01(AudioListener.volume + delta);
                PlayerPrefs.SetFloat("Settings.Volume", AudioListener.volume);
                PlayerPrefs.Save();
                volumeText.text = $"전체 음량  {AudioListener.volume:P0}";
            }
            MenuWidgets.Button(card, "음량 줄이기", new Vector2(-195, 95), new Vector2(350, 75), () => Volume(-0.1f));
            MenuWidgets.Button(card, "음량 높이기", new Vector2(195, 95), new Vector2(350, 75), () => Volume(0.1f));
            var fullscreen = MenuWidgets.Button(card, "", new Vector2(0, -25), new Vector2(740, 80), () => { });
            void RefreshFullscreen() => fullscreen.GetComponentInChildren<TMP_Text>().text = Screen.fullScreen ? "창 모드로 전환" : "전체 화면으로 전환";
            fullscreen.onClick.AddListener(() => { Screen.fullScreen = !Screen.fullScreen; RefreshFullscreen(); });
            RefreshFullscreen();
            MenuWidgets.Text(card, "이동  A / D 또는 방향키\n앞발  마우스 왼쪽 / 오른쪽 · 길게 눌러 충전\n일시정지  ESC", 29,
                new Vector2(0, -195), new Vector2(800, 150));
        }

        async void OpenRanking()
        {
            var card = OpenModal("온라인 TOP 10");
            int version = modalVersion;
            var body = MenuWidgets.Text(card, "기록을 불러오는 중…", 31, new Vector2(0, 15), new Vector2(820, 540), TextAlignmentOptions.TopLeft);
            MenuWidgets.Button(card, "새로고침", new Vector2(285, -355), new Vector2(220, 70), OpenRanking);
            try
            {
                var page = await OnlineRanking.TopAsync();
                if (this == null || version != modalVersion)
                    return;
                var sb = new StringBuilder();
                foreach (var entry in page.Results)
                    sb.AppendLine($"{entry.Rank + 1,2}위   {entry.PlayerName ?? "이름 없는 고양이"}   {entry.Score:N0}");
                body.text = page.Results.Count == 0 ? "아직 등록된 기록이 없습니다.\n첫 번째 기록을 남겨보세요!" : sb.ToString();
            }
            catch (System.Exception ex)
            {
                if (this == null || version != modalVersion)
                    return;
                body.text = "랭킹을 불러오지 못했습니다.\n연결을 확인하고 새로고침을 눌러 주세요.";
                Debug.LogWarning($"온라인 랭킹 조회 실패: {ex.Message}");
            }
        }

        void BuildTransition()
        {
            transition = CreatePanel("Transition caption overlay", new Color(0.12f, 0.09f, 0.06f, 0.12f));
            transition.SetActive(false);
        }

        IEnumerator OwnerTransition(bool intro)
        {
            transition.SetActive(true);
            var plaque = MenuWidgets.Rect("Transition caption", transition.transform, Vector2.zero, new Vector2(1400, 270));
            MenuWidgets.Paint(plaque, new Color(0.18f, 0.12f, 0.08f, 0.85f), true).raycastTarget = false;
            var group = plaque.gameObject.AddComponent<CanvasGroup>();
            var caption = MenuWidgets.Text(plaque,
                intro ? "집주인이 돌아오기 전에\n도자기를 모두 깨뜨리자!" : "철컥… 집주인이 돌아왔다!",
                66, Vector2.zero, new Vector2(1300, 220));
            caption.color = Color.white;
            float duration = intro ? 2.8f : 2f;
            for (float time = 0; time < duration; time += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Clamp01(time / 0.25f) * Mathf.Clamp01((duration - time) / 0.35f);
                yield return null;
            }
            Destroy(plaque.gameObject);
            transition.SetActive(false);
            if (intro) game.BeginPlaying();
            else game.ShowResults();
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
            if (game.State == GameState.Over && rankingFailed && !rankingBusy &&
                UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.tKey.wasPressedThisFrame)
                SubmitRanking();
            int sec = Mathf.CeilToInt(game.EtaLeft);
            etaText.text = $"집주인 도착까지  {sec / 60}:{sec % 60:00}";
            bool hurry = game.EtaLeft <= 10f && game.State == GameState.Playing;
            etaText.color = hurry ? Color.Lerp(new Color(1f, 0.35f, 0.3f), Color.white, Mathf.PingPong(Time.unscaledTime * 2f, 1f)) : Color.white;

            bannerText.enabled = !abilities.IsChoosing && game.State == GameState.Playing;
            etaText.enabled = game.State == GameState.Playing || game.State == GameState.Paused;
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
            titlePanel.SetActive(s == GameState.Title);
            foreach (var hud in FindObjectsByType<HudView>(FindObjectsSortMode.None))
                hud.GetComponent<Canvas>().enabled = s == GameState.Playing || s == GameState.Paused;
            pausePanel.SetActive(s == GameState.Paused);
            resultPanel.SetActive(s == GameState.Over);
            if (s == GameState.Intro)
                StartCoroutine(OwnerTransition(true));
            if (s == GameState.Ending)
            {
                resultSummary =
                    $"최종 점수   {score.Score:N0}\n" +
                    $"최고 콤보   {score.MaxCombo}\n" +
                    $"PURRFECT   {score.PurrfectCount}회\n" +
                    $"도달 레벨   {abilities.Level}";
                StartCoroutine(OwnerTransition(false));
                SubmitRanking();
            }
        }

        async void SubmitRanking()
        {
            if (rankingBusy)
                return;
            rankingBusy = true;
            rankingFailed = false;
            resultText.text = resultSummary;
            resultHeading.text = "오늘의 사고 기록";
            rankText.text = "온라인 순위 확인 중…";
            retryButton.interactable = false;
            background.texture = null;
            background.color = MenuWidgets.Cream;
            try
            {
                var result = await OnlineRanking.SubmitAsync(score.Score);
                if (this == null)
                    return;
                ShowRanking(result.rank, result.bestScore);
            }
            catch (System.Exception ex)
            {
                if (this == null)
                    return;
                rankingFailed = true;
                rankText.text = "순위 미확정 · 다시 확인해 주세요";
                retryButton.interactable = true;
                Debug.LogWarning($"온라인 랭킹 연결 실패: {ex.Message}");
            }
            finally
            {
                rankingBusy = false;
            }
        }

        void ShowRanking(int rank, double bestScore)
        {
            resultText.text = resultSummary + $"\n\n나의 최고 점수   {bestScore:N0}";
            rankText.text = $"최고 기록 기준 세계 {rank:N0}위 · {OnlineRanking.PlayerName}";
            resultHeading.text = rank <= 10 ? "난 아무것도 안 했는데?" : "들켰다! 도망가자!";
            background.texture = rank <= 10 ? seated : fleeing;
            background.color = Color.white;
            retryButton.interactable = true;
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
