using System;
using System.Diagnostics;
using System.Windows.Forms;
using log4net;
using uBasicLibrary;

namespace uBasicForm
{
    static class Program
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Trace.TraceInformation("Enter Main()");

            string[] args = Environment.GetCommandLineArgs();
            Parameter filePath = new Parameter();
            Parameter filename = new Parameter();

            // Get the default path directory

            filePath.Value = Environment.CurrentDirectory;
            filePath.Source = Parameter.SourceType.App;
            int items = args.Length;
            if (items == 2)
            {
                // Check if the config file has been paased in and overwrite the registry

                string filenamePath = args[1].Trim('"');
                int pos = filenamePath.LastIndexOf('.');
                if (pos > 0)
                {
                    string extension = filenamePath.Substring(pos + 1, filenamePath.Length - pos - 1);
                    filenamePath = filenamePath.Substring(0, pos);
                }
                pos = filenamePath.LastIndexOf('\\');
                if (pos > 0)
                {
                    filePath.Value = filenamePath.Substring(0, pos);
                    filePath.Source = Parameter.SourceType.Command;
                    filename.Value = filenamePath.Substring(pos + 1, filenamePath.Length - pos - 1);
                    filename.Source = Parameter.SourceType.Command;
                }
                else
                {
                    filename.Value = filenamePath;
                    filename.Source = Parameter.SourceType.Command;
                }
                Info("Use Filename=" + filename.Value);
                Info("Use Filepath=" + filePath.Value);
            }
            else
            {
                for (int item = 0; item < items; item++)
                {
                    switch (args[item])
                    {
                        case "/N":
                        case "--name":
                            filename.Value = args[item + 1];
                            filename.Value = filename.Value.TrimStart('"');
                            filename.Value = filename.Value.TrimEnd('"');
                            filename.Source = Parameter.SourceType.Command;
                            Debug("Use command value Name=" + filename);
                            break;
                        case "/P":
                        case "--path":
                            filePath.Value = args[item + 1];
                            filePath.Value = filePath.Value.TrimStart('"');
                            filePath.Value = filePath.Value.TrimEnd('"');
                            filePath.Source = Parameter.SourceType.Command;
                            Debug("Use command value Path=" + filePath);
                            break;
                    }
                }
            }
            Info("Use Filename=" + filename.Value);
            Info("Use Filepath=" + filePath.Value);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ConsoleForm(filePath.Value, filename.Value));

            Trace.TraceInformation("Exit Main()");

        }
        //--------------------------------------------------------------
        // Info

        static void Info(string s)
        {
            log.Info(s);
        }

        //--------------------------------------------------------------
        // Debug

        static void Debug(string s)
        {
            log.Debug(s);
        }

        //--------------------------------------------------------------
        // Report an Error

        static void Err(string s)
        {
            log.Error(s);
        }
    }
}
