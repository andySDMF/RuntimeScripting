using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    /// <summary>
    /// Input Manager is a simple wrapper for new/old input systems on Unity to allow support for both easily
    /// PureWeb requires new input system
    /// Vuplex keyboard input currently only works with old 
    /// </summary>
    public class InputManager : Singleton<InputManager>
    {
        public static InputManager Instance
        {
            get
            {
                return ((InputManager)instance);
            }
            set
            {
                instance = value;
            }
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        public GameObject HoveredObject(Vector2 position)
        {
            EventSystem uiEventSystem = EventSystem.current;

            if (uiEventSystem != null)
            {
                PointerEventData uiPointerEventData = new PointerEventData(uiEventSystem);
                uiPointerEventData.position = position;

                List<RaycastResult> uiRaycastResultCache = new List<RaycastResult>();

                uiEventSystem.RaycastAll(uiPointerEventData, uiRaycastResultCache);

                if (uiRaycastResultCache.Count > 0)
                {
                    return uiRaycastResultCache[0].gameObject;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }


        public bool IsStandardInputUsed()
        {
#if ENABLE_INPUT_SYSTEM

            return false;

#else
            return true;
#endif
        }

        public bool GetKey(string key)
        {
#if ENABLE_INPUT_SYSTEM

            Key keyEnum = (Key)Enum.Parse(typeof(Key), key);

            return Keyboard.current[keyEnum].isPressed;

#else
            KeyCode keyEnum = (KeyCode)Enum.Parse(typeof(KeyCode), key);

            return Input.GetKey(keyEnum);
#endif
        }

        public string GetKeyName(string key)
        {
#if ENABLE_INPUT_SYSTEM

            string resembler = key;

            //might need to check all just to ensure the correct Key is found on new InputSystem
            switch(key)
            {
                case "Return":
                    resembler = "Enter";
                    break;
            }

            return resembler;

#else
            KeyCode keyEnum = (KeyCode)Enum.Parse(typeof(KeyCode), key);

            if(keyEnum != null)
            {
                return keyEnum.ToString();
            }

            return "";
#endif
        }

        public bool AnyKeyHeldDown()
        {
#if ENABLE_INPUT_SYSTEM

            for(int i = 0; i < Keyboard.current.allKeys.Count; i++)
            {
                if (Keyboard.current.allKeys[i].isPressed)
                {
                    return true;
                }
            }

            return false;

#else
            return Input.anyKeyDown;
#endif
        }

        public bool GetKeyUp(InputKeyCode keyCode)
        {
            return GetKeyUp(keyCode.ToString());
        }

        public bool GetKeyUp(string key)
        {
#if ENABLE_INPUT_SYSTEM

            Key keyEnum = (Key)Enum.Parse(typeof(Key), key);

            return Keyboard.current[keyEnum].wasReleasedThisFrame;

#else
            KeyCode keyEnum = (KeyCode)Enum.Parse(typeof(KeyCode), key);

            return Input.GetKeyUp(keyEnum);
#endif
        }

        public bool GetKeyDown(InputKeyCode keyCode)
        {
            return GetKeyDown(keyCode);
        }

        public bool GetKeyDown(string key)
        {
#if ENABLE_INPUT_SYSTEM

            Key keyEnum = (Key)Enum.Parse(typeof(Key), key);

            return Keyboard.current[keyEnum].wasPressedThisFrame;
#else
            KeyCode keyEnum = (KeyCode)Enum.Parse(typeof(KeyCode), key);

            return Input.GetKeyDown(keyEnum);
#endif
        }

        public Vector2 GetMouseDelta(string axisX, string axisY)
        {
#if ENABLE_INPUT_SYSTEM

            return Mouse.current.delta.ReadValue();
#else
            return new Vector2(Input.GetAxisRaw(axisX), Input.GetAxisRaw(axisY));
#endif
        }

        public bool AnyMouseButtonDown()
        {
#if ENABLE_INPUT_SYSTEM

            if(Mouse.current.rightButton.isPressed || Mouse.current.leftButton.isPressed || Mouse.current.middleButton.isPressed)
            {
                return true;
            }

            return false;
#else
            if(Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2))
            {
                return true;
            }

            return false;
#endif
        }

        public bool GetMouseButton(int index)
        {
#if ENABLE_INPUT_SYSTEM

            if (index == 1) { return Mouse.current.rightButton.isPressed; }

            return Mouse.current.leftButton.isPressed;
#else
            return Input.GetMouseButton(index);
#endif
        }

        public bool GetMouseButtonUp(int index)
        {
#if ENABLE_INPUT_SYSTEM

            if (index == 1) { return Mouse.current.rightButton.wasReleasedThisFrame; }

            return Mouse.current.leftButton.wasReleasedThisFrame;
#else
            return Input.GetMouseButtonUp(index);
#endif
        }

        public bool GetMouseButtonDown(int index)
        {
#if ENABLE_INPUT_SYSTEM

            if (index == 1) { return Mouse.current.rightButton.wasPressedThisFrame; }

            return Mouse.current.leftButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(index);
#endif
        }

        public Vector3 GetMousePosition()
        {
#if ENABLE_INPUT_SYSTEM

            return Mouse.current.position.ReadValue();
#else
            return Input.mousePosition;
#endif
        }

        public float GetMouseScrollWheel()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current.scroll.ReadValue().y;
#else
            return Input.GetAxis("Mouse ScrollWheel");
#endif
        }

        public bool MultipleMouseInputs()
        {
            int count = 0;

            if (GetMouseButton(0))
            {
                count++;
            }

            if (GetMouseButton(1))
            {
                count++;
            }

            if (GetMouseScrollWheel() != 0)
            {
                count++;
            }

            return count > 1;
        }

        /// <summary>
        /// Checks if the user input is within the viewport
        /// </summary>
        /// <returns></returns>
        public bool CheckWithinViewport()
        {
#if ENABLE_INPUT_SYSTEM

            Vector3 vec = Mouse.current.position.ReadValue();

            if (vec.x >= 0.0f && vec.x < Screen.width && vec.y >= 0.0f && vec.y <= Screen.height)
            {
                return true;
            }
            else
            {
                return false;
            }
#else
            if (Input.mousePosition.x >= 0.0f && Input.mousePosition.x < Screen.width && Input.mousePosition.y >= 0.0f && Input.mousePosition.y <= Screen.height)
            {
                return true;
            }
            else
            {
                return false;
            }
#endif
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(InputManager), true)]
        public class InputManager_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();
            }
        }
