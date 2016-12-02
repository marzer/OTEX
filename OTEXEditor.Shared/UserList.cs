using Marzersoft;
using Marzersoft.Themes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OTEX.Editor
{
    /// <summary>
    /// A user list panel. Behaves like a ListBox (selection etc.), but uses Controls to allow for a more advanced interface.
    /// </summary>
    public sealed class UserList : ThemedFlowLayoutPanel
    {
        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////

            /*
        protected override Color DefaultThemeBackColour
        {
            get { return App.Theme == null ? SystemColors.Control : App.Theme.Workspace.Colour; }
        }
        */

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public SelectionGroup<User> Users
        {
            get
            {
                this.DisposeOrCrossThreadCheck();
                return users;
            }
        }
        private readonly SelectionGroup<User> users
            = new SelectionGroup<User>();
        private readonly Dictionary<User, UserListItem> userListItems
            = new Dictionary<User, UserListItem>();
        internal UserListItem lastFocused = null;

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTOR
        /////////////////////////////////////////////////////////////////////

        public UserList()
        {
            FlowDirection = FlowDirection.TopDown;
            AutoScroll = true;
            WrapContents = false;

            if (IsDesignMode)
                return;

            users.Added += (s, u) =>
            {
                SuspendLayout();
                Controls.Add(userListItems[u] = new UserListItem(this, u));
                Reflow();
                ResumeLayout(true);
            };

            users.Selected += (s, u) =>
            {
                userListItems[u].Selected = true;
            };

            users.Deselected += (s, u) =>
            {
                userListItems[u].Selected = false;
            };

            users.Removed += (s, u) =>
            {
                var item = userListItems[u];
                SuspendLayout();
                Controls.Remove(item);
                Reflow();
                ResumeLayout(true);
                userListItems.Remove(u);
                if (lastFocused == item)
                    lastFocused = null;
                item.Dispose();
            };

            users.SelectionChanged += (s) =>
            {
                Refresh();
            };

            users.Cleared += (s) =>
            {
                SuspendLayout();
                Controls.Clear();
                ResumeLayout(true);
                foreach (var kvp in userListItems)
                    kvp.Value.Dispose();
                userListItems.Clear();
                lastFocused = null;
            };
        }

        /////////////////////////////////////////////////////////////////////
        // CHILD CONTROL MANAGEMENT
        /////////////////////////////////////////////////////////////////////

        protected override void OnControlAdded(ControlEventArgs e)
        {
            var userPanel = (e.Control as UserListItem);
            if (userPanel == null || userPanel.list != this)
                throw new InvalidOperationException("You cannot add controls to the UserList directly");

            base.OnControlAdded(e);
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            Reflow(true);
        }

        protected override void OnStyleChanged(EventArgs e)
        {
            base.OnStyleChanged(e);
            Reflow(true);
        }

        private void Reflow(bool layout = false)
        {
            if (userListItems.Count == 0)
                return;
            if (layout)
                SuspendLayout();

            var w = Width;
            var h = 0;
            foreach (var kvp in userListItems)
                h += kvp.Value.Height;

            if (h >= ClientRectangle.Height)
                w -= SystemInformation.VerticalScrollBarWidth;

            foreach (var kvp in userListItems)
                kvp.Value.Width = w;

            if (layout)
                ResumeLayout(true);
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            Handle.HideHorizontalScrollbar();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left
                && (ModifierKeys & (Keys.Control | Keys.Shift)) == Keys.None)
            {
                Focus();
                users.DeselectAll();
                lastFocused = null;
            }
            else
                base.OnMouseDown(e);
        }

        /////////////////////////////////////////////////////////////////////
        // DISPOSE
        /////////////////////////////////////////////////////////////////////

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (!IsDisposed)
                {
                    if (disposing)
                    {
                        users.Dispose();
                        userListItems.Clear();
                    }
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }
    }

    /// <summary>
    /// An individual user's entry in the UserList.
    /// </summary>
    internal sealed class UserListItem : ThemedPanel
    {
        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////

            /*
        protected override Color DefaultThemeBackColour
        {
            get { return App.Theme == null ? SystemColors.Control : App.Theme.Workspace.Colour; }
        }
        */

        internal User user;
        internal UserList list;

        internal bool Selected
        {
            get { return selected; }
            set
            {
                if (selected == value)
                    return;
                selected = value;
            }
        }
        private volatile bool selected = false;

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTOR
        /////////////////////////////////////////////////////////////////////

        public UserListItem(UserList list, User user)
        {
            this.list = list;
            this.user = user;
            Height = 48;
        }

        /////////////////////////////////////////////////////////////////////
        // EVENTS
        /////////////////////////////////////////////////////////////////////

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            Refresh();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            Refresh();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left
                || e.Button == MouseButtons.Right)
            {
                Focus();
                var mod = ModifierKeys & (Keys.Control | Keys.Shift);

                if (e.Button == MouseButtons.Left)
                {
                    if ((mod & Keys.Shift) != Keys.None) //sets/adds to selection from last mouse down
                    {
                        if (list.lastFocused == null)
                            list.lastFocused = this;

                        if (list.lastFocused == this)
                        {
                            if (!list.lastFocused.user.IsLocal && (mod & Keys.Control) == Keys.None)
                                list.Users.SelectOnly(user);
                        }
                        else
                        {
                            int first = list.Controls.IndexOf(this);
                            int last = list.Controls.IndexOf(list.lastFocused);
                            if (first > last)
                            {
                                var temp = first;
                                first = last;
                                last = temp;
                            }
                            var users = list.Controls.Region<UserListItem>(first, (last - first) + 1)
                                .Select((uli) => { return uli.user; })
                                .Where(u => u.IsLocal == false);
                            if ((mod & Keys.Control) == Keys.None)
                                list.Users.SelectOnly(users);
                            else
                                list.Users.Select(users);
                        }
                    }
                    else if (mod == Keys.Control) //toggles, sets last focused
                    {
                        if (!user.IsLocal)
                            list.Users.Invert(user);
                        list.lastFocused = this;
                    }
                    else //selects, sets last focused
                    {
                        if (user.IsLocal)
                            list.Users.DeselectAll();
                        else
                            list.Users.SelectOnly(user);
                        list.lastFocused = this;
                    }
                }
                else
                {
                    if (user.IsLocal)
                        list.Users.DeselectAll();
                    else
                        list.Users.SelectOnly(user);
                    list.lastFocused = this;
                }
            }
            base.OnMouseDown(e);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Color? colour = null;
            if (selected)
                colour = App.Theme.Accent(0).Colour;
            if (ClientRectangle.Contains(PointToClient(Cursor.Position)))
            {
                if (colour == null)
                    colour = BackColor;
                colour = colour.Value.Brighten(0.1f);
            }
            if (colour != null)
                e.Graphics.Clear(colour.Value);
            else
                base.OnPaintBackground(e);
        }

        /////////////////////////////////////////////////////////////////////
        // DISPOSE
        /////////////////////////////////////////////////////////////////////

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (!IsDisposed)
                {
                    if (disposing)
                    {
                        user = null;
                        list = null;
                    }
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }
    }
}
