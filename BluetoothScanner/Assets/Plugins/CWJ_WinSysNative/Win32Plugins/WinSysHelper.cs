
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;
using UnityEngine.Rendering;

namespace CWJ
{
    using static WinSysUtil;

    public static class WinSysHelper
    {
        public static readonly string MyExePath;
        public static readonly string MyExeFolderPath;
        public static readonly string ShortCutLkPath;

        /// <summary>
        /// check server build (headless) mode
        /// </summary>
        public static bool IsServerBuild => SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;

        public static bool isVerifiedProcess { get; private set; } = false;

        static bool isMinimizeWhenStart => true;

        static bool IsInitialized = false;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RuntimeInitializeOnLoadMethod()
        {
            if (MonoBehaviourEventHelper.IS_EDITOR)
            {
                return;
            }
            if (IsInitialized)
            {
                return;
            }
            IsInitialized = true;
            isVerifiedProcess = !IsMyProcessExcuted();
            if (!isVerifiedProcess)
            {
                UnityEngine.Application.Quit();
                return;
            }

            if (IsServerBuild)
            {
                SetQuickEditModeDisable();

                //SetStartup(true, ShortCutLkPath, MyExePath, MyExeFolderPath);

                if (isMinimizeWhenStart)
                {
                    WinSysHelper.MinimizeMyWindow();
                }
            }

            MonoBehaviourEventHelper.AwakeEvent += () =>
            {
                Console.Clear();
                CWJ.DebugLogUtil.WriteLogForcibly("Run: " + $"{Application.companyName}.{Application.productName}");
                CWJ.DebugLogUtil.WriteLogForcibly("Ver: " + Application.version);
                CWJ.DebugLogUtil.WriteLogForcibly("Exe: " + WinSysHelper.MyExePath);
            };
        }

        static WinSysHelper()
        {
            if (MonoBehaviourEventHelper.IS_EDITOR)
            {
                return;
            }
            MyExePath = MyProcess.MainModule.FileName;

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
        const string WindowHandle_Default_IME = "Default IME";
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

                    isInit_MyWindowHandleMain = true;

                    string appName = GetMyAppName();
                    int index= IsServerBuild ? 
                        MyWindowHandles.FindIndex(wh =>
                        {
                            string title = GetWindowTitleText(wh);
                            return !title.Equals(appName) && !title.Equals(WindowHandle_Default_IME); 
                            //한글일경우 타이틀이름을 못가져오는경우가 생겨서
                            //"Default IME" 가 아니고, AppName과 같지않은 한개가 콘솔일거라고 추측하여 가져옴. 문제시 다른 방법 찾아야함 귀찮음.
                        }) 
                        : MyWindowHandles.FindIndex(wh => GetWindowTitleText(wh).Equals(appName));

                    if (index < 0)
                    {
                        UnityEngine.Debug.LogError("WindowHandle : NotFound");
                        throw new EntryPointNotFoundException("Process NotFound");
                    }
                    else
                    {
                        _MyWindowHandleMain = MyWindowHandles[index];
                    }
                    //_MyWindowHandle = MyProcess.MainWindowHandle;
                    //_MyWindowHandleMain = FindWindow(null, SystemUtil.IsServerBuild ? MyProcess.MainModule.FileName : GetMyAppName());
                    //이렇게 하니 한글폴더가 있으니 문제가생김. 그냥 process ID로 검색하는걸로 변경
                    //server build 모드에서는 console창을 window handle로 가져오기 위해 file실행경로를 윈도우타이틀 이름으로 넣어줌
                    
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
        public static void MinimizeMyWindow()
        {
            if (MyWindowHandles.Length == 0)
            {
                return;
            }
            WinSysUtil.WindowMinimize(MyWindowHandleMain);
        }
        public static void HideMyWindow()
        {
            if (MyWindowHandles.Length == 0)
            {
                return;
            }

            for (int i = 0; i < MyWindowHandles.Length; i++)
            {
                WinSysUtil.WindowHide(MyWindowHandles[i]);
            }
            //WinSysUtil.HideWindow(MyWindowHandleMain);
        }

        /// <summary>
        /// 화면 보이기
        /// </summary>
        public static void ShowMyWindow()
        {
            if (MyWindowHandles.Length == 0)
            {
                return;
            }

            //for (int i = 0; i < MyWindowHandles.Length; i++)
            //{
            //    WinSysUtil.ShowWindow(MyWindowHandles[i]);
            //    SetForegroundWindow(MyWindowHandles[i]);
            //}
            WinSysUtil.WindowShow(MyWindowHandleMain);
            //SetForegroundWindow(MyWindowHandleMain);
        }


        static System.Threading.Mutex mutex = null;

        /// <summary>
        /// mutex를 통한 현 프로세스 중복실행방지
        /// </summary>
        /// <returns></returns>
        public static bool IsMyProcessExcuted()
        {
            if (mutex != null)
            {
                return true;
            }
            MonoBehaviourEventHelper.LastQuitEvent += () =>
            {
                Debug.LogError("LastQuit");
            };
            MonoBehaviourEventHelper.QuitEvent += () =>
            {
                if (mutex != null)
                    mutex.Dispose();
                Debug.LogError("quit");
            };
            mutex = new System.Threading.Mutex(true, $"{Application.companyName}_{Application.productName}", out bool isCreatedNew);
            return !isCreatedNew;
        }

        /// <summary>
        /// 중복 실행 체크
        /// <para/><see langword="true"/>: overlapped
        /// </summary>
        /// <returns></returns>
        public static bool IsPreventProcessExecuted(string checkProcessName, bool isShowWhenOverlapped = false, bool hasDefaultErrorMsg = false)
        {
            bool isMyProcess = checkProcessName == GetMyAppName();

            var processesById = System.Diagnostics.Process.GetProcesses().Where((p) =>
            {
                if (p == null) return false;
                string pName = null;
                try
                {
                    pName = p.ProcessName;
                }
                catch (Exception)
                {
                    pName = null;
                }
                return pName != null && pName.Equals(checkProcessName);
            }).Select(p => (uint)p.Id).ToArray();

            if (processesById.Length > (isMyProcess ? 1 : 0))
            {
                if (isShowWhenOverlapped)
                    SetWindowTop(isMyProcess ? MyWindowHandleMain : FindWindowHandles(processesById[0])[0]);
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