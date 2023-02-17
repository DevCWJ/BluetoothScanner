using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;
using CWJ.Serializable;
using UnityEngine;

namespace CWJ
{
    public static class EnumUtil
    {
        public static DictionaryVisualized<TE, TUE> AddCallbackInDictionaryByEnum<TE, TUE>(this DictionaryVisualized<TE, TUE> callbackDic
                                    , UnityEngine.Object target, string exampleMethod, TE enumStartValue = default(TE), char separatorChr = '_')
                        where TE : Enum
                        where TUE : UnityEngine.Events.UnityEvent, new()
        {
            if (target == null)
            {
                return null;
            }
            if (!exampleMethod.Contains(separatorChr))
            {
                return null;
            }
            Type t = target.GetType();

            string methodNameBase = exampleMethod.Split(separatorChr)[0] + separatorChr;
            var enumArray = GetEnumArray<TE>();

            //string testMethodName = methodNameBase + enumArray[enumStartIndex].ToString();
            //var methodInfo = t.GetMethod(testMethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static);

            if (callbackDic == null)
                callbackDic = new DictionaryVisualized<TE, TUE>();

            int enumStartIndex = enumStartValue.Equals(default(TE)) ? 0 : enumStartValue.ToInt();
            for (int i = enumStartIndex; i < enumArray.Length; i++) // ignore [0]NULL
            {
                TE enumElem = enumArray[i];
                string methodName = methodNameBase + enumElem.ToString();
                var ua = ReflectionUtil.ConvertToUnityAction(methodName, target);
                if (ua != null)
                {
                    if (!callbackDic.TryGetValue(enumElem, out var ue))
                    {
                        callbackDic.Add(enumElem, ue = new TUE());
                    }
                    ue.AddListener_New(ua);
                }
                else
                {
                    Debug.LogError($"{t.Name} 에 '{methodName}' 함수 없음", target);
                }
            }
            return callbackDic;
        }

        public static DictionaryVisualized<TE, TUE> GetCallbackDictionaryByEnum<TE, TUE, TP0>(this DictionaryVisualized<TE, TUE> callbackDic
                                    , UnityEngine.Object target, string exampleMethod, int enumStartIndex = 0, char separatorChr = '_')
                where TE : Enum
                where TUE : UnityEngine.Events.UnityEvent<TP0>, new()
        {
            if (target == null)
            {
                return null;
            }
            if (!exampleMethod.Contains(separatorChr))
            {
                return null;
            }
            Type t = target.GetType();

            string methodNameBase = exampleMethod.Split(separatorChr)[0] + separatorChr;
            var enumArray = GetEnumArray<TE>();

            //string testMethodName = methodNameBase + enumArray[enumStartIndex].ToString();
            //var methodInfo = t.GetMethod(testMethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static);

            if (callbackDic == null)
                callbackDic = new DictionaryVisualized<TE, TUE>();

            for (int i = enumStartIndex; i < enumArray.Length; i++) // ignore [0]NULL
            {
                TE enumElem = enumArray[i];
                string methodName = methodNameBase + enumElem.ToString();
                var ua = ReflectionUtil.ConvertToUnityAction<TP0>(methodName, target);
                if (ua != null)
                {
                    if (!callbackDic.TryGetValue(enumElem, out var ue))
                    {
                        callbackDic.Add(enumElem, ue = new TUE());
                    }
                    ue.AddListener_New(ua);
                }
                else
                {
                    Debug.LogError($"{t.Name} 에 '{methodName}' 함수 없음", target);
                }
            }
            return callbackDic;
        }

        public static bool CanConvertToEnum<TEnum>(this string enumName)
        {
            return !string.IsNullOrWhiteSpace(enumName) && Enum.IsDefined(typeof(TEnum), enumName);
        }

        public static bool TryToEnum<TEnum>(this string enumName, out TEnum @enum)
        {
            @enum = default(TEnum);
            if (!CanConvertToEnum<TEnum>(enumName))
            {
                return false;
            }

            @enum = (TEnum)Enum.Parse(typeof(TEnum), enumName, true);
            return true;
        }
        public static bool EnumValueIsDefined<T>(this object value)
        {
            return EnumValueIsDefined(value, typeof(T));
        }

