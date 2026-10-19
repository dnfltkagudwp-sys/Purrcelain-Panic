using System;
using UnityEngine;

namespace PurrcelainPanic
{
    /// <summary>
    /// 경과 시간에 따라 스테이지를 바꾼다. 씬을 다시 불러오지 않고 생성 규칙과 쓸 수 있는 식탁 열(= 고양이 이동 범위)만 바꾼다.
    /// </summary>
    public class StageDirector : MonoBehaviour
    {
        [SerializeField] GameTuning tuning;
        [SerializeField] VaseSpawner spawner;
        [SerializeField] CatController cat;

        public event Action<int> StageChanged;

        public float Elapsed { get; private set; }
        public int Stage { get; private set; } = -1;

        void Start() => Apply(0);

        void FixedUpdate() => Tick(Time.fixedDeltaTime);

        public void Tick(float dt)
        {
            Elapsed += dt;
            int next = Stage;
            while (next + 1 < tuning.stages.Length && Elapsed >= tuning.stages[next + 1].startTime)
                next++;
            if (next != Stage)
                Apply(next);
        }

        void Apply(int stage)
        {
            Stage = stage;
            var s = tuning.stages[stage];
            spawner.SetStage(stage);
            // 쓰는 열의 양 끝 + 0.1 m까지 이동 가능 (B-1)
            cat.MoveRangeX = (s.columns - 1) / 2 * tuning.columnSpacing + 0.1f;
            StageChanged?.Invoke(stage);
        }
    }
}
