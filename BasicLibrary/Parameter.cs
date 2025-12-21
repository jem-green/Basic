//  Copyright (c) 2017, Jeremy Green All rights reserved.

using System;
using System.Collections.Generic;
using System.Text;

namespace BasicLibrary
{
    /// <summary>
    /// Storage class for a parameter
    /// </summary>
    public class Parameter : IParameter, IEquatable<Parameter>
    {
        #region Fields

        internal string _name = String.Empty;
        internal object _value = null;
        internal IParameter.SourceType _source = IParameter.SourceType.None;

        #endregion
        #region Constructor
        //public Parameter(string name)
        //{
        //    _value = null;
        //    _name = name;
        //}

        public Parameter(string name, object value)
        {
            _value = value;
            _source = IParameter.SourceType.App;
            _name = name;
        }
        public Parameter(string name, object value, IParameter.SourceType source)
        {
            _value = value;
            _source = source;
            _name = name;
        }
        #endregion
        #region Parameters

        public string Name
        {
            set
            {
                _name = value;
            }
            get
            {
                return _name;
            }
        }

        public object Value
        {
            set
            {
                this._value = value;
            }
            get
            {
                return (_value);
            }
        }

        public IParameter.SourceType Source
        {
            set
            {
                _source = value;
            }
            get
            {
                return (_source);
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            Parameter objAsPart = obj as Parameter;
            if (objAsPart == null) return false;
            else return Equals(objAsPart);
        }

        public bool Equals(Parameter other)
        {
            return (other != null && other.Name == this.Name);
        }

        #endregion
        #region Methods
        public override string ToString()
        {
            return (Convert.ToString(_value));
        }

        public override int GetHashCode()
        {
            return (GetHashCode());
        }
        #endregion
    }
}
