//  Copyright (c) 2017, Jeremy Green All rights reserved.

using System;
using System.Collections.Generic;
using System.Text;

namespace uBasicLibrary
{
    public interface IConsoleIO
    {
        #region Peroperties
		
        int Width { get; }
        int Height { get; }
        int Left { get; }
        int Top { get; }
        int CursorLeft { get; }
        int CursorTop { get; }
        int Zone { get; set; }
        int Compact { get; set; }
        string Input { set; }
        string Output { get; }

        #endregion
        #region Methods
		
        void Out(string theMsg);
        string In();
        void Error(string theErr);
        void Reset();
        event EventHandler<TextEventArgs> TextReceived;

        #endregion
    }
}
