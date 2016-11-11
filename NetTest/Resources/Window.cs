using System;
using System.Collections;
using System.Text;
using Common;
using System.ComponentModel;

namespace NetTest
{
    public class Window
    {
        public IntPtr hWnd;
        public string className;
        public string text;
        public User32.RECT rect;
        public string styles;
        public string extendedStyles;
        public string itemType;
        public IntPtr hMenu;
        public string[] itemStrings;
        public string[] itemStyles;
        public IntPtr parent, child, owner, next, previous;
    }
}