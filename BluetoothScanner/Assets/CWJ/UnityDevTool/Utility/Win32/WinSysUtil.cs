using System.Runtime.InteropServices;
using System;
using System.Text;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using static UnityEngine.Rendering.VirtualTexturing.Debugging;

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


        public static Version GetVersion()
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

        public static string ClipboardValue { get => GUIUtility.systemCopyBuffer; set => GUIUtility.systemCopyBuffer = value; }
        public static void CopyToClipboard(this string str)
        {
            //GUIUtility.systemCopyBuffer = str;
            var textEditor = new UnityEngine.TextEditor();
            textEditor.text = str;
            textEditor.SelectAll();
            textEditor.Copy();
            UnityEngine.Debug.Log($"Copied!\n(\"{str}\")");
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

                    using (Process process = new Process())
                    {
                        ProcessStartInfo procInfo = new ProcessStartInfo();
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

        #region DllImport
        /// <summary>
        /// Native C++ windows calls.
        /// reference: https://www.pinvoke.net
        /// </summary>
        public const string User32Dll = "user32.dll";
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

        [DllImport("shell32.dll")]
        public static extern IntPtr ExtractAssociatedIcon(IntPtr hInst, StringBuilder lpIconPath, out ushort lpiIcon);

        /// <summary>
        ///  안됨 확인필요. This function retrieves the visibility state of the specified window.
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        [DllImport("USER32.DLL")]
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


        [DllImport("user32.dll")]
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
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);


        // 맨앞으로
        [DllImport(User32Dll)]
        private static extern void BringWindowToTop(IntPtr hwnd);

        private const int ShowNORMAL = 1;
        private const int ShowMINIMIZED = 2;
        private const int ShowMAXIMIZED = 3;
        private const int SW_MAXIMIZE = 3;
        private const int SW_MINIMIZE = 6;

        [DllImport(User32Dll)]
        private static extern bool ShowWindowAsync(IntPtr findname, int howShow);

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
