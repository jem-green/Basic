using System;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using Microsoft.Win32;

namespace uBasicForm
{
	/// <summary>
	/// Represents a most recently used (MRU) menu.
	/// </summary>
	/// <remarks>This class shows the MRU list in a popup menu. To display
	/// the MRU list "inline" use <see labelName="MruMenuInline" />.
	/// <para>The class will optionally load the last set of files from the registry
	/// on construction and store them when instructed by the main program.</para>
	/// <para>Internally, this class uses zero-based numbering for the items.
	/// The displayed numbers, however, will start with one.</para></remarks>
	public class MruStripMenu
	{
        #region Fields

        private ClickedHandler    clickedHandler;
		protected ToolStripMenuItem recentFileMenuItem;
		protected int				numEntries = 0;
		protected int				maxEntries = 4;
		protected int				maxShortenPathLength = 96;
		protected Mutex				mruStripMutex;

        #endregion
        #region Contructors

        protected MruStripMenu()
        { }

        #endregion
        #region Properties

        public virtual ToolStripItemCollection MenuItems
        {
            get
            {
                return recentFileMenuItem.DropDownItems;
            }
        }

        public virtual int StartIndex
        {
            get
            {
                return 0;
            }
        }

        public virtual int EndIndex
        {
            get
            {
                return numEntries;
            }
        }

        public int NumEntries
        {
            get
            {
                return numEntries;
            }
        }

        public int MaxEntries
        {
            get
            {
                return maxEntries;
            }
            set
            {
                if (value > 16)
                {
                    maxEntries = 16;
                }
                else
                {
                    maxEntries = value < 4 ? 4 : value;

                    int index = StartIndex + maxEntries;
                    while (numEntries > maxEntries)
                    {
                        MenuItems.RemoveAt(index);
                        numEntries--;
                    }
                }
            }
        }

        public int MaxShortenPathLength
        {
            get
            {
                return maxShortenPathLength;
            }
            set
            {
                maxShortenPathLength = value < 16 ? 16 : value;
            }
        }

        public virtual bool IsInline
        {
            get
            {
                return false;
            }
        }

        #endregion
        #region Event Handling

        public delegate void ClickedHandler(int number, string filename);

		protected void OnClick(object sender, System.EventArgs e)
		{
			MruMenuItem menuItem = (MruMenuItem) sender;
			clickedHandler(MenuItems.IndexOf(menuItem) - StartIndex, menuItem.Filename);
		}

        #endregion
        #region Methods

        protected void Init(ToolStripMenuItem recentFileMenuItem, ClickedHandler clickedHandler, int maxEntries)
        {
            if (recentFileMenuItem == null)
                throw new ArgumentNullException("recentFileMenuItem");

            this.recentFileMenuItem = recentFileMenuItem;
            this.recentFileMenuItem.Checked = false;
            this.recentFileMenuItem.Enabled = false;

            MaxEntries = maxEntries;
            this.clickedHandler = clickedHandler;
        }

        protected virtual void Enable()
		{
			recentFileMenuItem.Enabled = true;
		}

		protected virtual void Disable()
		{
			recentFileMenuItem.Enabled = false;
			//recentFileMenuItem.MenuItems.RemoveAt(0);
		}

		protected virtual void SetFirstFile(MruMenuItem menuItem)
		{
		}

		public void SetFirstFile(int number)
		{
			if (number > 0 && numEntries > 1 && number < numEntries)
			{
				MruMenuItem menuItem = (MruMenuItem)MenuItems[StartIndex + number];

				MenuItems.RemoveAt(StartIndex + number);
				MenuItems.Insert(StartIndex, menuItem);

				SetFirstFile(menuItem);
				FixupPrefixes(0);
			}
		}

		public static string FixupEntryname(int number, string entryname)
		{
			if (number < 9)
				return "&" + (number + 1) + "  " + entryname;
			else if (number == 9)
				return "1&0" + "  " + entryname;
			else
				return (number + 1) + "  " + entryname;
		}

		protected void FixupPrefixes(int startNumber)
		{
			if (startNumber < 0)
				startNumber = 0;

			if (startNumber < maxEntries)
			{
				for (int i = StartIndex + startNumber; i < EndIndex; i++, startNumber++)
				{
					int offset = MenuItems[i].Text.Substring(0, 3) == "1&0" ? 5 : 4;
					MenuItems[i].Text = FixupEntryname(startNumber, MenuItems[i].Text.Substring(offset));
//					MenuItems[i].Text = FixupEntryname(startNumber, MenuItems[i].Text.Substring(startNumber == 10 ? 5 : 4));
				}
			}
		}

