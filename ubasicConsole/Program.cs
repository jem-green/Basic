using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using log4net;
using uBasicLibrary;
using Altair;

namespace uBasicConsole
{
    class Program
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        static IConsoleIO consoleIO = new ConsoleIO();

        [STAThread]
        static void Main(string[] args)
        {

            log.Debug("Enter Main()");
            int pos = 0;
            Parameter uBasicPath = new Parameter();
            Parameter uBasicName = new Parameter();

            // Get the default path directory

            uBasicPath.Value = System.Reflection.Assembly.GetExecutingAssembly().Location;
            pos = uBasicPath.Value.LastIndexOf('\\');
            uBasicPath.Value = uBasicPath.Value.Substring(0, pos);
            uBasicPath.Source = Parameter.SourceType.None;

            // Check if the config file has been paased in and overwrite the registry

            string filenamePath = "";

            for (int item = 0; item < args.Length; item++)
            {
                if (item == 0)
                {
                    filenamePath = args[0].Trim('"');
                    pos = filenamePath.LastIndexOf('\\');
                    if (pos > 0)
                    {
                        uBasicPath.Value = filenamePath.Substring(0, pos);
                        uBasicPath.Source = Parameter.SourceType.Command;
                        uBasicName.Value = filenamePath.Substring(pos + 1, filenamePath.Length - pos - 1);
                        uBasicName.Source = Parameter.SourceType.Command;
                    }
                    else
                    {
                        uBasicName.Value = filenamePath;
                        uBasicName.Source = Parameter.SourceType.Command;
                    }
                    log.Debug("Use command value " + filenamePath);
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
            log.Info("Use Name=" + uBasicName + " Path=" + uBasicPath);

            if ((uBasicName.Value != "") && (uBasicPath.Value != ""))
            {
                filenamePath = uBasicPath.Value + Path.DirectorySeparatorChar + uBasicName.Value;
                char[] program;
                try
                {
                    using (StreamReader sr = new StreamReader(filenamePath))
                    {
                        program = sr.ReadToEnd().ToCharArray();
                    }

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

            log.Debug("Exit Main()");
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
            consoleIO.Error("Error: " + s + "\n");
            log.Error(s);
        }

    }
}
