using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace UsdzSharpie
{
    public static class Extensions
    {
        public static int ToInt(this bool value)
        {
            return value ? 1 : 0;
        }

        public static T ToEnum<T>(this ulong value)
        {
            var intValue = (int)value;
            var enumType = typeof(T);
            if (!Enum.IsDefined(enumType, intValue))
            {
                throw new Exception("Undefined enum");
                //return default;
            }
            return (T)Enum.ToObject(enumType, intValue);
        }

        public static float UnpackFloat32(this ulong value)
        {
            return Unsafe.As<ulong, float>(ref value);
        }
    }
}
