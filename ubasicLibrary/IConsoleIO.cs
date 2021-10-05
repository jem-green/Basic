//  Copyright (c) 2017, Jeremy Green All rights reserved.

using System;
using System.Collections.Generic;
using System.Text;

namespace uBasicLibrary
{
    public interface IConsoleIO
    {
        int Width { get; }
        int Height { get; }
        int Left { get; }
        int Top { get; }
        int Zone { get; set; }
        int Compact { get; set; }
        string Input { set; }
        string Output { get; }
        void Out(string theMsg);
        string In();
        void Error(string theErr);
        void Reset();
        event EventHandler<TextEventArgs> TextReceived;

    }
}