        public static bool EnumValueIsDefined(object value, Type enumType)
        {
            if (enumType == null) throw new ArgumentNullException(nameof(enumType));
            if (!enumType.IsEnum) throw new ArgumentException("Must be enum type.", nameof(enumType));
            if (value == null) return false;

            try
            {
                if (value is string)
                    return Enum.IsDefined(enumType, value);
                else if (ConvertUtil.IsNumeric(value))
                {
                    value = ConvertUtil.ToPrim(value, Type.GetTypeCode(enumType));
                    return Enum.IsDefined(enumType, value);
                }
            }
            catch
            {
            }

            return false;
        }


        /// <summary>
        /// Enum을 int로 바꾸는 확장메소드
        /// generic형태의 Enum도 가능
        /// boxing없는, GC alloc 발생이 없는 형변환
        /// </summary>
        /// <typeparam name="TEnum">변형될 자료, 자기자신</typeparam>
        /// <returns></returns>
        public static int ToInt<TEnum>(this TEnum fromEnum) where TEnum : Enum => Cache<TEnum>.ConvertInt(fromEnum);

        public static byte ToByte<TEnum>(this TEnum fromEnum) where TEnum : Enum => Cache<TEnum>.ConvertByte(fromEnum);

        public static class Cache<TEnum> where TEnum : Enum
        {
            public static readonly Func<TEnum, int> ConvertInt = ConvertIntFunc();

            private static Func<TEnum, int> ConvertIntFunc()
            {
                ParameterExpression fromEnum = Expression.Parameter(typeof(TEnum), nameof(fromEnum));
                UnaryExpression convertChecked = Expression.ConvertChecked(fromEnum, typeof(int));
                return Expression.Lambda<Func<TEnum, int>>(convertChecked, fromEnum).Compile();
            }

            public static readonly Func<TEnum, byte> ConvertByte = ConvertByteFunc();

            private static Func<TEnum, byte> ConvertByteFunc()
            {
                ParameterExpression fromEnum = Expression.Parameter(typeof(TEnum), nameof(fromEnum));
                UnaryExpression convertChecked = Expression.ConvertChecked(fromEnum, typeof(byte));
                return Expression.Lambda<Func<TEnum, byte>>(convertChecked, fromEnum).Compile();
            }
        }

        /// <summary>
        /// int/string을 Enum으로 (GC alloc없이)
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="enumName"></param>
        /// <param name="isNoticeError"></param>
        /// <returns></returns>
        public static TEnum ToEnum<TEnum>(this string enumName, bool isNoticeError = true) where TEnum : Enum
        {
            if (isNoticeError && !Enum.IsDefined(typeof(TEnum), enumName))
            {
                return default(TEnum);
            }

            return (TEnum)Enum.Parse(typeof(TEnum), enumName, true);
        }
        public static TEnum ToEnum<TEnum>(this object @object) where TEnum : Enum
        {
            return (TEnum)Enum.ToObject(typeof(TEnum), @object);
        }
        //public static TEnum ToEnum<TEnum>(this object intObject) where TEnum : Enum
        //{
        //    return (TEnum)Enum.ToObject(typeof(TEnum), (int)intObject);
        //}
        public static TEnum ToEnum<TEnum>(this int enumIndex) where TEnum : Enum
        {
            return (TEnum)Enum.ToObject(typeof(TEnum), enumIndex);
        }

        public static string[] GetNames<TEnum>() where TEnum : Enum
        {
            return Enum.GetNames(typeof(TEnum));
        }
        /// <summary>
        /// Enum 갯수 가져오기
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <returns></returns>
        public static int GetLength<TEnum>() where TEnum : Enum
        {
            return GetNames<TEnum>().Length;
        }

        public static TEnum GetValidEnum<TEnum>(int index) where TEnum : Enum
        {
            int length = GetLength<TEnum>();
            return index >= 0 ? (index % length).ToEnum<TEnum>() : (length - (Math.Abs(index) % length)).ToEnum<TEnum>();
        }