#endif
    }

    public enum InputKeyCode
    {
        None = 0,
        Space = 1,
        Enter = 2,
        Tab = 3,
        Backquote = 4,
        Quote = 5,
        Semicolon = 6,
        Comma = 7,
        Period = 8,
        Slash = 9,
        Backslash = 10,
        LeftBracket = 11,
        RightBracket = 12,
        Minus = 13,
        Equals = 14,
        A = 15,
        B = 16,
        C = 17,
        D = 18,
        E = 19,
        F = 20,
        G = 21,
        H = 22,
        I = 23,
        J = 24,
        K = 25,
        L = 26,
        M = 27,
        N = 28,
        O = 29,
        P = 30,
        Q = 31,
        R = 32,
        S = 33,
        T = 34,
        U = 35,
        V = 36,
        W = 37,
        X = 38,
        Y = 39,
        Z = 40,
        Digit1 = 41,
        Digit2 = 42,
        Digit3 = 43,
        Digit4 = 44,
        Digit5 = 45,
        Digit6 = 46,
        Digit7 = 47,
        Digit8 = 48,
        Digit9 = 49,
        Digit0 = 50,
        LeftShift = 51,
        RightShift = 52,
        LeftAlt = 53,
        RightAlt = 54,
        AltGr = 54,
        LeftCtrl = 55,
        RightCtrl = 56,
        LeftMeta = 57,
        LeftWindows = 57,
        LeftApple = 57,
        LeftCommand = 57,
        RightMeta = 58,
        RightWindows = 58,
        RightApple = 58,
        RightCommand = 58,
        ContextMenu = 59,
        Escape = 60,
        LeftArrow = 61,
        RightArrow = 62,
        UpArrow = 63,
        DownArrow = 64,
        Backspace = 65,
        PageDown = 66,
        PageUp = 67,
        Home = 68,
        End = 69,
        Insert = 70,
        Delete = 71,
        CapsLock = 72,
        NumLock = 73,
        PrintScreen = 74,
        ScrollLock = 75,
        Pause = 76,
        NumpadEnter = 77,
        NumpadDivide = 78,
        NumpadMultiply = 79,
        NumpadPlus = 80,
        NumpadMinus = 81,
        NumpadPeriod = 82,
        NumpadEquals = 83,
        Numpad0 = 84,
        Numpad1 = 85,
        Numpad2 = 86,
        Numpad3 = 87,
        Numpad4 = 88,
        Numpad5 = 89,
        Numpad6 = 90,
        Numpad7 = 91,
        Numpad8 = 92,
        Numpad9 = 93,
        F1 = 94,
        F2 = 95,
        F3 = 96,
        F4 = 97,
        F5 = 98,
        F6 = 99,
        F7 = 100,
        F8 = 101,
        F9 = 102,
        F10 = 103,
        F11 = 104,
        F12 = 105,
        OEM1 = 106,
        OEM2 = 107,
        OEM3 = 108,
        OEM4 = 109,
        OEM5 = 110,
        IMESelected = 111
    }
}