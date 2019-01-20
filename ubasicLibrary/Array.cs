//  Copyright (c) 2017, Jeremy Green All rights reserved.

using System;
using System.Collections.Generic;
using System.Text;

namespace uBasicLibrary
{
    public class Array
    {
        #region Variables

        private string dimVariable;
        private int _dimensions;
        private int[] _dimension;
        private object[] _values;

        #endregion
        #region Constructors

        public Array(string dim_variable, int dimensions, int[] dimension, object initial)
        {
            int size = 1;
            this.dimVariable = dim_variable;
            this._dimension = dimension;
            this._dimensions = dimensions;

            for (int d = 1; d <= _dimensions; d++)
            {
                if (_dimension[d] == 0)
                {
                    _dimension[d] = 10;
                }
                size = size * (_dimension[d] + 1);
            }
            _values = new object[size];
            for (int i = 0; i < size; ++i)
            {
                _values[i] = initial;
            }
        }

        #endregion
        #region Properties

        public int Dimension
        {
            get
            {
                return (_dimensions);
            }
            set
            {
                _dimensions = value;
            }
        }

        public int[] Dimensions
        {
            get
            {
                return (_dimension);
            }
            set
            {
                _dimension = value;
            }
        }

        #endregion
        #region Methods

        public object Get(int[] position)
        {
            // a,b,c (dims)
            // x,y,z
            // offset = x + y * ( a+1 ) + z * ( a+1 ) * ( b+1 ) 
            // offset = x * (0+1) + y * ( a+1 ) + z * ( a+1 ) * ( b+1 )
            // offset = ( ( ( ( z * ( b+1 ) ) + y * ( a+1 ) ) + x ) * 1

            int offset = 0;
            for (int d = _dimensions; d > 0; d--)
            {
                offset = (offset + position[d]) * (_dimension[d - 1] + 1);
            }
            return (_values[offset]);
        }

        public void Set(int[] position, object value)
        {
            int offset = 0;
            try
            {
                for (int d = _dimensions; d > 0; d--)
                {
                    offset = (offset + position[d]) * (_dimension[d - 1] + 1);
                }
                _values[offset] = value;
            }
            catch (Exception e)
            {
                throw new Exception("Array error " + e);
            }
        }

        #endregion
    }
}
