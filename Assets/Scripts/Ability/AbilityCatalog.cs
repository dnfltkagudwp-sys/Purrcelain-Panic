namespace PurrcelainPanic
{
    /// <summary>능력 카드 11종. 모두 패시브이며 액티브 능력은 없다.</summary>
    public enum AbilityId
    {
        IronPaw,
        DominoEffect,
        QuickSwipe,
        WideArc,
        AlternatingPaws,
        ComboKeeper,
        DominoDisplay,
        FullSet,
        PackedDisplay,
        ClawGust,
        QuickCharge,
    }

    /// <summary>카드에 보여 줄 이름과 레벨별 설명. 기획서의 영문 이름은 작은 부제로 함께 보여 준다.</summary>
    public static class AbilityCatalog
    {
        public static readonly AbilityId[] All = (AbilityId[])System.Enum.GetValues(typeof(AbilityId));

        public static string Name(AbilityId id) => id switch
        {
            AbilityId.IronPaw => "강철 앞발",
            AbilityId.DominoEffect => "도미노 효과",
            AbilityId.QuickSwipe => "재빠른 앞발",
            AbilityId.WideArc => "넓은 휘두르기",
            AbilityId.AlternatingPaws => "번갈아 치기",
            AbilityId.ComboKeeper => "콤보 유지",
            AbilityId.DominoDisplay => "도미노 진열",
            AbilityId.FullSet => "풀 세트",
            AbilityId.PackedDisplay => "빽빽한 진열",
            AbilityId.QuickCharge => "순간 집중",
            _ => "발톱 바람",
        };

        public static string EnglishName(AbilityId id) => id switch
        {
            AbilityId.IronPaw => "Iron Paw",
            AbilityId.DominoEffect => "Domino Effect",
            AbilityId.QuickSwipe => "Quick Swipe",
            AbilityId.WideArc => "Wide Arc",
            AbilityId.AlternatingPaws => "Alternating Paws",
            AbilityId.ComboKeeper => "Combo Keeper",
            AbilityId.DominoDisplay => "Domino Display",
            AbilityId.FullSet => "Full Set",
            AbilityId.PackedDisplay => "Packed Display",
            AbilityId.QuickCharge => "Quick Charge",
            _ => "Claw Gust",
        };

        /// <summary>그 레벨이 됐을 때의 효과 (누적값).</summary>
        public static string Describe(AbilityId id, int level, GameTuning t) => id switch
        {
            AbilityId.IronPaw => $"휘두르는 힘 +{Pct(t.ironPawPerLevel * level)}",
            AbilityId.DominoEffect => $"도자기끼리 부딪힐 때 힘 전달 {Pct(System.Math.Min(t.chainTransferRate + t.dominoEffectPerLevel * level, 0.95f))}\n(기본 {Pct(t.chainTransferRate)})",
            AbilityId.QuickSwipe => $"앞발 재사용 대기시간 -{Pct(t.quickSwipePerLevel * level)}",
            AbilityId.QuickCharge => $"최대 충전까지 걸리는 시간 -{Pct(t.quickChargePerLevel * level)}\n{t.maxChargeTime:0.##}초 → {t.maxChargeTime * (1f - t.quickChargePerLevel * level):0.##}초",
            AbilityId.WideArc => $"휘두르기 판정 범위 +{Pct(t.wideArcPerLevel * level)}",
            AbilityId.AlternatingPaws => $"{t.alternatingWindow:0.#}초 안에 반대 앞발로 맞히면\n다음 휘두르기 힘 +{Pct(t.alternatingPerLevel * level)}",
            AbilityId.ComboKeeper => $"콤보 유지시간 +{t.comboKeeperPerLevel * level:0.##}초",
            AbilityId.DominoDisplay => level >= t.dominoDisplayLine3Level
                ? $"앞뒤로 줄 선 도자기가 더 자주 나옴\n3개짜리 줄도 등장"
                : $"앞뒤로 줄 선 도자기가 더 자주 나옴",
            AbilityId.FullSet => $"{Pct(t.fullSetPerLevel * level)} 확률로 도자기가 1개 더 나옴",
            AbilityId.PackedDisplay => $"{Pct(t.packedDisplayPerLevel * level)} 확률로 다른 도자기 옆에 나옴",
            _ => $"발톱 바람이 손끝보다 {(t.shockwaveRange + t.clawGustPerLevel * level) * 100f:0}cm 더 멀리 닿음\n(기본 {t.shockwaveRange * 100f:0}cm)",
        };

        static string Pct(float v) => $"{v * 100f:0}%";
    }
}
