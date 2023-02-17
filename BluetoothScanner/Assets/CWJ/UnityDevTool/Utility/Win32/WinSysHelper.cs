
using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

namespace CWJ
{
    using static WinSysUtil;

    public static class WinSysHelper
    {
        public static readonly string MyExePath;
        public static readonly string MyExeFolderPath;
        static readonly string ShortCutLkPath;

        static WinSysHelper()
        {
            MyExePath = MyProcess.MainModule.FileName;
            CWJ.DebugLogUtil.WriteLogForcibly($"Start CWJ.{nameof(WinSysHelper)}\nprocess exe:" + MyExePath);
            MyExeFolderPath = Path.GetDirectoryName(MyExePath);
            //소프트웨어가 시작될 때 기본 창 핸들 가져오기
            ShortCutLkPath = $"{Application.persistentDataPath}\\{Application.productName}.lnk";
        }


        private static System.Diagnostics.Process _MyProcess = null;
        public static System.Diagnostics.Process MyProcess
        {
            get
            {
                if (_MyProcess == null)
                {
                    var processes = System.Diagnostics.Process.GetProcessesByName(GetMyAppName());
                    _MyProcess = (processes.Length == 0) ? null : processes[0];
                }
                return _MyProcess;
            }
        }

        /// <summary>
        /// processID와 같은 모든 윈도우 핸들 리스트 찾음
        /// </summary>
        /// <param name="targetProcessID">프로세스 ID</param>
        /// <returns>윈도우 핸들 리스트</returns>
        public static IntPtr[] FindWindowHandles(uint targetProcessID)
        {
            List<IntPtr> windowHandleList = new List<IntPtr>();

            IntPtr windowHandle = IntPtr.Zero;

            do
            {
                windowHandle = FindWindowEx(IntPtr.Zero, windowHandle, null, null);

                GetWindowThreadProcessId(windowHandle, out uint processId);

                if (processId == targetProcessID)
                {
                    windowHandleList.Add(windowHandle);
                }
            }
            while (windowHandle != IntPtr.Zero);

            return windowHandleList.ToArray();
        }

        public static IntPtr FindInnerWindowHandle(string className, string windowTitleName)
        {
            IntPtr hw1 = FindWindow(className, windowTitleName);
            IntPtr hw2 = FindWindowEx(hw1, IntPtr.Zero, null, "");
            return hw2;
        }

        static bool isInit_MyWindowHandleMain = false;
        static IntPtr _MyWindowHandleMain;

        public static IntPtr MyWindowHandleMain
        {
            get
            {
                if (!isInit_MyWindowHandleMain)
                {
                    if (MyProcess == null)
                    {
                        UnityEngine.Debug.LogError("MyProcess : NULL");
                        throw new NullReferenceException("Process is Null");
                    }

                    //_MyWindowHandle = MyProcess.MainWindowHandle;
                    _MyWindowHandleMain = FindWindow(null, SystemUtil.IsServerBuild ? MyProcess.MainModule.FileName : GetMyAppName());
                    //server build 모드에서는 console창을 window handle로 가져오기 위해 file실행경로를 윈도우타이틀 이름으로 넣어줌
                    isInit_MyWindowHandleMain = true;
                }
                return _MyWindowHandleMain;
            }
        }

        static bool isInit_MyWindowHandles = false;

        static IntPtr[] _MyWindowHandles;

        public static IntPtr[] MyWindowHandles
        {
            get
            {
                if (!isInit_MyWindowHandles)
                {
                    if (MyProcess == null)
                    {
                        UnityEngine.Debug.LogError("MyProcess : NULL");
                        throw new NullReferenceException("Process is Null");
                    }

                    //_MyWindowHandle = MyProcess.MainWindowHandle;
                    _MyWindowHandles = FindWindowHandles((uint)MyProcess.Id);

                    isInit_MyWindowHandles = true;
                }
                return _MyWindowHandles;
            }
        }

        public static void HideWindow()
        {
            try
            {
                for (int i = 0; i < MyWindowHandles.Length; i++)
                {
                    WinSysUtil.HideWindow(MyWindowHandles[i]);
                }
                //WinSysUtil.HideWindow(MyWindowHandleMain);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }
        /// <summary>
        /// 작업표시줄 아이콘 표시
        /// </summary>
        public static void ShowWindow()
        {
            try
            {
                //for (int i = 0; i < MyWindowHandles.Length; i++)
                //{
                //    WinSysUtil.ShowWindow(MyWindowHandles[i]);
                //    SetForegroundWindow(MyWindowHandles[i]);
                //}
                WinSysUtil.ShowWindow(MyWindowHandleMain);
                SetForegroundWindow(MyWindowHandleMain);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }

        /// <summary>
        /// 시스템 시작 항목 추가
        /// </summary>
        /// <remarks>
        /// If disabled in taskmanager, entry gets added but key value will remain disabled.
        /// </remarks>
        /// <param name="enabled">true: set entry, false: delete entry.</param>
        public static void SetStartup(bool enabled)
        {
            //create shortcut first, overwrite if exist with new path.
            CreateShortcut();
            var rk = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (enabled)
            {
                rk.SetValue(UnityEngine.Application.productName, ShortCutLkPath);
            }
            else
            {
                rk.DeleteValue(UnityEngine.Application.productName, false);
            }
            rk.Close();
        }

        /// <summary>
        /// Creates application shortcut to link to windows startup in registry.
        /// </summary>
        private static void CreateShortcut()
        {
            try
            {
                var shell = new IWshRuntimeLibrary.WshShell();
                var windowsApplicationShortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(ShortCutLkPath);
                windowsApplicationShortcut.Description = "shortcut of " + Application.productName;
                windowsApplicationShortcut.TargetPath = MyExePath;
                windowsApplicationShortcut.WorkingDirectory = MyExeFolderPath;
                windowsApplicationShortcut.Save();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError(ex.Message);
            }
        }

        /// <summary>
        /// 중복 실행 체크
        /// <para/><see langword="true"/>: overlapped
        /// </summary>
        /// <returns></returns>
        public static bool IsPreventProcessExecuted(string checkProcessName = null, bool isShowWhenOverlapped = true, bool hasDefaultErrorMsg = false)
        {
            string myProcessName = GetMyAppName();
            checkProcessName = checkProcessName ?? myProcessName;

            var processes = System.Diagnostics.Process.GetProcesses().FindAll(p => p != null && p.ProcessName.Equals(checkProcessName));

            bool isMyProcess = checkProcessName == myProcessName;
            if (processes.Length > (isMyProcess ? 1 : 0))
            {
                if (isShowWhenOverlapped)
                    SetWindowTop(isMyProcess ? MyWindowHandleMain : FindWindowHandles((uint)processes[0].Id)[0]);
                if (hasDefaultErrorMsg)
                {
                    string msg = isMyProcess ? "The program is already running!\n\nPlease check again your task manager."
                                             : $"The '{checkProcessName}' is running!\n\nPlease kill '{checkProcessName}' process and try again.";
                    Debug.LogError(msg);
                    //MessageBox.Show(msg, myProcessName, MessageBoxButtons.OK);
                }
                return true;
            }
            return false;
        }
    }

}