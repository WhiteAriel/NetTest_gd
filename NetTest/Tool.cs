using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Common;

namespace NetTest
{
    class Tool
    {
        public IntPtr getRootOwner(POINT point, out IntPtr hWnd)
        {
            hWnd = User32.WindowFromPoint(point);
            IntPtr hOwner = User32.GetAncestor(hWnd, 3); // GA_ROOTOWNER = 3
            return hOwner;
        }

        public IntPtr getOwner(POINT pt) // for suspend Thread
        {
            try
            {
                IntPtr hWnd = User32.WindowFromPoint(pt);
                return User32.GetAncestor(hWnd, 3);
            }
            catch { }
            return IntPtr.Zero;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="pt"></param>
        /// <returns></returns>
        public Window getWindow(IntPtr hWnd, POINT pt)
        {
            Window win = null;
            try
            {
                // Get the window under the cursor
                if (hWnd == IntPtr.Zero) hWnd = User32.WindowFromPoint(pt);
                if (hWnd == IntPtr.Zero) return null;

                win = new Window();
                win.hWnd = hWnd;

                // Get the rect of window
                bool b = User32.GetWindowRect(hWnd, out win.rect);

                StringBuilder sb = new StringBuilder(128);
                // Get the class name
                User32.GetClassName(hWnd, sb, sb.Capacity);
                win.className = sb.ToString();
                //Console.Write("cls: " + win.clsName);

                // Get the text length
                int n = (int)User32.SendMessage(hWnd, WM.GETTEXTLENGTH, 0, 0);
                if (n > 0)
                {
                    // Get the text of window
                    n = (int)User32.SendMessage(hWnd, WM.GETTEXT, (uint)sb.Capacity, sb);
                    win.text = sb.ToString();
                    //Console.WriteLine("; text: " + win.text);
                }

                // Get the window styles
                n = (int)User32.GetWindowLongPtr(hWnd, (int)User32.GWL.GWL_STYLE);
                if (n != 0)
                {
                    if ((n & (uint)User32.WindowStyleFlags.WS_POPUP) != 0) win.styles += ", WS_POPUP";
                    if ((n & (int)User32.WindowStyleFlags.WS_CHILD) != 0) win.styles += ", WS_CHILD";
                    if ((n & (int)User32.WindowStyleFlags.WS_MINIMIZE) != 0) win.styles += ", WS_MINIMIZE";
                    if ((n & (int)User32.WindowStyleFlags.WS_VISIBLE) != 0) win.styles += ", WS_VISIBLE";
                    if ((n & (int)User32.WindowStyleFlags.WS_DISABLED) != 0) win.styles += ", WS_DISABLED";
                    if ((n & (int)User32.WindowStyleFlags.WS_CLIPSIBLINGS) != 0) win.styles += ", WS_CLIPSIBLINGS";
                    if ((n & (int)User32.WindowStyleFlags.WS_CLIPCHILDREN) != 0) win.styles += ", WS_CLIPCHILDREN";
                    if ((n & (int)User32.WindowStyleFlags.WS_MAXIMIZE) != 0) win.styles += ", WS_MAXIMIZE";
                    if ((n & (int)User32.WindowStyleFlags.WS_BORDER) != 0) win.styles += ", WS_BORDER";
                    if ((n & (int)User32.WindowStyleFlags.WS_DLGFRAME) != 0) win.styles += ", WS_DLGFRAME";
                    if ((n & (int)User32.WindowStyleFlags.WS_VSCROLL) != 0) win.styles += ", WS_VSCROLL";
                    if ((n & (int)User32.WindowStyleFlags.WS_HSCROLL) != 0) win.styles += ", WS_HSCROLL";
                    if ((n & (int)User32.WindowStyleFlags.WS_SYSMENU) != 0) win.styles += ", WS_SYSMENU";
                    if ((n & (int)User32.WindowStyleFlags.WS_THICKFRAME) != 0) win.styles += ", WS_THICKFRAME";
                    if ((n & (int)User32.WindowStyleFlags.WS_GROUP) != 0) win.styles += ", WS_GROUP";
                    if ((n & (int)User32.WindowStyleFlags.WS_TABSTOP) != 0) win.styles += ", WS_TABSTOP";
                    if ((n & (int)User32.WindowStyleFlags.WS_MINIMIZEBOX) != 0) win.styles += ", WS_MINIMIZEBOX";
                    if ((n & (int)User32.WindowStyleFlags.WS_MAXIMIZEBOX) != 0) win.styles += ", WS_MAXIMIZEBOX";
                    win.styles += ", WS_OVERLAPPED";
                    win.styles = win.styles.Remove(0, 2);
                }
                else win.styles = string.Empty;

                // get the extended window style
                n = (int)User32.GetWindowLongPtr(hWnd, (int)User32.GWL.GWL_EXSTYLE);
                if (n != 0)
                {
                    if ((n & (uint)User32.ExtendedWindowStyleFlags.WS_EX_DLGMODALFRAME) != 0) win.extendedStyles += ", WS_EX_DLGMODALFRAME";
                    if ((n & (uint)User32.ExtendedWindowStyleFlags.WS_EX_NOPARENTNOTIFY) != 0) win.extendedStyles += ", WS_EX_NOPARENTNOTIFY";
                    if ((n & (uint)User32.ExtendedWindowStyleFlags.WS_EX_TOPMOST) != 0) win.extendedStyles += ", WS_EX_TOPMOST";
                    if ((n & (uint)User32.ExtendedWindowStyleFlags.WS_EX_ACCEPTFILES) != 0) win.extendedStyles += ", WS_EX_ACCEPTFILES";
                    if ((n & (uint)User32.ExtendedWindowStyleFlags.WS_EX_TRANSPARENT) != 0) win.extendedStyles += ", WS_EX_TRANSPARENT";
                    if ((n & (uint)User32.ExtendedWindowStyleFlags.WS_EX_MDICHILD) != 0) win.extendedStyles += ", WS_EX_MDICHILD";
                    if ((n & (uint)User32.ExtendedWindowStyleFlags.WS_EX_TOOLWINDOW) != 0) win.extendedStyles += ", WS_EX_TOOLWINDOW";
                    if ((n & (uint)User32.ExtendedWindowStyleFlags.WS_EX_WINDOWEDGE) != 0) win.extendedStyles += ", WS_EX_WINDOWEDGE";
                    if ((n & (uint)User32.ExtendedWindowStyleFlags.WS_EX_CLIENTEDGE) != 0) win.extendedStyles += ", WS_EX_CLIENTEDGE";
                    if ((n & (uint)User32.ExtendedWindowStyleFlags.WS_EX_CONTEXTHELP) != 0) win.extendedStyles += ", WS_EX_CONTEXTHELP";
                    if ((n & (uint)User32.ExtendedWindowStyleFlags.WS_EX_RIGHT) != 0) win.extendedStyles += ", WS_EX_RIGHT";
                    if ((n & (uint)User32.ExtendedWindowStyleFlags.WS_EX_LEFT) != 0) win.extendedStyles += ", WS_EX_LEFT";
                    if ((n & (uint)User32.ExtendedWindowStyleFlags.WS_EX_RTLREADING) != 0) win.extendedStyles += ", WS_EX_RTLREADING";
                    if ((n & (uint)User32.ExtendedWindowStyleFlags.WS_EX_LTRREADING) != 0) win.extendedStyles += ", WS_EX_LTRREADING";
                    if ((n & (uint)User32.ExtendedWindowStyleFlags.WS_EX_LEFTSCROLLBAR) != 0) win.extendedStyles += ", WS_EX_LEFTSCROLLBAR";
                    if ((n & (uint)User32.ExtendedWindowStyleFlags.WS_EX_RIGHTSCROLLBAR) != 0) win.extendedStyles += ", WS_EX_RIGHTSCROLLBAR";
                    if ((n & (uint)User32.ExtendedWindowStyleFlags.WS_EX_CONTROLPARENT) != 0) win.extendedStyles += ", WS_EX_CONTROLPARENT";
                    if ((n & (uint)User32.ExtendedWindowStyleFlags.WS_EX_STATICEDGE) != 0) win.extendedStyles += ", WS_EX_STATICEDGE";
                    if ((n & (uint)User32.ExtendedWindowStyleFlags.WS_EX_APPWINDOW) != 0) win.extendedStyles += ", WS_EX_APPWINDOW";
                    if ((n & (uint)User32.ExtendedWindowStyleFlags.WS_EX_LAYERED) != 0) win.extendedStyles += ", WS_EX_LAYERED";
                    if ((n & (uint)User32.ExtendedWindowStyleFlags.WS_EX_NOINHERITLAYOUT) != 0) win.extendedStyles += ", WS_EX_NOINHERITLAYOUT";
                    if ((n & (uint)User32.ExtendedWindowStyleFlags.WS_EX_LAYOUTRTL) != 0) win.extendedStyles += ", WS_EX_LAYOUTRTL";
                    if ((n & (uint)User32.ExtendedWindowStyleFlags.WS_EX_COMPOSITED) != 0) win.extendedStyles += ", WS_EX_COMPOSITED";
                    if ((n & (uint)User32.ExtendedWindowStyleFlags.WS_EX_NOACTIVATE) != 0) win.extendedStyles += ", WS_EX_NOACTIVATE";
                    win.extendedStyles = win.extendedStyles.Remove(0, 2);
                }
                else win.extendedStyles = string.Empty;

                // Get parent, child, owner, next, previous
                win.parent = User32.GetParent(hWnd);
                win.owner = User32.GetAncestor(hWnd, 3);
                win.child = User32.GetWindow(hWnd, GW.CHILD);
                win.previous = User32.GetWindow(hWnd, GW.HWNDPREV);
                win.next = User32.GetWindow(hWnd, GW.HWNDNEXT);

                // Get Buttons
                int count = (int)User32.SendMessage(hWnd, TB.BUTTONCOUNT, 0, 0);
                //Console.WriteLine(count.ToString());
                if (count > 0)
                {
                    win.itemType = "Button";
                    win.itemStrings = new string[count];
                    win.itemStyles = new string[count];

                    unsafe
                    {
                        UInt32 processId = 0;
                        UInt32 threadId = User32.GetWindowThreadProcessId(hWnd, out processId);
                        IntPtr hProcess = Kernel32.OpenProcess(ProcessRights.ALL_ACCESS, false, processId);

                        // Create the local buffer of one page
                        const int BUFFER_SIZE = 0x1000;
                        byte* localBuffer = stackalloc byte[BUFFER_SIZE];
                        IntPtr ipLocalBuffer = new IntPtr(localBuffer);
                        Int32 bytesRead = 0;
                        IntPtr ipBytesRead = new IntPtr(bytesRead);

                        // Create the remote buffer of one page
                        IntPtr ipRemoteBuffer = Kernel32.VirtualAllocEx(
                            hProcess,
                            IntPtr.Zero,
                            new UIntPtr(BUFFER_SIZE),
                            MemAllocationType.COMMIT,
                            MemoryProtection.PAGE_READWRITE);

                        // Cast the local buffer pointer to the pointer of target data structures
                        TBBUTTON* tb = (TBBUTTON*)localBuffer;
                        TBBUTTONINFO* tbf = (TBBUTTONINFO*)localBuffer;
                        int idCommand = 0;

                        for (int i = 0; i < count; i++)
                        {
                            // Get the button idCommand 
                            n = (int)User32.SendMessage(hWnd, TB.GETBUTTON, (IntPtr)i, ipRemoteBuffer);
                            if (n == 0) continue;

                            // Read data from the remote buffer into the local buffer
                            b = Kernel32.ReadProcessMemory(
                                hProcess,
                                ipRemoteBuffer,
                                ipLocalBuffer,
                                (UInt32)sizeof(TBBUTTON),
                                ipBytesRead);
                            idCommand = tb->idCommand;

                            // Get the button text
                            n = (int)User32.SendMessage(hWnd, TB.GETBUTTONTEXTW, (IntPtr)idCommand, ipRemoteBuffer);
                            if (n != -1)
                            {
                                b = Kernel32.ReadProcessMemory(
                                    hProcess,
                                    ipRemoteBuffer,
                                    ipLocalBuffer,
                                    BUFFER_SIZE,
                                    ipBytesRead);
                                win.itemStrings[i] = Marshal.PtrToStringUni((IntPtr)localBuffer, n);
                            }

                            // get button info
                            for (n = 0; n < BUFFER_SIZE; n++) localBuffer[n] = 0;

                            tbf->cbSize = (uint)Marshal.SizeOf(typeof(TBBUTTONINFO));
                            tbf->dwMask = 0x08 | 0x40;
                            //Const TBIF_STYLE = $00000008
                            //Const TBIF_LPARAM = $00000010
                            //Const TBIF_COMMAND = $00000020
                            //Const TBIF_SIZE = $00000040

                            // Write data from the local buffer into the remote buffer.
                            b = Kernel32.WriteProcessMemory(
                                hProcess,
                                ipRemoteBuffer,
                                ipLocalBuffer,
                                (uint)sizeof(TBBUTTONINFO),
                                out n);

                            // Get the button styles.
                            n = (int)User32.SendMessage(hWnd, TB.GETBUTTONINFO, (IntPtr)idCommand, ipRemoteBuffer);
                            if (n != -1)
                            {
                                b = Kernel32.ReadProcessMemory(
                                    hProcess,
                                    ipRemoteBuffer,
                                    ipLocalBuffer,
                                    (UInt32)sizeof(TBBUTTONINFO),
                                    ipBytesRead);

                                string style = string.Empty;

                                //style += " -- (BTNS_BUTTON";
                                if ((tbf->fsStyle & BTNS.SEP) != 0x00) style += ", BTNS_SEP";
                                if ((tbf->fsStyle & BTNS.DROPDOWN) != 0x00) style += ", BTNS_DROPDOWN";
                                if ((tbf->fsStyle & BTNS.AUTOSIZE) != 0x00) style += ", BTNS_AUTOSIZE";
                                if ((tbf->fsStyle & BTNS.WHOLEDROPDOWN) != 0x00) style += ", BTNS_WHOLEDROPDOWN";

                                if (style.Length > 2) win.itemStyles[i] = style.Substring(2);
                                //Console.WriteLine(win.itemStyles[i]);
                            }
                        }

                        // Free remote buffer
                        Kernel32.VirtualFreeEx(
                            hProcess,
                            ipRemoteBuffer,
                            UIntPtr.Zero,
                            MemAllocationType.RELEASE);

                        // Release process handle
                        Kernel32.CloseHandle(hProcess);

                    }
                }
                else 
                {
                    // Get menu
                    const uint MF_BYPOSITION = 0x400;
                    const uint MN_GETHMENU = 0x1E1;
                    //IntPtr hMenu = User32.GetMenu(hWnd); //not work
                    IntPtr hMenu = User32.SendMessage(hWnd, MN_GETHMENU, IntPtr.Zero, IntPtr.Zero);

                    if (User32.IsMenu(hMenu))
                    {
                        win.itemType = "Menu Item";
                        win.hMenu = hMenu;

                        n = (int)User32.GetMenuItemCount(hMenu);
                        win.itemStrings = new string[n];

                        // the menu item info is always 0, so comment out.
                        //User32.MENUITEMINFO mif = new User32.MENUITEMINFO(); 
                        //mif.cbSize = (uint)Marshal.SizeOf(typeof(User32.MENUITEMINFO));
                        for (uint i = 0; i < n; i++)
                        {
                            //uint id = User32.GetMenuItemID(hMenu, i);
                            // Get menu the text of items
                            User32.GetMenuString(hMenu, i, sb, sb.Capacity, MF_BYPOSITION);
                            win.itemStrings[i] = sb.ToString();

                            //mif.fMask = 0x100; //MIIM_FTYPE
                            //b = User32.GetMenuItemInfo(hMenu, i, true, ref mif);
                            //Console.WriteLine("fType {0}: {1}\t{2}", b, mif.fType, sb.ToString());
                            // mif.fType
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error in public Window getWindow(POINT pt): " + e.Message);
            }
            return win;
        }
    }
}
