//  Copyright (c) 2017, Jeremy Green All rights reserved.

using System;
using System.Collections.Generic;
using System.Text;

namespace uBasicLibrary
{
    public interface IDefaultIO
    {
        #region Peroperties
        int Width { get; }
        int Height { get; }
        int Left { get; }
        int Top { get; }
        int CursorLeft { get; set; }
        int CursorTop { get; set; }
        int Zone { get; set; }
        int Compact { get; set; }
        string Input { set; }
        string Output { get; }

        #endregion
        #region Methods
        void Out(string theMsg);
        void Put(char character);
        string In();
        char Get();
        void Error(string theErr);
        void Reset();

        #endregion
        #region Events

        event EventHandler<TextEventArgs> TextReceived;

        #endregion
    }
}
