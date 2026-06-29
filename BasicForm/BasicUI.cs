using System;
using System.Drawing;
using System.Windows.Forms;
using BasicLibrary;
using TracerLibrary;
using System.Threading;
using BasicForm.Properties;
using System.IO;
using System.Diagnostics;

namespace BasicForm
{
    public partial class BasicUI : Form
    {
        #region Fields

        // Prepare Basic
        static IDefaultIO _textBoxIO = null;
        IInterpreter _basic = null;
        int _pos = 0;
        bool _stopped = true;

        
        // Declare a delegate used to communicate with the UI thread
        public delegate void UpdateTextDelegate();
        private UpdateTextDelegate updateTextDelegate = null;

        // Declare our worker thread
        private Thread _workerThread = null;

        // Manage the inputs
        string value = "";

        // Most recently used
        protected MruStripMenu _mruMenu;

        #endregion
        #region Constructor

        public BasicUI(string path, string name)
        {
            Debug.WriteLine("In ConsoleForm()");

            InitializeComponent();

            _textBoxIO = new TextBoxIO();
            _textBoxIO.TextReceived += new EventHandler<TextEventArgs>(OnMessageReceived);

            // Initialise the delegate
            this.updateTextDelegate = new UpdateTextDelegate(this.UpdateText);

            // Add most recent used
            _mruMenu = new MruStripMenuInline(fileMenuItem, recentFileToolStripMenuItem, new MruStripMenu.ClickedHandler(OnMruFile), 4);
            LoadFiles();

            Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            this.Text = "Basic " + version;

            if ((path.Length > 0) && (name.Length > 0))
            {
                consoleTextBox.Text = "";
                this.Text = "Basic " + version + " - " + name ;

                string filenamePath = "";
                filenamePath = path + Path.DirectorySeparatorChar + name + ".bas";
                char[] program;
                try
                {
                    using (StreamReader sr = new StreamReader(filenamePath))
                    {
                        program = sr.ReadToEnd().ToCharArray();
                    }

                    _mruMenu.AddFile(filenamePath);
                    _basic = new Altair.Interpreter(program, _textBoxIO);

                    _stopped = false;
                    this._workerThread = new Thread(new ThreadStart(this.Run));
                    _textBoxIO.Reset();
                    this._workerThread.Start();
                    consoleTextBox.Visible = true;
                    consoleTextBox.Enabled = true;
                }
                catch (Exception e1)
                {
                    TraceInternal.TraceError(e1.ToString());
                }
            }
			Debug.WriteLine("Out ConsoleForm()");
        }

        #endregion
        #region Private

        private void OnMruFile(int number, String filenamePath)
        {
            string path = "";
            string filename = "";

            consoleTextBox.Enabled = false;
            consoleTextBox.Visible = false;

            if (File.Exists(filenamePath) == true)
            {
                _mruMenu.SetFirstFile(number);
                _pos = filenamePath.LastIndexOf('\\');
                if (_pos > 0)
                {
                    path = filenamePath.Substring(0, _pos);
                    filename = filenamePath.Substring(_pos + 1, filenamePath.Length - _pos - 1);
                }
                else
                {
                    path = filenamePath;
                }
                TraceInternal.TraceInformation("Use Name=" + filename);
                TraceInternal.TraceInformation("Use Path=" + path);


                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                consoleTextBox.Text = "";
                this.Text = "Basic " + version + " - " + filename;

                filenamePath = path + Path.DirectorySeparatorChar + filename;
                char[] program;
                try
                {
                    using (StreamReader sr = new StreamReader(filenamePath))
                    {
                        program = sr.ReadToEnd().ToCharArray();
                    }

                    _basic = new Altair.Interpreter(program, _textBoxIO);

                    _stopped = false;
                    this._workerThread = new Thread(new ThreadStart(this.Run));
                    _textBoxIO.Reset();
                    this._workerThread.Start();
                    consoleTextBox.Visible = true;
                    consoleTextBox.Enabled = true;
                }
                catch (Exception e1)
                {
                    TraceInternal.TraceError(e1.ToString());
                }
            }
            else
            {
                _mruMenu.RemoveFile(number);
            }
        }

        private void UpdateText()
        {
            string output = _textBoxIO.Output;
            if (output.Length > 0)
            {
                this.consoleTextBox.AppendText(output);
                //int position = consoleTextBox.SelectionLength;
                double position = consoleTextBox.TextLength;
                double width = consoleTextBox.Width / 8;   // assume a fix width font
                double top = Math.Floor(position / width);
                _textBoxIO.CursorTop = (int)top;
                double left = position - width * Math.Floor(position / width);
                _textBoxIO.CursorLeft =(int)left;
            }
        }

