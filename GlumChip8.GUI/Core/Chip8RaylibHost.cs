using GlumChip8Extended.Core;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace GlumChip8.GUI.Core
{
    public class Chip8RaylibHost : HwndHost
    {
        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
        [DllImport("user32.dll")]
        public static extern IntPtr SetFocus(IntPtr hWnd);
        [DllImport("user32.dll")]
        static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        public const int GWL_STYLE = -16;
        public const int WS_CHILD = 0x40000000;
        public const int WS_VISIBLE = 0x10000000;
        private const int WM_ENTERSIZEMOVE = 0x0231;
        private const int WM_EXITSIZEMOVE = 0x0232;

        private IntPtr _raylibHandle;

        private bool _windowResizing = false;
        public bool _pausedEumlation = false;

        public Chip8RaylibHost()
        {
            // Listen to WPF rendering ticks
            CompositionTarget.Rendering += OnRender;
        }

        public void UpdateHandle(IntPtr raylibHandle)
        {
            _raylibHandle = raylibHandle;
        }

        public bool TryMapKey(System.Windows.Input.Key wpfKey, out byte chipKey)
        {
            chipKey = 0;
            switch (wpfKey)
            {
                case System.Windows.Input.Key.D1: chipKey = 0x1; break;
                case System.Windows.Input.Key.D2: chipKey = 0x2; break;
                case System.Windows.Input.Key.D3: chipKey = 0x3; break;
                case System.Windows.Input.Key.D4: chipKey = 0xC; break;
                case System.Windows.Input.Key.Q: chipKey = 0x4; break;
                case System.Windows.Input.Key.W: chipKey = 0x5; break;
                case System.Windows.Input.Key.E: chipKey = 0x6; break;
                case System.Windows.Input.Key.R: chipKey = 0xD; break;
                case System.Windows.Input.Key.A: chipKey = 0x7; break;
                case System.Windows.Input.Key.S: chipKey = 0x8; break;
                case System.Windows.Input.Key.D: chipKey = 0x9; break;
                case System.Windows.Input.Key.F: chipKey = 0xE; break;
                case System.Windows.Input.Key.Z: chipKey = 0xA; break;
                case System.Windows.Input.Key.X: chipKey = 0x0; break;
                case System.Windows.Input.Key.C: chipKey = 0xB; break;
                case System.Windows.Input.Key.V: chipKey = 0xF; break;
                default: return false;
            }
            return true;
        }

        protected override void OnWindowPositionChanged(Rect rcBoundingBox)
        {
            base.OnWindowPositionChanged(rcBoundingBox);

            int newWidth = (int)rcBoundingBox.Width;
            int newHeight = (int)rcBoundingBox.Height;

            if (newWidth > 0 && newHeight > 0)
            {
                // Resize the Win32 Window handle
                MoveWindow(_raylibHandle, 0, 0, newWidth, newHeight, true);

                //Tell Raylib the internal framebuffer has changed size
                Raylib.SetWindowSize(newWidth, newHeight);
            }
        }

        protected override void OnPreviewKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            if (TryMapKey(e.Key, out byte chipKey))
            {
                BusMaster.GlobalBus._keypad._keys[chipKey] = 1; // 1 = Pressed
                BusMaster.GlobalBus._keypad.LastPressed = chipKey;
                e.Handled = true;
            }
        }

        protected override void OnPreviewKeyUp(System.Windows.Input.KeyEventArgs e)
        {
            if (TryMapKey(e.Key, out byte chipKey))
            {
                BusMaster.GlobalBus._keypad._keys[chipKey] = 0; // FIX: 0 = Released
                                                                // Optional: if (Chip8System.Keyboard.LastPressed == chipKey) Chip8System.Keyboard.LastPressed = -1;
                e.Handled = true;
            }
        }

        protected override void OnMouseEnter(System.Windows.Input.MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            if (_raylibHandle != IntPtr.Zero)
            {
                SetFocus(_raylibHandle); // Direct Win32 focus
            }
        }

        private void OnRender(object sender, EventArgs e)
        {
            if (_windowResizing || _raylibHandle == IntPtr.Zero || !BusMaster.IsInitialized || _pausedEumlation) return;
            BusMaster.Run(true);
        }

        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            // Get current styles
            int style = GetWindowLong(_raylibHandle, GWL_STYLE);

            // force the window to be a child and REMOVE top-level/popup styles
            // WS_POPUP (0x80000000) and WS_CHILD (0x40000000) cannot coexist.
            const int WS_POPUP = unchecked((int)0x80000000);
            const int WS_CAPTION = 0x00C00000;
            const int WS_THICKFRAME = 0x00040000;

            style &= ~WS_POPUP;      // Remove popup style
            style &= ~WS_CAPTION;    // Remove title bar
            style &= ~WS_THICKFRAME; // Remove resizing border
            style |= WS_CHILD;       // Add child style
            style |= WS_VISIBLE;     // Ensure it stays visible

            SetWindowLong(_raylibHandle, GWL_STYLE, style);

            //Set the parent to the WPF-provided handle
            SetParent(_raylibHandle, hwndParent.Handle);
            SetFocus(_raylibHandle);
            HwndSource source = (HwndSource)PresentationSource.FromVisual(this);
            source.AddHook(WndProc);
            this.Focusable = true;
            return new HandleRef(this, _raylibHandle);
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);
            // Clear all keys so they don't get "stuck" down
            Array.Clear(BusMaster.GlobalBus._keypad._keys, 0, 16);
            BusMaster.GlobalBus._keypad.LastPressed = -1;
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_KEYDOWN = 0x0100;
            const int WM_KEYUP = 0x0101;

            if (msg == WM_KEYDOWN || msg == WM_KEYUP)
            {
                Key wpfKey = KeyInterop.KeyFromVirtualKey((int)wParam);
                if (TryMapKey(wpfKey, out byte chipKey))
                {
                    BusMaster.GlobalBus._keypad._keys[chipKey] = (msg == WM_KEYDOWN) ? (byte)1 : (byte)0;
                    handled = true;
                    return IntPtr.Zero;
                }
            }

            if (msg == WM_ENTERSIZEMOVE)
                _windowResizing = true;
            else if (msg == WM_EXITSIZEMOVE)
                _windowResizing = false;

            return IntPtr.Zero;
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            CompositionTarget.Rendering -= OnRender;
            Raylib.CloseWindow();
            //Raylib.CloseAudioDevice();
        }

        public void ToggleFps()
        {
            BusMaster.Settings.drawFPS = BusMaster.Settings.drawFPS ^ true;
        }

        public void TogglePause()
        {
            _pausedEumlation = _pausedEumlation ^ true;
        }
    }
}
