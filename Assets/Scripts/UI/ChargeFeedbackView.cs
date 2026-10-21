using UnityEngine;
using UnityEngine.UI;

namespace PurrcelainPanic
{
    /// <summary>양손 충전 중에만 앞발 가까이에 게이지를 표시하고, 완충은 손마다 한 번 알린다.</summary>
    public class ChargeFeedbackView : MonoBehaviour
    {
        [SerializeField] PawSwipe paws;
        [SerializeField] Camera worldCamera;
        [SerializeField] AudioClip fullChargeClip;

        readonly RectTransform[] bars = new RectTransform[2];
        readonly RectTransform[] fills = new RectTransform[2];
        readonly Image[] colors = new Image[2];
        readonly Outline[] glows = new Outline[2];
        readonly bool[] notified = new bool[2];
        RectTransform canvasRect;
        AudioSource sound;
        Texture2D capsuleTexture;
        Sprite capsule;

        void Awake()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 3;
            canvasRect = (RectTransform)transform;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            sound = gameObject.AddComponent<AudioSource>();
            sound.playOnAwake = false;
            sound.spatialBlend = 0f;
            sound.volume = 0.2f;
            sound.pitch = 1.45f;
            capsule = CreateCapsule();
            for (int i = 0; i < 2; i++)
            {
                bars[i] = Rect("Charge" + i, transform, new Vector2(128, 16));
                var background = bars[i].gameObject.AddComponent<Image>();
                background.sprite = capsule;
                background.type = Image.Type.Sliced;
                background.color = new Color(0.349f, 0.278f, 0.224f);
                background.raycastTarget = false;
                glows[i] = bars[i].gameObject.AddComponent<Outline>();
                glows[i].effectColor = new Color(1f, 0.886f, 0.612f, 0.4f);
                glows[i].effectDistance = new Vector2(2, -2);
                glows[i].enabled = false;
                fills[i] = Rect("Fill", bars[i], Vector2.zero);
                fills[i].anchorMin = fills[i].anchorMax = new Vector2(0, 0.5f);
                fills[i].pivot = new Vector2(0, 0.5f);
                fills[i].anchoredPosition = new Vector2(3, 0);
                fills[i].sizeDelta = new Vector2(0, 10);
                colors[i] = fills[i].gameObject.AddComponent<Image>();
                colors[i].sprite = capsule;
                colors[i].type = Image.Type.Sliced;
                colors[i].raycastTarget = false;
                bars[i].gameObject.SetActive(false);
            }
        }

        void LateUpdate()
        {
            for (int i = 0; i < 2; i++)
            {
                var paw = (Paw)i;
                bool charging = paws.IsPawCharging(paw);
                bool visible = charging && Time.timeScale > 0f;
                bars[i].gameObject.SetActive(visible);
                if (!charging)
                    notified[i] = false;
                if (!visible)
                    continue;
                float charge = paws.ChargeOf(paw);
                bool full = charge >= 1f;
                Vector3 screen = worldCamera.WorldToScreenPoint(paws.ChargePosition(paw));
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out var point);
                bars[i].anchoredPosition = point + new Vector2(i == 0 ? -145 : 145, -18);
                fills[i].sizeDelta = new Vector2(122 * charge, 10);
                colors[i].color = full ? new Color(0.957f, 0.780f, 0.337f) : new Color(0.965f, 0.898f, 0.729f);
                glows[i].enabled = full;
                if (full && !notified[i])
                {
                    notified[i] = true;
                    sound.PlayOneShot(fullChargeClip);
                }
            }
        }

        void OnDisable()
        {
            foreach (var bar in bars)
                if (bar != null)
                    bar.gameObject.SetActive(false);
            if (sound != null)
                sound.Stop();
        }

        void OnDestroy()
        {
            Destroy(capsule);
            Destroy(capsuleTexture);
        }

        Sprite CreateCapsule()
        {
            capsuleTexture = new Texture2D(32, 16, TextureFormat.RGBA32, false);
            capsuleTexture.name = "Charge capsule";
            capsuleTexture.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 32; x++)
                {
                    float dx = Mathf.Max(Mathf.Abs(x - 15.5f) - 8f, 0f);
                    float dy = y - 7.5f;
                    float alpha = Mathf.Clamp01(8f - Mathf.Sqrt(dx * dx + dy * dy));
                    capsuleTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            capsuleTexture.Apply();
            return Sprite.Create(capsuleTexture, new Rect(0, 0, 32, 16),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(8, 7, 8, 7));
        }

        static RectTransform Rect(string name, Transform parent, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            return rect;
        }
    }
}