        // Define the event handlers.
        private void OnMessageReceived(object source, TextEventArgs e)
        {
            if (e.Text.Length > 0)
            {
                this.Invoke(this.updateTextDelegate);
            }
        }

        private void Run()
        {
            _basic.Init(0);
            try
            {
                do
                {
                    _basic.Run();
                } while (!_basic.IsFinished());
            }
            catch (Exception e)
            {
                TraceInternal.TraceVerbose(e.ToString());
            }
        }

        /// <summary>
        /// Intercept the key press events and manage the content
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConsoleTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            char chr = e.KeyChar;
            if (chr == '\r')
            {
                value += chr;
                _textBoxIO.Input = value;
                this.consoleTextBox.AppendText("\r" + "\n");
                value = "";
            }
            else if (chr == '\b')
            {
                if (value.Length > 0)
                {
                    this.consoleTextBox.Text = this.consoleTextBox.Text.Substring(0, this.consoleTextBox.Text.Length - 1);
                    this.consoleTextBox.SelectionStart = consoleTextBox.Text.Length;
                    this.consoleTextBox.ScrollToCaret();
                    value = value.Substring(0, value.Length - 1);
                }
            }
            else
            {
                value += chr;
                this.consoleTextBox.AppendText(Convert.ToString(chr));
            }
        }

