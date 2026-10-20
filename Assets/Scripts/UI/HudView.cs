using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PurrcelainPanic
{
    /// <summary>
    /// 점수·콤보·레벨 HUD (깨진 자리 팝업은 ScorePopupView). 그레이박스 단계라 UI를 코드로 만든다.
    /// </summary>
    public class HudView : MonoBehaviour
    {
        [SerializeField] ScoreComboManager score;
        [SerializeField] ExperienceAbilityManager abilities;
        [SerializeField] float comboPunchScale = 0.35f;   // 콤보가 오를 때 튀는 크기
        [SerializeField] float comboPunchTime = 0.2f;     // 원래 크기로 돌아오는 시간, 초

        static readonly Color ComboHot = new Color(1f, 0.3f, 0.35f);

        int lastCombo;
        float comboPunch;
        TMP_Text scoreText;
        TMP_Text comboText;
        Image comboBar;
        TMP_Text levelText;
        Image xpBar;
        TMP_Text slotsText;

        void Awake()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            CreatePanel(new Vector2(0, 1), new Vector2(24, -24), new Vector2(520, 160));   // 왼쪽 위 글씨 뒤 반투명 바탕
            scoreText = CreateText("Score", new Vector2(0, 1), new Vector2(48, -40), 56, TextAlignmentOptions.TopLeft);
            comboText = CreateText("Combo", new Vector2(1, 1), new Vector2(-48, -40), 52, TextAlignmentOptions.TopRight);
            comboBar = CreateBar(new Vector2(1, 1), new Vector2(-48, -112), new Vector2(260, 10), Image.OriginHorizontal.Right, new Color(1f, 0.75f, 0.3f));
            levelText = CreateText("Level", new Vector2(0, 1), new Vector2(48, -112), 40, TextAlignmentOptions.TopLeft);
            xpBar = CreateBar(new Vector2(0, 1), new Vector2(48, -164), new Vector2(460, 10), Image.OriginHorizontal.Left, new Color(0.5f, 0.85f, 1f));
            slotsText = CreateText("Abilities", new Vector2(0, 0), new Vector2(48, 48), 32, TextAlignmentOptions.BottomLeft);
        }

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

            // 콤보가 오를 때마다 콤보 글씨가 툭 튀고, 배율이 높을수록 붉어진다
            if (score.Combo > lastCombo)
                comboPunch = 1f;
            lastCombo = score.Combo;
            comboPunch = Mathf.MoveTowards(comboPunch, 0f, Time.deltaTime / comboPunchTime);
            comboText.rectTransform.localScale = Vector3.one * (1f + comboPunchScale * comboPunch * comboPunch);
            float heat = Mathf.InverseLerp(1f, score.ComboMultiplier(int.MaxValue), score.Multiplier);
            comboText.color = Color.Lerp(Color.white, ComboHot, heat);
        }

        TMP_Text CreateText(string name, Vector2 anchor, Vector2 position, int size, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(700, 200);
            return UiText.Add(go, size, align);
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
