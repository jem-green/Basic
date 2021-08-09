using System.Windows.Forms;

namespace uBasicForm
{
    /// <summary>
    /// Represents an inline most recently used (mru) menu.
    /// </summary>
    /// <remarks>
    /// This class shows the MRU list "inline". To display
    /// the MRU list as a popup menu use <see labelName="MruMenu">MruMenu</see>.
    /// </remarks>
    public class MruStripMenuInline : MruStripMenu
    {
        #region Fields

        protected ToolStripMenuItem owningMenu;
        protected ToolStripMenuItem firstMenuItem;

        #endregion region
        #region Constructors

        public MruStripMenuInline(ToolStripMenuItem owningMenu, ToolStripMenuItem recentFileMenuItem, ClickedHandler clickedHandler, int maxEntries)
        {
            maxShortenPathLength = 48;
            this.owningMenu = owningMenu;
            this.firstMenuItem = recentFileMenuItem;
            Init(recentFileMenuItem, clickedHandler, maxEntries);
        }

        #endregion
        #region Properties

        public override ToolStripItemCollection MenuItems
        {
            get
            {
                return owningMenu.DropDownItems;
            }
        }

        public override int StartIndex
        {
            get
            {
                return MenuItems.IndexOf(firstMenuItem);
            }
        }

        public override int EndIndex
        {
            get
            {
                return StartIndex + numEntries;
            }
        }

        public override bool IsInline
        {
            get
            {
                return true;
            }
        }

        #endregion
        #region Methods

        protected override void Enable()
        {
            MenuItems.Remove(recentFileMenuItem);
        }

        protected override void SetFirstFile(MruMenuItem menuItem)
        {
            firstMenuItem = menuItem;
        }

        protected override void Disable()
        {
            int index = MenuItems.IndexOf(firstMenuItem);
            MenuItems.RemoveAt(index);
            MenuItems.Insert(index, recentFileMenuItem);
            firstMenuItem = recentFileMenuItem;
        }

        public override void RemoveAll()
        {
            // inline menu must remove items from the containing menu
            if (numEntries > 0)
            {
                for (int index = EndIndex - 1; index > StartIndex; index--)
                {
                    MenuItems.RemoveAt(index);
                }
                Disable();
                numEntries = 0;
            }
        }

        #endregion
    }
}
