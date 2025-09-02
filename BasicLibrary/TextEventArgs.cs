using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace uBasicLibrary
{
    public class TextEventArgs : EventArgs
    {
        #region Fields
        private string text = "";
        #endregion
        #region Constructor
        public TextEventArgs(string text)
        {
            this.text = text;
        }
        #endregion
        #region Properties
        public string Text
        {
            set
            {
                text = value;
            }
            get
            {
                return (text);
            }
        }
        #endregion
    }
}

