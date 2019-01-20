using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace uBasicForm
{
    #region MruMenuItem

    /// <summary>
    /// The menu item which will contain the MRU entry.
    /// </summary>
    /// <remarks>The menu may display a shortened or otherwise invalid pathname.
    /// This class stores the actual filename, preferably as a fully
    /// resolved labelName, that will be returned in the event handler.</remarks>
    public class MruMenuItem : ToolStripMenuItem
    {
        #region Contructors
        /// <summary>
        /// Initializes a new instance of the MruMenuItem class.
        /// </summary>
        public MruMenuItem()
        {
            Tag = "";
        }

        /// <summary>
        /// Initializes an MruMenuItem object.
        /// </summary>
        /// <param labelName="filename">The string to actually return in the <paramref labelName="eventHandler">eventHandler</paramref>.</param>
        /// <param labelName="entryname">The string that will be displayed in the menu.</param>
        /// <param labelName="eventHandler">The <see cref="EventHandler">EventHandler</see> that 
        /// handles the <see cref="MenuItem.Click">Click</see> event for this menu item.</param>
        public MruMenuItem(string filename, string entryname, EventHandler eventHandler)
        {
            Tag = filename;
            Text = entryname;
            Click += eventHandler;
        }
        #endregion
        #region Properties
        /// <summary>
        /// Gets the filename.
        /// </summary>
        /// <value>Gets the filename.</value>
        public string Filename
        {
            get
            {
                return (string)Tag;
            }
            set
            {
                Tag = value;
            }
        }
        #endregion
    }
    #endregion
}
