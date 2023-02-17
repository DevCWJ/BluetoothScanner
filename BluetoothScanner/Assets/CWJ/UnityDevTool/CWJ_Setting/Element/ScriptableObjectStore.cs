#if UNITY_EDITOR
using UnityEngine;
using CWJ.Serializable;
using UnityEditor;
using CWJ.AccessibleEditor;
using System;

namespace CWJ
{
    [System.Serializable] public class StrScriptableObjDictionary : SerializedDictionary<string, CWJScriptableObject> { }

    public sealed class ScriptableObjectStore : CWJScriptableObject
    {
        private static ScriptableObjectStore _Instance = null;
        public static ScriptableObjectStore Instanced
        {
            get
            {
                if (_Instance == null)
                {
                    var obj = AssetDatabase.LoadAssetAtPath<ScriptableObjectStore>(MyPath);
                    if (obj == null)
                    {
                        if (!MyPath.IsFolderExists(false, isPrintLog: false))
                        {
                            CWJ_EditorEventHelper.OnUnityDevToolDelete();
                            typeof(ScriptableObjectStore).PrintLogWithClassName($"CWJ.UnityDevTool is Deleted.\nor {nameof(ScriptableObjectStore)}'s PATH is Wrong", LogType.Error);
                        }
                        throw new NullReferenceException(string.Empty);
                        //obj = CreateScriptableObj<ScriptableObjectStore>(MyPath);
                    }
                    _Instance = obj;
                }
                return _Instance;
            }
        }

        const string CachePathFormat = "Assets/CWJ/UnityDevTool/_Cache/{0}.asset";

        public readonly static string MyPath = string.Format(CachePathFormat, nameof(ScriptableObjectStore));

        private static T CreateScriptableObj<T>(string path) where T : CWJScriptableObject
        {
            T ins = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(ins, /*AssetDatabase.GenerateUniqueAssetPath(*/path/*)*/);
            ins.OnConstruct();
            ins.SaveScriptableObj();
            AssetDatabase.SaveAssets();
            //AssetDatabase.Refresh();
            return ins;
        }

        [SerializeField, SerializableDictionary(isReadonly: true)] StrScriptableObjDictionary scriptableObjDic = new StrScriptableObjDictionary();

        const string SearchFileTypeFormat = "t:{0}";
        readonly static string[] CWJFolderPath = new[] { "Assets/CWJ" };
        public T GetScriptableObj<T>() where T : CWJScriptableObject
        {
            string key = typeof(T).FullName;
            bool hasKey;
            if ((hasKey = scriptableObjDic.TryGetValue(key, out var value)) && !value.IsNullOrMissing())
            {
                try
                {
                    T returnVal = (T)value;
                    return returnVal;
                }
                catch (System.InvalidCastException e)
                {
                    Debug.LogError("CWJ폴더를 삭제후 다시 import해주세요\n" + e.ToString());
                }
            }
            string typeName = typeof(T).Name;
            string path = string.Format(CachePathFormat, typeName);

            T obj = DelegateUtil.ManyConditions(
                checkNotNull: (o) => o != null,
                () => AssetDatabase.LoadAssetAtPath<T>(path),
                () =>
                {
                    string[] paths = AssetDatabase.FindAssets(string.Format(SearchFileTypeFormat, typeName), CWJFolderPath).ConvertAll(AssetDatabase.GUIDToAssetPath);
                    return paths.Length > 0 ? AssetDatabase.LoadAssetAtPath<T>(paths[0]) : CreateScriptableObj<T>(path);
                }
            );

            if (!hasKey)
                scriptableObjDic.Add(key, obj);
            else
                scriptableObjDic[key] = obj;

            SaveScriptableObj();
            return obj;
        }


    }

}
#endif