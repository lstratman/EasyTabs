using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Stratman.Windows.Forms.TitleBarTabs
{
    public class TitleBarTabEventArgs : EventArgs
    {
        public TabControlAction Action
        {
            get;
            set;
        }

        public TitleBarTab Tab
        {
            get;
            set;
        }

        public int TabIndex
        {
            get;
            set;
        }
    }
}