		/// <summary>
		/// Shortens a pathname for display purposes.
		/// </summary>
		/// <param labelName="pathname">The pathname to shorten.</param>
		/// <param labelName="maxLength">The maximum number of characters to be displayed.</param>
		/// <remarks>Shortens a pathname by either removing consecutive components of a path
		/// and/or by removing characters from the end of the filename and replacing
		/// then with three elipses (...)
		/// <para>In all cases, the root of the passed path will be preserved in it's entirety.</para>
		/// <para>If a UNC path is used or the pathname and maxLength are particularly short,
		/// the resulting path may be longer than maxLength.</para>
		/// <para>This method expects fully resolved pathnames to be passed to it.
		/// (Use Path.GetFullPath() to obtain this.)</para>
		/// </remarks>
		/// <returns></returns>
		static public string ShortenPathname(string pathname, int maxLength)
		{
			if (pathname.Length <= maxLength)
				return pathname;

			string root = Path.GetPathRoot(pathname);
			if (root.Length > 3)
				root += Path.DirectorySeparatorChar;

			string[] elements = pathname.Substring(root.Length).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

			int filenameIndex = elements.GetLength(0) - 1;

			if (elements.GetLength(0) == 1) // pathname is just a root and filename
			{
				if (elements[0].Length > 5) // long enough to shorten
				{
					// if path is a UNC path, root may be rather long
					if (root.Length + 6 >= maxLength)
					{
						return root + elements[0].Substring(0, 3) + "...";
					}
					else
					{
						return pathname.Substring(0, maxLength - 3) + "...";
					}
				}
			}
			else if ((root.Length + 4 + elements[filenameIndex].Length) > maxLength) // pathname is just a root and filename
			{
				root += "...\\";

				int len = elements[filenameIndex].Length;
				if (len < 6)
					return root + elements[filenameIndex];

				if ((root.Length + 6) >= maxLength)
				{
					len = 3;
				}
				else
				{
					len = maxLength - root.Length - 3;
				}
				return root + elements[filenameIndex].Substring(0, len) + "...";
			}
			else if (elements.GetLength(0) == 2)
			{
				return root + "...\\" + elements[1];
			}
			else
			{
				int len = 0;
				int begin = 0;

				for (int i = 0; i < filenameIndex; i++)
				{
					if (elements[i].Length > len)
					{
						begin = i;
						len = elements[i].Length;
					}
				}

				int totalLength = pathname.Length - len + 3;
				int end = begin + 1;

				while (totalLength > maxLength)
				{
					if (begin > 0)
						totalLength -= elements[--begin].Length - 1;

					if (totalLength <= maxLength)
						break;

					if (end < filenameIndex)
						totalLength -= elements[++end].Length - 1;

					if (begin == 0 && end == filenameIndex)
						break;
				}

				// assemble final string

				for (int i = 0; i < begin; i++)
				{
					root += elements[i] + '\\';
				}

				root += "...\\";

				for (int i = end; i < filenameIndex; i++)
				{
					root += elements[i] + '\\';
				}

				return root + elements[filenameIndex];
			}
			return pathname;
		}

		/// <summary>
		/// Returns the entry number matching the passed filename.
		/// </summary>
		/// <param name="filename">The filename to search for.</param>
		/// <returns>The entry number of the matching filename or -1 if not found.</returns>
		public int FindFilenameNumber(string filename)
		{
			if (filename == null)
				throw new ArgumentNullException("filename");

			if (filename.Length == 0)
				throw new ArgumentException("filename");

			if (numEntries > 0)
			{
				int number = 0;
				for (int i = StartIndex; i < EndIndex; i++, number++)
				{
					if (string.Compare(((MruMenuItem)MenuItems[i]).Filename, filename, true) == 0)
					{
						return number;
					}
				}
			}
			return -1;
		}

		/// <summary>
		/// Returns the menu index of the passed filename.
		/// </summary>
		/// <param name="filename">The filename to search for.</param>
		/// <returns>The menu index of the matching filename or -1 if not found.</returns>
		public int FindFilenameMenuIndex(string filename)
		{
			int number = FindFilenameNumber(filename);
			return number < 0 ? -1 : StartIndex + number;
		}

		/// <summary>
		/// Returns the menu index for a specified MRU item number.
		/// </summary>
		/// <param name="number">The MRU item number.</param>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <returns>The menu index of the passed MRU number.</returns>
		public int GetMenuIndex(int number)
		{
			if (number < 0 || number >= numEntries)
				throw new ArgumentOutOfRangeException("number");

			return StartIndex + number;
		}