        public static TEnum NextEnum<TEnum>(this TEnum curEnum, int nextInterval = 1) where TEnum : Enum
        {
            return GetValidEnum<TEnum>(curEnum.ToInt() + nextInterval);
        }

        public static TEnum PreviousEnum<TEnum>(this TEnum curEnum, int prevInterval = 1) where TEnum : Enum
        {
            return GetValidEnum<TEnum>(curEnum.ToInt() - prevInterval);
        }

        public static TEnum[] GetEnumArray<TEnum>() where TEnum : Enum
        {
            return (TEnum[])Enum.GetValues(typeof(TEnum));
        }

        public static IEnumerable<TEnum> GetHasFlags<TEnum>(TEnum input) where TEnum : Enum
        {
            return Enum.GetValues(typeof(TEnum)).Cast<TEnum>()
               .Where(f => input.HasFlag(f));
        }

        /// <summary>
        /// Enum을 다른 Enum으로
        /// </summary>
        //Enum 확정이면 그냥 (Enum)otherEnum 하기
        //public static TEnum ToOtherEnum<TEnum>(this Enum enumSource)
        //{
        //    return (TEnum)Enum.Parse(typeof(TEnum), enumSource.ToString(), true);
        //}
        public static bool IsObsolete<TEnum>(TEnum value) where TEnum : Enum
        {
            var attributes = (ObsoleteAttribute[])value.GetType().GetField(value.ToString()).GetCustomAttributes(typeof(ObsoleteAttribute), false);
            return (attributes != null && attributes.Length > 0);
        }

        public static object ToEnumsNumericType(Enum e)
        {
            if (e == null) return null;

            switch (e.GetTypeCode())
            {
                case TypeCode.SByte:
                    return Convert.ToSByte(e);

                case TypeCode.Byte:
                    return Convert.ToByte(e);

                case TypeCode.Int16:
                    return Convert.ToInt16(e);

                case TypeCode.UInt16:
                    return Convert.ToUInt16(e);

                case TypeCode.Int32:
                    return Convert.ToInt32(e);

                case TypeCode.UInt32:
                    return Convert.ToUInt32(e);

                case TypeCode.Int64:
                    return Convert.ToInt64(e);

                case TypeCode.UInt64:
                    return Convert.ToUInt64(e);

                default:
                    return null;
            }
        }

        private static object ToEnumsNumericType(ulong v, TypeCode code)
        {
            switch (code)
            {
                case TypeCode.Byte:
                    return (byte)v;

                case TypeCode.SByte:
                    return (sbyte)v;

                case TypeCode.Int16:
                    return (short)v;

                case TypeCode.UInt16:
                    return (ushort)v;

                case TypeCode.Int32:
                    return (int)v;

                case TypeCode.UInt32:
                    return (uint)v;

                case TypeCode.Int64:
                    return (long)v;

                case TypeCode.UInt64:
                    return v;

                default:
                    return null;
            }
        }



        public static T AddFlag<T>(this T e, T value) where T : struct, IConvertible
        {
            return (T)Enum.ToObject(typeof(T), Convert.ToInt64(e) | Convert.ToInt64(value));
        }

        public static T RemoveFlag<T>(this T e, T value) where T : struct, IConvertible
        {
            var x = Convert.ToInt64(e);
            var y = Convert.ToInt64(value);
            return (T)Enum.ToObject(typeof(T), x & ~(x & y));
        }

        public static T SetFlag<T>(this T e, T flag, bool isAdd) where T : struct, IConvertible
        {
            return isAdd ? e.AddFlag(flag) : e.RemoveFlag(flag);
        }

        public static T ReversalFlag<T>(this T e, T value) where T : struct, IConvertible
        {
            return (T)Enum.ToObject(typeof(T), Convert.ToInt64(e) ^ Convert.ToInt64(value));
        }

        public static bool IsNull(this Enum e)
        {
            return Convert.ToInt64(e) == 0;
        }

        public static bool IsAll(this Enum e)
        {
            return Convert.ToInt64(e) == ~0;
        }

        public static bool HasFlagOrEquals(this Enum e, Enum value)
        {
            return e == value || e.HasFlag(value);
        }

