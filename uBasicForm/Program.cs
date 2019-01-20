using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using log4net;
using uBasicLibrary;

namespace uBasicForm
{
    static class Program
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static Parameter uBasicPath;
        private static Parameter uBasicName;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            log.Debug("Enter Main()");

            string[] args = Environment.GetCommandLineArgs();

            int pos = 0;
            uBasicPath = new Parameter("");
            uBasicName = new Parameter("");

            // Get the default path directory

            uBasicPath.Value = System.Reflection.Assembly.GetExecutingAssembly().Location;
            pos = uBasicPath.Value.LastIndexOf('\\');
            uBasicPath.Value = uBasicPath.Value.Substring(0, pos);
            uBasicPath.Source = Parameter.SourceType.None;

            // Check if the config file has been paased in and overwrite the registry

            for (int item = 1; item < args.Length; item++)
            {
                if (item == 1)
                {
                    string filenamepath = args[item];
                    filenamepath = filenamepath.Trim('"');
                    pos = filenamepath.LastIndexOf('\\');
                    if (pos > 0)
                    {
                        uBasicPath.Value = filenamepath.Substring(0, pos);
                        uBasicPath.Source = Parameter.SourceType.Command;
                        uBasicName.Value = filenamepath.Substring(pos + 1, filenamepath.Length - pos - 1);
                        uBasicName.Source = Parameter.SourceType.Command;
                    }
                    else
                    {
                        uBasicName.Value = filenamepath;
                        uBasicName.Source = Parameter.SourceType.Command;
                    }
                    log.Debug("Use command value " + filenamepath);
                }
                else
                {

                    switch (args[item])
                    {
                        case "/N":
                        case "--name":
                            uBasicName.Value = args[item + 1];
                            uBasicName.Value = uBasicName.Value.TrimStart('"');
                            uBasicName.Value = uBasicName.Value.TrimEnd('"');
                            uBasicName.Source = Parameter.SourceType.Command;
                            log.Debug("Use command value Name=" + uBasicName);
                            break;
                        case "/P":
                        case "--path":
                            uBasicPath.Value = args[item + 1];
                            uBasicPath.Value = uBasicPath.Value.TrimStart('"');
                            uBasicPath.Value = uBasicPath.Value.TrimEnd('"');
                            uBasicPath.Source = Parameter.SourceType.Command;
                            log.Debug("Use command value Path=" + uBasicPath);
                            break;
                    }
                }
            }
            log.Info("Use Name=" + uBasicName.Value + " Path=" + uBasicPath.Value);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ConsoleForm(uBasicPath,uBasicName));

            log.Debug("Exit Main()");

        }
    }
}
