using UnityEngine;

namespace RedBjorn.ProtoTiles.Example
{
    public class MyInput
    {
        public class FrameInfo
        {
            public int Frame;
            public GameObject OverObject;
            public Vector3 CameraGroundPosition;
        }

        static FrameInfo LastFrame = new FrameInfo();

        static Vector3 MousePosition
        {
            get
            {
                Vector3 result;
#if ENABLE_INPUT_SYSTEM
                result = UnityEngine.InputSystem.Mouse.current.position.value;
#elif ENABLE_LEGACY_INPUT_MANAGER
                result = Input.mousePosition;
#endif
                return result;
            }
        }

        static void Validate(Plane plane)
        {
            if (LastFrame.Frame != Time.frameCount)
            {
                LastFrame.Frame = Time.frameCount;
                RaycastHit hit;
                if (Physics.Raycast(Camera.main.ScreenPointToRay(MousePosition), out hit, 100f))
                {
                    LastFrame.OverObject = hit.collider.gameObject;
                }
                else
                {
                    LastFrame.OverObject = null;
                }
                var screemCenterRay = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
                float enter = 0f;
                if (plane.Raycast(screemCenterRay, out enter))
                {
                    LastFrame.CameraGroundPosition = screemCenterRay.GetPoint(enter);
                }
                else
                {
                    LastFrame.CameraGroundPosition = Vector3.zero;
                }
            }
        }

        public static bool GetOnWorldDownFree(Plane plane)
        {
            Validate(plane);
            var result = false;
#if ENABLE_INPUT_SYSTEM
            result = UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            result = UnityEngine.Input.GetMouseButtonDown(0);
#endif
            return result;
        }

        public static bool GetOnWorldUpFree(Plane plane)
        {
            Validate(plane);
            var result = false;
#if ENABLE_INPUT_SYSTEM
            result = UnityEngine.InputSystem.Mouse.current.leftButton.wasReleasedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            result = UnityEngine.Input.GetMouseButtonUp(0);
#endif
            return result;
        }

        public static bool GetOnWorldUp(Plane plane)
        {
            Validate(plane);
            return GetOnWorldUpFree(plane) && !CameraController.IsMovingByPlayer;
        }

        public static bool GetOnWorldFree(Plane plane)
        {
            Validate(plane);
            var result = false;
#if ENABLE_INPUT_SYSTEM
            result = UnityEngine.InputSystem.Mouse.current.leftButton.isPressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
            result = UnityEngine.Input.GetMouseButton(0);
#endif
            return result;
        }

        public static Vector3 CameraGroundPosition(Plane plane)
        {
            Validate(plane);
            return LastFrame.CameraGroundPosition;
        }

        public static Vector3 GroundPosition(Plane plane)
        {
            var mouseRay = Camera.main.ScreenPointToRay(MousePosition);
            float enter = 0f;
            if (plane.Raycast(mouseRay, out enter))
            {
                return mouseRay.GetPoint(enter);
            }
            return Vector3.zero;
        }

        public static Vector3 GroundPositionCameraOffset(Plane plane)
        {
            var mouseRay = Camera.main.ScreenPointToRay(MousePosition);
            float enter = 0f;
            if (plane.Raycast(mouseRay, out enter))
            {
                return mouseRay.GetPoint(enter) - Camera.main.transform.position;
            }
            return Vector3.zero;
        }

        public static bool GetGKeyUp()
        {
            var result = false;
#if ENABLE_INPUT_SYSTEM
            result = UnityEngine.InputSystem.Keyboard.current.gKey.wasReleasedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            result = UnityEngine.Input.GetKeyUp(UnityEngine.KeyCode.G);
#endif
            return result;
        }
    }
}

