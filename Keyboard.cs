using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace chatsignal {
	// https://stackoverflow.com/questions/1639331/using-global-keyboard-hook-wh-keyboard-ll-in-wpf-c-sharp

	public class KeyboardListener : IDisposable {
		private KeyboardCallbackAsync hookedKeyboardCallbackAsync;
		private InterceptKeys.LowLevelKeyboardProc hookedLowLevelKeyboardProc;

		public KeyboardListener() {
			// we have to store the HookCallback so that it is not garbage collected runtime
			hookedLowLevelKeyboardProc = (InterceptKeys.LowLevelKeyboardProc)LowLevelKeyboardProc;

			hookId = InterceptKeys.SetHook(hookedLowLevelKeyboardProc);
			hookedKeyboardCallbackAsync = new KeyboardCallbackAsync(KeyboardListener_KeyboardCallbackAsync);
		}

		~KeyboardListener() {
			Dispose();
		}

		public event RawKeyEventHandler KeyDown;
		public event RawKeyEventHandler KeyUp;

		private IntPtr hookId = IntPtr.Zero;
		private delegate void KeyboardCallbackAsync(InterceptKeys.KeyEvent keyEvent, int vkCode);

		[MethodImpl(MethodImplOptions.NoInlining)]
		private IntPtr LowLevelKeyboardProc(int nCode, UIntPtr wParam, IntPtr lParam) {
			if (nCode >= 0)
				if (wParam.ToUInt32() == (uint)InterceptKeys.KeyEvent.WM_KEYDOWN ||
					wParam.ToUInt32() == (uint)InterceptKeys.KeyEvent.WM_KEYUP ||
					wParam.ToUInt32() == (uint)InterceptKeys.KeyEvent.WM_SYSKEYDOWN ||
					wParam.ToUInt32() == (uint)InterceptKeys.KeyEvent.WM_SYSKEYUP)
					hookedKeyboardCallbackAsync.BeginInvoke((InterceptKeys.KeyEvent)wParam.ToUInt32(), Marshal.ReadInt32(lParam), null, null);

			return InterceptKeys.CallNextHookEx(hookId, nCode, wParam, lParam);
		}

		void KeyboardListener_KeyboardCallbackAsync(InterceptKeys.KeyEvent keyEvent, int vkCode) {
			switch (keyEvent) {
			// KeyDown events
			case InterceptKeys.KeyEvent.WM_KEYDOWN:
				if (KeyDown != null)
					KeyDown(this, new RawKeyEventArgs(vkCode, false));
				break;
			case InterceptKeys.KeyEvent.WM_SYSKEYDOWN:
				if (KeyDown != null)
					KeyDown(this, new RawKeyEventArgs(vkCode, true));
				break;

			// KeyUp events
			case InterceptKeys.KeyEvent.WM_KEYUP:
				if (KeyUp != null)
					KeyUp(this, new RawKeyEventArgs(vkCode, false));
				break;
			case InterceptKeys.KeyEvent.WM_SYSKEYUP:
				if (KeyUp != null)
					KeyUp(this, new RawKeyEventArgs(vkCode, true));
				break;

			default:
				break;
			}
		}

		public void Dispose() {
			InterceptKeys.UnhookWindowsHookEx(hookId);
		}
	}

	public class RawKeyEventArgs : EventArgs {
		public int VKCode;
		public Key Key;
		public bool IsSysKey;

		public RawKeyEventArgs(int VKCode, bool isSysKey) {
			this.VKCode = VKCode;
			this.IsSysKey = isSysKey;
			this.Key = System.Windows.Input.KeyInterop.KeyFromVirtualKey(VKCode);
		}
	}

	public delegate void RawKeyEventHandler(object sender, RawKeyEventArgs args);

	internal static class InterceptKeys {
		public delegate IntPtr LowLevelKeyboardProc(int nCode, UIntPtr wParam, IntPtr lParam);
		public static int WH_KEYBOARD_LL = 13;

		public enum KeyEvent : int {
			WM_KEYDOWN = 256,
			WM_KEYUP = 257,
			WM_SYSKEYUP = 261,
			WM_SYSKEYDOWN = 260
		}

		public static IntPtr SetHook(LowLevelKeyboardProc proc) {
			using (Process curProcess = Process.GetCurrentProcess())
			using (ProcessModule curModule = curProcess.MainModule) {
				return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
					GetModuleHandle(curModule.ModuleName), 0);
			}
		}

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool UnhookWindowsHookEx(IntPtr hhk);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, UIntPtr wParam, IntPtr lParam);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr GetModuleHandle(string lpModuleName);
	}
}