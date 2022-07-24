//  Copyright (c) 2017, Jeremy Green All rights reserved.

using System;
using System.Collections.Generic;
using System.Text;
using uBasicLibrary;

namespace uBasicConsole
{
    public class ConsoleIO : IuBasicIO
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
        #region Fields

        // Formatting constraints

        private int _consoleHeight = 80;
        private int _consoleWidth = 75;
		private int _consoleLeft = 0;
		private int _consoleTop = 0;
        private int _zoneWidth = 15;
        private int _compactWidth = 3;
        private Cursor _cursor;
        private string _input = "";
        private string _output = "";
        protected readonly object _lockObject = new Object();

        struct Cursor
        {
            int _left;
            int _top;

            public Cursor(int left, int top)
            {
                _left = left;
                _top = top;
            }

            public int Left
            {
                get
                {
                    return (_left);
                }
                set
                {
                    _left = value;
                }
            }
            public int Top
            {
                get
                {
                    return (_top);
                }
                set
                {
                    _top = value;
                }
            }
        }

        #endregion
        #region Constructors
        public ConsoleIO()
        {
            _consoleLeft = 0;
            _consoleTop = 0;
            _consoleWidth = 75;
            _consoleHeight = 80;
            Console.CursorVisible = false;
            //Console.WindowHeight = _consoleHeight;
            Console.WindowWidth = _consoleWidth;
            //Console.BufferHeight = Console.WindowHeight;
            //Console.BufferWidth = Console.WindowWidth;
        }
        #endregion
        #region Properties

        public int Width
        {
            get
            {
                return (_consoleWidth);
            }
			set
            {
                _consoleWidth = value;
            }
        }

        public int Height
        {
            get
            {
                return (_consoleHeight);
            }
			set
            {
                _consoleHeight = value;
            }
        }

        public int Left
        {
            get
            {
                return (_consoleLeft);
            }
            set
            {
                _consoleLeft = value;
            }
        }

        public int Top
        {
            get
            {
                return (_consoleTop);
            }
            set
            {
                _consoleTop = value;
            }
        }

        public string Input
        {
            set
            {
                // need to wait here while the input is being read
                lock (_lockObject)
                {
                    _input += value;
                }
            }
        }

        public string Output
        {
            get
            {
                string temp;
                // need to wait here while the output is being written
                lock (_lockObject)
                {
                    temp = _output;
                    _output = "";
                }
                return (temp);
            }
        }

        public int CursorLeft
        {
            get
            {
                return (_cursor.Left);
            }
            set
            {
                _cursor.Left = value;
            }
        }

        public int CursorTop
        {
            get
            {
                return (_cursor.Top);
            }
            set
            {
                _cursor.Top = value;
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
            lock (_lockObject)
            {
                string check = s.TrimEnd(' ');
                _cursor.Left += s.Length;
                if (_cursor.Left > _consoleWidth)
                {
                    if (check.Length > 0)
                    {
                        _cursor.Left = s.Length;
                        _cursor.Top++;
                        System.Console.Out.Write("\n");
                        System.Console.Out.Write(s);
                    }
                    else
                    {
                        _cursor.Left = 0;
                        _cursor.Top++;
                        System.Console.Out.Write("\n");
                    }
                }
                else
                {
                    System.Console.Out.Write(s);
                    // fix the carriage return not setting hpos
                    if (s.EndsWith("\n"))
                    {
                        _cursor.Left = 0;
                    }
                }
            }
        }

        public string In()
        {
            ConsoleKeyInfo key;
            string value = "";
            do
            {
                while (System.Console.KeyAvailable == false)
                {
                    System.Threading.Thread.Sleep(250); // Loop until input is entered.
                }
                key = System.Console.ReadKey(false);
                // Issue here with deleting characters should allow for this
                lock (_lockObject)
                {
                    if (key.Key == ConsoleKey.Backspace)
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
