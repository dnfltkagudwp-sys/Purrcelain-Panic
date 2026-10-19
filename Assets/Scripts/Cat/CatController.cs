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
            paws.SetHeld(Paw.Left, leftPawAction.IsPressed());
            paws.SetHeld(Paw.Right, rightPawAction.IsPressed());
            Move(moveAction.ReadValue<float>(), Time.deltaTime);
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
