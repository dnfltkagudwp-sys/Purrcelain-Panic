using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PurrcelainPanic
{
    /// <summary>
    /// 깨진 자리에 뜨는 등급·점수 팝업 (기획서 부록 C). 등급이 높을수록 크게 튀어나오고, 흔들리고, 불꽃이 퍼진다.
    /// 콤보 중이면 배율 태그가, 수집용 도자기면 보너스 태그가 아래에 붙는다. UI는 코드로 만든다 (기본 내장 폰트).
    /// </summary>
    public class ScorePopupView : MonoBehaviour
    {
        [SerializeField] ScoreComboManager score;
        [SerializeField] Camera worldCamera;
        [SerializeField] float rise = 90f;              // 떠오르는 높이, 기준 해상도 픽셀
        [SerializeField] float jitter = 30f;            // 같은 자리에 겹치지 않게 좌우로 흩는 폭, 픽셀
        [SerializeField] float stackWidth = 300f;       // 가로로 이 거리 안에 떠 있는 팝업이 있으면 그 위로 쌓음, 픽셀
        [SerializeField] float countUpTime = 0.25f;     // 점수가 0부터 올라가는 시간, 초
        [SerializeField] int sortingOrder = 5;          // HUD 위, 카드 화면(10) 아래

        class GradeStyle
        {
            public string name;
            public Color color;
            public int labelSize;
            public float lifetime;
            public float overshoot;     // 처음 튀어나올 때 최대 배율
            public float wobble;        // 좌우 기울어짐, 도
            public float shake;         // 처음 흔들림, 픽셀
            public int sparks;
            public bool ring;
            public bool shimmer;        // 금색·흰색 반짝임
        }

        static readonly GradeStyle[] Styles =
        {
            new GradeStyle { name = "CRACK", color = new Color(0.95f, 0.95f, 0.95f), labelSize = 38, lifetime = 0.7f, overshoot = 1.1f },
            new GradeStyle { name = "SMASH!", color = new Color(1f, 0.62f, 0.25f), labelSize = 54, lifetime = 0.9f, overshoot = 1.3f, wobble = 6f, shake = 6f, sparks = 6 },
            new GradeStyle { name = "PURRFECT!", color = new Color(1f, 0.85f, 0.2f), labelSize = 76, lifetime = 1.2f, overshoot = 1.5f, wobble = 10f, shake = 10f, sparks = 12, ring = true, shimmer = true },
        };
        const int MaxSparks = 12;
        static readonly Color ComboLow = new Color(1f, 1f, 1f);
        static readonly Color ComboHigh = new Color(1f, 0.3f, 0.35f);
        static readonly Color CollectorColor = new Color(0.55f, 0.75f, 1f);

        class Popup
        {
            public RectTransform root;
            public RectTransform body;      // 글씨 묶음 (기울어짐·배율)
            public Text label, points, tag;
            public Image ring;
            public Image[] sparks = new Image[MaxSparks];
            public Vector2[] sparkDirs = new Vector2[MaxSparks];
            public GradeStyle style;
            public Vector3 worldPoint;
            public Vector2 offset;
            public float height;            // 쌓을 때 쓰는 대략적인 높이, 픽셀
            public int targetPoints;
            public bool hasTag;
            public Color tagColor;
            public float age;
            public bool active;
        }

        Font font;
        Sprite square, ringSprite;
        Canvas canvas;
        RectTransform canvasRect;
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
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasRect = (RectTransform)transform;
            square = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
            ringSprite = CreateRingSprite(128, 0.82f);
        }

        void OnEnable() => score.Scored += OnScored;
        void OnDisable() => score.Scored -= OnScored;

        void Update()
        {
            foreach (var p in popups)
                if (p.active)
                    Animate(p, Time.deltaTime);
        }

        void OnScored(ScoreEvent e)
        {
            var p = popups.Find(x => !x.active) ?? CreatePopup();
            var style = Styles[(int)e.grade];
            p.style = style;
            p.worldPoint = e.source.point + Vector3.up * 0.25f;
            p.targetPoints = e.points;
            p.age = 0f;
            p.label.text = style.name;
            p.label.fontSize = style.labelSize;
            p.points.fontSize = Mathf.RoundToInt(style.labelSize * 0.6f);

            var tag = new System.Text.StringBuilder();
            if (e.combo > 1)
                tag.Append($"콤보 {e.combo}  x{e.multiplier:0.00}");
            if (e.source.kind == VaseKind.Collector)
                tag.Append(tag.Length > 0 ? "   " : "").Append("수집품 x2");
            p.hasTag = tag.Length > 0;
            p.tag.text = tag.ToString();
            float comboT = Mathf.InverseLerp(1f, score.ComboMultiplier(int.MaxValue), e.multiplier);
            p.tagColor = e.combo > 1 ? Color.Lerp(ComboLow, ComboHigh, comboT) : CollectorColor;
            p.tag.fontSize = 30 + Mathf.RoundToInt(comboT * 10f);
            p.height = style.labelSize * 1.6f + (p.hasTag ? 45f : 0f);

            // 가까이 떠 있는 팝업이 있으면 그 위로 쌓고, 좌우로 조금 흩는다
            Vector2 at = ToCanvas(p.worldPoint);
            float y = 0f;
            foreach (var q in popups)
            {
                if (!q.active || q == p)
                    continue;
                Vector2 d = ToCanvas(q.worldPoint) + q.offset - at;
                if (Mathf.Abs(d.x) < stackWidth && d.y > -p.height)
                    y = Mathf.Max(y, d.y + (q.height + p.height) * 0.5f);
            }
            p.offset = new Vector2(Random.Range(-jitter, jitter), y);

            p.ring.gameObject.SetActive(style.ring);
            for (int i = 0; i < MaxSparks; i++)
            {
                bool on = i < style.sparks;
                p.sparks[i].gameObject.SetActive(on);
                if (!on)
                    continue;
                float angle = (i + Random.Range(-0.3f, 0.3f)) / style.sparks * Mathf.PI * 2f;
                p.sparkDirs[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(0.75f, 1.15f);
                p.sparks[i].color = style.color;
                p.sparks[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg - 90f);
            }

            p.active = true;
            p.root.gameObject.SetActive(true);
            p.root.SetAsLastSibling();
            Animate(p, 0f);
        }

        void Animate(Popup p, float dt)
        {
            var s = p.style;
            p.age += dt;
            float a = p.age, t = a / s.lifetime;
            if (t >= 1f)
            {
                p.active = false;
                p.root.gameObject.SetActive(false);
                return;
            }

            // 위치: 깨진 자리를 따라가며 떠오름 + 처음 잠깐 흔들림
            Vector2 pos = ToCanvas(p.worldPoint) + p.offset + Vector2.up * rise * EaseOut(t);
            if (s.shake > 0f && a < 0.25f)
            {
                float k = s.shake * (1f - a / 0.25f);
                pos += new Vector2(Mathf.PerlinNoise(a * 60f, 0f) - 0.5f, Mathf.PerlinNoise(0f, a * 60f) - 0.5f) * 2f * k;
            }
            p.root.anchoredPosition = pos;

            // 글씨: 튀어나왔다 돌아오고, 기울어졌다 잦아듦
            float scale = a < 0.12f ? Mathf.Lerp(0.4f, s.overshoot, a / 0.12f)
                : a < 0.3f ? Mathf.Lerp(s.overshoot, 1f, EaseOut((a - 0.12f) / 0.18f)) : 1f;
            p.body.localScale = Vector3.one * scale;
            p.body.localRotation = Quaternion.Euler(0f, 0f, s.wobble * Mathf.Sin(a * 40f) * Mathf.Exp(-a * 8f));

            float alpha = t < 0.65f ? 1f : 1f - (t - 0.65f) / 0.35f;
            Color labelColor = s.shimmer ? Color.Lerp(s.color, Color.white, 0.5f + 0.5f * Mathf.Sin(a * 30f)) : s.color;
            labelColor.a = alpha;
            p.label.color = labelColor;
            p.points.text = "+" + Mathf.RoundToInt(p.targetPoints * Mathf.Clamp01(a / countUpTime)).ToString("N0");
            p.points.color = new Color(1f, 1f, 1f, alpha);

            // 태그: 0.1초 늦게 튀어나옴
            p.tag.gameObject.SetActive(p.hasTag && a >= 0.1f);
            if (p.hasTag)
            {
                float ta = a - 0.1f;
                float tagScale = ta < 0.1f ? Mathf.Lerp(0.5f, 1.25f, ta / 0.1f) : Mathf.Lerp(1.25f, 1f, Mathf.Clamp01((ta - 0.1f) / 0.15f));
                p.tag.rectTransform.localScale = Vector3.one * tagScale;
                var c = p.tagColor;
                c.a = alpha;
                p.tag.color = c;
            }

            // 고리: 0.4초 동안 퍼지며 사라짐
            if (s.ring)
            {
                float r = Mathf.Clamp01(a / 0.4f);
                p.ring.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.3f, 2.4f, EaseOut(r));
                var c = s.color;
                c.a = 0.8f * (1f - r);
                p.ring.color = c;
            }

            // 불꽃: 0.5초 동안 바깥으로 날아가며 줄어듦
            float f = Mathf.Clamp01(a / 0.5f);
            for (int i = 0; i < s.sparks; i++)
            {
                var rt = p.sparks[i].rectTransform;
                rt.anchoredPosition = p.sparkDirs[i] * (40f + 150f * EaseOut(f));
                rt.localScale = Vector3.one * (1f - f);
            }
        }

        Vector2 ToCanvas(Vector3 world)
        {
            Vector3 screen = worldCamera.WorldToScreenPoint(world);
            var uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, uiCamera, out var local);
            return local;
        }

        static float EaseOut(float t) => 1f - (1f - t) * (1f - t);

        Popup CreatePopup()
        {
            var p = new Popup();
            p.root = CreateRect("Popup", transform, new Vector2(400, 200));
            p.ring = CreateImage("Ring", p.root, ringSprite, new Vector2(140, 140));
            for (int i = 0; i < MaxSparks; i++)
                p.sparks[i] = CreateImage("Spark", p.root, square, new Vector2(8, 30));
            p.body = CreateRect("Body", p.root, new Vector2(400, 200));
            p.label = CreateText("Label", p.body, new Vector2(0, 22));
            p.points = CreateText("Points", p.body, new Vector2(0, -28));
            p.tag = CreateText("Tag", p.body, new Vector2(0, -68));
            popups.Add(p);
            return p;
        }

        RectTransform CreateRect(string name, Transform parent, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            return rt;
        }

        Image CreateImage(string name, Transform parent, Sprite sprite, Vector2 size)
        {
            var img = CreateRect(name, parent, size).gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            return img;
        }

        Text CreateText(string name, Transform parent, Vector2 position)
        {
            var rt = CreateRect(name, parent, new Vector2(700, 100));
            rt.anchoredPosition = position;
            var text = rt.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            var outline = rt.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.7f);
            outline.effectDistance = new Vector2(2f, -2f);
            return text;
        }

        /// <summary>가장자리가 부드러운 고리 텍스처. inner = 안쪽 반지름 비율.</summary>
        static Sprite CreateRingSprite(int size, float inner)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            float half = size * 0.5f, edge = 1.5f / half;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = new Vector2(x + 0.5f - half, y + 0.5f - half).magnitude / half;
                    float alpha = Mathf.Clamp01((1f - d) / edge) * Mathf.Clamp01((d - inner) / edge);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f);
        }
    }
}
