using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Text;

namespace GlumChip8Extended.Core
{
    public class Keypad
    {
        public readonly byte[] _keys = new byte[16];
        private int _lastPressed = -1;

        // Make LastPressed read/write for WPF updates
        public int LastPressed
        {
            get => _lastPressed;
            set => _lastPressed = value;
        }

        private readonly Dictionary<Raylib_cs.KeyboardKey, byte> _keyMap = new()
    {
        { KeyboardKey.One, 0x1 }, { KeyboardKey.Two, 0x2 }, { KeyboardKey.Three, 0x3 }, { KeyboardKey.Four, 0xC },
        { KeyboardKey.Q, 0x4 },   { KeyboardKey.W, 0x5 },   { KeyboardKey.E, 0x6 },     { KeyboardKey.R, 0xD },
        { KeyboardKey.A, 0x7 },   { KeyboardKey.S, 0x8 },   { KeyboardKey.D, 0x9 },     { KeyboardKey.F, 0xE },
        { KeyboardKey.Z, 0xA },   { KeyboardKey.X, 0x0 },   { KeyboardKey.C, 0xB },     { KeyboardKey.V, 0xF }
    };

        public void UpdateFromPCKeyBoard()
        {
            _lastPressed = -1;

            foreach (var pair in _keyMap)
            {
                if (Raylib.IsKeyDown(pair.Key))
                {
                    _keys[pair.Value] = 1;
                    _lastPressed = pair.Value; 
                }
                else
                {
                    _keys[pair.Value] = 0;
                }
            }
        }

        public int GetLastPressedHexKey()
        {
            var val = _lastPressed;
            _lastPressed = -1;
            return val;
        }

        public void Reset()
        {
            Array.Clear(_keys);
        }
    }
}
