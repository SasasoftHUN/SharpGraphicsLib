#if !CSLANG_8_0
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGraphics.Utils
{
    public readonly struct Index : IEquatable<Index>
    {
        private readonly int _value;

        //
        // Summary:
        //     Create an Index pointing at first element.
        public static Index Start => new Index(0);

        //
        // Summary:
        //     Create an Index pointing at beyond last element.
        public static Index End => new Index(-1);

        //
        // Summary:
        //     Returns the index value.
        public int Value
        {
            get
            {
                if (_value < 0)
                {
                    return ~_value;
                }

                return _value;
            }
        }

        //
        // Summary:
        //     Indicates whether the index is from the start or the end.
        public bool IsFromEnd => _value < 0;

        //
        // Summary:
        //     Construct an Index using a value and indicating if the index is from the start
        //     or from the end.
        //
        // Parameters:
        //   value:
        //     The index value. it has to be zero or positive number.
        //
        //   fromEnd:
        //     Indicating if the index is from the start or from the end.
        //
        // Remarks:
        //     If the Index constructed from the end, index value 1 means pointing at the last
        //     element and index value 0 means pointing at beyond last element.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Index(int value, bool fromEnd = false)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException("value", value, "Non-negative number required.");
            }

            if (fromEnd)
            {
                _value = ~value;
            }
            else
            {
                _value = value;
            }
        }

        private Index(int value)
        {
            _value = value;
        }

        //
        // Summary:
        //     Create an Index from the start at the position indicated by the value.
        //
        // Parameters:
        //   value:
        //     The index value from the start.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Index FromStart(int value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException("value", value, "Non-negative number required.");
            }

            return new Index(value);
        }

        //
        // Summary:
        //     Create an Index from the end at the position indicated by the value.
        //
        // Parameters:
        //   value:
        //     The index value from the end.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Index FromEnd(int value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException("value", value, "Non-negative number required.");
            }

            return new Index(~value);
        }

        //
        // Summary:
        //     Calculate the offset from the start using the giving collection length.
        //
        // Parameters:
        //   length:
        //     The length of the collection that the Index will be used with. length has to
        //     be a positive value
        //
        // Remarks:
        //     For performance reason, we don't validate the input length parameter and the
        //     returned offset value against negative values. we don't validate either the returned
        //     offset is greater than the input length. It is expected Index will be used with
        //     collections which always have non negative length/count. If the returned offset
        //     is negative and then used to index a collection will get out of range exception
        //     which will be same affect as the validation.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetOffset(int length)
        {
            int num = _value;
            if (IsFromEnd)
            {
                num += length + 1;
            }

            return num;
        }

        //
        // Summary:
        //     Indicates whether the current Index object is equal to another object of the
        //     same type.
        //
        // Parameters:
        //   value:
        //     An object to compare with this object
        public override bool Equals(object? value)
        {
            if (value is Index)
            {
                return _value == ((Index)value)._value;
            }

            return false;
        }

        //
        // Summary:
        //     Indicates whether the current Index object is equal to another Index object.
        //
        // Parameters:
        //   other:
        //     An object to compare with this object
        public bool Equals(Index other)
        {
            return _value == other._value;
        }

        //
        // Summary:
        //     Returns the hash code for this instance.
        public override int GetHashCode()
        {
            return _value;
        }

        //
        // Summary:
        //     Converts integer number to an Index.
        public static implicit operator Index(int value)
        {
            return FromStart(value);
        }

        //
        // Summary:
        //     Converts the value of the current Index object to its equivalent string representation.
        public override string ToString()
        {
            if (IsFromEnd)
            {
                return "^" + (uint)Value;
            }

            return ((uint)Value).ToString();
        }
    }
}
#endif