using System.Runtime.InteropServices;
using System;
using System.Text;
using System.IO;
using UnityEngine;

namespace CWJ
{
    public static class WinSysUtil
    {

        public static string GetMyAppName()
        {
            return
#if UNITY_STANDALONE || UNITY_EDITOR || UNITY_STANDALONE_WIN
                Application.productName
#else
            Assembly.GetEntryAssembly().GetName().Name
#endif
            ;
        }


        public static Version GetAssemblyVersion()
        {
            //if (System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed)
            //{
            //    return System.Deployment.Application.ApplicationDeployment.CurrentDeployment.CurrentVersion;
            //}
            //else
            //{
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            //}
        }


        /// <summary>
        /// 시스템 시작 항목 추가
        /// </summary>
        /// <remarks>
        /// If disabled in taskmanager, entry gets added but key value will remain disabled.
        /// </remarks>
        /// <param name="enabled">true: set entry, false: delete entry.</param>
        public static void SetStartup(bool enabled, string shortCutLkPath = null, string exePath = null, string workingDir = null)
        {
            if (shortCutLkPath == null) shortCutLkPath = WinSysHelper.ShortCutLkPath;
            //create shortcut first, overwrite if exist with new path.
            CreateShortcut(shortCutLkPath, exePath ?? WinSysHelper.MyExePath, workingDir ?? WinSysHelper.MyExeFolderPath);
            var rk = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (enabled)
            {
                rk.SetValue(UnityEngine.Application.productName, shortCutLkPath);
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
        private static void CreateShortcut(string shortCutLkPath, string exePath, string workingDir = null)
        {
            try
            {
                var shell = new IWshRuntimeLibrary.WshShell();
                var windowsApplicationShortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(shortCutLkPath);
                windowsApplicationShortcut.Description = "shortcut of " + Application.productName;
                windowsApplicationShortcut.TargetPath = exePath;
                windowsApplicationShortcut.WorkingDirectory = workingDir ?? Path.GetDirectoryName(exePath);
                windowsApplicationShortcut.Save();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError(ex.Message);
            }
        }

        public enum OverlapHandling
        {
            Nothing,
            PreventNewProcess,
            KillOverlapped,
        }

        public static void OpenNotepad(string path, OverlapHandling overlapHandling = OverlapHandling.Nothing)
        {
            RunProcess("notepad.exe", path, overlapHandling);
        }


        public static void RunProcess(string processName, string filePath, OverlapHandling overlapHandling = OverlapHandling.Nothing)
        {
            if (overlapHandling != OverlapHandling.Nothing)
            {
                string fileName = System.IO.Path.GetFileName(filePath) + " ";
                foreach (var p in System.Diagnostics.Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(processName).ToUpper()))
                {
                    if (p.MainWindowTitle.StartsWith(fileName))
                    { //중복
                        if (overlapHandling == OverlapHandling.PreventNewProcess)
                        {
                            return;
                        }
                        else if (overlapHandling == OverlapHandling.KillOverlapped)
                        {
                            p.Kill();
                        }
                    }
                }
            }

            using (var process = new System.Diagnostics.Process())
            {
                process.StartInfo.FileName = processName;
                process.StartInfo.Arguments = filePath;
                process.Start();
            }
        }

        public static void AddPort(string name, int port)
        {
            try
            {
                string path = System.Environment.CurrentDirectory + "\\port.cmd";
                string result = "netsh advfirewall firewall add rule name = \"" + name + "\" dir =in action = allow protocol = tcp localport = " + port + Environment.NewLine;

                //C# 8.0에선 https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-8.0/using 이게 됨
                using (FileStream fs = File.Open(path, File.Exists(path) ? FileMode.Open : FileMode.Create))
                {
                    byte[] textToBytes = System.Text.Encoding.UTF8.GetBytes(result);
                    fs.Write(textToBytes, 0, textToBytes.Length);

                    using (var process = new System.Diagnostics.Process())
                    {
                        var procInfo = new System.Diagnostics.ProcessStartInfo();
                        procInfo.Verb = "runas"; //관리자 권한으로 해야 포트 추가가능
                        procInfo.FileName = path;
                        process.StartInfo = procInfo;
                        process.Start();
                        process.WaitForExit();
                    }
                }

                File.Delete(path);
            }
            catch
            {
            }
        }


        #region Windows System Enum

        public enum ShowWindowCommands : uint
        {
            /// <summary>
            ///        Hides the window and activates another window.
            /// </summary>
            SW_HIDE = 0,

            /// <summary>
            ///        Activates and displays a window. If the window is minimized or maximized, the system restores it to its original size and position. An application should specify this flag when displaying the window for the first time.
            /// </summary>
            SW_SHOWNORMAL = 1,

            /// <summary>
            ///        Activates and displays a window. If the window is minimized or maximized, the system restores it to its original size and position. An application should specify this flag when displaying the window for the first time.
            /// </summary>
            SW_NORMAL = 1,

            /// <summary>
            ///        Activates the window and displays it as a minimized window.
            /// </summary>
            SW_SHOWMINIMIZED = 2,

            /// <summary>
            ///        Activates the window and displays it as a maximized window.
            /// </summary>
            SW_SHOWMAXIMIZED = 3,

            /// <summary>
            ///        Maximizes the specified window.
            /// </summary>
            SW_MAXIMIZE = 3,

            /// <summary>
            ///        Displays a window in its most recent size and position. This value is similar to <see cref="ShowWindowCommands.SW_SHOWNORMAL"/>, except the window is not activated.
            /// </summary>
            SW_SHOWNOACTIVATE = 4,

            /// <summary>
            ///        Activates the window and displays it in its current size and position.
            /// </summary>
            SW_SHOW = 5,

            /// <summary>
            ///        Minimizes the specified window and activates the next top-level window in the z-order.
            /// </summary>
            SW_MINIMIZE = 6,

            /// <summary>
            ///        Displays the window as a minimized window. This value is similar to <see cref="ShowWindowCommands.SW_SHOWMINIMIZED"/>, except the window is not activated.
            /// </summary>
            SW_SHOWMINNOACTIVE = 7,

            /// <summary>
            ///        Displays the window in its current size and position. This value is similar to <see cref="ShowWindowCommands.SW_SHOW"/>, except the window is not activated.
            /// </summary>
            SW_SHOWNA = 8,

            /// <summary>
            ///        Activates and displays the window. If the window is minimized or maximized, the system restores it to its original size and position. An application should specify this flag when restoring a minimized window.
            /// </summary>
            SW_RESTORE = 9,

            /// <summary>
            ///        Items 10, 11 and 11 existed in the VB definition but not the c# definition - so I am assuming this was a mistake and have added them here.
            ///         Please forgive me if this is wrong!  I don't think it should have any negative impact.
            ///         According to what I have read elsewhere: The SW_SHOWDEFAULT makes sure the window is restored prior to showing, then activating.
            ///         And the 11's try to coerce a window to minimized or maximized.
            /// </summary>
            SW_SHOWDEFAULT = 10,
            SW_FORCEMINIMIZE = 11,
            SW_MAX = 11

        }

        #endregion


        #region DllImport
        /// <summary>
        /// Native C++ windows calls.
        /// reference: https://www.pinvoke.net
        /// </summary>
        public const string User32Dll = "user32";
        public const string Kerner32Dll = "kernel32";
        /// <summary>
        ///  Set Display Window
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="nCmdShow"></param>
        /// <returns></returns>
        [DllImport(User32Dll)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow(IntPtr hWnd, uint nCmdShow);

        public static bool WindowMinimize(IntPtr hWnd)
        {
            return ShowWindow(hWnd, (uint)ShowWindowCommands.SW_MINIMIZE);
        }
        public static bool WindowShow(IntPtr hWnd)
        {
            return ShowWindow(hWnd, (uint)ShowWindowCommands.SW_RESTORE);
        }
        public static bool WindowHide(IntPtr hWnd)
        {
            return ShowWindow(hWnd, (uint)ShowWindowCommands.SW_HIDE);
        }

        [DllImport("shell32")]
        public static extern IntPtr ExtractAssociatedIcon(IntPtr hInst, StringBuilder lpIconPath, out ushort lpiIcon);

        /// <summary>
        ///  안됨 확인필요. This function retrieves the visibility state of the specified window.
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        [DllImport(User32Dll)]
        public static extern bool IsWindowVisible(IntPtr windowHandle);


        /// <summary>
        /// This function returns the handle to the foreground window — the window with which the user is currently working.
        /// </summary>
        /// <returns></returns>
        [DllImport(User32Dll)]
        public static extern IntPtr GetForegroundWindow();

        /// <summary>
        /// This function puts the thread that created the specified window into the foreground and activates the window.
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        [DllImport(User32Dll, SetLastError = true)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);


        [DllImport(User32Dll)]
        public static extern IntPtr FindWindow(string SClassName, string SWindowName);


        /// <summary>
        /// 윈도우 찾기 Extension
        /// </summary>
        /// <param name="parentWindowHandle">부모 윈도우 핸들</param>
        /// <param name="childAfterWindowHandle">찾기 이후 자식 윈도우 핸들</param>
        /// <param name="className">클래스명</param>
        /// <param name="windowText">윈도우 텍스트</param>
        /// <returns>윈도우 핸들</returns>
        [DllImport(User32Dll)]
        public static extern IntPtr FindWindowEx(IntPtr parentWindowHandle, IntPtr childAfterWindowHandle, string className, string windowText);


        /// <summary>
        /// 윈도우 스레드 프로세스 ID 구하기
        /// </summary>
        /// <param name="windowHandle">윈도우 핸들</param>
        /// <param name="processID">프로세스 ID</param>
        /// <returns>처리 결과</returns>
        [DllImport(User32Dll)]
        public static extern uint GetWindowThreadProcessId(IntPtr windowHandle, out uint processID);


        [DllImport(User32Dll)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);


        // 맨앞으로
        [DllImport(User32Dll)]
        public static extern void BringWindowToTop(IntPtr hwnd);

        private const int ShowNORMAL = 1;
        private const int ShowMINIMIZED = 2;
        private const int ShowMAXIMIZED = 3;
        private const int SW_MAXIMIZE = 3;
        private const int SW_MINIMIZE = 6;

        [DllImport(User32Dll)]
        private static extern bool ShowWindowAsync(IntPtr findname, int howShow);


        [DllImport(Kerner32Dll, SetLastError = true)]
        public static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport(Kerner32Dll)]
        public static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport(Kerner32Dll)]
        public static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);


