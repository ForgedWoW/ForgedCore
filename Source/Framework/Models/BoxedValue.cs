﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Constants;

namespace Framework.Models
{
    public class BoxedValue<T> : IEquatable<BoxedValue<T>>,
                                  IEquatable<T>,
                                  IEqualityComparer<BoxedValue<T>>,
                                  IEqualityComparer<T> where T : struct
    {
        public BoxedValue(T value)
        {
            Value = value;
        }

        public T Value { get; set; }

        public static implicit operator int(BoxedValue<T> boxedValue)
        {
            return Convert.ToInt32(boxedValue.Value);
        }

        public static implicit operator uint(BoxedValue<T> boxedValue)
        {
            return Convert.ToUInt32(boxedValue.Value);
        }

        public static implicit operator bool(BoxedValue<T> boxedValue)
        {
            return Convert.ToBoolean(boxedValue.Value);
        }

        public static implicit operator double(BoxedValue<T> boxedValue)
        {
            return Convert.ToDouble(boxedValue.Value);
        }

        public static implicit operator BoxedValue<T>(bool val)
        {
            return new BoxedValue<T>((T)(object)val);   
        }

        public static implicit operator BoxedValue<T>(int val)
        {
            return new BoxedValue<T>((T)(object)val);
        }

        public static bool operator ==(BoxedValue<T> x, BoxedValue<T> y)
        {
            return x.Equals(y);
        }

        public static bool operator !=(BoxedValue<T> x, BoxedValue<T> y)
        {
            return !x.Equals(y);
        }

        public bool Equals(BoxedValue<T> other)
        {
            return Value.Equals(other.Value);
        }

        public bool Equals(BoxedValue<T> x, BoxedValue<T> y)
        {
            return x.Value.Equals(y.Value);
        }

        public bool Equals(T other)
        {
            return Value.Equals(other);
        }

        public bool Equals(T x, T y)
        {
            return x.Equals(y);
        }

        public int GetHashCode([DisallowNull] BoxedValue<T> obj)
        {
            return obj.Value.GetHashCode();
        }

        public int GetHashCode([DisallowNull] T obj)
        {
            return obj.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as BoxedValue<T>);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}