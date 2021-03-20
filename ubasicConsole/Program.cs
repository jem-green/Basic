using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using log4net;
using uBasicLibrary;
using Altair;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace uBasicConsole
{
    class Program
    {
        protected static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        static readonly IConsoleIO consoleIO = new ConsoleIO();
        public static bool isclosing = false;
        static private HandlerRoutine ctrlCHandler;
        #region unmanaged
        // Declare the SetConsoleCtrlHandler function
        // as external and receiving a delegate.

        [DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);

        // A delegate type to be used as the handler routine
        // for SetConsoleCtrlHandler.
        public delegate bool HandlerRoutine(CtrlTypes CtrlType);

        // An enumerated type for the control messages
        // sent to the handler routine.
        public enum CtrlTypes
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }

        #endregion

        static void Main(string[] args)
        {
            Trace.TraceInformation("Enter Main()");

            ctrlCHandler = new HandlerRoutine(ConsoleCtrlCheck);
            SetConsoleCtrlHandler(ctrlCHandler, true);
            int pos = 0;
            Parameter filePath = new Parameter();
            Parameter filename = new Parameter();

            // Get the default path directory

            filePath.Value = Environment.CurrentDirectory;
            filePath.Source = Parameter.SourceType.App;

            // Check if the config file has been paased in and overwrite the registry

            string filenamePath = "";
            string extension = "";
            int items = args.Length;
            if (items == 1)
            {
                filenamePath = args[0].Trim('"');
                pos = filenamePath.LastIndexOf('.');
                if (pos > 0)
                {
                    extension = filenamePath.Substring(pos + 1, filenamePath.Length - pos - 1);
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
                Info("Use filename=" + filename.Value);
                Info("use filePath=" + filePath.Value);
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

            if ((filename.Value != "") && (filePath.Value != ""))
            {
                filenamePath = filePath.Value + Path.DirectorySeparatorChar + filename.Value;
                char[] program;
                try
                {
                    using (StreamReader sr = new StreamReader(filenamePath))
                    {
                        program = sr.ReadToEnd().ToCharArray();
                    }

                    // 

                    IInterpreter basic = new Altair.Interpreter(program, consoleIO);
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
                        Debug(e.ToString());
                        //Err("Program " + e.Message);
                    }
                }
                catch (Exception e1)
                {
                    Debug(e1.ToString());
                    Err("Input " + e1.Message);
                }
            }
            else
            {
                Err("Program name not supplied");
            }

            Trace.TraceInformation("Exit Main()");
        }
		
		private static bool ConsoleCtrlCheck(CtrlTypes ctrlType)
        {
            Debug("Enter ConsoleCtrlCheck()");

            switch (ctrlType)
            {
                case CtrlTypes.CTRL_C_EVENT:
                    isclosing = true;
                    Info("CTRL+C received:");
                    break;

                case CtrlTypes.CTRL_BREAK_EVENT:
                    isclosing = true;
                    Info("CTRL+BREAK received:");
                    break;

                case CtrlTypes.CTRL_CLOSE_EVENT:
                    isclosing = true;
                    Info("Program being closed:");
                    break;

                case CtrlTypes.CTRL_LOGOFF_EVENT:
                case CtrlTypes.CTRL_SHUTDOWN_EVENT:
                    isclosing = true;
                    Info("User is logging off:");
                    break;

            }
            Debug("Exit ConsoleCtrlCheck()");

            Environment.Exit(0);

            return (true);

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
