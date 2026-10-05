using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace plomfX.Services
{
    public static class GlobalHotkeyService
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public const int WM_HOTKEY = 0x0312;

        private static int _currentId = 9000;
        private static IntPtr _hWnd = IntPtr.Zero;
        private static int _activeId = -1;
        private static Action? _callback;

        public static void Initialize(Window window)
        {
            var helper = new WindowInteropHelper(window);
            _hWnd = helper.Handle;
            HwndSource.FromHwnd(_hWnd)?.AddHook(HwndHook);
        }

        private static IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == _activeId)
            {
                _callback?.Invoke();
                handled = true;
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// Registers a global hotkey. Modifiers is a WPF ModifierKeys value.
        /// Returns true on success (fails if another app already owns the combo).
        /// </summary>
        public static bool Register(ModifierKeys modifiers, Key key, Action callback)
        {
            Unregister();

            uint mod = 0;
            if (modifiers.HasFlag(ModifierKeys.Alt)) mod |= 0x0001;
            if (modifiers.HasFlag(ModifierKeys.Control)) mod |= 0x0002;
            if (modifiers.HasFlag(ModifierKeys.Shift)) mod |= 0x0004;
            if (modifiers.HasFlag(ModifierKeys.Windows)) mod |= 0x0008;

            uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);
            _activeId = _currentId++;
            _callback = callback;

            if (RegisterHotKey(_hWnd, _activeId, mod, vk))
                return true;

            _activeId = -1;
            _callback = null;
            return false;
        }

        public static void Unregister()
        {
            if (_activeId != -1)
            {
                UnregisterHotKey(_hWnd, _activeId);
                _activeId = -1;
                _callback = null;
            }
        }
    }
}