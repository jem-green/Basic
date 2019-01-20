using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using uBasicLibrary;
using System.Threading;
using log4net;
using uBasicForm.Properties;
using System.IO;

namespace uBasicForm
{
    public partial class ConsoleForm : Form
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        // Prepare the uBasic
        static IConsoleIO textBoxIO = null;
        IInterpreter basic = null;
        int pos = 0;
        Parameter uBasicPath = new Parameter();
        Parameter uBasicName = new Parameter();
        bool stop = false;
        
        // Declare a delegate used to communicate with the UI thread
        private delegate void UpdateTextDelegate();
        private UpdateTextDelegate updateTextDelegate = null;

        // Declare our worker thread
        private Thread workerThread = null;

        // Manage the inputs
        string value = "";

        // Most recently used
        protected MruStripMenu mruMenu;

        public ConsoleForm(Parameter path, Parameter name)
        {
            log.Debug("In ConsoleForm");

            InitializeComponent();

            this.Icon = Resources.uBasic;

            textBoxIO = new TextBoxIO();
            textBoxIO.TextReceived += new EventHandler<TextEventArgs>(OnMessageReceived);

            // Initialise the delegate
            this.updateTextDelegate = new UpdateTextDelegate(this.UpdateText);

            // Initialise and start worker thread
            this.workerThread = new Thread(new ThreadStart(this.Run));

            // Add most recent used
            mruMenu = new MruStripMenuInline(fileMenuItem, recentFileToolStripMenuItem, new MruStripMenu.ClickedHandler(OnMruFile), 4);
            LoadFiles();

            if ((path.Value.Length > 0) && (name.Value.Length > 0))
            {
                uBasicPath = path;
                uBasicName = name;
                consoleTextBox.Text = "";
                pos = uBasicName.Value.LastIndexOf('.');
                this.Text = uBasicName.Value.Substring(0, pos) + " - uBasic";

                string filenamePath = "";
                filenamePath = uBasicPath.Value + Path.DirectorySeparatorChar + uBasicName.Value;
                char[] program;
                try
                {
                    using (StreamReader sr = new StreamReader(filenamePath))
                    {
                        program = sr.ReadToEnd().ToCharArray();
                    }

                    basic = new Altair.Interpreter(program, textBoxIO);

                    stop = true;
                    textBoxIO.Reset();
                    this.workerThread.Start();
                    consoleTextBox.Visible = true;
                    consoleTextBox.Enabled = true;
                }
                catch (Exception e1)
                {
                    log.Error(e1.ToString());
                }
            }
        }

        private void OnMruFile(int number, String filenamepath)
        {
            consoleTextBox.Enabled = false;
            consoleTextBox.Visible = false;

            if (File.Exists(filenamepath) == true)
            {
                mruMenu.SetFirstFile(number);
                pos = filenamepath.LastIndexOf('\\');
                if (pos > 0)
                {
                    uBasicPath.Value = filenamepath.Substring(0, pos);
                    uBasicPath.Source = Parameter.SourceType.App;
                    uBasicName.Value = filenamepath.Substring(pos + 1, filenamepath.Length - pos - 1);
                    uBasicName.Source = Parameter.SourceType.App;
                }
                else
                {
                    uBasicName.Value = filenamepath;
                    uBasicName.Source = Parameter.SourceType.App;
                }
                log.Info("Use Name=" + uBasicName.Value + " Path=" + uBasicPath.Value);

                consoleTextBox.Text = "";
                pos = uBasicName.Value.LastIndexOf('.');
                this.Text = uBasicName.Value.Substring(0, pos) + " - uBasic";

                string filenamePath = "";
                filenamePath = uBasicPath.Value + Path.DirectorySeparatorChar + uBasicName.Value;
                char[] program;
                try
                {
                    using (StreamReader sr = new StreamReader(filenamePath))
                    {
                        program = sr.ReadToEnd().ToCharArray();
                    }

                    basic = new Altair.Interpreter(program, textBoxIO);

                    stop = false;
                    this.workerThread = new Thread(new ThreadStart(this.Run));
                    textBoxIO.Reset();
                    this.workerThread.Start();
                    consoleTextBox.Visible = true;
                    consoleTextBox.Enabled = true;
                }
                catch (Exception e1)
                {
                    log.Error(e1.ToString());
                }
            }
            else
            {
                mruMenu.RemoveFile(number);
            }
        }


        private void UpdateText()
        {
            string output = textBoxIO.Output;
            if (output.Length > 0)
            {
                this.consoleTextBox.AppendText(output);
            }
        }

        // Define the event handlers.
        private void OnMessageReceived(object source, TextEventArgs e)
        {
            if (e.Text.Length > 0)
            {
                //this.consoleTextBox.AppendText(e.Text);
                this.Invoke(this.updateTextDelegate);
            }
        }

