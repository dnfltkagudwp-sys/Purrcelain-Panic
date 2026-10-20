using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PurrcelainPanic
{
    /// <summary>
    /// 레벨업 카드 화면. 게임이 멈춘 동안 카드 최대 3장을 보여 주고 숫자 1·2·3으로 고른다.
    /// 그레이박스 단계라 UI를 코드로 만든다.
    /// </summary>
    public class CardSelectView : MonoBehaviour
    {
        [SerializeField] GameTuning tuning;
        [SerializeField] ExperienceAbilityManager abilities;

        class Card
        {
            public GameObject root;
            public TMP_Text key;
            public TMP_Text title;
            public TMP_Text subtitle;
            public TMP_Text level;
            public TMP_Text body;
        }

        GameObject panel;
        TMP_Text header;
        readonly List<Card> cards = new List<Card>();

        void Awake()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            panel = CreateRect("Panel", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            AddImage(panel, new Color(0f, 0f, 0f, 0.6f));
            header = CreateText(panel.transform, "레벨 업!", 64, new Vector2(0.5f, 0.5f), new Vector2(0, 330), new Vector2(900, 100));
            header.color = new Color(1f, 0.85f, 0.3f);
            for (int i = 0; i < 3; i++)
                cards.Add(CreateCard(i));
            panel.SetActive(false);
        }

        void OnEnable()
        {
            abilities.OffersShown += Show;
            abilities.OffersClosed += Hide;
        }

        void OnDisable()
        {
            abilities.OffersShown -= Show;
            abilities.OffersClosed -= Hide;
        }

        void Update()
        {
            if (!abilities.IsChoosing)
                return;
            var kb = Keyboard.current;
            if (kb == null)
                return;
            if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) abilities.Choose(0);
            else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) abilities.Choose(1);
            else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) abilities.Choose(2);
        }

        void Show(IReadOnlyList<CardOffer> offers)
        {
            header.text = $"레벨 {abilities.Level} 달성!  카드를 고르세요";
            for (int i = 0; i < cards.Count; i++)
            {
                var c = cards[i];
                bool on = i < offers.Count;
                c.root.SetActive(on);
                if (!on)
                    continue;
                var o = offers[i];
                c.key.text = $"[{i + 1}]";
                c.title.text = AbilityCatalog.Name(o.id);
                c.subtitle.text = AbilityCatalog.EnglishName(o.id);
                c.level.text = o.IsNew ? "새 능력" : $"Lv {o.nextLevel - 1}  >  Lv {o.nextLevel}";
                c.level.color = o.IsNew ? new Color(0.5f, 1f, 0.6f) : new Color(1f, 0.85f, 0.3f);
                c.body.text = AbilityCatalog.Describe(o.id, o.nextLevel, tuning);
            }
            // 장수가 적으면 가운데로 모은다
            float spacing = 440f;
            float start = -(offers.Count - 1) * spacing * 0.5f;
            for (int i = 0; i < offers.Count; i++)
                ((RectTransform)cards[i].root.transform).anchoredPosition = new Vector2(start + i * spacing, -20f);
            panel.SetActive(true);
        }

        void Hide() => panel.SetActive(false);

        Card CreateCard(int index)
        {
            var rt = CreateRect($"Card{index + 1}", panel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400, 520));
            AddImage(rt.gameObject, new Color(0.16f, 0.13f, 0.12f, 0.95f));
            var outline = rt.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.8f, 0.4f, 0.8f);
            outline.effectDistance = new Vector2(3f, -3f);
            return new Card
            {
                root = rt.gameObject,
                key = CreateText(rt, "", 44, new Vector2(0.5f, 1f), new Vector2(0, -50), new Vector2(360, 60)),
                title = CreateText(rt, "", 44, new Vector2(0.5f, 1f), new Vector2(0, -125), new Vector2(380, 60)),
                subtitle = CreateText(rt, "", 24, new Vector2(0.5f, 1f), new Vector2(0, -170), new Vector2(380, 36), new Color(0.75f, 0.7f, 0.65f)),
                level = CreateText(rt, "", 32, new Vector2(0.5f, 1f), new Vector2(0, -220), new Vector2(360, 50)),
                body = CreateText(rt, "", 30, new Vector2(0.5f, 1f), new Vector2(0, -350), new Vector2(350, 220)),
            };
        }

        static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        static void AddImage(GameObject go, Color color)
        {
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }

        static TMP_Text CreateText(Transform parent, string text, int size, Vector2 anchor, Vector2 position, Vector2 box, Color? color = null)
        {
            var rt = CreateRect("Text", parent, anchor, anchor, position, box);
            var t = UiText.Add(rt.gameObject, size, TextAlignmentOptions.Center, wrap: true);
            t.text = text;
            t.color = color ?? Color.white;
            return t;
        }
    }
}
