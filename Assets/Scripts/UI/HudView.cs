using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PurrcelainPanic
{
    /// <summary>
    /// 점수·콤보 HUD와 깨진 자리에 뜨는 등급 문구. 그레이박스 단계라 UI를 코드로 만든다 (기본 내장 폰트).
    /// </summary>
    public class HudView : MonoBehaviour
    {
        [SerializeField] ScoreComboManager score;
        [SerializeField] ExperienceAbilityManager abilities;
        [SerializeField] Camera worldCamera;
        [SerializeField] float popupLifetime = 0.9f;
        [SerializeField] float popupRise = 80f;   // 기준 해상도 픽셀

        static readonly string[] GradeNames = { "CRACK", "SMASH", "PURRFECT!" };
        static readonly Color[] GradeColors =
        {
            new Color(0.95f, 0.95f, 0.95f),
            new Color(1f, 0.62f, 0.25f),
            new Color(1f, 0.85f, 0.2f),
        };
        static readonly int[] GradeFontSize = { 40, 52, 72 };

        class Popup
        {
            public Text text;
            public Vector3 worldPoint;
            public float age;
            public bool active;
        }

        Font font;
        RectTransform canvasRect;
        Text scoreText;
        Text comboText;
        Image comboBar;
        Text levelText;
        Image xpBar;
        Text slotsText;
        readonly List<Popup> popups = new List<Popup>();

        public int ActivePopupCount
        {
            get
            {
                int n = 0;
                foreach (var p in popups)
                    if (p.active)
                        n++;
                return n;
            }
        }

        void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasRect = (RectTransform)transform;

            CreatePanel(new Vector2(0, 1), new Vector2(24, -24), new Vector2(520, 160));   // 왼쪽 위 글씨 뒤 반투명 바탕
            scoreText = CreateText("Score", new Vector2(0, 1), new Vector2(48, -40), 56, TextAnchor.UpperLeft);
            comboText = CreateText("Combo", new Vector2(1, 1), new Vector2(-48, -40), 52, TextAnchor.UpperRight);
            comboBar = CreateBar(new Vector2(1, 1), new Vector2(-48, -112), new Vector2(260, 10), Image.OriginHorizontal.Right, new Color(1f, 0.75f, 0.3f));
            levelText = CreateText("Level", new Vector2(0, 1), new Vector2(48, -112), 40, TextAnchor.UpperLeft);
            xpBar = CreateBar(new Vector2(0, 1), new Vector2(48, -164), new Vector2(460, 10), Image.OriginHorizontal.Left, new Color(0.5f, 0.85f, 1f));
            slotsText = CreateText("Abilities", new Vector2(0, 0), new Vector2(48, 48), 32, TextAnchor.LowerLeft);
        }

        void OnEnable() => score.Scored += OnScored;
        void OnDisable() => score.Scored -= OnScored;

        void Update()
        {
            scoreText.text = $"점수  {score.Score:N0}";
            bool combo = score.Combo > 1;
            comboText.text = combo ? $"콤보 {score.Combo}   x{score.Multiplier:0.00}" : "";
            comboBar.enabled = combo;
            comboBar.fillAmount = score.ComboWindow > 0f ? score.ComboTimeLeft / score.ComboWindow : 0f;
            if (abilities != null)
            {
                levelText.text = $"레벨 {abilities.Level}   경험치 {abilities.Xp} / {abilities.XpToNext}";
                xpBar.fillAmount = (float)abilities.Xp / abilities.XpToNext;
                var sb = new System.Text.StringBuilder();
                foreach (var id in abilities.Slots)
                    sb.Append(AbilityCatalog.Name(id)).Append("  Lv ").Append(abilities.LevelOf(id)).AppendLine();
                slotsText.text = sb.ToString();
            }

            foreach (var p in popups)
                if (p.active)
                    Animate(p, Time.deltaTime);
        }

        void OnScored(ScoreEvent e)
        {
            var p = popups.Find(x => !x.active);
            if (p == null)
            {
                p = new Popup { text = CreateText("Popup", new Vector2(0, 0), Vector2.zero, 40, TextAnchor.MiddleCenter) };
                popups.Add(p);
            }
            int g = (int)e.grade;
            p.text.text = $"{GradeNames[g]}\n+{e.points}";
            p.text.color = GradeColors[g];
            p.text.fontSize = GradeFontSize[g];
            p.worldPoint = e.source.point + Vector3.up * 0.25f;
            p.age = 0f;
            p.active = true;
            p.text.gameObject.SetActive(true);
            Animate(p, 0f);
        }

        void Animate(Popup p, float dt)
        {
            p.age += dt;
            float t = p.age / popupLifetime;
            if (t >= 1f)
            {
                p.active = false;
                p.text.gameObject.SetActive(false);
                return;
            }
            Vector3 screen = worldCamera.WorldToScreenPoint(p.worldPoint);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out var local);
            var rt = p.text.rectTransform;
            rt.anchoredPosition = local + canvasRect.rect.size * 0.5f + Vector2.up * popupRise * t;
            float pop = t < 0.15f ? Mathf.Lerp(0.6f, 1.15f, t / 0.15f) : Mathf.Lerp(1.15f, 1f, (t - 0.15f) / 0.2f);
            rt.localScale = Vector3.one * pop;
            var c = p.text.color;
            c.a = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
            p.text.color = c;
        }

        Text CreateText(string name, Vector2 anchor, Vector2 position, int size, TextAnchor align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            if (align == TextAnchor.MiddleCenter)
                rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(700, 200);
            var text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = align;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.6f);
            outline.effectDistance = new Vector2(2f, -2f);
            return text;
        }

        void CreatePanel(Vector2 anchor, Vector2 position, Vector2 size)
        {
            var go = new GameObject("Backing", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.35f);
            img.raycastTarget = false;
        }

        Image CreateBar(Vector2 anchor, Vector2 position, Vector2 size, Image.OriginHorizontal origin, Color color)
        {
            var go = new GameObject("Bar", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = (int)origin;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }
    }
}
