using System.Drawing;
using System.Windows.Forms;
using Timer = System.Timers.Timer;

namespace EasyTabs
{
    internal class TooltipHelper
    {
        private readonly TitleBarTabsOverlay _parent;

        /// <summary>
        /// The showTooltipTimer.
        /// </summary>
        public Timer? ShowTooltipTimer
        {
            get;
        }

        public TooltipHelper(TitleBarTabsOverlay parent)
        {
            _parent = parent;
            ShowTooltipTimer = new Timer
                               {
                                   AutoReset = false
                               };

            ShowTooltipTimer.Elapsed += ShowTooltipTimerOnElapsed;
        }

        internal void HideTooltip()
        {
            ShowTooltipTimer?.Stop();

            if (_parent.ParentFormValue != null && _parent.ParentFormValue.InvokeRequired)
            {
                _parent.ParentFormValue.Invoke(() =>
                {
                    _parent.ParentFormValue.Tooltip.Hide(_parent.ParentFormValue);
                });
            }

            else
            {
                _parent.ParentFormValue?.Tooltip.Hide(_parent.ParentFormValue);
            }
        }

        private void ShowTooltip(TitleBarTabs? tabsForm, string caption)
        {
            Point tooltipLocation = new Point(Cursor.Position.X + 7, Cursor.Position.Y + 55);
            if (tabsForm != null)
            {
                tabsForm.Tooltip.Show(caption, tabsForm, tabsForm.PointToClient(tooltipLocation), tabsForm.Tooltip.AutoPopDelay);
            }
        }

        private void ShowTooltipTimerOnElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!_parent.ParentFormValue?.ShowTooltips ?? true)
            {
                return;
            }

            Point relativeCursorPosition = _parent.GetRelativeCursorPosition(Cursor.Position);
            TitleBarTab? hoverTab = _parent.ParentFormValue.TabRenderer?.OverTab(_parent.ParentFormValue.Tabs, relativeCursorPosition);

            if (hoverTab != null)
            {
                TitleBarTabs? hoverTabForm = hoverTab.Parent;

                if (hoverTabForm?.InvokeRequired ?? false)
                {
                    hoverTabForm.Invoke(() =>
                    {
                        ShowTooltip(hoverTabForm, hoverTab.Caption);
                    });
                }

                else
                {
                    ShowTooltip(hoverTabForm, hoverTab.Caption);
                }
            }
        }

        internal void StartTooltipTimer()
        {
            if (!(_parent.ParentFormValue?.ShowTooltips ?? false))
            {
                return;
            }

            Point relativeCursorPosition = _parent.GetRelativeCursorPosition(Cursor.Position);
            TitleBarTab? hoverTab = _parent.ParentFormValue.TabRenderer?.OverTab(_parent.ParentFormValue.Tabs, relativeCursorPosition);

            if (hoverTab != null)
            {
                if (ShowTooltipTimer != null)
                {
                    if (hoverTab.Parent != null)
                    {
                        ShowTooltipTimer.Interval = hoverTab.Parent.Tooltip.AutomaticDelay;
                    }

                    ShowTooltipTimer.Start();
                }
            }
        }

    }
}
