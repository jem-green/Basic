using System;
using TracerLibrary;
using System.Diagnostics;
using System.Windows.Forms;
using uBasicLibrary;
using System.IO;
using Microsoft.Win32;

namespace uBasicForm
{
    static class Program
    {
        #region Fields

        #endregion
        #region Methods

        /// <summary>
        /// The main entry point for the application.
        /// </summary>

        [STAThread]
        static void Main()
        {
            // Read in specific configuration

            Debug.WriteLine("Enter Main()");

            string[] args = Environment.GetCommandLineArgs();
			int pos = 0;
            Parameter<string> filePath = new Parameter<string>();
            Parameter<string> fileName = new Parameter<string>();
            Parameter<string> fileExtension = new Parameter<string>();

            // Get the default path directory

            filePath.Value = Environment.CurrentDirectory;
            filePath.Source = Parameter<string>.SourceType.App;

            Parameter<string> logPath = new Parameter<string>("");
            Parameter<string> logName = new Parameter<string>("ubasicform");

            logPath.Value = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + System.IO.Path.DirectorySeparatorChar + "ubasic";
            //logPath.Value = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + System.IO.Path.DirectorySeparatorChar + "ubasic";
            //logPath.Value = "d:\\";
            logPath.Source = Parameter<string>.SourceType.App;

            Parameter<SourceLevels> traceLevels = new Parameter<SourceLevels>
            {
                Value = TraceInternal.TraceLookup("VERBOSE"),
                Source = Parameter<SourceLevels>.SourceType.App
            };

            // Configure tracer options

            if (!Directory.Exists(logPath.Value))
            {
                Directory.CreateDirectory(logPath.Value);
            }
            string logFilenamePath = logPath.Value.ToString() + Path.DirectorySeparatorChar + logName.Value.ToString() + ".log";
            FileStreamWithRolling dailyRolling = new FileStreamWithRolling(logFilenamePath, new TimeSpan(1, 0, 0, 0), FileMode.Append);
            TextWriterTraceListenerWithTime listener = new TextWriterTraceListenerWithTime(dailyRolling);
            Trace.AutoFlush = true;
            TraceFilter fileTraceFilter = new System.Diagnostics.EventTypeFilter(SourceLevels.Verbose);
            listener.Filter = fileTraceFilter;
            System.Diagnostics.Trace.Listeners.Clear();
            System.Diagnostics.Trace.Listeners.Add(listener);

            if (IsLinux == false)
            {
                // Check if the registry has been set and overwrite the application defaults

                RegistryKey key = RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, RegistryView.Registry64);
                string keys = "software\\green\\ubasic";
                foreach (string subkey in keys.Split('\\'))
                {
                    key = key.OpenSubKey(subkey);
                    if (key == null)
                    {
                        TraceInternal.TraceVerbose("Failed to open " + subkey);
                        break;
                    }
                }

                // Get the log path

                try
                {
                    if (key.GetValue("logpath", "").ToString().Length > 0)
                    {
                        logPath.Value = (string)key.GetValue("logpath", logPath);
                        logPath.Source = Parameter<string>.SourceType.Registry;
                        TraceInternal.TraceVerbose("Use registry value; logPath=" + logPath);
                    }
                }
                catch (NullReferenceException)
                {
                    TraceInternal.TraceVerbose("Registry error use default values; logPath=" + logPath.Value);
                }
                catch (Exception e)
                {
                    TraceInternal.TraceError(e.ToString());
                }

                // Get the log name

                try
                {
                    if (key.GetValue("logname", "").ToString().Length > 0)
                    {
                        logName.Value = (string)key.GetValue("logname", logName);
                        logName.Source = Parameter<string>.SourceType.Registry;
                        TraceInternal.TraceVerbose("Use registry value; LogName=" + logName);
                    }
                }
                catch (NullReferenceException)
                {
                    TraceInternal.TraceVerbose("Registry error use default values; LogName=" + logName.Value);
                }
                catch (Exception e)
                {
                    TraceInternal.TraceError(e.ToString());
                }

                // Get the name

                try
                {
                    if (key.GetValue("name", "").ToString().Length > 0)
                    {
                        fileName.Value = (string)key.GetValue("name", fileName);
                        fileName.Source = Parameter<string>.SourceType.Registry;
                        TraceInternal.TraceVerbose("Use registry value Name=" + fileName);
                    }
                }
                catch (NullReferenceException)
                {
                    TraceInternal.TraceVerbose("Registry error use default values; Name=" + fileName.Value);
                }
                catch (Exception e)
                {
                    TraceInternal.TraceError(e.ToString());
                }

                // Get the path

                try
                {
                    if (key.GetValue("path", "").ToString().Length > 0)
                    {
                        filePath.Value = (string)key.GetValue("path", filePath);
                        filePath.Source = Parameter<string>.SourceType.Registry;
                        TraceInternal.TraceVerbose("Use registry value Path=" + filePath);
                    }
                }
                catch (NullReferenceException)
                {
                    TraceInternal.TraceVerbose("Registry error use default values; Path=" + filePath.Value);
                }
                catch (Exception e)
                {
                    TraceInternal.TraceError(e.ToString());
                }

                // Get the traceLevels

                try
                {
                    if (key.GetValue("debug", "").ToString().Length > 0)
                    {
                        traceLevels.Value = TraceInternal.TraceLookup((string)key.GetValue("debug", "verbose"));
                        traceLevels.Source = Parameter<SourceLevels>.SourceType.Registry;
                        TraceInternal.TraceVerbose("Use registry value; Debug=" + traceLevels.Value);
                    }
                }
                catch (NullReferenceException)
                {
                    TraceInternal.TraceWarning("Registry error use default values; Debug=" + traceLevels.Value);
                }
                catch (Exception e)
                {
                    TraceInternal.TraceError(e.ToString());
                }
            }

