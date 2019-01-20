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

        int consoleWidth = 75;
        int zoneWidth = 15;
        int compactWidth = 3;
        int hpos = 0;
        int vpos = 0;
        string input = "";
        string output = "";
        protected readonly object lockObject = new Object();

        #endregion
        #region Properties

        public int Width
        {
            get
            {
                return (consoleWidth);
            }
        }

        public int Hpos
        {
            get
            {
                return (hpos);
            }
        }

        public int Vpos
        {
            get
            {
                return (vpos);
            }
        }

        public int Console
        {
            get
            {
                return (vpos);
            }
            set
            {
                consoleWidth = value;
            }
        }

        public int Zone
        {
            get
            {
                return (zoneWidth);
            }
            set
            {
                zoneWidth = value;
            }
        }

        public int Compact
        {
            get
            {
                return (compactWidth);
            }
            set
            {
                compactWidth = value;
            }
        }

        public string Input
        {
            set
            {
                // need to wait here while the input is being read
                lock (lockObject)
                {
                    input = input + value;
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
                    temp = output;
                    output = "";
                }
                return (temp);
            }
        }

        #endregion
        #region Methods

        public void Out(string s)
        {
            lock (lockObject)
            {
                output = output + s;
            }
        }

        public string In()
        {
            string value = "";

            do
            {
                while (input == "")
                {
                    System.Threading.Thread.Sleep(250); // Loop until input is entered.
                }
                int pos = 0;
                lock (lockObject)
                {
                    pos = input.IndexOf('\n');
                    if (pos < 0 )
                    {
                        pos = input.IndexOf('\r');
                    }
                    if (pos > 0)
                    {
                        // read the input to the first \n or \r then trim the remaining
                        value = input.Substring(0, pos + 1);
                        input = input.Substring(pos + 1, input.Length - pos - 1);
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
            input = "";
            output = "";
        }

        #endregion
    }
}