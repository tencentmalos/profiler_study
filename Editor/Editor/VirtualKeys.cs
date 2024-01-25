using System.Collections.Generic;

namespace Editor;

public class VirtualKeys
{
	public enum Key
	{
		Invalid = -1,
		None = 0,
		VK_LBUTTON = 1,
		VK_RBUTTON = 2,
		VK_CANCEL = 3,
		VK_MBUTTON = 4,
		VK_XBUTTON1 = 5,
		VK_XBUTTON2 = 6,
		VK_BACK = 8,
		VK_TAB = 9,
		VK_CLEAR = 12,
		VK_RETURN = 13,
		VK_SHIFT = 16,
		VK_CONTROL = 17,
		VK_MENU = 18,
		VK_PAUSE = 19,
		VK_CAPITAL = 20,
		VK_KANA = 21,
		VK_HANGUL = 21,
		VK_JUNJA = 23,
		VK_FINAL = 24,
		VK_HANJA = 25,
		VK_KANJI = 25,
		VK_ESCAPE = 27,
		VK_CONVERT = 28,
		VK_NONCONVERT = 29,
		VK_ACCEPT = 30,
		VK_MODECHANGE = 31,
		VK_SPACE = 32,
		VK_PRIOR = 33,
		VK_NEXT = 34,
		VK_END = 35,
		VK_HOME = 36,
		VK_LEFT = 37,
		VK_UP = 38,
		VK_RIGHT = 39,
		VK_DOWN = 40,
		VK_SELECT = 41,
		VK_PRINT = 42,
		VK_EXECUTE = 43,
		VK_SNAPSHOT = 44,
		VK_INSERT = 45,
		VK_DELETE = 46,
		VK_HELP = 47,
		VK_0 = 48,
		VK_1 = 49,
		VK_2 = 50,
		VK_3 = 51,
		VK_4 = 52,
		VK_5 = 53,
		VK_6 = 54,
		VK_7 = 55,
		VK_8 = 56,
		VK_9 = 57,
		VK_A = 65,
		VK_B = 66,
		VK_C = 67,
		VK_D = 68,
		VK_E = 69,
		VK_F = 70,
		VK_G = 71,
		VK_H = 72,
		VK_I = 73,
		VK_J = 74,
		VK_K = 75,
		VK_L = 76,
		VK_M = 77,
		VK_N = 78,
		VK_O = 79,
		VK_P = 80,
		VK_Q = 81,
		VK_R = 82,
		VK_S = 83,
		VK_T = 84,
		VK_U = 85,
		VK_V = 86,
		VK_W = 87,
		VK_X = 88,
		VK_Y = 89,
		VK_Z = 90,
		VK_LWIN = 91,
		VK_RWIN = 92,
		VK_APPS = 93,
		VK_SLEEP = 95,
		VK_NUMPAD0 = 96,
		VK_NUMPAD1 = 97,
		VK_NUMPAD2 = 98,
		VK_NUMPAD3 = 99,
		VK_NUMPAD4 = 100,
		VK_NUMPAD5 = 101,
		VK_NUMPAD6 = 102,
		VK_NUMPAD7 = 103,
		VK_NUMPAD8 = 104,
		VK_NUMPAD9 = 105,
		VK_MULTIPLY = 106,
		VK_ADD = 107,
		VK_SEPARATOR = 108,
		VK_SUBTRACT = 109,
		VK_DECIMAL = 110,
		VK_DIVIDE = 111,
		VK_F1 = 112,
		VK_F2 = 113,
		VK_F3 = 114,
		VK_F4 = 115,
		VK_F5 = 116,
		VK_F6 = 117,
		VK_F7 = 118,
		VK_F8 = 119,
		VK_F9 = 120,
		VK_F10 = 121,
		VK_F11 = 122,
		VK_F12 = 123,
		VK_F13 = 124,
		VK_F14 = 125,
		VK_F15 = 126,
		VK_F16 = 127,
		VK_F17 = 128,
		VK_F18 = 129,
		VK_F19 = 130,
		VK_F20 = 131,
		VK_F21 = 132,
		VK_F22 = 133,
		VK_F23 = 134,
		VK_F24 = 135,
		VK_NUMLOCK = 144,
		VK_SCROLL = 145,
		VK_LSHIFT = 160,
		VK_RSHIFT = 161,
		VK_LCONTROL = 162,
		VK_RCONTROL = 163,
		VK_LMENU = 164,
		VK_RMENU = 165,
		VK_BROWSER_BACK = 166,
		VK_BROWSER_FORWARD = 167,
		VK_BROWSER_REFRESH = 168,
		VK_BROWSER_STOP = 169,
		VK_BROWSER_SEARCH = 170,
		VK_BROWSER_FAVORITES = 171,
		VK_BROWSER_HOME = 172,
		VK_VOLUME_MUTE = 173,
		VK_VOLUME_DOWN = 174,
		VK_VOLUME_UP = 175,
		VK_MEDIA_NEXT_TRACK = 176,
		VK_MEDIA_PREV_TRACK = 177,
		VK_MEDIA_STOP = 178,
		VK_MEDIA_PLAY_PAUSE = 179,
		VK_LAUNCH_MAIL = 180,
		VK_LAUNCH_MEDIA_SELECT = 181,
		VK_LAUNCH_APP1 = 182,
		VK_LAUNCH_APP2 = 183,
		VK_OEM_1 = 186,
		VK_OEM_PLUS = 187,
		VK_OEM_COMMA = 188,
		VK_OEM_MINUS = 189,
		VK_OEM_PERIOD = 190,
		VK_OEM_2 = 191,
		VK_OEM_3 = 192,
		VK_OEM_4 = 219,
		VK_OEM_5 = 220,
		VK_OEM_6 = 221,
		VK_OEM_7 = 222,
		VK_OEM_8 = 223,
		VK_OEM_102 = 226,
		VK_PROCESSKEY = 229,
		VK_PACKET = 231,
		VK_ATTN = 246,
		VK_CRSEL = 247,
		VK_EXSEL = 248,
		VK_EREOF = 249,
		VK_PLAY = 250,
		VK_ZOOM = 251,
		VK_NONAME = 252,
		VK_PA1 = 253,
		VK_OEM_CLEAR = 254
	}

