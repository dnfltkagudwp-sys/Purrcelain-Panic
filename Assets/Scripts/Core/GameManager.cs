using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace PurrcelainPanic
{
    public enum GameState { Playing, Paused, Over }

    /// <summary>
    /// 게임 상태와 Owner ETA (기획서 3장). ETA는 계속 줄어들기만 하고, 0이 되면 집주인이 돌아와 게임이 끝난다.
    /// ESC로 일시정지, 결과 화면에서 R로 다시 시작한다.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [SerializeField] GameTuning tuning;
        [SerializeField] PawSwipe paws;
        [SerializeField] ExperienceAbilityManager abilities;

        public event Action<GameState> StateChanged;

        public GameState State { get; private set; } = GameState.Playing;
        public float EtaLeft { get; private set; }

        void Awake()
        {
            EtaLeft = tuning.ownerEta;
            Time.timeScale = 1f;   // 다시 시작했을 때 이전 판의 일시정지가 남지 않게
        }

        void FixedUpdate() => Tick(Time.fixedDeltaTime);

        public void Tick(float dt)
        {
            if (State != GameState.Playing)
                return;
            EtaLeft = Mathf.Max(0f, EtaLeft - dt);
            if (EtaLeft <= 0f)
                EndGame();
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null)
                return;
            if (kb.escapeKey.wasPressedThisFrame)
                TogglePause();
            if (State == GameState.Over && kb.rKey.wasPressedThisFrame)
                Restart();
        }

        public void TogglePause()
        {
            if (State == GameState.Over || abilities.IsChoosing)
                return;   // 카드 화면은 카드를 골라야 넘어간다
            SetState(State == GameState.Paused ? GameState.Playing : GameState.Paused);
            Time.timeScale = State == GameState.Paused ? 0f : 1f;
            paws.CancelCharge();
        }

        public void EndGame()
        {
            if (State == GameState.Over)
                return;
            Time.timeScale = 0f;
            paws.CancelCharge();
            SetState(GameState.Over);
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        void SetState(GameState s)
        {
            State = s;
            StateChanged?.Invoke(s);
        }
    }
}
