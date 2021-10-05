using System;
using uBasicLibrary;

namespace uBasicForm
{
    public class TextBoxIO : IConsoleIO
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
        private int _zoneWidth = 15;
        private int _compactWidth = 3;
        private Cursor cursor;
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

        public string Input
        {
            set
            {
                // need to wait here while the input is being read
                lock (_lockObject)
                {
                    _input = _input + value;
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

        public int Left
        {
            get
            {
                return (cursor.Left);
            }
        }

        public int Top
        {
            get
            {
                return (cursor.Top);
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
                s = s.Replace("\n", "\r\n");
                _output = _output + s;
            }
            TextEventArgs args = new TextEventArgs(s);
            OnTextReceived(args);         
        }

        public string In()
        {
            string value = "";
            do
            {
                while (_input.Length == 0)
                {
                    System.Threading.Thread.Sleep(250); // Loop until input is entered.
                }
                int pos = 0;
                lock (_lockObject)
                {
                    pos = _input.IndexOf('\n');
                    if (pos < 0)
                    {
                        pos = _input.IndexOf('\r');
                    }
                    if (pos > -1)
                    {
                        // read the input to the first \n or \r then trim the remaining
                        value = _input.Substring(0, pos);
                        _input = _input.Substring(pos + 1, _input.Length - pos - 1);
                    }
                }
            }
            while (value.Length == 0);
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
