using System;
using System.Runtime.InteropServices;

namespace Docker;

internal static class NativeMethods
{
	[DllImport("User32.dll", CharSet = CharSet.Auto)]
	public static extern int SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
}
