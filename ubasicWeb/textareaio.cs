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
        #region Variables

        // Formatting constraints

        readonly int _consoleHeight = 80;
        int _consoleWidth = 75;
        int _zoneWidth = 15;
        int _compactWidth = 3;
        readonly int _hpos = 0;
        readonly int _vpos = 0;
        string _input = "";
        string _output = "";
        protected readonly object lockObject = new Object();

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
                // need to wait here while the input is being read
                lock (lockObject)
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
                lock (lockObject)
                {
                    temp = _output;
                    _output = "";
                }
                return (temp);
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
       

        #endregion
        #region Methods

        public void Out(string s)
        {
            lock (lockObject)
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

                lock (lockObject)
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