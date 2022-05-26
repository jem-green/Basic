using System;
using uBasicLibrary;

namespace uBasicWeb
{
    public class TextAreaIO : IConsoleIO
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
        }

        public int CursorTop
        {
            get
            {
                return (_cursor.Top);
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
       

        #endregion
        #region Methods

        public void Out(string s)
        {
            lock (_lockObject)
            {
                _output += s;
            }
        }

        public string In()
        {
            string value = "";

            do
            {
                while (_input == "")
                {
                    System.Threading.Thread.Sleep(250); // Loop until input is entered.
                }

                lock (_lockObject)
                {
                    int pos = _input.IndexOf('\n');
                    if (pos < 0 )
                    {
                        pos = _input.IndexOf('\r');
                    }
                    if (pos > 0)
                    {
                        // read the input to the first \n or \r then trim the remaining
                        value = _input.Substring(0, pos + 1);
                        _input = _input.Substring(pos + 1, _input.Length - pos - 1);
                    }
                }
            }
            while (value == "");
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