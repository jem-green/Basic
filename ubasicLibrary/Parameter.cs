//  Copyright (c) 2017, Jeremy Green All rights reserved.

using System;
using System.Collections.Generic;
using System.Text;

namespace uBasicLibrary
{
    public class Parameter
    {
        #region Variables

        string value = "";
        SourceType source = SourceType.None;
        public enum SourceType
        {
            None = 0,
            Command = 1,
            Registry = 2,
            App = 3
        }

        #endregion
        #region Constructor
        public Parameter()
        {
        }
        public Parameter(string value)
        {
            this.value = value;
            source = SourceType.App;
        }
        public Parameter(string value, SourceType source)
        {
            this.value = value;
            this.source = source;
        }
        #endregion
        #region Parameters
        public string Value
        {
            set
            {
                this.value = value;
            }
            get
            {
                return (value);
            }
        }

        public SourceType Source
        {
            set
            {
                source = value;
            }
            get
            {
                return (source);
            }
        }
        #endregion
        #region Methods
        public override string ToString()
        {
            return (value);
        }
        #endregion
    }
}
