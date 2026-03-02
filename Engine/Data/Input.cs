using PBG.MathLibrary;
using Silk.NET.Input;

namespace PBG.Data
{
    public static class Input
    {
        private static Keytate[] Keytates = new Keytate[(int)Key.Menu + 1];
        private static Keytate[] MouseStates = new Keytate[14];
        private static Vector2? _mouseScroll = Vector2.Zero;

        private static Vector2 _oldMousePosition;
        private static Vector2 _fixedOldMousePosition = Vector2.Zero;

        public static HashSet<Key> PressedKey = new HashSet<Key>();
        public static HashSet<MouseButton> PressedButtons = new HashSet<MouseButton>();

        public static Vector2 ScrollDelta { get; private set; }
        public static Vector2 MousePosition { get; private set; }
        public static Vector2 MouseDelta { get; private set; }
        public static Vector2i MovementInput { get; private set; }
        public static Vector2 FixedMousePosition { get; private set; }

        private static CursorMode _oldCursorMode = CursorMode.Normal;

        public static void Start(IMouse mouse)
        {
            MouseDelta = Vector2.Zero;
            MousePosition = mouse.Position;
        }

        public static void Update(IMouse mouse)
        {
            HandleStates(Keytates);
            HandleStates(MouseStates);

            _oldMousePosition = MousePosition;
            _fixedOldMousePosition = FixedMousePosition;

            Vector2i movementInput = Vector2i.Zero;
            if (IsKeyDown(Key.W))
                movementInput.Y += 1;
            if (IsKeyDown(Key.S))
                movementInput.Y -= 1;

            if (IsKeyDown(Key.A))
                movementInput.X += 1;
            if (IsKeyDown(Key.D))
                movementInput.X -= 1;

            if (!Game.IsCursorState(CursorMode.Disabled))
                FixedMousePosition = mouse.Position;
            
            if (_mouseScroll == null)
            {
                ScrollDelta = (0, 0);
            }
            else
            {
                ScrollDelta = _mouseScroll.Value;
                _mouseScroll = null;
            }
            
            MousePosition = mouse.Position;

            if (_oldCursorMode != CursorMode.Disabled || Game.Instance.CursorMode != CursorMode.Normal)
                MouseDelta = MousePosition - _oldMousePosition;

            MovementInput = movementInput;

            _oldCursorMode = Game.Instance.CursorMode;
        }

        public static void OnKeyDown(Key key)
        {
            if (key == Key.Unknown)
                return;
            
            var state = Keytates[(int)key];
            if (state.IsDown)
                return;

            state.ConfirmPressed = true;
            state.ConfirmReleased = false;
            Keytates[(int)key] = state;
        }

        public static void OnKeyUp(Key key)
        {
            if (key == Key.Unknown)
                return;

            var state = Keytates[(int)key];
            state.ConfirmReleased = true;
            state.ConfirmPressed = false;
            Keytates[(int)key] = state;
        }

        public static void OnMouseDown(MouseButton button)
        {
            var state = MouseStates[(int)button];
            if (state.IsDown)
                return;

            state.ConfirmPressed = true;
            state.ConfirmReleased = false;
            MouseStates[(int)button] = state;
        }

        public static void OnMouseUp(MouseButton button)
        {
            var state = MouseStates[(int)button];
            state.ConfirmReleased = true;
            state.ConfirmPressed = false;
            MouseStates[(int)button] = state;
        }

        public static void OnMouseWheel(Vector2 scroll)
        {
            if (_mouseScroll != null)
                _mouseScroll += scroll;
            else
                _mouseScroll = scroll;
        }

        public static bool IsMousePressed(MouseButton button)
        {
            return MouseStates[(int)button].Pressed;
        }

        public static bool IsMouseDown(MouseButton button)
        {
            return MouseStates[(int)button].IsDown;
        }

        public static bool IsMouseReleased(MouseButton button)
        {
            return MouseStates[(int)button].Released;
        }

        public static bool IsKeyPressed(Key key)
        {
            return Keytates[(int)key].Pressed;
        }

        public static bool IsKeyDown(Key key)
        {
            return Keytates[(int)key].IsDown;
        }

        public static bool IsKeyReleased(Key key)
        {
            return Keytates[(int)key].Released;
        }


        public static bool IsKeyAndControlPressed(Key key)
        {
            return IsKeyDown(Key.ControlLeft) && IsKeyPressed(key);
        }

        public static bool IsAnyKeyPressed(params Key[] Key)
        {
            foreach (var k in Key)
            {
                if (IsKeyPressed(k))
                    return true;
            }
            return false;
        }

        public static bool IsAnyKeyReleased(params Key[] Key)
        {
            foreach (var k in Key)
            {
                if (IsKeyReleased(k))
                    return true;
            }
            return false;
        }

        public static bool AreKeyPressed(params Key[] Key)
        {
            return Key.All(IsKeyPressed);
        }

        public static bool AreKeyDown(out int index, params Key[] Key)
        {
            index = 0;
            foreach (var k in Key)
            {
                if (Keytates[(int)k].IsDown)
                    return true;
                index++;
            }

            index = -1;
            return false;
        }

        public static bool AreKeyDown(out Key? key, params Key[] Key)
        {
            if (AreKeyDown(out int index, Key))
            {
                key = Key[index];
                return true;
            }

            key = null;
            return false;
        }

        public static bool AreKeysDown(params Key[] Key)
        {
            return AreKeyDown(out int _, Key);
        }

        public static bool AreAllKeysDown(params Key[] Key)
        {
            return Key.All(k => Keytates[(int)k].IsDown);
        }

        public static Vector2 GetMousePosition()
        {
            return FixedMousePosition;
        }

        public static Vector3 GetMousePosition3()
        {
            return new Vector3(FixedMousePosition.X, FixedMousePosition.Y, 0f);
        }

        public static Vector2 GetMouseDelta()
        {
            return MouseDelta;
        }

        public static Vector2 GetFixedMouseDelta()
        {
            return FixedMousePosition - _fixedOldMousePosition;
        }

        public static Vector2 GetOldMousePosition()
        {
            return _oldMousePosition;
        }

        public static Vector2 GetMouseScrollDelta()
        {
            return ScrollDelta;
        }

        public static bool AnyKeyReleased(params Key[] Key)
        {
            foreach (var k in Key)
            {
                if (IsKeyReleased(k))
                    return true;
            }
            return false;
        }


        private static void HandleStates(Keytate[] states)
        {
            for (int i = 0; i < states.Length; i++)
            {
                var state = states[i];

                state.WasDown = state.IsDown;

                if (state.ConfirmPressed)
                {
                    state.Pressed = true;
                    state.IsDown = true;
                    state.ConfirmPressed = false;
                    state.ConfirmReleased = false;
                    state.Released = false;
                }
                else
                {
                    state.Pressed = false;
                }

                if (state.ConfirmReleased)
                {
                    state.Released = true;
                    state.IsDown = false;
                    state.ConfirmPressed = false;
                    state.ConfirmReleased = false;
                    state.Pressed = false;
                }
                else
                {
                    state.Released = false;
                }

                states[i] = state;
            }
        }

        private struct Keytate
        {
            public bool IsDown;
            public bool WasDown;
            public bool ConfirmPressed;
            public bool Pressed;
            public bool ConfirmReleased;
            public bool Released;
        }
    }
}