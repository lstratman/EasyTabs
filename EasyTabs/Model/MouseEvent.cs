using System;
using Win32Interop.Structs;

namespace EasyTabs.Model;

/// <summary>
/// Contains information on mouse events captured by <see cref="MouseHookCallback" /> and processed by
/// <see cref="InterpretMouseEvents" />.
/// </summary>
public class MouseEvent
{
    /// <summary>Code for the event.</summary>
    // ReSharper disable InconsistentNaming
    public int NumericCode
    {
        get;
        set;
    }

    /// <summary>wParam value associated with the event.</summary>
    public IntPtr WideParam
    {
        get;
        set;
    }

    /// <summary>lParam value associated with the event.</summary>
    public IntPtr LongParam
    {
        get;
        set;
    }

    // ReSharper restore InconsistentNaming

    /// <summary>Data associated with the mouse event.</summary>
    public MSLLHOOKSTRUCT? MouseData
    {
        get;
        set;
    }
}