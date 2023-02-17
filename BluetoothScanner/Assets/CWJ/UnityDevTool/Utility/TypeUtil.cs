using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using UnityObject = UnityEngine.Object;
namespace CWJ
{
    public static class TypeUtil
    {
        public static Type GetValidParamType(Type t)
        {
            if (t.BaseType == null && t.Name.EndsWith("&"))
            {
                string validName = t.Name.Remove(t.Name.Length - 1, 1);
                return Type.GetType((!string.IsNullOrEmpty(t.Namespace) ? $"{t.Namespace}.{validName}, {t.Namespace.Split('.')[0]}" : validName));
            }
            return t;
        }

        public static bool IsNullOrMissing<T>(this T obj) where T : UnityObject
        {
            return obj == null
#if UNITY_EDITOR
                || obj.Equals(null)
#endif
                ;
        }

        public static bool IsNullOrMissing(this object obj)
        {
            return obj == null
#if UNITY_EDITOR
                || obj.Equals(null)
#endif
                ;
        }

        /// <summary>
        /// struct를 찾는 방법. IsValueType = <see langword="true"/>, IsEnum = <see langword="false"/> 이면 Struct다.
        /// Primitive Type 또한 predefined Struct로 생각할 수 있음. 이 경우에는 위에서 다 걸러졌으므로 그냥 쓰면 됨.
        /// https://stackoverflow.com/questions/1827425/how-to-check-programmatically-if-a-type-is-a-struct-or-a-class
        /// Class도 Struct랑 다를 게 없으니 한번에.
        /// Custom Class만 거르는 방법 https://stackoverflow.com/a/5932705
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsClassOrStructOrTuple(this Type type) => !type.IsPrimitive && !type.IsEnum && (type.IsValueType || (!type.IsArrayOrList() && type.IsClass && type != typeof(UnityEngine.Coroutine)));

        public static bool IsGenericList(this Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
        public static bool IsHashSet(this Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(HashSet<>);

        public static bool IsArrayOrList(this Type type) => type.IsArray || type.IsGenericList();

        /// <summary>
        /// namespaceStr까지 적어주는게 오류적고 확실
        /// </summary>
        /// <param name="typeFullName"></param>
        /// <param name="namespaceStr"></param>
        /// <returns></returns>
        public static Type ConvertToType(this string typeFullName, string namespaceStr = null)
        {
            var type = Type.GetType(typeFullName);
            if (type != null) return type;
            if (!typeFullName.Contains('.'))
            {
                return null;
            }
            var assemblyName = namespaceStr ?? typeFullName.Substring(0, typeFullName.LastIndexOf('.')); //UnityEngine.UI.Image를 예로들면 끝에 Image를 빼고 가져옴
            
            System.Reflection.Assembly assembly;
            try
            {
                assembly = System.Reflection.Assembly.Load(assemblyName);
            }
            catch
            {
                assembly = null;
            }

            if (namespaceStr != null && !typeFullName.Contains(namespaceStr + "."))
            {
                typeFullName = namespaceStr + "." + typeFullName;
            }

            return assembly?.GetType(typeFullName);
        }

        public static Type ConvertToTypeHard(this string typeFullName)
        {
            Type type = Type.GetType(typeFullName);
            if (type != null) return type;

            var currentAssembly = System.Reflection.Assembly.GetExecutingAssembly();
            var referencedAssemblies = currentAssembly.GetReferencedAssemblies();
            foreach (var assemblyName in referencedAssemblies)
            {
                var assembly = System.Reflection.Assembly.Load(assemblyName);
                type = assembly?.GetType(typeFullName);
                if (type != null) return type;
            }

            return null;
        }

        public static T[] ConvertObjects<T>(this UnityObject[] rawObjects) where T : UnityObject
        {
            if (rawObjects == null) return null;
            
            if (rawObjects.Length == 0) return new T[0];
            T[] typedObjects = new T[rawObjects.Length];
            for (int i = 0; i < rawObjects.Length; i++)
                typedObjects[i] = (T)rawObjects[i];
            return typedObjects;
        }

        public static IEnumerable<Type> GetAllTypes(System.Func<System.Type, bool> predicate)
        {
            foreach (var assemb in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var tp in assemb.GetTypes())
                {
                    if (predicate == null || predicate(tp)) yield return tp;
                }
            }
        }

        public static IEnumerable<Type> GetTypesAssignableFrom(System.Type rootType)
        {
            foreach (var assemb in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var tp in assemb.GetTypes())
                {
                    if (rootType.IsAssignableFrom(tp)) yield return tp;
                }
            }
        }

