//  Copyright (c) 2017, Jeremy Green All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using log4net;

namespace uBasicLibrary
{
    public interface IEvaluator
    {
        void Randomize();
        void Expression();
        void Term();
        void Exponent();
        void Factor();
        void Relation();
    }
}