        #endregion


        #region Console Quick Edit
        const uint ENABLE_QUICK_EDIT = 0x0040;
        // STD_INPUT_HANDLE (DWORD): -10 is the standard input device.
        const int STD_INPUT_HANDLE = -10;

        public static bool SetQuickEditModeDisable()
        {
            var consoleHandle = GetStdHandle(STD_INPUT_HANDLE);

            // get current console mode
            if (!GetConsoleMode(consoleHandle, out uint consoleMode))
            {
                return false;
            }

            // Clear the quick edit bit in the mode flags
            // set the new mode
            if (!SetConsoleMode(consoleHandle, consoleMode &= ~ENABLE_QUICK_EDIT))
            {
                return false;
            }

            return true;
        }
        #endregion

        public static string GetWindowTitleText(IntPtr whandle)
        {
            const int nChars = 256;
            var builder = new StringBuilder(nChars);
            if (GetWindowText(whandle, builder, nChars) > 0)
            {
                string title = builder.ToString();
                builder.Clear(); builder = null;
                return title;
            }
            return null;
        }

        public static void SetWindowTop(IntPtr handleName)
        {
            try
            {
                ShowWindowAsync(handleName, ShowNORMAL);
                WindowShow(handleName);
                BringWindowToTop(handleName);
                SetForegroundWindow(handleName);
            }
            catch
            {
                return;
            }
        }

    }
}