        public static IEnumerable<Type> GetTypesAssignableFrom(System.Reflection.Assembly assemb, System.Type rootType)
        {
            foreach (var tp in assemb.GetTypes())
            {
                if (rootType.IsAssignableFrom(tp) && rootType != tp) yield return tp;
            }
        }

        public static bool IsType(this System.Type type, System.Type assignableType)
        {
            return assignableType.IsAssignableFrom(type);
        }

        public static bool IsType(this System.Type type, params System.Type[] assignableTypes)
        {
            foreach (var t in assignableTypes)
            {
                if (t.IsAssignableFrom(type)) return true;
            }

            return false;
        }

        public static object GetDefaultValue(this System.Type type)
        {
            if (type == null) throw new System.ArgumentNullException(nameof(type));
            return type.IsValueType ? System.Activator.CreateInstance(type) : null;
        }

        public static System.Type ParseType(string assembName, string typeName)
        {
            var assemb = (from a in System.AppDomain.CurrentDomain.GetAssemblies()
                          where a.GetName().Name == assembName || a.FullName == assembName
                          select a).FirstOrDefault();
            if (assemb != null)
            {
                return (from t in assemb.GetTypes()
                        where t.FullName == typeName
                        select t).FirstOrDefault();
            }
            else
            {
                return null;
            }
        }

