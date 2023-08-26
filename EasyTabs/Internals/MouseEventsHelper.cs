using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using EasyTabs.Model;
using Win32Interop.Enums;
using Win32Interop.Structs;

namespace EasyTabs
{
    internal class MouseEventsHelper
    {
        private readonly TitleBarTabsOverlay _parent;

        /// <summary>Tab that has been torn off from this window and is being dragged.</summary>
        protected internal static TitleBarTab? TornTab
        {
            get;
            set;
        }

        /// <summary>Thumbnail representation of <see cref="TornTab" /> used when dragging.</summary>
        protected internal static TornTabForm? TornTabForm;
        /// <summary>Semaphore to control access to <see cref="TornTab" />.</summary>
        protected static object TornTabLock = new();

        public static void ProcessMouseEvents(TitleBarTabsOverlay titleBarTabsOverlay)
        {
            // Spin up a consumer thread to process mouse events from _mouseEvents
            titleBarTabsOverlay.MouseEventsThread = new Thread(new MouseEventsHelper(titleBarTabsOverlay).InterpretMouseEvents)
                                                    {
                                                        Name = "Low level mouse hooks processing thread",
                                                        Priority = ThreadPriority.Highest
                                                    };
            titleBarTabsOverlay.MouseEventsThread.Start();
        }

        private MouseEventsHelper(TitleBarTabsOverlay parent)
        {
            _parent = parent;
        }

        /// <summary>Consumer method that processes mouse events in <see cref="MouseEvents" /> that are recorded by <see cref="MouseHookCallback" />.</summary>
        public void InterpretMouseEvents()
        {
            foreach (MouseEvent mouseEvent in _parent.MouseEvents.GetConsumingEnumerable())
            {
                int nCode = mouseEvent.NumericCode;
                IntPtr wParam = mouseEvent.WideParam;
                MSLLHOOKSTRUCT? hookStruct = mouseEvent.MouseData;

                if (nCode >= 0)
                {
                    if ((int)WM.WM_MOUSEMOVE == (int)wParam)
                    {
                        HandleMouseMove(hookStruct);
                    }
                    else if ((int)WM.WM_LBUTTONDBLCLK == (int)wParam)
                    {
                        HandleDoubleClick();
                    }

                    else if ((int)WM.WM_LBUTTONDOWN == (int)wParam)
                    {
                        HandleMouseDown();
                    }

                    else if ((int)WM.WM_LBUTTONUP == (int)wParam)
                    {
                        HandleMouseUp();
                    }

                    else if ((int)WM.WM_LBUTTONDBLCLK == (int)wParam)
                    {
                        HandleDoubleClick();
                    }

                    else if ((int)WM.WM_LBUTTONDOWN == (int)wParam)
                    {
                        HandleMouseDown();
                    }

                    else if ((int)WM.WM_LBUTTONUP == (int)wParam)
                    {
                        HandleMouseUp();
                    }
                }
            }
        }

        private void HandleMouseUp()
        {
            // If we released the mouse button while we were dragging a torn tab, put that tab into a new window
            if (TornTab != null)
            {
                TitleBarTab? tabToRelease = null;

                lock (TornTabLock)
                {
                    if (TornTab != null)
                    {
                        tabToRelease = TornTab;
                        TornTab = null;
                    }
                }

                if (tabToRelease != null)
                {
                    _parent.Invoke(
                        () =>
                        {
                            var type = _parent.ParentFormValue?.GetType();
                            if (type != null)
                            {
                                TitleBarTabs? newWindow = (TitleBarTabs?)Activator.CreateInstance(type);

                                // Set the initial window position and state properly
                                if (newWindow?.WindowState == FormWindowState.Maximized)
                                {
                                    Screen screen = Screen.AllScreens.First(s => s.WorkingArea.Contains(Cursor.Position));

                                    newWindow.StartPosition = FormStartPosition.Manual;
                                    newWindow.WindowState = FormWindowState.Normal;
                                    newWindow.Left = screen.WorkingArea.Left;
                                    newWindow.Top = screen.WorkingArea.Top;
                                    newWindow.Width = screen.WorkingArea.Width;
                                    newWindow.Height = screen.WorkingArea.Height;
                                }

                                else
                                {
                                    if (newWindow != null)
                                    {
                                        newWindow.Left = Cursor.Position.X;
                                        newWindow.Top = Cursor.Position.Y;
                                    }
                                }

                                tabToRelease.Parent = newWindow;
                                _parent.ParentFormValue?.ApplicationContext?.OpenWindow(newWindow);

                                if (newWindow != null)
                                {
                                    newWindow.Show();
                                    newWindow.Tabs.Add(tabToRelease);
                                    newWindow.SelectedTabIndex = 0;
                                    newWindow.ResizeTabContents();
                                }
                            }

                            TornTabForm?.Close();
                            TornTabForm = null;

                            if (_parent.ParentFormValue?.Tabs?.Count == 0)
                            {
                                _parent.ParentFormValue.Close();
                            }
                        });
                }
            }

            _parent.Invoke(
                () =>
                {
                    try
                    {
                        _parent.DoOnMouseUp(new MouseEventArgs(MouseButtons.Left, 1, Cursor.Position.X, Cursor.Position.Y, 0));
                    }
                    catch (ThreadInterruptedException e)
                    {
                        Trace.Write(e);
                    }
                });
        }