        private void FileOpenMenuItem_Click(object sender, EventArgs e)
        {
            Debug.WriteLine("In FileOpenMenuItem_Click()");

            string path = "";
            string filename = "";

            consoleTextBox.Enabled = false;
            consoleTextBox.Visible = false;
            if (_stopped == false)
            {
                _textBoxIO.Input = "\r\n";
                _basic.Stop();
                _workerThread.Join(1000);
            }

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Basic (*.bas)|*.bas",
                FilterIndex = 1,
                RestoreDirectory = true
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filenamePath = openFileDialog.FileName;
                _pos = filenamePath.LastIndexOf('\\');
                if (_pos > 0)
                {
                    path= filenamePath.Substring(0, _pos);
                    filename = filenamePath.Substring(_pos + 1, filenamePath.Length - _pos - 1);
                }
                else
                {
                    filename = filenamePath;
                }
                TraceInternal.TraceInformation("Use Name=" + filename);
                TraceInternal.TraceInformation("Use Path=" + path);

                consoleTextBox.Text = "";
                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                this.Text = "Basic " + version + " - " + filename;


                filenamePath = path + Path.DirectorySeparatorChar + filename;
                char[] program;
                try
                {
                    using (StreamReader sr = new StreamReader(filenamePath))
                    {
                        program = sr.ReadToEnd().ToCharArray();
                    }
                	_mruMenu.AddFile(filenamePath);
                    _basic = new Altair.Interpreter(program, _textBoxIO);

                    _stopped = false;
                    this._workerThread = new Thread(new ThreadStart(this.Run));
                    _textBoxIO.Reset();
                    this._workerThread.Start();
                    consoleTextBox.Visible = true;
                    consoleTextBox.Enabled = true;
                }
                catch (Exception e1)
                {
                    TraceInternal.TraceError(e1.ToString());
                }
            }
            Debug.WriteLine("Out FileOpenMenuItem_Click()");
        }

        private void FormatFontMenuItem_Click(object sender, EventArgs e)
        {
            Debug.WriteLine("In FormatFontMenuItem_Click()");
            FontDialog fontDialog = new FontDialog
            {
                Font = consoleTextBox.Font,
                ShowColor = true,
                Color = consoleTextBox.ForeColor
            };

            if (fontDialog.ShowDialog() == DialogResult.OK)
            {
                Font font = fontDialog.Font;
                Color color = fontDialog.Color;
                consoleTextBox.Font = font;
                consoleTextBox.ForeColor = color;
                Properties.Settings.Default.ConsoleFont = font;
                Properties.Settings.Default.ConsoleFontColor = color;
                // Save settings
                Settings.Default.Save();
            }
            Debug.WriteLine("Out FormatFontMenuItem_Click()");
        }

        private void ConsoleForm_Load(object sender, EventArgs e)
        {
            Debug.WriteLine("In ConsoleForm_Load()");

            Settings.Default.Upgrade();

            // Set window location
            if (Settings.Default.ConsoleLocation != null)
            {
                // fIx errors with location being negative or off the main display

                this.Location = Settings.Default.ConsoleLocation;
                if ((this.Location.X<0) || (this.Location.Y<0))
                {
                    this.Location = new Point(0, 0);
                }
            }

            this.WindowState = FormWindowState.Normal;

            // Fixed windows size

            this.Width = _textBoxIO.Width;

            // Set window size
            if (Settings.Default.ConsoleSize != null)
            {
                this.Size = Settings.Default.ConsoleSize;
            }

            // Set Console font
            if (Settings.Default.ConsoleFont != null)
            {
                this.consoleTextBox.Font = Settings.Default.ConsoleFont;
            }

            // Set Console font color
            if (Settings.Default.ConsoleFontColor != null)
            {
                this.consoleTextBox.ForeColor = Settings.Default.ConsoleFontColor;
            }

            // Set Console color
            if (Settings.Default.ConsoleColor != null)
            {
                this.consoleTextBox.BackColor = Settings.Default.ConsoleColor;
            }

            Debug.WriteLine("Out ConsoleForm_Load()");

        }

        private void ConsoleForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Debug.WriteLine("In ConsoleForm_FormClosing()");

            // Need to stop the thread
            // think i will try a better approach
            // add new method to end the tokeniser

            if (_stopped == false)
            {
                _textBoxIO.Input = "\r\n";
                _basic.Stop();
                _workerThread.Join(1000);
            }

            // Copy window location to app settings
            Settings.Default.ConsoleLocation = this.Location;

            // Copy window size to app settings
            if (this.WindowState == FormWindowState.Normal)
            {
                Settings.Default.ConsoleSize = this.Size;
            }
            else
            {
                Settings.Default.ConsoleSize = this.RestoreBounds.Size;
            }

            // Copy console font type to app settings
            Settings.Default.ConsoleFont = this.consoleTextBox.Font;

            // Copy console font color to app settings
            Settings.Default.ConsoleFontColor = this.consoleTextBox.ForeColor;

            // Copy console color to app settings
            Settings.Default.ConsoleColor = this.consoleTextBox.BackColor;

            // Safe Mru
            SaveFiles();

            // Save settings
            Settings.Default.Save();

            // Upgrade settings
            Settings.Default.Reload();

            Debug.WriteLine("Out ConsoleForm_FormClosing()");
        }

        private void FileExitMenuItem_Click(object sender, EventArgs e)
        {
            Debug.WriteLine("Out FileExitMenuItem_Click()");
            this.Close();
        }

        private void FormatColorMenuItem_Click(object sender, EventArgs e)
        {
            Debug.WriteLine("In FileExitMenuItem_Click()");
            ColorDialog colorDialog = new ColorDialog
            {
                Color = consoleTextBox.BackColor
            };

            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                Color color = colorDialog.Color;
                consoleTextBox.BackColor = color;
                Properties.Settings.Default.ConsoleColor = color;
            }
            Debug.WriteLine("Out FileExitMenuItem_Click()");
        }

        private void LoadFiles()
        {
            Debug.WriteLine("In LoadFiles()");
            TraceInternal.TraceVerbose("Files " + Properties.Settings.Default.FileCount);
            for (int i = 0; i < 4; i++)
            {
                string property = "File" + (i + 1);
                string file = (string)Properties.Settings.Default[property];
                if (file != "")
                {
                    _mruMenu.AddFile(file);
                    TraceInternal.TraceVerbose("Load " + file);
                }
            }
            Debug.WriteLine("Out LoadFiles()");
        }

        private void SaveFiles()
        {
            Debug.WriteLine("In SaveFiles");
            string[] files = _mruMenu.GetFiles();
            Properties.Settings.Default["FileCount"] = files.Length;
            TraceInternal.TraceVerbose("Files=" + files.Length);
            for (int i=0; i < 4; i++)
            {
                string property = "File" + (i + 1);
                if (i < files.Length)
                {
                    Properties.Settings.Default[property] = files[i];
                    TraceInternal.TraceVerbose("Save " + property + "="+ files[i]);
                }
                else
                {
                    Properties.Settings.Default[property] = "";
                    TraceInternal.TraceVerbose("Save " + property + "=");
                }
            }
            Debug.WriteLine("Out SaveFiles");
        }

        #endregion
    }
}