        //public static bool HasFlag(this Enum e, Enum value)
        //{
        //    long v = Convert.ToInt64(value);
        //    return (Convert.ToInt64(e) & v) == v;
        //}

        public static bool HasFlag(this Enum e, ulong value)
        {
            return (Convert.ToUInt64(e) & value) == value;
        }

        public static bool HasFlag(this Enum e, long value)
        {
            return (Convert.ToInt64(e) & value) == value;
        }

        public static bool HasAnyFlag(this Enum e, Enum value)
        {
            return (Convert.ToInt64(e) & Convert.ToInt64(value)) != 0;
        }

        public static bool HasAnyFlag(this Enum e, ulong value)
        {
            return (Convert.ToUInt64(e) & value) != 0;
        }

        public static bool HasAnyFlag(this Enum e, long value)
        {
            return (Convert.ToInt64(e) & value) != 0;
        }

        public static IEnumerable<Enum> EnumerateFlags(Enum e)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));

            var tp = e.GetType();
            ulong max = 0;
            foreach (var en in Enum.GetValues(tp))
            {
                ulong v = Convert.ToUInt64(en);
                if (v > max) max = v;
            }
            int loops = (int)Math.Log(max, 2) + 1;

            ulong ie = Convert.ToUInt64(e);
            for (int i = 0; i < loops; i++)
            {
                ulong j = (ulong)Math.Pow(2, i);
                if ((ie & j) != 0)
                {
                    var js = ToEnumsNumericType(j, e.GetTypeCode());
                    if (Enum.IsDefined(tp, js)) yield return (Enum)Enum.Parse(tp, js.ToString());
                }
            }
        }

        public static IEnumerable<T> EnumerateFlags<T>(T e) where T : struct, IConvertible
        {
            var tp = e.GetType();
            if (!tp.IsEnum) throw new ArgumentException("Type must be an enum.", "T");

            ulong max = 0;
            foreach (var en in Enum.GetValues(tp))
            {
                ulong v = Convert.ToUInt64(en);
                if (v > max) max = v;
            }
            int loops = (int)Math.Log(max, 2) + 1;

            ulong ie = Convert.ToUInt64(e);
            for (int i = 0; i < loops; i++)
            {
                ulong j = (ulong)Math.Pow(2, i);
                if ((ie & j) != 0)
                {
                    var js = ToEnumsNumericType(j, e.GetTypeCode());
                    if (Enum.IsDefined(tp, js))
                    {
                        yield return (T)js;
                    }
                }
            }
        }

        public static IEnumerable<Enum> GetUniqueEnumFlags(Type enumType)
        {
            if (enumType == null) throw new ArgumentNullException(nameof(enumType));
            if (!enumType.IsEnum) throw new ArgumentException("Type must be an enum.", nameof(enumType));

            foreach (Enum e in Enum.GetValues(enumType))
            {
                //var d = Convert.ToDecimal(e);
                //if (d > 0 && MathUtil.IsPowerOfTwo(Convert.ToUInt64(d))) yield return e as Enum;

                switch (e.GetTypeCode())
                {
                    case TypeCode.Byte:
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                        if (MathUtil.IsPowerOfTwo(Convert.ToUInt64(e))) yield return e;
                        break;

                    case TypeCode.SByte:
                        {
                            sbyte i = Convert.ToSByte(e);
                            if (i == sbyte.MinValue || (i > 0 && MathUtil.IsPowerOfTwo((ulong)i))) yield return e;
                        }
                        break;

                    case TypeCode.Int16:
                        {
                            short i = Convert.ToInt16(e);
                            if (i == short.MinValue || (i > 0 && MathUtil.IsPowerOfTwo((ulong)i))) yield return e;
                        }
                        break;

                    case TypeCode.Int32:
                        {
                            int i = Convert.ToInt32(e);
                            if (i == int.MinValue || (i > 0 && MathUtil.IsPowerOfTwo((ulong)i))) yield return e;
                        }
                        break;

                    case TypeCode.Int64:
                        {
                            long i = Convert.ToInt64(e);
                            if (i == long.MinValue || (i > 0 && MathUtil.IsPowerOfTwo((ulong)i))) yield return e;
                        }
                        break;
                }
            }
        }
    }
}