	private static Dictionary<Key, string> m_VirtualKeyNames;

	static VirtualKeys()
	{
		m_VirtualKeyNames = new Dictionary<Key, string>();
		m_VirtualKeyNames[Key.None] = "None";
		m_VirtualKeyNames[Key.VK_LBUTTON] = "Left mouse button";
		m_VirtualKeyNames[Key.VK_RBUTTON] = "Right mouse button";
		m_VirtualKeyNames[Key.VK_CANCEL] = "Control-break processing";
		m_VirtualKeyNames[Key.VK_MBUTTON] = "Middle mouse button(three-button mouse)";
		m_VirtualKeyNames[Key.VK_XBUTTON1] = "X1 mouse button";
		m_VirtualKeyNames[Key.VK_XBUTTON2] = "X2 mouse button";
		m_VirtualKeyNames[Key.VK_BACK] = "BACKSPACE key";
		m_VirtualKeyNames[Key.VK_TAB] = "TAB key";
		m_VirtualKeyNames[Key.VK_CLEAR] = "CLEAR key";
		m_VirtualKeyNames[Key.VK_RETURN] = "ENTER key";
		m_VirtualKeyNames[Key.VK_SHIFT] = "SHIFT key";
		m_VirtualKeyNames[Key.VK_CONTROL] = "CTRL key";
		m_VirtualKeyNames[Key.VK_MENU] = "ALT key";
		m_VirtualKeyNames[Key.VK_PAUSE] = "PAUSE key";
		m_VirtualKeyNames[Key.VK_CAPITAL] = "CAPS LOCK key";
		m_VirtualKeyNames[Key.VK_KANA] = "IME Kana mode";
		m_VirtualKeyNames[Key.VK_KANA] = "IME Hangul mode";
		m_VirtualKeyNames[Key.VK_JUNJA] = "IME Junja mode";
		m_VirtualKeyNames[Key.VK_FINAL] = "IME final mode";
		m_VirtualKeyNames[Key.VK_HANJA] = "IME Hanja mode";
		m_VirtualKeyNames[Key.VK_HANJA] = "IME Kanji mode";
		m_VirtualKeyNames[Key.VK_ESCAPE] = "ESC key";
		m_VirtualKeyNames[Key.VK_CONVERT] = "IME convert";
		m_VirtualKeyNames[Key.VK_NONCONVERT] = "IME nonconvert";
		m_VirtualKeyNames[Key.VK_ACCEPT] = "IME accept";
		m_VirtualKeyNames[Key.VK_MODECHANGE] = "IME mode change request";
		m_VirtualKeyNames[Key.VK_SPACE] = "SPACEBAR";
		m_VirtualKeyNames[Key.VK_PRIOR] = "PAGE UP key";
		m_VirtualKeyNames[Key.VK_NEXT] = "PAGE DOWN key";
		m_VirtualKeyNames[Key.VK_END] = "END key";
		m_VirtualKeyNames[Key.VK_HOME] = "HOME key";
		m_VirtualKeyNames[Key.VK_LEFT] = "LEFT ARROW key";
		m_VirtualKeyNames[Key.VK_UP] = "UP ARROW key";
		m_VirtualKeyNames[Key.VK_RIGHT] = "RIGHT ARROW key";
		m_VirtualKeyNames[Key.VK_DOWN] = "DOWN ARROW key";
		m_VirtualKeyNames[Key.VK_SELECT] = "SELECT key";
		m_VirtualKeyNames[Key.VK_PRINT] = "PRINT key";
		m_VirtualKeyNames[Key.VK_EXECUTE] = "EXECUTE key";
		m_VirtualKeyNames[Key.VK_SNAPSHOT] = "PRINT SCREEN key";
		m_VirtualKeyNames[Key.VK_INSERT] = "INS key";
		m_VirtualKeyNames[Key.VK_DELETE] = "DEL key";
		m_VirtualKeyNames[Key.VK_HELP] = "HELP key";
		m_VirtualKeyNames[Key.VK_0] = "0 key";
		m_VirtualKeyNames[Key.VK_1] = "1 key";
		m_VirtualKeyNames[Key.VK_2] = "2 key";
		m_VirtualKeyNames[Key.VK_3] = "3 key";
		m_VirtualKeyNames[Key.VK_4] = "4 key";
		m_VirtualKeyNames[Key.VK_5] = "5 key";
		m_VirtualKeyNames[Key.VK_6] = "6 key";
		m_VirtualKeyNames[Key.VK_7] = "7 key";
		m_VirtualKeyNames[Key.VK_8] = "8 key";
		m_VirtualKeyNames[Key.VK_9] = "9 key";
		m_VirtualKeyNames[Key.VK_A] = "A key";
		m_VirtualKeyNames[Key.VK_B] = "B key";
		m_VirtualKeyNames[Key.VK_C] = "C key";
		m_VirtualKeyNames[Key.VK_D] = "D key";
		m_VirtualKeyNames[Key.VK_E] = "E key";
		m_VirtualKeyNames[Key.VK_F] = "F key";
		m_VirtualKeyNames[Key.VK_G] = "G key";
		m_VirtualKeyNames[Key.VK_H] = "H key";
		m_VirtualKeyNames[Key.VK_I] = "I key";
		m_VirtualKeyNames[Key.VK_J] = "J key";
		m_VirtualKeyNames[Key.VK_K] = "K key";
		m_VirtualKeyNames[Key.VK_L] = "L key";
		m_VirtualKeyNames[Key.VK_M] = "M key";
		m_VirtualKeyNames[Key.VK_N] = "N key";
		m_VirtualKeyNames[Key.VK_O] = "O key";
		m_VirtualKeyNames[Key.VK_P] = "P key";
		m_VirtualKeyNames[Key.VK_Q] = "Q key";
		m_VirtualKeyNames[Key.VK_R] = "R key";
		m_VirtualKeyNames[Key.VK_S] = "S key";
		m_VirtualKeyNames[Key.VK_T] = "T key";
		m_VirtualKeyNames[Key.VK_U] = "U key";
		m_VirtualKeyNames[Key.VK_V] = "V key";
		m_VirtualKeyNames[Key.VK_W] = "W key";
		m_VirtualKeyNames[Key.VK_X] = "X key";
		m_VirtualKeyNames[Key.VK_Y] = "Y key";
		m_VirtualKeyNames[Key.VK_Z] = "Z key";
		m_VirtualKeyNames[Key.VK_LWIN] = "Left Windows key(Natural keyboard)";
		m_VirtualKeyNames[Key.VK_RWIN] = "Right Windows key(Natural keyboard)";
		m_VirtualKeyNames[Key.VK_APPS] = "Applications key(Natural keyboard)";
		m_VirtualKeyNames[Key.VK_SLEEP] = "Computer Sleep key";
		m_VirtualKeyNames[Key.VK_NUMPAD0] = "Numeric keypad 0 key";
		m_VirtualKeyNames[Key.VK_NUMPAD1] = "Numeric keypad 1 key";
		m_VirtualKeyNames[Key.VK_NUMPAD2] = "Numeric keypad 2 key";
		m_VirtualKeyNames[Key.VK_NUMPAD3] = "Numeric keypad 3 key";
		m_VirtualKeyNames[Key.VK_NUMPAD4] = "Numeric keypad 4 key";
		m_VirtualKeyNames[Key.VK_NUMPAD5] = "Numeric keypad 5 key";
		m_VirtualKeyNames[Key.VK_NUMPAD6] = "Numeric keypad 6 key";
		m_VirtualKeyNames[Key.VK_NUMPAD7] = "Numeric keypad 7 key";
		m_VirtualKeyNames[Key.VK_NUMPAD8] = "Numeric keypad 8 key";
		m_VirtualKeyNames[Key.VK_NUMPAD9] = "Numeric keypad 9 key";
		m_VirtualKeyNames[Key.VK_MULTIPLY] = "Multiply key";
		m_VirtualKeyNames[Key.VK_ADD] = "Add key";
		m_VirtualKeyNames[Key.VK_SEPARATOR] = "Separator key";
		m_VirtualKeyNames[Key.VK_SUBTRACT] = "Subtract key";
		m_VirtualKeyNames[Key.VK_DECIMAL] = "Decimal key";
		m_VirtualKeyNames[Key.VK_DIVIDE] = "Divide key";
		m_VirtualKeyNames[Key.VK_F1] = "F1 key";
		m_VirtualKeyNames[Key.VK_F2] = "F2 key";
		m_VirtualKeyNames[Key.VK_F3] = "F3 key";
		m_VirtualKeyNames[Key.VK_F4] = "F4 key";
		m_VirtualKeyNames[Key.VK_F5] = "F5 key";
		m_VirtualKeyNames[Key.VK_F6] = "F6 key";
		m_VirtualKeyNames[Key.VK_F7] = "F7 key";
		m_VirtualKeyNames[Key.VK_F8] = "F8 key";
		m_VirtualKeyNames[Key.VK_F9] = "F9 key";
		m_VirtualKeyNames[Key.VK_F10] = "F10 key";
		m_VirtualKeyNames[Key.VK_F11] = "F11 key";
		m_VirtualKeyNames[Key.VK_F12] = "F12 key";
		m_VirtualKeyNames[Key.VK_F13] = "F13 key";
		m_VirtualKeyNames[Key.VK_F14] = "F14 key";
		m_VirtualKeyNames[Key.VK_F15] = "F15 key";
		m_VirtualKeyNames[Key.VK_F16] = "F16 key";
		m_VirtualKeyNames[Key.VK_F17] = "F17 key";
		m_VirtualKeyNames[Key.VK_F18] = "F18 key";
		m_VirtualKeyNames[Key.VK_F19] = "F19 key";
		m_VirtualKeyNames[Key.VK_F20] = "F20 key";
		m_VirtualKeyNames[Key.VK_F21] = "F21 key";
		m_VirtualKeyNames[Key.VK_F22] = "F22 key";
		m_VirtualKeyNames[Key.VK_F23] = "F23 key";
		m_VirtualKeyNames[Key.VK_F24] = "F24 key";
		m_VirtualKeyNames[Key.VK_NUMLOCK] = "NUM LOCK key";
		m_VirtualKeyNames[Key.VK_SCROLL] = "SCROLL LOCK key";
		m_VirtualKeyNames[Key.VK_LSHIFT] = "Left SHIFT key";
		m_VirtualKeyNames[Key.VK_RSHIFT] = "Right SHIFT key";
		m_VirtualKeyNames[Key.VK_LCONTROL] = "Left CONTROL key";
		m_VirtualKeyNames[Key.VK_RCONTROL] = "Right CONTROL key";
		m_VirtualKeyNames[Key.VK_LMENU] = "Left MENU key";
		m_VirtualKeyNames[Key.VK_RMENU] = "Right MENU key";
		m_VirtualKeyNames[Key.VK_BROWSER_BACK] = "Browser Back key";
		m_VirtualKeyNames[Key.VK_BROWSER_FORWARD] = "Browser Forward key";
		m_VirtualKeyNames[Key.VK_BROWSER_REFRESH] = "Browser Refresh key";
		m_VirtualKeyNames[Key.VK_BROWSER_STOP] = "Browser Stop key";
		m_VirtualKeyNames[Key.VK_BROWSER_SEARCH] = "Browser Search key";
		m_VirtualKeyNames[Key.VK_BROWSER_FAVORITES] = "Browser Favorites key";
		m_VirtualKeyNames[Key.VK_BROWSER_HOME] = "Browser Start and Home key";
		m_VirtualKeyNames[Key.VK_VOLUME_MUTE] = "Volume Mute key";
		m_VirtualKeyNames[Key.VK_VOLUME_DOWN] = "Volume Down key";
		m_VirtualKeyNames[Key.VK_VOLUME_UP] = "Volume Up key";
		m_VirtualKeyNames[Key.VK_MEDIA_NEXT_TRACK] = "Next Track key";
		m_VirtualKeyNames[Key.VK_MEDIA_PREV_TRACK] = "Previous Track key";
		m_VirtualKeyNames[Key.VK_MEDIA_STOP] = "Stop Media key";
		m_VirtualKeyNames[Key.VK_MEDIA_PLAY_PAUSE] = "Play/Pause Media key";
		m_VirtualKeyNames[Key.VK_LAUNCH_MAIL] = "Start Mail key";
		m_VirtualKeyNames[Key.VK_LAUNCH_MEDIA_SELECT] = "Select Media key";
		m_VirtualKeyNames[Key.VK_LAUNCH_APP1] = "Start Application 1 key";
		m_VirtualKeyNames[Key.VK_LAUNCH_APP2] = "Start Application 2 key";
		m_VirtualKeyNames[Key.VK_OEM_1] = "Used for miscellaneous characters; it can vary by keyboard. For the US standard keyboard, the ';:' key";
		m_VirtualKeyNames[Key.VK_OEM_PLUS] = "For any country/region, the '+' key";
		m_VirtualKeyNames[Key.VK_OEM_COMMA] = "For any country/region, the ',' key";
		m_VirtualKeyNames[Key.VK_OEM_MINUS] = "For any country/region, the '-' key";
		m_VirtualKeyNames[Key.VK_OEM_PERIOD] = "For any country/region, the '.' key";
		m_VirtualKeyNames[Key.VK_OEM_2] = "Used for miscellaneous characters; it can vary by keyboard. For the US standard keyboard, the '/?' key";
		m_VirtualKeyNames[Key.VK_OEM_3] = "Used for miscellaneous characters; it can vary by keyboard. For the US standard keyboard, the '`~' key ";
		m_VirtualKeyNames[Key.VK_OEM_4] = "Used for miscellaneous characters; it can vary by keyboard. For the US standard keyboard, the '[Key.{' key";
		m_VirtualKeyNames[Key.VK_OEM_5] = "Used for miscellaneous characters; it can vary by keyboard. For the US standard keyboard, the '\\|' key";
		m_VirtualKeyNames[Key.VK_OEM_6] = "Used for miscellaneous characters; it can vary by keyboard. For the US standard keyboard, the '] = ";
		m_VirtualKeyNames[Key.VK_OEM_7] = "Used for miscellaneous characters; it can vary by keyboard. For the US standard keyboard, the 'single-quote/double-quote' key";
		m_VirtualKeyNames[Key.VK_OEM_8] = "Used for miscellaneous characters; it can vary by keyboard.";
		m_VirtualKeyNames[Key.VK_OEM_102] = "Either the angle bracket key or the backslash key on the RT 102-key keyboard";
		m_VirtualKeyNames[Key.VK_PROCESSKEY] = "IME PROCESS key";
		m_VirtualKeyNames[Key.VK_PACKET] = "Used to pass Unicode characters as if they were keystrokes.The VK_PACKET key is the low word of a 32-bit Virtual Key value used for non-keyboard input methods.For more information, see Remark in KEYBDINPUT, SendInput, WM_KEYDOWN, and WM_KEYUP] = 0xE8";
		m_VirtualKeyNames[Key.VK_ATTN] = "Attn key";
		m_VirtualKeyNames[Key.VK_CRSEL] = "CrSel key";
		m_VirtualKeyNames[Key.VK_EXSEL] = "ExSel key";
		m_VirtualKeyNames[Key.VK_EREOF] = "Erase EOF key";
		m_VirtualKeyNames[Key.VK_PLAY] = "Play key";
		m_VirtualKeyNames[Key.VK_ZOOM] = "Zoom key";
		m_VirtualKeyNames[Key.VK_NONAME] = "Reserved";
		m_VirtualKeyNames[Key.VK_PA1] = "PA1 key";
		m_VirtualKeyNames[Key.VK_OEM_CLEAR] = "Clear Key";
	}

	public static string ToString(Key virtual_key)
	{
		if (!m_VirtualKeyNames.TryGetValue(virtual_key, out var value))
		{
			return $"Virtual Key {virtual_key:X}";
		}
		return value;
	}

	public static Key FromString(string value)
	{
		foreach (Key key in m_VirtualKeyNames.Keys)
		{
			if (m_VirtualKeyNames[key] == value)
			{
				return key;
			}
		}
		return Key.Invalid;
	}
}
