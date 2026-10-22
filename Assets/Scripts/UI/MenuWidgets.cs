using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PurrcelainPanic
{
    /// <summary>시작·결과 화면에서 공통으로 쓰는 따뜻한 카툰 UI.</summary>
    public static class MenuWidgets
    {
        public static readonly Color Ink = new Color32(77, 48, 35, 255);
        public static readonly Color Cream = new Color32(255, 246, 225, 255);
        public static readonly Color Orange = new Color32(244, 174, 76, 255);
        public static readonly Color Sage = new Color32(194, 216, 180, 255);
        static Sprite rounded;
        static Material textMaterial;

        public static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Full(string name, Transform parent)
        {
            var rt = Rect(name, parent, Vector2.zero, Vector2.zero);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            return rt;
        }

        public static Image Paint(RectTransform rt, Color color, bool round = false)
        {
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            if (round)
            {
                if (rounded == null)
                {
                    var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                    texture.name = "Menu rounded panel";
                    for (int y = 0; y < 64; y++)
                        for (int x = 0; x < 64; x++)
                        {
                            float dx = Mathf.Max(Mathf.Abs(x - 31.5f) - 17f, 0f);
                            float dy = Mathf.Max(Mathf.Abs(y - 31.5f) - 17f, 0f);
                            texture.SetPixel(x, y, new Color(1, 1, 1,
                                Mathf.Clamp01(15f - Mathf.Sqrt(dx * dx + dy * dy))));
                        }
                    texture.Apply();
                    rounded = Sprite.Create(texture, new Rect(0, 0, 64, 64),
                        Vector2.one * 0.5f, 100, 0, SpriteMeshType.FullRect, new Vector4(24, 24, 24, 24));
                }
                image.sprite = rounded;
                image.type = Image.Type.Sliced;
            }
            return image;
        }

        public static TMP_Text Text(Transform parent, string value, float size, Vector2 position,
            Vector2 bounds, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var rt = Rect("Label", parent, position, bounds);
            var text = UiText.Add(rt.gameObject, size, align, true);
            if (textMaterial == null)
            {
                textMaterial = new Material(text.font.material);
                textMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0);
                textMaterial.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0);
                textMaterial.DisableKeyword("UNDERLAY_ON");
            }
            text.fontSharedMaterial = textMaterial;
            text.color = Ink;
            text.richText = false;
            text.text = value;
            return text;
        }

        public static Button Button(Transform parent, string label, Vector2 position, Vector2 size,
            UnityAction action, bool primary = false)
        {
            var rt = Rect(label, parent, position, size);
            var image = Paint(rt, primary ? Orange : Cream, true);
            var shadow = rt.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.3f, 0.15f, 0.07f, 0.3f);
            shadow.effectDistance = new Vector2(0, -5);
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(1f, 0.91f, 0.74f);
            colors.pressedColor = new Color(0.82f, 0.75f, 0.64f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            button.onClick.AddListener(action);
            Text(rt, label, 32, Vector2.zero, size - new Vector2(24, 8));
            return button;
        }
    }
}
