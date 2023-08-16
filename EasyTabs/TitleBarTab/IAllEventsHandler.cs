using System;
using System.Collections.Generic;

namespace EasyTabs;

/// <summary>
/// IAllEventsHandler interface.
/// </summary>
public interface IAllEventsHandler<TEventArgs> where TEventArgs : EventArgs
{
    /// <summary>Gets the list of delegates for CreatingForm event.</summary>
    IEnumerable<EventHandler<TEventArgs>> AllDelegates
    {
        get;
    }

    /// <summary>Removes all event handlers.</summary>
    void RemoveAllEvents();

    /// <summary>
    /// The event with delegates.
    /// </summary>
    event EventHandler<TEventArgs>? InternalEventWithDelegates;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    void RaiseEventWithDelegates(object sender, TEventArgs e);

    /// <summary>
    /// Replaces delegates once.
    /// </summary>
    /// <param name="newDelegate">The the delegate called once.</param>
    void ReplaceEventWithDelegatesHandlersOnce(EventHandler<TEventArgs> newDelegate);

}