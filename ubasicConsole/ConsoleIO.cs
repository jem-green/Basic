//  Copyright (c) 2017, Jeremy Green All rights reserved.

using System;
using System.Collections.Generic;
using System.Text;
using uBasicLibrary;

namespace uBasicConsole
{
    public class ConsoleIO : IConsoleIO
    {
        #region Event handling

        /// <summary>
        /// Occurs when the Zmachine recives a message.
        /// </summary>
        public event EventHandler<TextEventArgs> TextReceived;

        /// <summary>
        /// Handles the actual event
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnTextReceived(TextEventArgs e)
        {
            EventHandler<TextEventArgs> handler = TextReceived;
            if (handler != null)
                handler(this, e);
        }

        #endregion
        #region Variables

        // Formatting constraints

        readonly int _consoleHeight = 80;
        int _consoleWidth = 75;
        int _zoneWidth = 15;
        int _compactWidth = 3;
        int _hpos = 0;
        int _vpos = 0;
        string _input = "";
        string _output = "";

        #endregion
        #region Properties

        public int Width
        {
            get
            {
                return (_consoleWidth);
            }
        }

        public int Height
        {
            get
            {
                return (_consoleHeight);
            }
        }

        public string Input
        {
            set
            {
                _input = value;
            }
        }

        public string Output
        {
            get
            {
                return (_output);
            }
        }

        public int Hpos
        {
            get
            {
                return (_hpos);
            }
        }

        public int Vpos
        {
            get
            {
                return (_vpos);
            }
        }

        public int Console
        {
            get
            {
                return (_vpos);
            }
            set
            {
                _consoleWidth = value;
            }
        }

        public int Zone
        {
            get
            {
                return (_zoneWidth);
            }
            set
            {
                _zoneWidth = value;
            }
        }

        public int Compact
        {
            get
            {
                return (_compactWidth);
            }
            set
            {
                _compactWidth = value;
            }
        }

        #endregion
        #region Methods

        public void Out(string s)
        {
            string check = s.TrimEnd(' ');
            _hpos += s.Length;
            if (_hpos > _consoleWidth)
            {
                if (check.Length > 0)
                {
                    _hpos = s.Length;
                    _vpos++;
                    System.Console.Out.Write("\n");
                    System.Console.Out.Write(s);
                }
                else
                {
                    _hpos = 0;
                    _vpos++;
                    System.Console.Out.Write("\n");
                }
            }
            else
            {
                System.Console.Out.Write(s);
                // fix the carriage return not setting hpos
                if (s.EndsWith("\n"))
                {
                    _hpos = 0;
                }
            }
        }

        public string In()
        {
            ConsoleKeyInfo key;
            string value = "";
            //do
            {
                while (System.Console.KeyAvailable == false)
                {
                    System.Threading.Thread.Sleep(250); // Loop until input is entered.
                }
                key = System.Console.ReadKey(false);
                // Issue here with deleting characters should allow for this
                if ( key.Key == ConsoleKey.Backspace)
                {
                    if (value.Length > 0)
                    {
                        value = value.Substring(0, value.Length - 1);
                        System.Console.Write(' ');
                        System.Console.CursorLeft--;

                    }
                    else
                    {
                        
                        System.Console.CursorLeft++;
                    }
                }
                else
                {
                    value += key.KeyChar;
                }
            }
            while (key.Key != ConsoleKey.Enter);
            return (value);
        }

        public void Error(string e)
        {
            System.Console.Error.Write(e);
        }

        public void Reset()
        {
            _input = "";
            _output = "";
        }

        #endregion
    }
}