        private void HandleMouseDown()
        {
            if (!_parent.FirstClick)
            {
                _parent.LastTwoClickCoordinates[1] = _parent.LastTwoClickCoordinates[0];
            }

            _parent.LastTwoClickCoordinates[0] = Cursor.Position;

            _parent.FirstClick = false;
            TitleBarTabsOverlay.WasDragging = false;
        }

        private void HandleDoubleClick()
        {
            if (_parent.DesktopBounds.Contains(_parent.LastTwoClickCoordinates[0]) && _parent.DesktopBounds.Contains(_parent.LastTwoClickCoordinates[1]))
            {
                _parent.Invoke(
                    () =>
                    {
                        if (_parent.ParentFormValue != null)
                        {
                            _parent.ParentFormValue.WindowState = _parent.ParentFormValue.WindowState == FormWindowState.Maximized
                                ? FormWindowState.Normal
                                : FormWindowState.Maximized;
                        }
                    });
            }
        }

        private void HandleMouseMove(MSLLHOOKSTRUCT? hookStruct)
        {
            _parent.TooltipHelper.HideTooltip();

            if (hookStruct == null)
            {
                return;
            }

            Point cursorPosition = new Point(hookStruct.Value.pt.x, hookStruct.Value.pt.y);
            bool reRender = false;

            if (TornTab != null && _parent.DropAreas != null)
            {
                for (int i = 0; i < _parent.DropAreas.Length; i++)
                {
                    // If the cursor is within the drop area, combine the tab for the window that belongs to that drop area
                    if (_parent.DropAreas[i].Item2.Contains(cursorPosition))
                    {
                        TitleBarTab? tabToCombine = null;

                        lock (TornTabLock)
                        {
                            if (TornTab != null)
                            {
                                tabToCombine = TornTab;
                                TornTab = null;
                            }
                        }

                        if (tabToCombine != null)
                        {
                            int index = i;

                            // In all cases where we need to affect the UI, we call Invoke so that those changes are made on the main UI thread since
                            // we are on a separate processing thread in this case
                            _parent.Invoke(
                                () =>
                                {
                                    _parent.DropAreas[index].Item1.TabRenderer?.CombineTab(tabToCombine, cursorPosition);

                                    tabToCombine = null;
                                    TornTabForm?.Close();
                                    TornTabForm = null;

                                    if ((_parent.ParentFormValue?.Tabs?.Count ?? 0) == 0)
                                    {
                                        _parent.ParentFormValue?.Close();
                                    }
                                });
                        }
                    }
                }
            }
            else
            {
                var parentFormTabRenderer = _parent.ParentFormValue?.TabRenderer;
                if (!parentFormTabRenderer?.IsTabRepositioning ?? false)
                {
                    _parent.TooltipHelper.StartTooltipTimer();

                    Point relativeCursorPosition = _parent.GetRelativeCursorPosition(cursorPosition);

                    // If we were over a close button previously, check to see if the cursor is still over that tab's
                    // close button; if not, re-render
                    if (_parent.ParentFormValue != null &&
                        _parent.IsOverCloseButtonForTab != -1 &&
                        (_parent.IsOverCloseButtonForTab >= (_parent.ParentFormValue?.Tabs?.Count ?? 0) ||
                         !(_parent.ParentFormValue?.TabRenderer?.IsOverCloseButton(_parent.ParentFormValue.Tabs?[_parent.IsOverCloseButtonForTab], relativeCursorPosition) ?? true)))
                    {
                        reRender = true;
                        _parent.IsOverCloseButtonForTab = -1;
                    }

                    // Otherwise, see if any tabs' close button is being hovered over
                    else
                    {
                        // ReSharper disable ForCanBeConvertedToForeach
                        for (int i = 0; i < (_parent.ParentFormValue?.Tabs?.Count ?? 0); i++)
                        // ReSharper restore ForCanBeConvertedToForeach
                        {
                            if (_parent.ParentFormValue != null && _parent.ParentFormValue.TabRenderer != null &&
                                _parent.ParentFormValue.TabRenderer.IsOverCloseButton(_parent.ParentFormValue.Tabs?[i], relativeCursorPosition))
                            {
                                _parent.IsOverCloseButtonForTab = i;
                                reRender = true;

                                break;
                            }
                        }
                    }

                    if (_parent.ParentFormValue != null && _parent.ParentFormValue.TabRenderer != null && _parent.IsOverCloseButtonForTab == -1 &&
                        _parent.ParentFormValue.TabRenderer.RendersEntireTitleBar)
                    {
                        if (_parent.ParentFormValue.TabRenderer.IsOverSizingBox(relativeCursorPosition))
                        {
                            _parent.IsOverSizingBox = true;
                            reRender = true;
                        }

                        else if (_parent.IsOverSizingBox)
                        {
                            _parent.IsOverSizingBox = false;
                            reRender = true;
                        }
                    }

                    if (_parent.ParentFormValue != null && _parent.ParentFormValue.TabRenderer != null && _parent.ParentFormValue.TabRenderer.IsOverAddButton(relativeCursorPosition))
                    {
                        _parent.IsOverAddButton = true;
                        reRender = true;
                    }

                    else if (_parent.IsOverAddButton)
                    {
                        _parent.IsOverAddButton = false;
                        reRender = true;
                    }
                }

                else
                {
                    _parent.Invoke(
                        () =>
                        {
                            TitleBarTabsOverlay.WasDragging = true;

                            // When determining if a tab has been torn from the window while dragging, we take the drop area for this window and inflate it by the
                            // TabTearDragDistance setting
                            Rectangle dragArea = _parent.TabDropArea;
                            if (_parent.ParentFormValue?.TabRenderer != null)
                            {
                                dragArea.Inflate(_parent.ParentFormValue.TabRenderer.TabTearDragDistance, _parent.ParentFormValue.TabRenderer.TabTearDragDistance);

                                // If the cursor is outside the tear area, tear it away from the current window
                                if (!dragArea.Contains(cursorPosition) && TornTab == null)
                                {
                                    lock (TornTabLock)
                                    {
                                        if (TornTab == null)
                                        {
                                            _parent.ParentFormValue.TabRenderer.IsTabRepositioning = false;

                                            // Clear the event handler subscriptions from the tab and then create a thumbnail representation of it to use when dragging
                                            TornTab = _parent.ParentFormValue.SelectedTab;
                                            TornTab?.ClearSubscriptions();
                                            TornTabForm = new TornTabForm(TornTab, _parent.ParentFormValue.TabRenderer);
                                        }
                                    }

                                    if (TornTab != null)
                                    {
                                        _parent.ParentFormValue.SelectedTabIndex =
                                            _parent.ParentFormValue.Tabs != null && _parent.ParentFormValue.SelectedTabIndex == _parent.ParentFormValue.Tabs.Count - 1
                                                ? _parent.ParentFormValue.SelectedTabIndex - 1
                                                : _parent.ParentFormValue.SelectedTabIndex + 1;
                                        _parent.ParentFormValue.Tabs?.Remove(TornTab);

                                        // If this tab was the only tab in the window, hide the parent window
                                        if (_parent.ParentFormValue.Tabs != null && _parent.ParentFormValue.Tabs.Count == 0)
                                        {
                                            _parent.ParentFormValue.Hide();
                                        }

                                        TornTabForm?.Show();
                                        _parent.DropAreas = (from window in _parent.ParentFormValue.ApplicationContext?.OpenWindows.Where(w => w.Tabs.Count > 0)
                                                             select new Tuple<TitleBarTabs, Rectangle>(window, window.TabDropArea)).ToArray();
                                    }
                                }
                            }
                        });
                }
            }

            _parent.Invoke(() => _parent.DoOnMouseMove(new MouseEventArgs(MouseButtons.None, 0, cursorPosition.X, cursorPosition.Y, 0)));

            if (_parent.ParentFormValue?.TabRenderer != null && _parent.ParentFormValue.TabRenderer.IsTabRepositioning)
            {
                reRender = true;
            }

            if (reRender)
            {
                _parent.Invoke(() => _parent.Render(cursorPosition, true));
            }
        }
    }
}
