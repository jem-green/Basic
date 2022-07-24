using System;
using System.IO;
using TracerLibrary;
using uBasicLibrary;
using Altair;
using System.Diagnostics;
using System.Threading;

namespace uBasicTerminal
{
    class Program
    {
        #region Fields

        static readonly IuBasicIO consoleIO = new ConsoleIO();

        #endregion
        #region Methods

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            Debug.WriteLine("Enter Main()");

            int pos = 0;
            Parameter<string> filePath = new Parameter<string>();
            Parameter<string> filename = new Parameter<string>();
            Parameter<string> fileExtension = new Parameter<string>();

            // Get the default path directory

            filePath.Value = Environment.CurrentDirectory;
            filePath.Source = Parameter<string>.SourceType.App;

            Parameter<string> logPath = new Parameter<string>("");
            Parameter<string> logName = new Parameter<string>("ubasicterminal");

            logPath.Value = System.Reflection.Assembly.GetExecutingAssembly().Location;
            logPath.Value = filePath.Value = Environment.CurrentDirectory;
            logPath.Source = Parameter<string>.SourceType.App;

            Parameter<SourceLevels> traceLevels = new Parameter<SourceLevels>
            {
                Value = TraceInternal.TraceLookup("CRITICAL"),
                Source = Parameter<SourceLevels>.SourceType.App
            };

            // Configure tracer options

            string logFilenamePath = logPath.Value.ToString() + Path.DirectorySeparatorChar + logName.Value.ToString() + ".log";
            FileStreamWithRolling dailyRolling = new FileStreamWithRolling(logFilenamePath, new TimeSpan(1, 0, 0, 0), FileMode.Append);
            TextWriterTraceListenerWithTime listener = new TextWriterTraceListenerWithTime(dailyRolling);
            Trace.AutoFlush = true;
            TraceFilter fileTraceFilter = new System.Diagnostics.EventTypeFilter(SourceLevels.Verbose);
            listener.Filter = fileTraceFilter;
            Trace.Listeners.Clear();
            Trace.Listeners.Add(listener);

            // Check if the config file has been paased in and overwrite the registry

            string filenamePath = "";
            int items = args.Length;
            if (items == 1)
            {
                filenamePath = args[0].Trim('"');
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
                    filename.Value = filenamePath.Substring(pos + 1, filenamePath.Length - pos - 1);
                    filename.Source = Parameter<string>.SourceType.Command;
                }
                else
                {
                    filename.Value = filenamePath;
                    filename.Source = Parameter<string>.SourceType.Command;
                }
                TraceInternal.TraceVerbose("Use filename=" + filename.Value.ToString());
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
                                filename.Value = args[item + 1];
                                filename.Value = filename.Value.ToString().TrimStart('"');
                                filename.Value = filename.Value.ToString().TrimEnd('"');
                                filename.Source = Parameter<string>.SourceType.Command;
                                TraceInternal.TraceVerbose("Use command value Name=" + filename);
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
            Trace.Listeners.Remove(listener);
            listener.Close();
            listener.Dispose();

            dailyRolling = new FileStreamWithRolling(logFilenamePath, new TimeSpan(1, 0, 0, 0), FileMode.Append);
            listener = new TextWriterTraceListenerWithTime(dailyRolling);
            Trace.AutoFlush = true;
            SourceLevels sourceLevels = TraceInternal.TraceLookup(traceLevels.Value.ToString());
            fileTraceFilter = new System.Diagnostics.EventTypeFilter(sourceLevels);
            listener.Filter = fileTraceFilter;
            Trace.Listeners.Add(listener);


            Trace.TraceInformation("Use Name=" + filename);
            Trace.TraceInformation("Use Path=" + filePath);
            Trace.TraceInformation("Use Log Name=" + logName);
            Trace.TraceInformation("Use Log Path=" + logPath);

            if ((filename.Value.ToString().Length > 0) && (filePath.Value.ToString().Length > 0))
            {
                filenamePath = filePath.Value.ToString() + Path.DirectorySeparatorChar + filename.Value.ToString() + ".bas";
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
                        TraceInternal.TraceError(e.ToString());
                    }
                }
                catch (Exception e1)
                {
                    TraceInternal.TraceVerbose(e1.ToString());
                    TraceInternal.TraceError("Input " + e1.Message);
                }
            }
            else
            {
                TraceInternal.TraceError("Program name not supplied");
            }

            ManualResetEvent manualResetEvent = new ManualResetEvent(false);

            manualResetEvent.WaitOne();

            Debug.WriteLine("Exit Main()");
        }

        #endregion
    }
}