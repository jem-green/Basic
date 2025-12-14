//  Copyright (c) 2017, Jeremy Green All rights reserved.

using System;
using System.Collections.Generic;
using System.Text;

namespace BasicLibrary
{
    public class Parameter<T> : Parameter, IEquatable<Parameter>
    {
        #region Fields

        #endregion
        #region Constructor
        public Parameter(string name, T value) : base(name, value)
        {
        }
        public Parameter(string name, T value, IParameter.SourceType source) : base(name, value, source)
        {
        }

        #endregion
        #region Parameters
        public new T Value
        {
            set
            {
                this._value = value;
            }
            get
            {
                return ((T)_value);
            }
        }

        #endregion
        #region Methods
        public override string ToString()
        {
            return (Convert.ToString(_value));
        }
        #endregion
    }
}
