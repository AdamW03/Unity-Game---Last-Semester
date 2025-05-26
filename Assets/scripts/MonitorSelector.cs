using UnityEngine;
using TMPro;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class MonitorSelector : MonoBehaviour
{
    public TMP_Dropdown MonitorDropdown;

    const uint SWP_SHOWWINDOW = 0x0040;

    [DllImport("user32.dll")]
    static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip,
        MonitorEnumDelegate lpfnEnum, IntPtr dwData);

    delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData);

    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        public int left, top, right, bottom;
    }

    List<Rect> monitorRects = new List<Rect>();

    void Start()
    {
        // Clear any existing dropdown options
        MonitorDropdown.ClearOptions();

        var monitorOptions = new List<string>();

        // Enumerate all connected monitors
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, MonitorEnum, IntPtr.Zero);

        // Populate dropdown with monitor labels
        for (int i = 0; i < monitorRects.Count; i++)
        {
            monitorOptions.Add("Monitor " + (i + 1));
        }

        MonitorDropdown.AddOptions(monitorOptions);
        MonitorDropdown.onValueChanged.AddListener(OnMonitorChanged);
    }

    // Callback for monitor enumeration
    bool MonitorEnum(IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData)
    {
        monitorRects.Add(lprcMonitor);
        return true;
    }

    // Called when a new monitor is selected
    void OnMonitorChanged(int monitorIndex)
    {
        if (monitorIndex >= 0 && monitorIndex < monitorRects.Count)
        {
            MoveWindowToMonitor(monitorIndex);

            // Optionally, reset resolution to current one
            Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, true);
            Debug.Log("Moved window to monitor " + (monitorIndex + 1));
        }
    }

    // Moves the game window to the selected monitor
    void MoveWindowToMonitor(int monitorIndex)
    {
        if (monitorIndex >= monitorRects.Count) return;

        Rect r = monitorRects[monitorIndex];
        IntPtr windowHandle = GetActiveWindow();

        int width = r.right - r.left;
        int height = r.bottom - r.top;

        SetWindowPos(windowHandle, IntPtr.Zero, r.left, r.top, width, height, SWP_SHOWWINDOW);
    }
}