        public static System.Type FindType(string typeName, bool useFullName = false, bool ignoreCase = false)
        {
            if (string.IsNullOrEmpty(typeName)) return null;

            bool isArray = typeName.EndsWith("[]");
            if (isArray)
                typeName = typeName.Substring(0, typeName.Length - 2);

            StringComparison e = (ignoreCase) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            if (useFullName)
            {
                foreach (var assemb in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var t in assemb.GetTypes())
                    {
                        if (string.Equals(t.FullName, typeName, e))
                        {
                            if (isArray)
                                return t.MakeArrayType();
                            else
                                return t;
                        }
                    }
                }
            }
            else
            {
                foreach (var assemb in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var t in assemb.GetTypes())
                    {
                        if (string.Equals(t.Name, typeName, e) || string.Equals(t.FullName, typeName, e))
                        {
                            if (isArray)
                                return t.MakeArrayType();
                            else
                                return t;
                        }
                    }
                }
            }
            return null;
        }

        public static System.Type FindType(string typeName, System.Type baseType, bool useFullName = false, bool ignoreCase = false)
        {
            if (string.IsNullOrEmpty(typeName)) return null;
            if (baseType == null) throw new System.ArgumentNullException(nameof(baseType));

            bool isArray = typeName.EndsWith("[]");
            if (isArray)
                typeName = typeName.Substring(0, typeName.Length - 2);

            StringComparison e = (ignoreCase) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            if (useFullName)
            {
                foreach (var assemb in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var t in assemb.GetTypes())
                    {
                        if (baseType.IsAssignableFrom(t) && string.Equals(t.FullName, typeName, e))
                        {
                            if (isArray)
                                return t.MakeArrayType();
                            else
                                return t;
                        }
                    }
                }
            }
            else
            {
                foreach (var assemb in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var t in assemb.GetTypes())
                    {
                        if (baseType.IsAssignableFrom(t) && (string.Equals(t.Name, typeName, e) || string.Equals(t.FullName, typeName, e)))
                        {
                            if (isArray)
                                return t.MakeArrayType();
                            else
                                return t;
                        }
                    }
                }
            }

            return null;
        }

        public static bool IsListType(this Type type)
        {
            if (type == null) return false;

            if (type.IsArray) return type.GetArrayRank() == 1;

            var interfaces = type.GetInterfaces();
            //if (interfaces.Contains(typeof(System.Collections.IList)) || interfaces.Contains(typeof(IList<>)))
            if (Array.IndexOf(interfaces, typeof(System.Collections.IList)) >= 0 || Array.IndexOf(interfaces, typeof(IList<>)) >= 0)
            {
                return true;
            }

            return false;
        }

        public static bool IsListType(this System.Type type, bool ignoreAsInterface)
        {
            if (type == null) return false;

            if (type.IsArray) return type.GetArrayRank() == 1;

            if (ignoreAsInterface)
            {
                //if (tp == typeof(System.Collections.ArrayList) || (tp.IsGenericType && tp.GetGenericTypeDefinition() == typeof(List<>))) return true;
                if (type.IsGenericList()) return true;
            }
            else
            {
                var interfaces = type.GetInterfaces();
                //if (interfaces.Contains(typeof(System.Collections.IList)) || interfaces.Contains(typeof(IList<>)))
                if (Array.IndexOf(interfaces, typeof(System.Collections.IList)) >= 0 || Array.IndexOf(interfaces, typeof(IList<>)) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsListType(this System.Type type, out System.Type innerType)
        {
            innerType = null;
            if (type == null) return false;

            if (type.IsArray)
            {
                if (type.GetArrayRank() == 1)
                {
                    innerType = type.GetElementType();
                    return true;
                }
                else
                    return false;
            }

            var interfaces = type.GetInterfaces();
            if (Array.IndexOf(interfaces, typeof(System.Collections.IList)) >= 0 || Array.IndexOf(interfaces, typeof(IList<>)) >= 0)
            {
                if (type.IsGenericType)
                {
                    innerType = type.GetGenericArguments()[0];
                }
                else
                {
                    innerType = typeof(object);
                }
                return true;
            }

            return false;
        }

        public static bool IsListType(this System.Type type, bool ignoreAsInterface, out System.Type innerType)
        {
            innerType = null;
            if (type == null) return false;

            if (type.IsArray)
            {
                if (type.GetArrayRank() == 1)
                {
                    innerType = type.GetElementType();
                    return true;
                }
                else
                    return false;
            }

            if (ignoreAsInterface)
            {
                if (type.IsGenericList())
                {
                    innerType = type.GetGenericArguments()[0];
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                var interfaces = type.GetInterfaces();
                if (Array.IndexOf(interfaces, typeof(System.Collections.IList)) >= 0 || Array.IndexOf(interfaces, typeof(IList<>)) >= 0)
                {
                    if (type.IsGenericType)
                    {
                        innerType = type.GetGenericArguments()[0];
                    }
                    else
                    {
                        innerType = typeof(object);
                    }
                    return true;
                }
            }

            return false;
        }

        public static System.Type GetElementTypeOfListType(this System.Type type)
        {
            if (type == null) return null;

            if (type.IsArray) return type.GetElementType();

            var interfaces = type.GetInterfaces();
            //if (interfaces.Contains(typeof(System.Collections.IList)) || interfaces.Contains(typeof(IList<>)))
            if (Array.IndexOf(interfaces, typeof(System.Collections.IList)) >= 0 || Array.IndexOf(interfaces, typeof(IList<>)) >= 0)
            {
                if (type.IsGenericType) return type.GetGenericArguments()[0];
                else return typeof(object);
            }

            return null;
        }

        public static Vector2 ConvertToSize(this Rect rect)
        {
            return new Vector2(rect.width, rect.height);
        }

        public static Vector2Int ConvertToIntSize(this Rect rect)
        {
            return new Vector2Int((int)rect.width, (int)rect.height);
        }
    }
}