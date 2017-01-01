// Copyright (C) 1988 Jack W. Crenshaw. All rights reserved. 

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using log4net;
using ubasicLibrary;
using Altair;

namespace ubasicConsole
{
    class Program
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        static IConsoleIO consoleIO = new ConsoleIO();

        [STAThread]
        static void Main(string[] args)
        {
            string input = "";

            // need to think about case sensitivity
            // need to think about CR/LF in input files
            //
            //input = @"program.bas";
            //input = @"logic.bas";
            //input = @"case.bas";
            //input = @"variables.bas";
            //input = @"dim.bas";
            //input = @"dim_0.bas";
            //input = @"dim_1.bas";
            //input = @"dim_2.bas";
            //input = @"dim_3.bas";
            //input = @"dim_4.bas";
            //input = @"dim_5.bas";
            //input = @"dim_6.bas";
            //input = @"string.bas";
            //input = @"goto.bas";
            //input = @"goto_single.bas";
            //input = @"gosub.bas";
            //input = @"if.bas";
            //input = @"if_single.bas";
            //input = @"print.bas";
            //input = @"fornext.bas";
            //input = @"fornext_single.bas";
            //input = @"read.bas";
            //input = @"let.bas";
            //input = @"input.bas";
            //input = @"numbers.bas";
            //input = @"def.bas";
            //input = @"restore.bas";
            //input = @"on_goto.bas";
            //input = @"randomize.bas";
            //input = @"gosub_inline.bas";
            //
            // OCT64 - version 2
            //
            //input = @"OCT64_PAGE3.bas";
            //input = @"OCT64_PAGE8.bas";
            //input = @"OCT64_PAGE12.bas";
            //input = @"OCT64_PAGE13.bas";
            //input = @"OCT64_PAGE19.bas";
            //input = @"OCT64_PAGE32.bas";
            //input = @"OCT64_PAGE33.bas";
            //input = @"OCT64_PAGE35.bas";
            //input = @"OCT64_PAGE37.bas";
            //input = @"OCT64_PAGE40a.bas";
            //input = @"OCT64_PAGE42b.bas";
            //input = @"OCT64_PAGE44.bas";
            //input = @"OCT64_PAGE46.bas";
            //input = @"OCT64_PAGE47a.bas";
            //input = @"OCT64_PAGE47b.bas";
            //
            // SEP66 - version 3
            //
            //input = @"SEP66_PAGE14.bas";
            //input = @"SEP66_PAGE18.bas";
            //input = @"SEP66_PAGE23a.bas";
            //input = @"SEP66_PAGE23b.bas";
            //input = @"SEP66_PAGE23c.bas";
            //input = @"SEP66_PAGE25.bas";
            //input = @"SEP66_PAGE26a.bas";
            //input = @"SEP66_PAGE26b.bas";
            //input = @"SEP66_PAGE26c.bas";
            //input = @"SEP66_PAGE29.bas";
            //input = @"SEP66_PAGE30.bas";
            //input = @"SEP66_PAGE31.bas";
            //
            // JAN68 - version 4
            //
            //input = @"JAN68_PAGE64.bas";
            //input = @"JAN68_PAGE65.bas";
            //input = @"JAN68_PAGE66a.bas";
            // 
            // MAY72 - version 5
            //
            //input = @"MAY72_PAGE??.bas";
            //
            // OCT75 - altair
            //
            //input = @"OCT75_PAGE11.bas";
            //input = @"OCT75_PAGE15.bas";
            //input = @"OCT75_PAGE15A.bas";
            //input = @"OCT75_PAGE18a.bas";
            //input = @"OCT75_PAGE18b.bas";
            //input = @"OCT75_PAGE18c.bas";
            //input = @"OCT75_PAGE18d.bas";
            //input = @"OCT75_PAGE19a.bas";
            //input = @"OCT75_PAGE21.bas"; 
            //input = @"OCT75_PAGE20a.bas";
            //input = @"OCT75_PAGE28.bas";
            //
            // Creative Computing
            //
            //input = @"creative_computing\amazing.bas";
            //input = @"creative_computing\aceyducey.bas";
            //input = @"creative_computing\mugwump.bas";
            //input = @"creative_computing\stockmarket.bas";
            //input = @"creative_computing\hurkle.bas";
            //input = @"creative_computing\target.bas";
            //input = @"creative_computing\ticktacktoe1.bas";
            //input = @"creative_computing\ticktacktoe2.bas";
            input = @"creative_computing\superstartrek.txt";
            //input = @"creative_computing\superstartrek.bas";
            //input = @"creative_computing\superstartrekins.bas";

            char[] program;
            try
            {
                using (StreamReader sr = new StreamReader(input))
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
                    Err("Program " + e.Message);
                }
            }
            catch (Exception e1)
            {
                Debug(e1.ToString());
                Err("Input " + e1.Message);
            }
        }

        //--------------------------------------------------------------
        // Debug

        static void Debug(string s)
        {
            if (log.IsDebugEnabled == true) { log.Debug(s); }
        }

        //--------------------------------------------------------------
        // Report an Error

        static void Err(string s)
        {
            consoleIO.Error("Error: " + s + "\n");
            if (log.IsErrorEnabled == true) { log.Error(s); }
        }

    }
}
