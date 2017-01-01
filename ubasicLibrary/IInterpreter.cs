//  Copyright (c) 2017, Jeremy Green All rights reserved.

using System;
using System.Collections.Generic;
using System.Text;

namespace ubasicLibrary
{
    public interface IInterpreter
    {
        void Init(int pos);
        void Run();
        bool Finished();
    }
}
