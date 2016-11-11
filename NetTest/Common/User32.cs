using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Common
{
	public enum GW : uint
	{
		HWNDFIRST        = 0,
		HWNDLAST         = 1,
		HWNDNEXT         = 2,
		HWNDPREV         = 3,
		OWNER            = 4,
		CHILD            = 5,
		MAX              = 6
	}

	internal class ICON
	{
		public const UInt32 SMALL          = 0;
		public const UInt32 BIG            = 1;
		public const UInt32 SMALL2         = 2; // XP+
	}

	public enum MB : uint
	{
		SimpleBeep      = 0xFFFFFFFF,
		IconAsterisk    = 0x00000040,
		IconWarning     = 0x00000030,
		IconError       = 0x00000010,
		IconQuestion    = 0x00000020,
		OK              = 0x00000000
	}

	internal class SW
	{
		public const int HIDE               = 0;
		public const int SHOWNORMAL         = 1;
		public const int NORMAL             = 1;
		public const int SHOWMINIMIZED      = 2;
		public const int SHOWMAXIMIZED      = 3;
		public const int MAXIMIZE           = 3;
		public const int SHOWNOACTIVATE     = 4;
		public const int SHOW               = 5;
		public const int MINIMIZE           = 6;
		public const int SHOWMINNOACTIVE    = 7;
		public const int SHOWNA             = 8;
		public const int RESTORE            = 9;
		public const int SHOWDEFAULT        = 10;
		public const int FORCEMINIMIZE      = 11;
		public const int MAX                = 11;
	}

	internal class TB
	{
		public const uint GETBUTTON       = WM.USER + 23 ;
		public const uint BUTTONCOUNT     = WM.USER + 24 ;
		public const uint CUSTOMIZE       = WM.USER + 27 ;
		public const uint GETBUTTONTEXTA  = WM.USER + 45 ;
        public const uint GETBUTTONTEXTW = WM.USER + 75;
        public const uint SETBUTTONINFO = WM.USER + 64;
        public const uint GETBUTTONSIZE = WM.USER + 58;
        public const uint GETSTATE = WM.USER + 18;
        public const uint GETBUTTONINFO = WM.USER + 63;
        public const uint ISBUTTONPRESSED = WM.USER + 11;
    }

	internal class TBSTATE
	{
		public const uint CHECKED        =  0x01 ;
		public const uint PRESSED        =  0x02 ;
		public const uint ENABLED        =  0x04 ;
		public const uint HIDDEN         =  0x08 ;
		public const uint INDETERMINATE  =  0x10 ;
		public const uint WRAP           =  0x20 ;
		public const uint ELLIPSES       =  0x40 ;
		public const uint MARKED         =  0x80 ;
	}

	internal class WM
	{
		public const uint CLOSE   = 0x0010;
		public const uint GETICON = 0x007F;
		public const uint KEYDOWN = 0x0100;
		public const uint COMMAND = 0x0111;
		public const uint USER    = 0x0400; // 0x0400 - 0x7FFF
		public const uint APP     = 0x8000; // 0x8000 - 0xBFFF
        public const uint KILLFOCUS = 0x0008;
        public const uint GETTEXTLENGTH = 0x000E;
        public const uint GETTEXT = 0x000D;
    }

	internal class GCL
	{
		public const int MENUNAME       = - 8;
		public const int HBRBACKGROUND  = -10;
		public const int HCURSOR        = -12;
		public const int HICON          = -14;
		public const int HMODULE        = -16;
		public const int CBWNDEXTRA     = -18;
		public const int CBCLSEXTRA     = -20;
		public const int WNDPROC        = -24;
		public const int STYLE          = -26;
		public const int ATOM           = -32;
		public const int HICONSM        = -34;

		// GetClassLongPtr ( 64-bit )
		public const int GCW_ATOM           = -32;
		public const int GCL_CBCLSEXTRA     = -20;
		public const int GCL_CBWNDEXTRA     = -18;
		public const int GCLP_MENUNAME      = - 8;
		public const int GCLP_HBRBACKGROUND = -10;
		public const int GCLP_HCURSOR       = -12;
		public const int GCLP_HICON         = -14;
		public const int GCLP_HMODULE       = -16;
		public const int GCLP_WNDPROC       = -24;
		public const int GCLP_HICONSM       = -34;
		public const int GCL_STYLE          = -26;

	}

    public enum MouseMessages
    {
        WM_LBUTTONDOWN = 0x0201,
        WM_LBUTTONUP = 0x0202,
        WM_MOUSEMOVE = 0x0200,
        WM_MOUSEWHEEL = 0x020A,
        WM_RBUTTONDOWN = 0x0204,
        WM_RBUTTONUP = 0x0205
    }

 [Flags]
  public enum MouseEventFlags
  {
    LEFTDOWN   = 0x00000002,
    LEFTUP     = 0x00000004,
    MIDDLEDOWN = 0x00000020,
    MIDDLEUP   = 0x00000040,
    MOVE       = 0x00000001,
    ABSOLUTE   = 0x00008000,
    RIGHTDOWN  = 0x00000008,
    RIGHTUP    = 0x00000010
  }

    internal class BTNS
    {
        public const int BUTTON = 0;
        public const int SEP = 0x1;
        public const int DROPDOWN = 0x8;
        public const int AUTOSIZE = 0x10;
        public const int WHOLEDROPDOWN = 0x80;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    public enum ChildWindowFromPointFlags : uint
    {
        CWP_ALL = 0x0000,
        CWP_SKIPINVISIBLE = 0x0001,
        CWP_SKIPDISABLED = 0x0002,
        CWP_SKIPTRANSPARENT = 0x0004
    }

	[ StructLayout( LayoutKind.Sequential ) ]
	internal struct TBBUTTON 
	{
		public Int32 iBitmap;
		public Int32 idCommand;
		public byte fsState;
		public byte fsStyle;
//		[ MarshalAs( UnmanagedType.ByValArray, SizeConst=2 ) ]
//		public byte[] bReserved;
		public byte bReserved1;
		public byte bReserved2;
		public UInt32 dwData;
		public IntPtr ipText;
	};

    [StructLayout(LayoutKind.Sequential)]
    internal struct TBBUTTONINFO
    {
        public UInt32 cbSize;
        public UInt32 dwMask;
        public int idCommand;
        public int iImage;
        public byte fsState;
        public byte fsStyle;
        public ushort cx;
        public UInt32 lParam;
        public IntPtr pszText;
        public int cchText;
    }

	public class User32
	{
		public User32() {}

//		public const UInt32 WM_USER = 0x0400;

//		public const UInt32 WM_KEYDOWN = 0x0100;
		[DllImport("user32.dll")]
		public static extern UInt32 SendMessage(
			IntPtr hWnd,
			UInt32 msg,
			UInt32 wParam,
			UInt32 lParam );

        [DllImport("user32.dll")]
        public static extern UInt32 SendMessage(
            IntPtr hWnd,
            UInt32 msg,
            UInt32 wParam,
            StringBuilder lParam);
        
        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(
            IntPtr hWnd,
            UInt32 msg,
            IntPtr wParam,
            object lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(
            IntPtr hWnd,
            UInt32 msg,
            IntPtr wParam,
            IntPtr lParam);

		[ DllImport( "User32.dll" ) ]
		public static extern bool PostMessage
		(
			IntPtr hWnd,
			UInt32 Msg,
			IntPtr wParam,
			IntPtr lParam
		);

		[ DllImport( "User32.dll" ) ]
		public static extern bool PostMessage
		(
			IntPtr hWnd,
			UInt32 Msg,
			UInt32 wParam,
			UInt32 lParam
		);

		[ DllImport( "User32.dll" ) ]
		public static extern bool MessageBeep
		(
			MB BeepType
		);

		[DllImport("user32.dll")]
		public static extern bool ShowWindow
		(
			IntPtr hWnd,
			int nCmdShow
		);

		[DllImport("user32.dll")]
		public static extern bool SetForegroundWindow
		(
			IntPtr hWnd
		);


		[ DllImport( "User32.dll" ) ]
		public static extern IntPtr GetDesktopWindow
		(
		);

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string strclassName, string strWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, IntPtr windowTitle);

		[ DllImport( "user32.dll", CharSet = CharSet.Unicode ) ]
		public static extern IntPtr FindWindowEx(
			IntPtr hwndParent,
			IntPtr hwndChildAfter,
			string lpszClass,
			string lpszWindow);
		
		[ DllImport( "User32.dll" ) ]
		public static extern IntPtr GetWindow
		(
			IntPtr hWnd,
			GW     uCmd
		);

		[ DllImport( "User32.dll" ) ]
		public static extern Int32 GetWindowTextLength
		(
			IntPtr hWnd
		);

		[ DllImport( "User32.dll", SetLastError = true, CharSet = CharSet.Auto ) ]
		public static extern Int32 GetWindowText
		(
			IntPtr hWnd,
			out StringBuilder lpString,
			Int32 nMaxCount
		);

//		[ DllImport( "user32.dll", EntryPoint = "GetClassLongPtrW" ) ]
		[ DllImport( "user32.dll" ) ]
		public static extern UInt32 GetClassLong
		(
			IntPtr hWnd,
			int nIndex
		);

		[DllImport("user32.dll")]
		public static extern uint SetClassLong
		(
			IntPtr hWnd,
			int nIndex,
			uint dwNewLong
		);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int X;
            public int Y;
            public int Right;
            public int Bottom;

            public int Width
            {
                get { return Right - X; }
            }

            public int Height
            {
                get { return Bottom - Y; }
            }
        }

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lprect);

		[ DllImport( "User32.dll", CharSet=CharSet.Auto ) ]
		public static extern UInt32 GetWindowThreadProcessId
		(
			IntPtr hWnd,
//			[ MarshalAs( UnmanagedType.
			out UInt32 lpdwProcessId
		);

        [DllImport("user32")]
        public static extern IntPtr LoadMenu(IntPtr hInstance, int id);

        [DllImport("user32")]
        public static extern bool IsMenu(IntPtr hMenu);

        [DllImport("user32")]
        public static extern IntPtr GetMenu(IntPtr hwnd);

        [DllImport("user32")]
        public static extern uint GetMenuItemCount(IntPtr hMenu);

        [DllImport("user32")]
        public static extern uint GetMenuItemID(IntPtr hMenu, uint nPos);

        [DllImport("user32.dll")]
        public static extern bool GetMenuItemInfo(IntPtr hMenu, uint uItem, bool fByPosition,
           ref MENUITEMINFO lpmii);

        [StructLayout(LayoutKind.Sequential)]
        public struct MENUITEMINFO
        {
            public uint cbSize;
            public uint fMask;
            public uint fType;
            public uint fState;
            public int wID;
            public int hSubMenu;
            public int hbmpChecked;
            public int hbmpUnchecked;
            public int dwItemData;
            public string dwTypeData;
            public uint cch;
            public int hbmpItem;
        };

        // Values for the fMask parameter
        //From winuser.h

        const UInt32 MIM_MAXHEIGHT    =       0x00000001;
        const UInt32 MIM_BACKGROUND   =       0x00000002;
        const UInt32 MIM_HELPID       =       0x00000004;
        const UInt32 MIM_MENUDATA     =       0x00000008;
        const UInt32 MIM_STYLE    =       0x00000010;
        const UInt32 MIM_APPLYTOSUBMENUS =    0x80000000;

        //public static const int MF_BYPOSITION = 0x400;
        [DllImport("user32")]
        public static extern uint GetMenuString(
            IntPtr hMenu,
            uint uIDItem,
            StringBuilder lpString,
            int nMaxCount,
            uint uFlag
        );

        [DllImport("user32")]
        public static extern IntPtr GetSubMenu(IntPtr hInstance, int id);

        [DllImport("user32")]
        public static extern int TrackPopupMenu(      
            IntPtr hMenu,
            uint uFlags,
            int x,
            int y,
            int nReserved,
            IntPtr hWnd,
            IntPtr prcRect);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(POINT lpPoint);

        [DllImport("user32.dll")]
        public static extern IntPtr ChildWindowFromPointEx(IntPtr hWndParent, POINT pt, uint uFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetClassName(IntPtr hWnd,
           StringBuilder lpClassName,
           int nMaxCount
        );

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern uint MessageBox(IntPtr hWnd, String text, String caption, uint options);

        [DllImport("user32.dll")]
        public static extern void mouse_event(MouseEventFlags dwFlags, int dx, int dy, uint dwData, int dwExtraInfo);

        [DllImport("user32.dll")]
        public static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        public static extern bool CloseWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);
        [DllImport("user32.dll")]
        static extern IntPtr GetLastActivePopup(IntPtr hWnd);
        [DllImport("user32.dll")]
        public static extern IntPtr GetParent(IntPtr hWnd);
        [DllImport("user32.dll")]
        public static extern IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);
        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowDC(IntPtr hWnd);
        [DllImport("user32.dll")]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [Flags]
        public enum WindowStyleFlags : uint
        {
            WS_OVERLAPPED = 0x00000000,
            WS_POPUP = 0x80000000,
            WS_CHILD = 0x40000000,
            WS_MINIMIZE = 0x20000000,
            WS_VISIBLE = 0x10000000,
            WS_DISABLED = 0x08000000,
            WS_CLIPSIBLINGS = 0x04000000,
            WS_CLIPCHILDREN = 0x02000000,
            WS_MAXIMIZE = 0x01000000,
            WS_BORDER = 0x00800000,
            WS_DLGFRAME = 0x00400000,
            WS_VSCROLL = 0x00200000,
            WS_HSCROLL = 0x00100000,
            WS_SYSMENU = 0x00080000,
            WS_THICKFRAME = 0x00040000,
            WS_GROUP = 0x00020000,
            WS_TABSTOP = 0x00010000,
            WS_MINIMIZEBOX = 0x00020000,
            WS_MAXIMIZEBOX = 0x00010000,
        }

        [Flags]
        public enum ExtendedWindowStyleFlags : int
        {
            WS_EX_DLGMODALFRAME = 0x00000001,
            WS_EX_NOPARENTNOTIFY = 0x00000004,
            WS_EX_TOPMOST = 0x00000008,
            WS_EX_ACCEPTFILES = 0x00000010,
            WS_EX_TRANSPARENT = 0x00000020,

            WS_EX_MDICHILD = 0x00000040,
            WS_EX_TOOLWINDOW = 0x00000080,
            WS_EX_WINDOWEDGE = 0x00000100,
            WS_EX_CLIENTEDGE = 0x00000200,
            WS_EX_CONTEXTHELP = 0x00000400,

            WS_EX_RIGHT = 0x00001000,
            WS_EX_LEFT = 0x00000000,
            WS_EX_RTLREADING = 0x00002000,
            WS_EX_LTRREADING = 0x00000000,
            WS_EX_LEFTSCROLLBAR = 0x00004000,
            WS_EX_RIGHTSCROLLBAR = 0x00000000,

            WS_EX_CONTROLPARENT = 0x00010000,
            WS_EX_STATICEDGE = 0x00020000,
            WS_EX_APPWINDOW = 0x00040000,

            WS_EX_LAYERED = 0x00080000,

            WS_EX_NOINHERITLAYOUT = 0x00100000, // Disable inheritence of mirroring by children
            WS_EX_LAYOUTRTL = 0x00400000, // Right to left mirroring

            WS_EX_COMPOSITED = 0x02000000,
            WS_EX_NOACTIVATE = 0x08000000
        }
     
        public enum GWL
        {
            GWL_WNDPROC = (-4),
            GWL_HINSTANCE = (-6),
            GWL_HWNDPARENT = (-8),
            GWL_STYLE = (-16),
            GWL_EXSTYLE = (-20),
            GWL_USERDATA = (-21),
            GWL_ID = (-12)
        }
        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        public static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "MoveWindow")]
        public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);

    }
}
