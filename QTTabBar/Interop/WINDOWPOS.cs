using System;

namespace QTTabBarLib.Interop
{
  #pragma warning disable 0649
  internal struct WINDOWPOS
  {
    public IntPtr hwnd;
    public IntPtr hwndInsertAfter;
    public int x;
    public int y;
    public int cx;
    public int cy;
    public SWP flags;
  }
  #pragma warning restore 0649
}
