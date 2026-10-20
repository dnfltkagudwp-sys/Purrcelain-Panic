using UnityEngine;

namespace PurrcelainPanic
{
    /// <summary>
    /// 아직 열리지 않은 식탁 구간(양 끝 열)을 어둡게 덮어 보여 준다. 스테이지가 오르면 덮개가 줄어들며 구간이 열린다.
    /// </summary>
    public class TableZoneView : MonoBehaviour
    {
        [SerializeField] GameTuning tuning;
        [SerializeField] StageDirector stages;
        [SerializeField] Material coverMaterial;
        [SerializeField] float openTime = 0.6f;

        Transform left, right;
        float halfOpen;        // 지금 열려 있는 폭의 절반, m
        float targetHalfOpen;

        void Awake()
        {
            left = CreateCover("LockedZone_Left");
            right = CreateCover("LockedZone_Right");
        }

        void OnEnable() => stages.StageChanged += OnStageChanged;
        void OnDisable() => stages.StageChanged -= OnStageChanged;

        void OnStageChanged(int stage)
        {
            int columns = tuning.stages[stage].columns;
            targetHalfOpen = ((columns - 1) / 2 + 0.5f) * tuning.columnSpacing;
            if (stage == 0)
                halfOpen = targetHalfOpen;   // 처음에는 바로
        }

        void Update()
        {
            var table = TableSurface.Instance;
            if (table == null)
                return;
            halfOpen = Mathf.MoveTowards(halfOpen, targetHalfOpen, Time.deltaTime / openTime);
            float tableHalf = table.HalfWidth;
            float coverWidth = Mathf.Max(0f, tableHalf - halfOpen);
            bool show = coverWidth > 0.01f;
            left.gameObject.SetActive(show);
            right.gameObject.SetActive(show);
            if (!show)
                return;
            float y = table.TopY + 0.002f;
            float z = table.NearZ + table.Depth * 0.5f;
            float cx = table.CenterX;
            left.position = new Vector3(cx - halfOpen - coverWidth * 0.5f, y, z);
            right.position = new Vector3(cx + halfOpen + coverWidth * 0.5f, y, z);
            left.localScale = right.localScale = new Vector3(coverWidth, table.Depth, 1f);
        }

        Transform CreateCover(string name)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);   // 위를 보게 눕힘
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = coverMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }
    }
}
