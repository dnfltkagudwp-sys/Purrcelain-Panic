using System;
using UnityEngine;

namespace PurrcelainPanic
{
    /// <summary>점수가 들어간 파괴 한 번. 연출·HUD·경험치(6단계)가 사용한다.</summary>
    public struct ScoreEvent
    {
        public VaseBreak source;
        public BreakGrade grade;
        public int points;          // 콤보 배율까지 적용한 점수
        public int xp;
        public int combo;           // 이 파괴로 올라간 콤보 단계 (첫 파괴 = 1)
        public float multiplier;
    }

    /// <summary>
    /// 도자기가 깨질 때마다 등급에 따른 점수·경험치를 주고 콤보를 관리한다 (기획서 15·16장).
    /// 콤보는 마지막 파괴 후 유지시간 안에 다음 파괴가 오면 1씩 오르고, 시간이 지나면 0으로 돌아간다.
    /// </summary>
    public class ScoreComboManager : MonoBehaviour
    {
        [SerializeField] GameTuning tuning;

        public event Action<ScoreEvent> Scored;
        public event Action ComboEnded;

        public int Score { get; private set; }
        public int Combo { get; private set; }
        public int MaxCombo { get; private set; }
        public int PurrfectCount { get; private set; }
        public float ComboTimeLeft { get; private set; }
        public float Multiplier => ComboMultiplier(Combo);

        /// <summary>Combo Keeper(6단계)가 늘리는 유지시간, 초.</summary>
        public float ComboWindowBonus { get; set; }
        public float ComboWindow =>
            Mathf.Max(tuning.comboWindowMinimum,
                tuning.comboWindow - (Combo / 10) * tuning.comboWindowReductionPerTier)
            + ComboWindowBonus;

        void OnEnable() => BreakableVase.Broken += OnBroken;
        void OnDisable() => BreakableVase.Broken -= OnBroken;

        void FixedUpdate() => Tick(Time.fixedDeltaTime);

        public void Tick(float dt)
        {
            if (Combo == 0)
                return;
            ComboTimeLeft -= dt;
            if (ComboTimeLeft <= 0f)
            {
                Combo = 0;
                ComboTimeLeft = 0f;
                ComboEnded?.Invoke();
            }
        }

        /// <summary>콤보와 관계없는 보너스 점수 (고를 카드가 없는 레벨업 등).</summary>
        public void AddBonus(int points) => Score += points;

        public float ComboMultiplier(int combo) =>
            Mathf.Min(1f + Mathf.Max(combo - 1, 0) * tuning.comboStep, tuning.comboMaxMultiplier);

        void OnBroken(VaseBreak b)
        {
            int g = (int)b.impact.grade;
            Combo++;
            MaxCombo = Mathf.Max(MaxCombo, Combo);
            ComboTimeLeft = ComboWindow;
            if (b.impact.grade == BreakGrade.Purrfect)
                PurrfectCount++;

            bool collector = b.kind == VaseKind.Collector;
            float multiplier = ComboMultiplier(Combo);
            int points = Mathf.RoundToInt(tuning.gradeScore[g] * (collector ? tuning.collectorScoreMultiplier : 1f) * multiplier);
            int xp = tuning.gradeXp[g] + (collector ? tuning.collectorBonusXp : 0);   // 경험치에는 콤보 배율을 적용하지 않음
            Score += points;

            Scored?.Invoke(new ScoreEvent
            {
                source = b, grade = b.impact.grade, points = points, xp = xp, combo = Combo, multiplier = multiplier,
            });
        }
    }
}
