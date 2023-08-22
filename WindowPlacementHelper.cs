using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace Git_Monitor;

public static partial class WindowPlacementHelper
{
    public static void SetWindowPlacement(Window window, WindowPlacement wp)
    {
        try
        {
            wp.length = Marshal.SizeOf(typeof(WindowPlacement));
            wp.flags = 0;
            wp.showCmd = (wp.showCmd == SwShowminimized ? SwShownormal : wp.showCmd);
            var hwnd = new WindowInteropHelper(window).Handle;
            _SetWindowPlacement(hwnd, ref wp);
        }
        catch
        {
            // ignore
        }
    }

    public static WindowPlacement GetWindowPlacement(Window window)
    {
        WindowPlacement wp;
        var hwnd = new WindowInteropHelper(window).Handle;
        _GetWindowPlacement(hwnd, out wp);
        return wp;
    }

    [LibraryImport("user32.dll", EntryPoint = "SetWindowPlacement")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool _SetWindowPlacement(IntPtr hWnd, ref WindowPlacement lpwndpl);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowPlacement")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool _GetWindowPlacement(IntPtr hWnd, out WindowPlacement lpwndpl);

    private const int SwShownormal = 1;
    private const int SwShowminimized = 2;

    [StructLayout(LayoutKind.Sequential)]
    public struct WindowPlacement
    {
        [JsonInclude] public int length;
        [JsonInclude] public int flags;
        [JsonInclude] public int showCmd;
        [JsonInclude] public Point minPosition;
        [JsonInclude] public Point maxPosition;
        [JsonInclude] public Rect normalPosition;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Point
    {
        [JsonInclude] public int X;
        [JsonInclude] public int Y;

        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        [JsonInclude] public int Left;
        [JsonInclude] public int Top;
        [JsonInclude] public int Right;
        [JsonInclude] public int Bottom;

        public Rect(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }
    }
}
