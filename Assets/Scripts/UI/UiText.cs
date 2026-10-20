using TMPro;
using UnityEngine;

namespace PurrcelainPanic
{
    /// <summary>
    /// 코드로 만드는 UI 글씨 공통 설정. 글꼴은 TMP 기본 글꼴(Jua SDF, TMP Settings)을 쓰고,
    /// 테두리·그림자는 글꼴 재질(Jua SDF Material)에 넣어 두어 모든 글씨가 같은 모양이 된다.
    /// </summary>
    public static class UiText
    {
        public static TextMeshProUGUI Add(GameObject go, float size, TextAlignmentOptions alignment, bool wrap = false)
        {
            var t = go.AddComponent<TextMeshProUGUI>();
            t.fontSize = size;
            t.alignment = alignment;
            t.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            return t;
        }
    }
}
