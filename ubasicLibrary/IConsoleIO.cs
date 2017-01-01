//  Copyright (c) 2017, Jeremy Green All rights reserved.

using System;
using System.Collections.Generic;
using System.Text;

namespace ubasicLibrary
{
    public interface IConsoleIO
    {
        int Hpos { get; }
        int Vpos { get; }
        int Console { get; set; }
        int Zone { get; set; }
        int Compact { get; set; }
        void Out(string theMsg);
        string In();
        void Error(string theErr);
    }
}
