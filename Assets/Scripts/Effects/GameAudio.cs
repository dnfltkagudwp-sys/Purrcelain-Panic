using System.Collections.Generic;
using UnityEngine;

namespace PurrcelainPanic
{
    /// <summary>게임 이벤트에 연결되는 2D 오디오. 파괴음은 고정된 음원 풀로 동시 재생 수를 제한한다.</summary>
    public class GameAudio : MonoBehaviour
    {
        [SerializeField] GameManager game;
        [SerializeField] PawSwipe paws;
        [SerializeField] ScoreComboManager score;
        [SerializeField] ExperienceAbilityManager abilities;
        [SerializeField] StageDirector stages;
        [SerializeField] AudioClip musicClip;
        [SerializeField] AudioClip[] crack;
        [SerializeField] AudioClip[] smash;
        [SerializeField] AudioClip[] purrfect;
        [SerializeField] AudioClip[] shards;
        [SerializeField] AudioClip hit;
        [SerializeField] AudioClip levelUp;
        [SerializeField] AudioClip select;
        [SerializeField] AudioClip pause;
        [SerializeField] AudioClip resume;
        [SerializeField] AudioClip stage;
        [SerializeField] AudioClip gameOver;
        [SerializeField, Range(0f, 1f)] float musicVolume = 0.12f;
        [SerializeField, Range(0f, 1f)] float effectsVolume = 0.65f;
        [SerializeField, Range(0f, 1f)] float uiVolume = 0.45f;

        readonly AudioSource[] effects = new AudioSource[12];
        readonly System.Random random = new System.Random();
        AudioSource music;
        AudioSource ui;
        AudioClip swipe;
        int nextVoice;
        float nextHitTime;
        bool suspended;

        void Awake()
        {
            music = Source("Music");
            music.clip = musicClip;
            music.loop = true;
            music.volume = musicVolume;
            ui = Source("UI");
            ui.priority = 32;
            for (int i = 0; i < effects.Length; i++)
                effects[i] = Source($"Effect {i + 1}");
            swipe = CreateSwipe();
        }

        void OnEnable()
        {
            paws.SwipeReleased += OnSwipe;
            paws.VaseHit += OnHit;
            score.Scored += OnScored;
            abilities.OffersShown += OnOffers;
            abilities.AbilitiesChanged += OnSelected;
            stages.StageChanged += OnStage;
            game.StateChanged += OnState;
        }

        void Start() => music.Play();

        void OnDisable()
        {
            paws.SwipeReleased -= OnSwipe;
            paws.VaseHit -= OnHit;
            score.Scored -= OnScored;
            abilities.OffersShown -= OnOffers;
            abilities.AbilitiesChanged -= OnSelected;
            stages.StageChanged -= OnStage;
            game.StateChanged -= OnState;
            music.Stop();
            ui.Stop();
            foreach (var source in effects)
                source.Stop();
        }

        void OnDestroy() => Destroy(swipe);

        void Update()
        {
            bool shouldSuspend = game.State == GameState.Paused || abilities.IsChoosing;
            if (shouldSuspend != suspended && game.State != GameState.Over)
            {
                foreach (var source in effects)
                {
                    if (shouldSuspend) source.Pause();
                    else source.UnPause();
                }
                suspended = shouldSuspend;
            }
            // 카드 선택 중에는 BGM을 낮추고, 일시정지 중에는 BGM도 정지한다.
            float target = abilities.IsChoosing ? musicVolume * 0.35f : musicVolume;
            music.volume = Mathf.MoveTowards(music.volume, target, Time.unscaledDeltaTime * 0.5f);
        }

        AudioSource Source(string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(transform, false);
            var source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            return source;
        }

        void Effect(AudioClip clip, float volume, float pitch = 1f)
        {
            var source = effects[nextVoice];
            nextVoice = (nextVoice + 1) % effects.Length;
            source.Stop();
            source.clip = clip;
            source.pitch = pitch;
            source.volume = effectsVolume * volume;
            source.Play();
        }

        void Ui(AudioClip clip) => ui.PlayOneShot(clip, uiVolume);

        void OnSwipe(float charge) =>
            Effect(swipe, Mathf.Lerp(0.25f, 0.5f, charge), Mathf.Lerp(1.15f, 0.85f, charge));

        void OnHit(PawHit hitInfo)
        {
            if (Time.unscaledTime < nextHitTime)
                return;
            nextHitTime = Time.unscaledTime + 0.06f;
            Effect(hit, 0.3f, RandomPitch());
        }

        void OnScored(ScoreEvent e)
        {
            var clips = e.grade == BreakGrade.Purrfect ? purrfect : e.grade == BreakGrade.Smash ? smash : crack;
            Effect(clips[random.Next(clips.Length)], 0.65f + (int)e.grade * 0.15f, RandomPitch());
            if (e.grade != BreakGrade.Crack)
                Effect(shards[(int)e.grade - 1], 0.3f, RandomPitch());
        }

        float RandomPitch() => 0.94f + (float)random.NextDouble() * 0.12f;

        void OnOffers(IReadOnlyList<CardOffer> offers) => Ui(levelUp);
        void OnSelected() => Ui(select);
        void OnStage(int index) { if (index > 0) Ui(stage); }

        void OnState(GameState state)
        {
            if (state == GameState.Ending || state == GameState.Over)
            {
                music.Stop();
                foreach (var source in effects)
                    source.Stop();
                if (state == GameState.Ending)
                    Ui(gameOver);
            }
            else if (state == GameState.Paused)
            {
                music.Pause();
                Ui(pause);
            }
            else if (state == GameState.Playing)
            {
                music.UnPause();
                Ui(resume);
            }
        }

        // 짧은 저역 통과 노이즈에 부드러운 포락선을 적용한 앞발 바람 소리.
        static AudioClip CreateSwipe()
        {
            const int rate = 22050;
            var samples = new float[3307];
            var random = new System.Random(17);
            float filtered = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)(samples.Length - 1);
                filtered = Mathf.Lerp(filtered, (float)random.NextDouble() * 2f - 1f, 0.35f);
                samples[i] = filtered * Mathf.Sin(Mathf.PI * t) * (1f - t);
            }
            var clip = AudioClip.Create("Paw whoosh", samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
