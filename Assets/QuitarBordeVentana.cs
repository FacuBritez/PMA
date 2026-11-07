using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;

public class QuitarBordeVentana : MonoBehaviour
{
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    [StructLayout(LayoutKind.Sequential)]
    struct RECT { public int left, top, right, bottom; }

    [DllImport("user32.dll")] static extern IntPtr GetActiveWindow();
    [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

    const int GWL_STYLE = -16;
    const int WS_CAPTION = 0x00C00000;
    const uint SWP_NOSIZE = 0x0001;
    const uint SWP_NOMOVE = 0x0002;
    const uint SWP_NOZORDER = 0x0004;
    const uint SWP_FRAMECHANGED = 0x0020;

    IntPtr hWnd;

    IEnumerator Start()
    {
        yield return null;

        hWnd = GetActiveWindow();
        int style = GetWindowLong(hWnd, GWL_STYLE);
        style &= ~WS_CAPTION;
        SetWindowLong(hWnd, GWL_STYLE, style);

        SetWindowPos(hWnd, IntPtr.Zero, 0, 0, 0, 0,
            SWP_NOSIZE | SWP_NOMOVE | SWP_NOZORDER | SWP_FRAMECHANGED);
    }

    void Update()
    {
        if (hWnd == IntPtr.Zero) return;

        bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        if (!ctrl && !alt) return;

        int step = Input.GetKey(KeyCode.LeftShift) ? 50 : 1;
        int dx = 0, dy = 0;
        if (Input.GetKey(KeyCode.LeftArrow)) dx = -step;
        if (Input.GetKey(KeyCode.RightArrow)) dx = step;
        if (Input.GetKey(KeyCode.UpArrow)) dy = -step;
        if (Input.GetKey(KeyCode.DownArrow)) dy = step;
        if (dx == 0 && dy == 0) return;

        GetWindowRect(hWnd, out RECT r);
        int x = r.left, y = r.top;
        int w = r.right - r.left, h = r.bottom - r.top;

        if (ctrl) { x += dx; y += dy; }
        else { w += dx; h += dy; }

        SetWindowPos(hWnd, IntPtr.Zero, x, y, w, h, SWP_NOZORDER);
    }
#endif
}