        private void Run()
        {
            //this.Invoke(this.updateTextDelegate);
            basic.Init(0);
            try
            {
                do
                {
                    basic.Run();
                } while (!basic.Finished());
            }
            catch (Exception e)
            {
                log.Debug(e.ToString());
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
                value = value + chr;
                textBoxIO.Input = value;
                this.consoleTextBox.AppendText("\r" + "\n");
                value = "";
            }
            else if (chr == '\b')
            {
                this.consoleTextBox.Text = this.consoleTextBox.Text.Substring(0, this.consoleTextBox.Text.Length - 1);
                this.consoleTextBox.SelectionStart = consoleTextBox.Text.Length;
                this.consoleTextBox.ScrollToCaret();
                value = value.Substring(0, value.Length - 1);
            }
            else
            {
                value = value + chr;
                this.consoleTextBox.AppendText(Convert.ToString(chr));
            }
        }

        private void FileOpenMenuItem_Click(object sender, EventArgs e)
        {
            log.Debug("In FileOpenMenuItem_Click");

            consoleTextBox.Enabled = false;
            consoleTextBox.Visible = false;
            workerThread.Abort();

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "uBasic (*.bas)|*.bas",
                FilterIndex = 1,
                RestoreDirectory = true
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filenamepath = openFileDialog.FileName;
                pos = filenamepath.LastIndexOf('\\');
                if (pos > 0)
                {
                    uBasicPath.Value = filenamepath.Substring(0, pos);
                    uBasicPath.Source = Parameter.SourceType.App;
                    uBasicName.Value = filenamepath.Substring(pos + 1, filenamepath.Length - pos - 1);
                    uBasicName.Source = Parameter.SourceType.App;
                }
                else
                {
                    uBasicName.Value = filenamepath;
                    uBasicName.Source = Parameter.SourceType.App;
                }
                log.Info("Use Name=" + uBasicName.Value + " Path=" + uBasicPath.Value);

                consoleTextBox.Text = "";
                pos = uBasicName.Value.LastIndexOf('.');
                this.Text = uBasicName.Value.Substring(0, pos) + " - uBasic";
                
				string filenamePath = "";
                filenamePath = uBasicPath.Value + Path.DirectorySeparatorChar + uBasicName.Value;
                char[] program;
                try
                {
                    using (StreamReader sr = new StreamReader(filenamePath))
                    {
                        program = sr.ReadToEnd().ToCharArray();
                    }
                	mruMenu.AddFile(filenamepath);
                    basic = new Altair.Interpreter(program, textBoxIO);

                    stop = false;
                    textBoxIO.Reset();
                    this.workerThread = new Thread(new ThreadStart(this.Run));
                    this.workerThread.Start();
                    consoleTextBox.Visible = true;
                    consoleTextBox.Enabled = true;
                }
                catch (Exception e1)
                {
                    log.Error(e1.ToString());
                }
            }
            log.Debug("Out FileOpenMenuItem_Click");
        }

        private void FormatFontMenuItem_Click(object sender, EventArgs e)
        {
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
        }

        private void ConsoleForm_Load(object sender, EventArgs e)
        {
            log.Debug("In ConsoleForm_Load");

            Settings.Default.Upgrade();

            // Set window location
            if (Settings.Default.ConsoleLocation != null)
            {
                this.Location = Settings.Default.ConsoleLocation;
            }

            // Fixed windows size

            this.Width = textBoxIO.Width;

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

            log.Debug("Out ConsoleForm_Load");

        }

        private void ConsoleForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            log.Debug("In ConsoleForm_FormClosing");

            // Need to stop the thread
            // think i will try a better approach

            workerThread.Abort();

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

            log.Debug("Out ConsoleForm_FormClosing");
        }

        private void FileExitMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void FormatColorMenuItem_Click(object sender, EventArgs e)
        {
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
        }

        private void LoadFiles()
        {
            log.Debug("In LoadFiles");
            log.Debug("Files " + Properties.Settings.Default.FileCount);

            string file = "";
            string property = "";
            for (int i = 0; i < 4; i++)
            {
                property = "File" + (i + 1);
                file = (string)Properties.Settings.Default[property];
                if (file != "")
                {
                    mruMenu.AddFile(file);
                    log.Debug("Load " + file);
                }
            }
            log.Debug("Out LoadFiles");
        }

        public void SaveFiles()
        {
            log.Debug("In SaveFiles");
            string[] files = mruMenu.GetFiles();
            string property = "";
            Properties.Settings.Default["FileCount"] = files.Length;
            log.Debug("Files=" + files.Length);
            for (int i=0; i < 4; i++)
            {
                property = "File" + (i + 1);
                if (i < files.Length)
                {
                    Properties.Settings.Default[property] = files[i];
                    log.Debug("Save " + property + "="+ files[i]);
                }
                else
                {
                    Properties.Settings.Default[property] = "";
                    log.Debug("Save " + property + "=");
                }
            }
            log.Debug("Out SaveFiles");
        }
    }
}