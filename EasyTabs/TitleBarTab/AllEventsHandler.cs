using System;
using System.Collections.Generic;

namespace EasyTabs;

/// <summary>
/// AllEventsHandler class.
/// </summary>
/// <typeparam name="TEventArgs"></typeparam>
public class AllEventsHandler<TEventArgs>: IAllEventsHandler<TEventArgs>
    where TEventArgs : EventArgs
{

    private readonly IList<EventHandler<TEventArgs>> eventDelegates = new List<EventHandler<TEventArgs>>();

    /// <summary>Gets the list of delegates for CreatingForm event.</summary>
    public IEnumerable<EventHandler<TEventArgs>> EventDelegates => eventDelegates;

    IEnumerable<EventHandler<TEventArgs>> IAllEventsHandler<TEventArgs>.AllDelegates => EventDelegates;

    /// <summary>This event is raised when creating a form.</summary>
    public event EventHandler<TEventArgs>? EventWithDelegates
    {
        add
        {
            _eventWithDelegates += value;
            if (value != null)
            {
                eventDelegates.Add(value);
            }
        }

        remove
        {
            _eventWithDelegates -= value;
            if (value != null)
            {
                eventDelegates.Remove(value);
            }
        }
    }

    event EventHandler<TEventArgs>? IAllEventsHandler<TEventArgs>.InternalEventWithDelegates
    {
        add => EventWithDelegates += value;

        remove => EventWithDelegates -= value;
    }

    /// <summary>Removes all event handlers.</summary>
    public void RemoveAllEvents()
    {
        foreach (var eh in EventDelegates)
        {
            _eventWithDelegates -= eh;
        }

        eventDelegates.Clear();
    }

    /// <summary>Raises the EventWithDelegates event.</summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public virtual void RaiseEventWithDelegates(object sender, TEventArgs e)
    {
        _eventWithDelegates?.Invoke(sender, e);
    }

    /// <summary>
    /// Replaces delegates once.
    /// </summary>
    /// <param name="newDelegate">The the delegate called once.</param>
    public void ReplaceEventWithDelegatesHandlersOnce(EventHandler<TEventArgs> newDelegate)
    {
        var eventHandlers = EventDelegates;
        RemoveAllEvents();
        EventWithDelegates += (s, e) =>
        {
            newDelegate.Invoke(s, e);
            RemoveAllEvents();
            foreach (var eventHandler in eventHandlers)
            {
                EventWithDelegates += eventHandler;
            }
        };
    }

    private event EventHandler<TEventArgs>? _eventWithDelegates;
}