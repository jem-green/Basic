//  Copyright (c) 2017, Jeremy Green All rights reserved.

using System;
using System.Collections.Generic;
using System.Text;

namespace BasicLibrary
{
    public interface IInterpreter
    {
        void Abort(string text);
        void Init(int pos);
        void Run();
        bool IsFinished();
        void Stop();
    }
}
