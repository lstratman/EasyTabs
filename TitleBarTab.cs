using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Stratman.Windows.Forms.TitleBarTabs
{
    public class TitleBarTab
    {
        protected bool _active = false;
        protected TitleBarTabs _parent = null;
        protected Form _content = null;

        public TitleBarTab(TitleBarTabs parent)
        {
            ShowCloseButton = true;
            _parent = parent;
        }

        public bool ShowCloseButton
        {
            get;
            set;
        }

        public string Caption
        {
            get
            {
                return Content.Text;
            }

            set
            {
                Content.Text = value;
            }
        }

        public bool Active
        {
            get
            {
                return _active;
            }

            internal set
            {
                _active = value;
                TabImage = null;
                Content.Visible = value;
            }
        }

        public Icon Icon
        {
            get
            {
                return Content.Icon;
            }

            set
            {
                Content.Icon = value;
            }
        }

        internal Rectangle Area
        {
            get;
            set;
        }

        internal Rectangle CloseButtonArea
        {
            get;
            set;
        }

        internal Bitmap TabImage
        {
            get;
            set;
        }

        public Form Content
        {
            get
            {
                return _content;
            }

            set
            {
                _content = value;

                Content.FormBorderStyle = FormBorderStyle.None;
                Content.TopLevel = false;
                Content.Parent = _parent;
            }
        }
    }
}
