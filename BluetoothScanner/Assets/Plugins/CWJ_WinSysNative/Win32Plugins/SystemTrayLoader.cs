using CWJ;

using Microsoft.Win32;
using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using UnityEngine;

/// <summary>
/// Systemtray menu & actions
/// </summary>
public class SystemTrayLoader : MonoBehaviour
{
    public SystemTray tray;

    public static SystemTrayLoader instance = null;
    void Awake()
    {
        ////singleton
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
    }

    //..menuitems variables
    MenuItem[] weathers = new MenuItem[2];
    MenuItem displaySetup;
    public MenuItem startup;
    public MenuItem video;
    public MenuItem update;
    MenuItem gear_clock, circle_clock, simple_clock;
    MenuItem auto_ui_color, manual_ui_color;

    /// <summary>
    /// initialize traymenu, called after menucontroller script initilization.  Running on Main Unity Thread x_x..
    /// </summary>
    public void Start()
    {
        tray = new SystemTray(GetIconFromExePath());

        if (tray != null)
        {
            tray.SetTitle(UnityEngine.Application.productName);
            MenuItem weather = new MenuItem("Weather");

            weathers[0] = tray.trayMenu.MenuItems.Add("Clear", new EventHandler(Weather_Btn));
            weathers[1] = tray.trayMenu.MenuItems.Add("Thunder & Heavy Rain", new EventHandler(Weather_Btn));


            weather.MenuItems.AddRange(weathers);
            tray.trayMenu.MenuItems.Add(weather);
            tray.trayMenu.MenuItems.Add("-");

            MenuItem clockType = new MenuItem("Clock Style");
            gear_clock = tray.trayMenu.MenuItems.Add("Gear", Clock_Btn);
            circle_clock = tray.trayMenu.MenuItems.Add("Circle", new EventHandler(Clock_Btn));
            simple_clock = tray.trayMenu.MenuItems.Add("Simple", new EventHandler(Clock_Btn));
            clockType.MenuItems.Add(gear_clock);
            clockType.MenuItems.Add(circle_clock);
            clockType.MenuItems.Add(simple_clock);
            tray.trayMenu.MenuItems.Add(clockType);

            MenuItem uiColor = new MenuItem("UI Color");
            auto_ui_color = tray.trayMenu.MenuItems.Add("Auto", new EventHandler(UI_Btn));
            manual_ui_color = tray.trayMenu.MenuItems.Add("Pick One", new EventHandler(UI_Btn));
            uiColor.MenuItems.Add(auto_ui_color);
            uiColor.MenuItems.Add(manual_ui_color);
            tray.trayMenu.MenuItems.Add(uiColor);



            startup = new MenuItem("Run at Startup", new EventHandler(System_Startup_Btn));
            tray.trayMenu.MenuItems.Add(startup);
            tray.trayMenu.MenuItems.Add("-");

            MenuItem website = new MenuItem("Project Website", new EventHandler(WebpageBtn));
            tray.trayMenu.MenuItems.Add(website);



            MenuItem settings = new MenuItem("Settings", new EventHandler(Settings_Launcher));
            tray.trayMenu.MenuItems.Add(settings);
            tray.trayMenu.MenuItems.Add("-");

            MenuItem close = new MenuItem("Exit", new EventHandler(Close_Action));
            tray.trayMenu.MenuItems.Add(close);

            tray.trayIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(Settings_Launcher_Mouse);

            tray.ShowNotification("Hello..", "I'll just stay in systemtray, right click for more option...", 1000);


            startup.Checked = false;

            WeatherBtnCheckMark();
            ClockCheckMark();
            ColorCheckMark();

        }
    }

    //static System.Drawing.Icon ConvertTextureToIcon(Texture2D iconTexture)
    //{
    //    using var memStream = new MemoryStream(iconTexture.EncodeToPNG());
    //    memStream.Seek(0, System.IO.SeekOrigin.Begin);
    //    var bitmap = new System.Drawing.Bitmap(memStream);
    //    var icon2 = System.Drawing.Icon.FromHandle(bitmap.GetHicon());
    //    icon2.Save(memStream);
    //    return icon2;
    //}
#if UNITY_EDITOR
    [SerializeField, Readonly] string editorBuildFilePath;