		public string GetFileAt(int number)
		{
			if (number < 0 || number >= numEntries)
				throw new ArgumentOutOfRangeException("number");

			return ((MruMenuItem)MenuItems[StartIndex + number]).Filename;
		}

		public string[] GetFiles()
		{
			string[] filenames = new string[numEntries];

			int index = StartIndex;
			for (int i = 0; i < filenames.GetLength(0); i++, index++)
			{
				filenames[i] = ((MruMenuItem)MenuItems[index]).Filename;
			}

			return filenames;
		}

		// This is used for testing
		public string[] GetFilesFullEntrystring()
		{
			string[] filenames = new string[numEntries];

			int index = StartIndex;
			for (int i = 0; i < filenames.GetLength(0); i++, index++)
			{
				filenames[i] = MenuItems[index].Text;
			}

			return filenames;
		}

		public void SetFiles(string[] filenames)
		{
			RemoveAll();
			for (int i = filenames.GetLength(0) - 1; i >= 0; i--)
			{
				AddFile(filenames[i]);
			}
		}

		public void AddFiles(string[] filenames)
		{
			for (int i = filenames.GetLength(0) - 1; i >= 0; i--)
			{
				AddFile(filenames[i]);
			}
		}

		public void AddFile(string filename)
		{
			string pathname = Path.GetFullPath(filename);
			AddFile(pathname, ShortenPathname(pathname, MaxShortenPathLength));
		}

		public void AddFile(string filename, string entryname)
		{
			if (filename == null)
				throw new ArgumentNullException("filename");

			if (filename.Length == 0)
				throw new ArgumentException("filename");

			if (numEntries > 0)
			{
				int index = FindFilenameMenuIndex(filename);
				if (index >= 0)
				{
					SetFirstFile(index - StartIndex);
					return;
				}
			}

			if (numEntries < maxEntries)
			{
				MruMenuItem menuItem = new MruMenuItem(filename, FixupEntryname(0, entryname), new System.EventHandler(OnClick));
				MenuItems.Insert(StartIndex, menuItem);
				SetFirstFile(menuItem);

				if (numEntries++ == 0)
				{
					Enable();
				}
				else
				{
					FixupPrefixes(1);
				}
			}
			else if (numEntries > 1)
			{
				MruMenuItem menuItem = (MruMenuItem) MenuItems[StartIndex + numEntries - 1];
				MenuItems.RemoveAt(StartIndex + numEntries - 1);

				menuItem.Text = FixupEntryname(0, entryname);
				menuItem.Filename = filename;

				MenuItems.Insert(StartIndex, menuItem);

				SetFirstFile(menuItem);
				FixupPrefixes(1);
			}
		}

		public void RemoveFile(int number)
		{
			if (number >= 0 && number < numEntries)
			{
				if (--numEntries == 0)
				{
					Disable();
				}
				else
				{
					int startIndex = StartIndex;
					if (number == 0)
					{
						SetFirstFile((MruMenuItem)MenuItems[startIndex + 1]);
					}

					MenuItems.RemoveAt(startIndex + number);

					if (number < numEntries)
					{
						FixupPrefixes(number);
					}
				}
			}
		}

		public void RemoveFile(string filename)
		{
			if (numEntries > 0)
			{
				RemoveFile(FindFilenameNumber(filename));
			}
		}

		public virtual void RemoveAll()
		{
			if (numEntries > 0)
			{
				// remove all items in the sub menu
				MenuItems.Clear(); 
				Disable();
				numEntries = 0;
			}
		}

		public void RenameFile(string oldFilename, string newFilename)
		{
			string newPathname = Path.GetFullPath(newFilename);

			RenameFile(Path.GetFullPath(oldFilename), newPathname, ShortenPathname(newPathname, MaxShortenPathLength));
		}

		public void RenameFile(string oldFilename, string newFilename, string newEntryname)
		{
			if (newFilename == null)
				throw new ArgumentNullException("newFilename");

			if (newFilename.Length == 0)
				throw new ArgumentException("newFilename");

			if (numEntries > 0)
			{
				int index = FindFilenameMenuIndex(oldFilename);
				if (index >= 0)
				{
					MruMenuItem menuItem = (MruMenuItem)MenuItems[index];
					menuItem.Text = FixupEntryname(0, newEntryname);
					menuItem.Filename = newFilename;
					return;
				}
			}

			AddFile(newFilename, newEntryname);
		}
		#endregion
	}
}