            // Check if the config file has been paased in and overwrite the registry

            string filenamePath = "";
            int items = args.Length;
            if (items == 2)
            {
                filenamePath = args[1].Trim('"');
                pos = filenamePath.LastIndexOf('.');
                if (pos > 0)
                {
                    fileExtension.Value = filenamePath.Substring(pos + 1, filenamePath.Length - pos - 1);
                    filePath.Source = Parameter<string>.SourceType.Command;
                    filenamePath = filenamePath.Substring(0, pos);
                }
                pos = filenamePath.LastIndexOf('\\');
                if (pos > 0)
                {
                    filePath.Value = filenamePath.Substring(0, pos);
                    filePath.Source = Parameter<string>.SourceType.Command;
                    fileName.Value = filenamePath.Substring(pos + 1, filenamePath.Length - pos - 1);
                    fileName.Source = Parameter<string>.SourceType.Command;
                }
                else
                {
                    fileName.Value = filenamePath;
                    fileName.Source = Parameter<string>.SourceType.Command;
                }
                TraceInternal.TraceVerbose("Use filename=" + fileName.Value.ToString());
                TraceInternal.TraceVerbose("use filePath=" + filePath.Value.ToString());
                TraceInternal.TraceVerbose("use fileExtension=" + fileExtension.Value.ToString());

            }
            else
            {
                // Check if the config file has been paased in and overwrite the defaults

                for (int item = 0; item < items; item++)
                {
                    string lookup = args[item];
                    if (lookup.Length > 1)
                    {
                        lookup = lookup.ToLower();
                    }
                    switch (lookup)
                    {
					    case "/d":
                        case "--debug":
                            {
                                string traceName = args[item + 1];
                                traceName = traceName.TrimStart('"');
                                traceName = traceName.TrimEnd('"');
                                traceLevels.Value = TraceInternal.TraceLookup(traceName);
                                traceLevels.Source = Parameter<SourceLevels>.SourceType.Command;
                                TraceInternal.TraceVerbose("Use command value traceLevels=" + traceLevels);
                                break;
                            }
                        case "/n":
                        case "--logname":
                            {
                                logName.Value = args[item + 1];
                                logName.Value = logName.Value.ToString().TrimStart('"');
                                logName.Value = logName.Value.ToString().TrimEnd('"');
                                logName.Source = Parameter<string>.SourceType.Command;
                                TraceInternal.TraceVerbose("Use command value logName=" + logName);
                                break;
                            }
                        case "/p":
                        case "--logpath":
                            {
                                logPath.Value = args[item + 1];
                                logPath.Value = logPath.Value.ToString().TrimStart('"');
                                logPath.Value = logPath.Value.ToString().TrimEnd('"');
                                logPath.Source = Parameter<string>.SourceType.Command;
                                TraceInternal.TraceVerbose("Use command value logPath=" + logPath);
                                break;
                            }
                        case "/N":
                        case "--name":
                            {
	                            fileName.Value = args[item + 1];
	                            fileName.Value = fileName.Value.ToString().TrimStart('"');
	                            fileName.Value = fileName.Value.ToString().TrimEnd('"');
	                            fileName.Source = Parameter<string>.SourceType.Command;
                                TraceInternal.TraceVerbose("Use command value Name=" + fileName);
                            	break;
                            }
                        case "/P":
                        case "--path":
                            {
	                            filePath.Value = args[item + 1];
	                            filePath.Value = filePath.Value.ToString().TrimStart('"');
	                            filePath.Value = filePath.Value.ToString().TrimEnd('"');
	                            filePath.Source = Parameter<string>.SourceType.Command;
                                TraceInternal.TraceVerbose("Use command value Path=" + filePath);
                           	 	break;
                            }
                    }
                }
            }

            // Adjust the log location if it has been overridden by the arguments

            logFilenamePath = logPath.Value.ToString() + Path.DirectorySeparatorChar + logName.Value.ToString() + ".log";

            // Redirect the output

            listener.Flush();
            System.Diagnostics.Trace.Listeners.Remove(listener);
            listener.Close();
            listener.Dispose();
            if (!Directory.Exists(logPath.Value))
            {
                Directory.CreateDirectory(logPath.Value);
            }
            dailyRolling = new FileStreamWithRolling(logFilenamePath, new TimeSpan(1, 0, 0, 0), FileMode.Append);
            listener = new TextWriterTraceListenerWithTime(dailyRolling);
            Trace.AutoFlush = true;
            SourceLevels sourceLevels = TraceInternal.TraceLookup(traceLevels.Value.ToString());
            fileTraceFilter = new System.Diagnostics.EventTypeFilter(sourceLevels);
            listener.Filter = fileTraceFilter;
            System.Diagnostics.Trace.Listeners.Add(listener);

            Trace.TraceInformation("Use Name=" + fileName);
            Trace.TraceInformation("Use Path=" + filePath);
            Trace.TraceInformation("Use Log Name=" + logName);
            Trace.TraceInformation("Use Log Path=" + logPath);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ConsoleForm(filePath.Value.ToString(), fileName.Value.ToString()));

            Debug.WriteLine("Exit Main()");

        }

        #endregion
        #region Private

        private static bool IsLinux
        {
            get
            {
                int p = (int)Environment.OSVersion.Platform;
                return (p == 4) || (p == 6) || (p == 128);
            }
        }

        #endregion
    }
}