    [InvokeButton]
    void Editor_SetEditorBuildFile()
    {
        PathUtil.OpenFileDialog("Select BuildFile",
            (path) =>
            {
                if (path.Length == 0)
                    Debug.LogError("선택안함");
                else
                    editorBuildFilePath = path;
            }, UnityEngine.Application.dataPath, "실행파일", "exe");
    }
#endif
    public System.Drawing.Icon GetIconFromExePath()
    {
        string exePath = WinSysHelper.MyExePath;

#if UNITY_EDITOR
        exePath = editorBuildFilePath;
#endif

        string iconPath = Path.ChangeExtension(exePath, "ico");
        bool isExists = File.Exists(iconPath);
        System.Drawing.Icon resultIcon = null;
        if (!isExists)
        {
            Debug.Log($"{nameof(SystemTray)}: use exe icon");
            try
            {
                resultIcon = Icon.ExtractAssociatedIcon(exePath);
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
                return null;
            }

            using (var stream = new System.IO.FileStream(iconPath, System.IO.FileMode.CreateNew))
            {
                resultIcon.Save(stream);
            }
        }
        else
        {
            Debug.Log($"{nameof(SystemTray)}: use custom icon");
            var strB = new StringBuilder(iconPath);
            var handle = WinSysUtil.ExtractAssociatedIcon(IntPtr.Zero, strB, out _);
            resultIcon = Icon.FromHandle(handle);
            strB.Clear();
        }

        return resultIcon;
    }

    //static System.Drawing.Icon GetIconFromPngPath (string pngPathInStreaming)
    //{
    //    string icoFilePath = UnityEngine.Application.persistentDataPath + "\\" + Path.ChangeExtension(pngPathInStreaming, "ico");
        
    //    using (FileStream stream = File.OpenWrite(icoFilePath))
    //    {
    //        Bitmap bitmap = (Bitmap)Image.FromFile(pngPathInStreaming);
    //        var ico = Icon.FromHandle(bitmap.GetHicon());
    //        ico.Save(stream);
    //        return ico;
    //    }
    //}

    private void Settings_Launcher_Mouse(object sender, MouseEventArgs e)
    {
        Debug.Log($"{nameof(SystemTrayLoader)}: tray.trayIcon.MouseDoubleClick ");
    }

    private void Weather_Btn(object sender, EventArgs e)
    {
        Debug.Log($"{nameof(SystemTrayLoader)}: {((sender as MenuItem).Text)}");
    }

    #region multimoniotr_menu

    private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
    {
        UpdateTrayMenuDisplay();
        MoveToDisplay(0);
    }

    //future use.
    void UpdateTrayMenuDisplay()
    {
        displaySetup.MenuItems.Clear();
        System.Windows.Forms.Screen[] screens = System.Windows.Forms.Screen.AllScreens;
        int i = 0;
        //displaySetup.MenuItems.Add("Span", new EventHandler(UserDisplayMenu));
        foreach (var item in screens)
        {
            displaySetup.MenuItems.Add("Display " + i.ToString(), new EventHandler(UserDisplayMenu));
            i++;
        }

        if (screens.Length > 1)
            displaySetup.Enabled = true;
        else
            displaySetup.Enabled = false;
    }

    private void UserDisplayMenu(object sender, EventArgs e)
    {
        int i = 0;
        string s = (sender as MenuItem).Text;
        /*
        if (s == "Span")
        {
            MoveToDisplay(-1);
            return;
        }
        */

        foreach (var item in System.Windows.Forms.Screen.AllScreens)
        {
            if (s == "Display " + i.ToString())
            {
                MoveToDisplay(i);
                break;
            }
            i++;
        }
    }

    void MoveToDisplay(int i)
    {
        Debug.Log($"{nameof(SystemTrayLoader)}: {i}");
    }

