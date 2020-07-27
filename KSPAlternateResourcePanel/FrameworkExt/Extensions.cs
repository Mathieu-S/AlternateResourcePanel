using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace KSPPluginFramework
{
    public static class EnumExtensions
    {
        public static string Description(this Enum e)
        {
            var desc = (DescriptionAttribute[]) e.GetType().GetMember(e.ToString())[0]
                .GetCustomAttributes(typeof(DescriptionAttribute), false);
            if (desc.Length > 0)
                return desc[0].Description;
            return e.ToString();
        }

        //public static List<KeyValuePair<TEnum, string>> ToEnumDescriptionsList<TEnum>(TEnum value) 
        //{
        //    return Enum
        //        .GetValues(typeof(TEnum))
        //        .Cast<TEnum>()
        //        .Select(x => new KeyValuePair<TEnum, string>(x, ((Enum)((object)x)).Description()))
        //        .ToList();
        //}
        //public static List<KeyValuePair<TEnum, string>> ToEnumDescriptionsList<TEnum>()
        //{
        //    return ToEnumDescriptionsList<TEnum>(default(TEnum));
        //}

        //limit it to accept enums only
        public static List<string> ToEnumDescriptions<TEnum>(TEnum value) where TEnum : struct, IConvertible
        {
            var temp = Enum
                .GetValues(typeof(TEnum))
                .Cast<TEnum>()
                .Select(x => new KeyValuePair<TEnum, string>(x, ((Enum) (object) x).Description()))
                .ToList();
            return temp.Select(x => x.Value).ToList();
        }

        public static List<string> ToEnumDescriptions<TEnum>() where TEnum : struct, IConvertible
        {
            return ToEnumDescriptions(default(TEnum)).ToList();
        }


        public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            if (val.CompareTo(max) > 0) return max;
            return val;
        }

        public static int ToInt32(this string s)
        {
            return Convert.ToInt32(s);
        }

        public static int NormalizeAngle360(this int val)
        {
            return (int) Convert.ToDouble(val).NormalizeAngle360();
        }

        public static double NormalizeAngle360(this double val)
        {
            val %= 360;
            if (val < 0)
                val += 360;
            return val;
        }
    }
}