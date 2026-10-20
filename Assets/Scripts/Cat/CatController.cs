using UnityEngine;
using UnityEngine.InputSystem;

namespace PurrcelainPanic
{
    /// <summary>
    /// 입력을 받아 고양이를 X축으로 옮기고, 앞발 버튼 상태를 PawSwipe에 넘긴다.
    /// 하체가 보이지 않으므로 걷기 애니메이션 없이 위치만 직접 옮긴다.
    /// </summary>
    [RequireComponent(typeof(PawSwipe))]
    public class CatController : MonoBehaviour
    {
        [SerializeField] GameTuning tuning;

        PawSwipe paws;
        InputAction moveAction;
        InputAction leftPawAction;
        InputAction rightPawAction;
        float velocityX;
        float rangeCenterX;
        bool waitMoveRelease, waitLeftRelease, waitRightRelease;

        public float MoveRangeX { get; set; }

        void Awake()
        {
            paws = GetComponent<PawSwipe>();
            rangeCenterX = transform.position.x;
            MoveRangeX = tuning.moveRangeX;

            moveAction = new InputAction("Move", InputActionType.Value);
            moveAction.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/a")
                .With("Positive", "<Keyboard>/d");
            moveAction.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/leftArrow")
                .With("Positive", "<Keyboard>/rightArrow");
            leftPawAction = new InputAction("LeftPaw", InputActionType.Button, "<Mouse>/leftButton");
            rightPawAction = new InputAction("RightPaw", InputActionType.Button, "<Mouse>/rightButton");
        }

        void OnEnable()
        {
            moveAction.Enable();
            leftPawAction.Enable();
            rightPawAction.Enable();
        }

        void OnDisable()
        {
            moveAction.Disable();
            leftPawAction.Disable();
            rightPawAction.Disable();
        }

        void OnDestroy()
        {
            moveAction.Dispose();
            leftPawAction.Dispose();
            rightPawAction.Dispose();
        }

        void Update()
        {
            // 멈춰 있는 동안(카드 선택 등)에는 입력을 받지 않는다. 다시 진행된 뒤에도
            // 멈추기 전부터 누르고 있던 키·버튼은 한 번 뗄 때까지 무시해서, 카드를 고르자마자 고양이가 달려가지 않게 한다
            if (Time.timeScale <= 0f)
            {
                velocityX = 0f;
                paws.SetHeld(Paw.Left, false);
                paws.SetHeld(Paw.Right, false);
                paws.Moving = false;
                waitMoveRelease = waitLeftRelease = waitRightRelease = true;
                return;
            }

            float input = moveAction.ReadValue<float>();
            bool left = leftPawAction.IsPressed(), right = rightPawAction.IsPressed();
            if (waitMoveRelease && input == 0f) waitMoveRelease = false;
            if (waitLeftRelease && !left) waitLeftRelease = false;
            if (waitRightRelease && !right) waitRightRelease = false;

            paws.SetHeld(Paw.Left, left && !waitLeftRelease);
            paws.SetHeld(Paw.Right, right && !waitRightRelease);
            Move(waitMoveRelease ? 0f : input, Time.deltaTime);
        }

        void Move(float input, float dt)
        {
            float maxSpeed = tuning.moveSpeed * (paws.IsCharging ? tuning.chargingMoveMultiplier : 1f);
            float accel = tuning.moveSpeed / Mathf.Max(tuning.moveAccelTime, 0.001f);
            velocityX = Mathf.MoveTowards(velocityX, input * maxSpeed, accel * dt);

            Vector3 p = transform.position;
            p.x += velocityX * dt;
            float min = rangeCenterX - MoveRangeX, max = rangeCenterX + MoveRangeX;
            if (p.x <= min || p.x >= max)
            {
                p.x = Mathf.Clamp(p.x, min, max);
                velocityX = 0f;
            }
            transform.position = p;
            paws.Moving = Mathf.Abs(velocityX) > tuning.tuckMoveSpeed;
        }
    }
}
