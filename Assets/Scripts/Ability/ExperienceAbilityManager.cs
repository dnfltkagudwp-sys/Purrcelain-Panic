using System;
using System.Collections.Generic;
using UnityEngine;

namespace PurrcelainPanic
{
    /// <summary>카드 한 장: 어떤 능력을 몇 레벨로 올리는지.</summary>
    public struct CardOffer
    {
        public AbilityId id;
        public int nextLevel;   // 1이면 새 능력
        public bool IsNew => nextLevel == 1;
    }

    /// <summary>
    /// 경험치를 모아 레벨업하면 게임을 멈추고 카드 3장을 보여 준다 (기획서 17·18장).
    /// 고른 능력의 레벨에 따라 앞발·연쇄·콤보·생성 규칙의 값을 바꾼다.
    /// </summary>
    public class ExperienceAbilityManager : MonoBehaviour
    {
        [SerializeField] GameTuning tuning;
        [SerializeField] ScoreComboManager score;
        [SerializeField] PawSwipe paws;
        [SerializeField] VaseSpawner spawner;

        public event Action<IReadOnlyList<CardOffer>> OffersShown;
        public event Action OffersClosed;
        public event Action AbilitiesChanged;

        public int Level { get; private set; } = 1;
        public int Xp { get; private set; }
        public int XpToNext => tuning.XpRequired(Level);
        public bool IsChoosing { get; private set; }
        public IReadOnlyList<CardOffer> Offers => offers;
        public IReadOnlyList<AbilityId> Slots => slots;   // 얻은 순서

        readonly Dictionary<AbilityId, int> levels = new Dictionary<AbilityId, int>();
        readonly List<AbilityId> slots = new List<AbilityId>();
        readonly List<CardOffer> offers = new List<CardOffer>();
        readonly List<CardOffer> candidates = new List<CardOffer>();
        int pendingLevelUps;
        float timeScaleBeforePause = 1f;

        public int LevelOf(AbilityId id) => levels.TryGetValue(id, out var l) ? l : 0;

        void Awake() => ApplyEffects();
        void OnEnable() => score.Scored += OnScored;
        void OnDisable() => score.Scored -= OnScored;

        void OnScored(ScoreEvent e) => AddXp(e.xp);

        public void AddXp(int amount)
        {
            Xp += amount;
            while (Xp >= XpToNext)
            {
                Xp -= XpToNext;
                Level++;
                pendingLevelUps++;
            }
            if (!IsChoosing)
                ShowNextOffers();
        }

        /// <summary>카드 화면에서 index번째 카드를 고른다 (숫자 1, 2, 3).</summary>
        public void Choose(int index)
        {
            if (!IsChoosing || index < 0 || index >= offers.Count)
                return;
            var pick = offers[index];
            if (pick.IsNew)
                slots.Add(pick.id);
            levels[pick.id] = pick.nextLevel;
            ApplyEffects();
            AbilitiesChanged?.Invoke();
            pendingLevelUps--;
            Close();
            ShowNextOffers();   // 한 번에 여러 레벨이 올랐으면 이어서
        }

        void ShowNextOffers()
        {
            while (pendingLevelUps > 0)
            {
                BuildOffers();
                if (offers.Count > 0)
                {
                    Pause();
                    OffersShown?.Invoke(offers);
                    return;
                }
                // 고를 수 있는 카드가 없음 (모두 최대 레벨): 점수로 대신
                score.AddBonus(tuning.emptyLevelUpBonus);
                pendingLevelUps--;
            }
        }

        void BuildOffers()
        {
            candidates.Clear();
            bool slotsFull = slots.Count >= tuning.maxAbilitySlots;
            foreach (var id in AbilityCatalog.All)
            {
                int l = LevelOf(id);
                if (l >= tuning.maxAbilityLevel)
                    continue;               // 최대 레벨은 제외
                if (l == 0 && slotsFull)
                    continue;               // 슬롯이 차면 보유 능력 강화만
                candidates.Add(new CardOffer { id = id, nextLevel = l + 1 });
            }
            offers.Clear();
            while (offers.Count < tuning.cardsPerLevelUp && candidates.Count > 0)
            {
                int i = UnityEngine.Random.Range(0, candidates.Count);
                offers.Add(candidates[i]);
                candidates.RemoveAt(i);
            }
        }

        void Pause()
        {
            IsChoosing = true;
            if (Time.timeScale > 0f)
                timeScaleBeforePause = Time.timeScale;
            Time.timeScale = 0f;
            paws.CancelCharge();
        }

        void Close()
        {
            IsChoosing = false;
            offers.Clear();
            Time.timeScale = timeScaleBeforePause;
            OffersClosed?.Invoke();
        }

        void ApplyEffects()
        {
            int Lv(AbilityId id) => LevelOf(id);
            paws.ImpulseMultiplier = 1f + tuning.ironPawPerLevel * Lv(AbilityId.IronPaw);
            BreakableVase.ChainTransferBonus = tuning.dominoEffectPerLevel * Lv(AbilityId.DominoEffect);
            paws.CooldownMultiplier = 1f - tuning.quickSwipePerLevel * Lv(AbilityId.QuickSwipe);
            paws.CastRadiusMultiplier = 1f + tuning.wideArcPerLevel * Lv(AbilityId.WideArc);
            paws.AlternatingBonus = tuning.alternatingPerLevel * Lv(AbilityId.AlternatingPaws);
            score.ComboWindowBonus = tuning.comboKeeperPerLevel * Lv(AbilityId.ComboKeeper);

            int display = Lv(AbilityId.DominoDisplay);
            spawner.LineWeightBonus = tuning.dominoDisplayPerLevel * display;
            spawner.Line3Weight = display >= tuning.dominoDisplayLine3Level ? tuning.dominoDisplayLine3Weight : 0f;
            spawner.ExtraVaseChance = tuning.fullSetPerLevel * Lv(AbilityId.FullSet);
            spawner.PackedChance = tuning.packedDisplayPerLevel * Lv(AbilityId.PackedDisplay);
        }
    }
}
