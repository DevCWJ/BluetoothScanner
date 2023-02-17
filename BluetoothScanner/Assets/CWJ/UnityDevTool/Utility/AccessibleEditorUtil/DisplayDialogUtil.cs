using System;
using System.Diagnostics;

#if UNITY_EDITOR

using UnityEditor;

#endif

namespace CWJ.AccessibleEditor
{
    public static class DisplayDialogUtil
    {
        private static bool _DisplayDialog(Type classType, string message, string ok, string cancel, bool isPrintLog, bool isError, UnityEngine.Object logObj = null, bool isPreventOverlapMsg = false)
        {
#if UNITY_EDITOR
            string title = classType.GetCWJClassNameMark();
            if (string.IsNullOrEmpty(ok) && string.IsNullOrEmpty(cancel))
            {
                ok = "Ok";
            }

            bool isSelectedOk = EditorUtility.DisplayDialog(title, (isError ? "[ERROR]\n" : "") + message.ExcludeRichTextFormat(), ok, cancel);

            message += $"\n[  {ok + (isSelectedOk ? " √" : "  ")}]{(string.IsNullOrEmpty(cancel) ? "" : $",  [  {cancel + (!isSelectedOk ? " √" : "  ")}]")}";

            if (isPrintLog)
            {
                DebugLogUtil.PrintLogWithClassName(classType, message, logType: isError ? UnityEngine.LogType.Error : UnityEngine.LogType.Log, isComment: false, isBigFont: false, obj: logObj, isPreventOverlapMsg: isPreventOverlapMsg);
            }

            return isSelectedOk;
#else
            DebugLogUtil.PrintLogWithClassName(classType, message, logType: isError ? UnityEngine.LogType.Error : UnityEngine.LogType.Log, isComment: false, isBigFont: false);
            return true;
#endif
        }

        public static bool DisplayDialogReflection(string message, string ok = null, string cancel = null, bool isPrintLog = true, bool isError = false, UnityEngine.Object logObj = null, bool isPreventOverlapMsg = false)
        {
            return _DisplayDialog(classType: new StackTrace().GetFrame(1).GetMethod().ReflectedType, message: message, ok: ok, cancel: cancel, isPrintLog: isPrintLog, isError: isError, logObj: logObj, isPreventOverlapMsg: isPreventOverlapMsg);
        }

        public static bool DisplayDialog<T>(string message, string ok = null, string cancel = null, bool isPrintLog = true, bool isError = false, UnityEngine.Object logObj = null, bool isPreventOverlapMsg = false)
        {
            return _DisplayDialog(classType: typeof(T), message, ok: ok, cancel: cancel, isPrintLog: isPrintLog, isError: isError, logObj: logObj, isPreventOverlapMsg: isPreventOverlapMsg);
        }

        public static bool DisplayDialog(this Type classType, string message, string ok = null, string cancel = null, bool isPrintLog = true, bool isError = false, UnityEngine.Object logObj = null, bool isPreventOverlapMsg = false)
        {
            return _DisplayDialog(classType: classType, message, ok: ok, cancel: cancel, isPrintLog: isPrintLog, isError: isError, logObj: logObj, isPreventOverlapMsg: isPreventOverlapMsg);
        }
    }
}