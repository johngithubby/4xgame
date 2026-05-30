using UnityEngine;
using UnityEngine.UI;

namespace LaneSurvivor.Gameplay
{
    public sealed class SquadLaneInput : MonoBehaviour
    {
        [SerializeField]
        private PlayerSquad playerSquad;

        [SerializeField]
        private Button leftButton;

        [SerializeField]
        private Button rightButton;

        private int lastTouchFrame = -1;

        private bool buttonsRegistered;

        public void Configure(PlayerSquad squad, Button laneLeftButton, Button laneRightButton)
        {
            playerSquad = squad;
            leftButton = laneLeftButton;
            rightButton = laneRightButton;
            RegisterButtons();
        }

        private void OnEnable()
        {
            RegisterButtons();
        }

        private void OnDisable()
        {
            UnregisterButtons();
        }

        private void Update()
        {
            if (playerSquad == null)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                MoveLane(-1);
            }

            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                MoveLane(1);
            }

            HandlePointerLaneInput();
        }

        private void HandlePointerLaneInput()
        {
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                Touch touch = Input.GetTouch(0);

                // Record the touch frame before returning so Unity's synthetic mouse event does not double-handle the same tap.
                lastTouchFrame = Time.frameCount;

                // Lane buttons already move through onClick, so raw gestures only ignore those button rectangles.
                if (!IsPointerOverLaneButton(touch.position))
                {
                    MoveTowardScreenSide(touch.position.x);
                }
            }

            if (Input.GetMouseButtonDown(0) && lastTouchFrame != Time.frameCount && !IsPointerOverLaneButton(Input.mousePosition))
            {
                MoveTowardScreenSide(Input.mousePosition.x);
            }
        }

        private bool IsPointerOverLaneButton(Vector2 screenPosition)
        {
            // Overlay canvases use a null camera for rectangle hit testing.
            return IsPointerOverButton(leftButton, screenPosition) || IsPointerOverButton(rightButton, screenPosition);
        }

        private static bool IsPointerOverButton(Button button, Vector2 screenPosition)
        {
            // Missing buttons should never suppress raw gestures in hand-built test scenes.
            return button != null
                && button.TryGetComponent(out RectTransform rectTransform)
                && RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPosition);
        }

        private void MoveTowardScreenSide(float screenX)
        {
            float thirdWidth = Screen.width / 3f;
            if (screenX < thirdWidth)
            {
                MoveLane(-1);
            }
            else if (screenX > thirdWidth * 2f)
            {
                MoveLane(1);
            }
        }

        private void MoveLane(int direction)
        {
            playerSquad.MoveLane(direction);
        }

        private void MoveLeft()
        {
            MoveLane(-1);
        }

        private void MoveRight()
        {
            MoveLane(1);
        }

        private void RegisterButtons()
        {
            if (buttonsRegistered || leftButton == null || rightButton == null)
            {
                return;
            }

            leftButton.onClick.AddListener(MoveLeft);
            rightButton.onClick.AddListener(MoveRight);
            buttonsRegistered = true;
        }

        private void UnregisterButtons()
        {
            if (!buttonsRegistered || leftButton == null || rightButton == null)
            {
                return;
            }

            leftButton.onClick.RemoveListener(MoveLeft);
            rightButton.onClick.RemoveListener(MoveRight);
            buttonsRegistered = false;
        }
    }
}
