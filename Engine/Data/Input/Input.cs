
using PBG.MathLibrary;



namespace PBG.Data
{
    public static class Input
    {
        private static KeyState[] Keytates = new KeyState[(int)Key.Menu + 1];
        private static KeyState[] MouseStates = new KeyState[14];
        private static Vector2? _mouseScroll = Vector2.Zero;

        private static Vector2 _oldMousePosition;
        private static Vector2 _oldAbsoluteMousePosition;
        private static Vector2 _fixedOldMousePosition = Vector2.Zero;

        private static List<int> _activeKeys = [];
        private static List<int> _activeMouseButtons = [];

        public static Vector2 ScrollDelta { get; private set; }
        public static Vector2 MousePosition { get; private set; }
        public static Vector2 MouseDelta { get; private set; }
        public static bool MouseMoved { get; private set; }

        public static Vector2 AbsoluteMousePosition { get; private set; }
        public static Vector2 AbsoluteMouseDelta { get; private set; }

        public static Vector2i MovementInput { get; private set; }
        public static Vector2 FixedMousePosition { get; private set; }

        private static CursorMode _oldCursorMode = CursorMode.Normal;

        public static void Start(Silk.NET.Input.IMouse mouse)
        {
            MouseDelta = Vector2.Zero;
            MousePosition = mouse.Position;
            AbsoluteMousePosition = MousePosition;

            mouse.MouseMove += (mouse, position) =>
            {
                MouseMoved = true;

                MousePosition = position;
                AbsoluteMousePosition = MousePosition;

                MouseDelta += MousePosition - _oldMousePosition;
                AbsoluteMouseDelta += AbsoluteMousePosition - _oldAbsoluteMousePosition;

                _oldMousePosition = position;
                _oldAbsoluteMousePosition = AbsoluteMousePosition;
            };
        }

        public static void Update()
        {
            _oldMousePosition = MousePosition;
            _fixedOldMousePosition = FixedMousePosition;

            Vector2i movementInput = Vector2i.Zero;
            if (IsKeyDown(Key.W)) movementInput.Y += 1;
            if (IsKeyDown(Key.S)) movementInput.Y -= 1;
            if (IsKeyDown(Key.A)) movementInput.X += 1;
            if (IsKeyDown(Key.D)) movementInput.X -= 1;

            if (Game.Instance.CursorMode != CursorMode.Disabled)
                FixedMousePosition = MousePosition;

            if (_mouseScroll == null)
            {
                ScrollDelta = (0, 0);
            }
            else
            {
                ScrollDelta = _mouseScroll.Value;
                _mouseScroll = null;
            }

            MovementInput = movementInput;
            _oldCursorMode = Game.Instance.CursorMode;
        }

        public static void LateUpdate()
        {
            for (int i = _activeKeys.Count - 1; i >= 0; i--)
            {
                int key = _activeKeys[i];
                ref var state = ref Keytates[key];

                if (state.Released)
                    _activeKeys.RemoveAt(i);

                state.WasDown = state.IsDown;
                state.Pressed = false;
                state.Released = false;
            }

            for (int i = _activeMouseButtons.Count - 1; i >= 0; i--)
            {
                int key = _activeMouseButtons[i];
                ref var state = ref MouseStates[key];

                if (state.Released)
                    _activeMouseButtons.RemoveAt(i);

                state.WasDown = state.IsDown;
                state.Pressed = false;
                state.Released = false;
            }

            MouseDelta = Vector2.Zero;
            AbsoluteMouseDelta = Vector2.Zero;

            MouseMoved = false;
        }

        public static void OnKeyDown(Key key)
        {
            if (key == Key.Unknown)
                return;

            ref var state = ref Keytates[(int)key];

            if (state.IsDown)
                return;

            state.IsDown = true;
            state.Pressed = true;

            _activeKeys.Add((int)key);
        }

        public static void OnKeyUp(Key key)
        {
            if (key == Key.Unknown)
                return;

            ref var state = ref Keytates[(int)key];

            if (!state.IsDown)
                return;

            state.IsDown = false;
            state.Released = true;
        }

        public static void OnMouseDown(MouseButton button)
        {
            ref var state = ref MouseStates[(int)button];

            if (state.IsDown)
                return;

            state.IsDown = true;
            state.Pressed = true;

            _activeMouseButtons.Add((int)button);
        }

        public static void OnMouseUp(MouseButton button)
        {
            ref var state = ref MouseStates[(int)button];

            if (!state.IsDown)
                return;

            state.IsDown = false;
            state.Released = true;
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

        private struct KeyState
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