    #endregion
    //unity might be intercepting the messages or windows fked up, todo:- have to find a solution
    #region power_suspend_resume_UNUSED
    void OnPowerChange(object sender, PowerModeChangedEventArgs e)
    {
        Debug.Log($"POWER CHANGE {e.Mode}");
    }
    #endregion power_suspend_resume

    /// <summary>
    /// Update traymenu color submenu checkmark
    /// </summary>
    public void ColorCheckMark()
    {
        if (UnityEngine.Application.isEditor == false)
        {
            if (auto_ui_color.Enabled == true)
                auto_ui_color.Enabled = false;

            auto_ui_color.Checked = false;
            manual_ui_color.Checked = true;

        }

    }

    /// <summary>
    /// traymenu color picker submenu action
    /// </summary>
    private void UI_Btn(object sender, System.EventArgs e)
    {
        ColorCheckMark();
    }

    /// <summary>
    /// Update traymenu clocks submenu checkmark
    /// </summary>
    public void ClockCheckMark()
    {
        gear_clock.Checked = false;
        circle_clock.Checked = false;
        simple_clock.Checked = false;
    }

    /// <summary>
    /// Update traymenu weather submenu checkmark
    /// </summary>
    public void WeatherBtnCheckMark()
    {
        try
        {
            foreach (var item in weathers) //button text
            {
                item.Checked = false;
            }
        }
        catch (Exception e)
        {
            Debug.Log("Error weathrbtn checkmark" + e.Message);
        }

    }



    private void WebpageBtn(object sender, System.EventArgs e)
    {
        //System.Diagnostics.Process.Start("");
    }

    private void KofiBtn(object sender, System.EventArgs e)
    {
        //System.Diagnostics.Process.Start("");
    }

    /// <summary>
    /// Multimonitor display utility launch.
    /// </summary>
    private void DisplayBtn(object sender, System.EventArgs e)
    {

        Debug.Log("Controller script not found");
    }

    private void Clock_Btn(object sender, System.EventArgs e)
    {
        string s = (sender as MenuItem).Text;
        Debug.Log($"{nameof(SystemTrayLoader)}: {s}");
        ClockCheckMark();
    }

    private void Update_Check(object sender, System.EventArgs e)
    {
        //System.Diagnostics.Process.Start("");
    }

    /// <summary>
    /// Enable/Disable weather selection traymenu.
    /// </summary>
    /// <param name="enabled">Enable/Disable traymenu.</param>
    public void WeatherMenuEnable(bool enabled)
    {
        if (enabled == false)
        {
            foreach (var item in weathers)
            {
                item.Enabled = false;
            }
        }
        else
        {
            foreach (var item in weathers)
            {
                item.Enabled = true;
            }
        }
    }


    /// <summary>
    /// traymenu, launch configuration utility.
    /// </summary>
    private void Settings_Launcher(object sender, System.EventArgs e)
    {
        Debug.Log($"{nameof(SystemTrayLoader)}:{nameof(Settings_Launcher)} ");
    }


    /// <summary>
    /// traymenu - Exit Application.
    /// </summary>
    /// <remarks>
    /// Disposes traymenu, stops dxva playback instance, refreshes desktop by calling setwallpaper, closes all open windows.
    /// </remarks>
    public void Close_Action(object sender, System.EventArgs e)
    {
#if UNITY_EDITOR
        if (UnityEditor.EditorApplication.isPlaying)
        {
            UnityEditor.EditorApplication.isPlaying = false;
            return;
        }
#endif
        UnityEngine.Application.Quit();
    }


    private void OnApplicationQuit()
    {
        tray?.Dispose();
    }



    /// <summary>
    /// traymenu run at startup button
    /// </summary>
    private void System_Startup_Btn(object sender, System.EventArgs e)
    {
        runAtStartup = !runAtStartup;
        if (runAtStartup == true) //btn checkmark
            startup.Checked = true;
        else
            startup.Checked = false;
        WinSysUtil.SetStartup(runAtStartup);
    }
    private bool runAtStartup = false;
}
