using CWJ;
using CWJ.Serializable;
using System;
using System.IO;
using System.Text;

using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class BleScannerProcessMngr : DisposableMonoBehaviour
{
    public enum CommandType
    {
        NULL = 0,
        start,
        stop,
        show,
        hide,
        quit,
        exit
    }

    static bool isVerified = false;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        if (!Application.isEditor)
        {
            if (WinSysHelper.IsPreventProcessExecuted(isShowWhenOverlapped: false, hasDefaultErrorMsg: false))
            {
                UnityEngine.Application.Quit();
                return;
            }
            WinSysHelper.HideWindow();
        }
        isVerified = true;
    }

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

    [SerializeField, ShowConditional(EPlayMode.PlayMode), Readonly] string commandTxtPath;
    [SerializeField, ShowConditional(EPlayMode.PlayMode), Readonly] string detectedTxtPath;

    [MustRequiredComp, SerializeField, Readonly] FileChangedChecker fileChangedChecker;

    [MustRequiredComp, SerializeField, Readonly] BleScanner bleScanner;

    [SerializeField, Readonly] CommandType commandType = CommandType.NULL;

    StringBuilder detectedDeviceNameBuilder = null;

    string separatorStr;
    const string Tag_System = "SYSTEM PATH AND CONFIGURATION";
    const string Tag_CommandTextFile = "Command Text File";
    const string Tag_DetectedTextFile = "Detected Text File";
    const string Tag_SeparatorChar = "Separator Char";
    
    [SerializeField]
    DictionaryVisualized<CommandType, UnityEvent> callbackByCmdType = new DictionaryVisualized<CommandType, UnityEvent>();

    private void Awake()
    {
        if (!isVerified)
        {
            return;
        }

        try
        {
            callbackByCmdType.AddCallbackInDictionaryByEnum(this, nameof(OnCommand_start), enumStartValue: CommandType.start, separatorChr: '_');

            bleScanner.deviceUpdateEvent.AddListener_New(OnDetectedDevice);

            string buildFolderPath = null;
            if (!Application.isEditor)
            {
                buildFolderPath = WinSysHelper.MyExeFolderPath;
                Debug.LogError(buildFolderPath);
            }

#if UNITY_EDITOR
            buildFolderPath = Path.GetDirectoryName(editorBuildFilePath);
#endif

            var exeFolder = buildFolderPath;
            string iniPath = Path.Combine(exeFolder, "Path.ini");
            IniFile ini = new IniFile();

            if (!File.Exists(iniPath))
            {
                ini[Tag_System][Tag_CommandTextFile] = "Command.txt";
                ini[Tag_System][Tag_DetectedTextFile] = "DetectedNames.txt";
                ini[Tag_System][Tag_SeparatorChar] = ",";

                ini.Save(iniPath, FileMode.Create);
            }
            else
            {
                ini.Load(iniPath);
            }

            detectedTxtPath = Path.Combine(exeFolder, ini[Tag_System][Tag_DetectedTextFile].ToString());

            TextReadOrWriter.CreateOrWriteText(detectedTxtPath, string.Empty);
            

            separatorStr = ini[Tag_System][Tag_SeparatorChar].ToString();

            commandTxtPath = Path.Combine(exeFolder, ini[Tag_System][Tag_CommandTextFile].ToString());
            if (!File.Exists(commandTxtPath))
                TextReadOrWriter.CreateText(commandTxtPath);
            else if (!Application.isEditor)
                UpdateCommandByTxt(commandTxtPath);

            fileChangedChecker.fileChangedEvent.AddListener_New(UpdateCommandByTxt);
            fileChangedChecker.InitSystemWatcher(Path.GetDirectoryName(commandTxtPath), Path.GetFileName(commandTxtPath));

        }
        catch (Exception ex)
        {
            Debug.LogError(ex.ToString());
            TextReadOrWriter.CreateOrWriteText(detectedTxtPath, "ERROR");
        }

    }

    bool hasValue = false;
    void OnDetectedDevice(string name, bool isConnectable)
    {
        if (name.Length == 0)
        {
            return;
        }

        if (hasValue)
            detectedDeviceNameBuilder.Append(separatorStr);

        detectedDeviceNameBuilder.Append(name);

        if (!hasValue)
            hasValue = true;

        string detectedNames = detectedDeviceNameBuilder.ToString();
        TextReadOrWriter.CreateOrWriteText(detectedTxtPath, detectedNames);
    }

    string lastCommandStr = string.Empty;
    void UpdateCommandByTxt(string path)
    {
        string curCommandStr = TextReadOrWriter.ReadText(path).Trim();
        if (lastCommandStr != curCommandStr)
        {
            lastCommandStr = curCommandStr;
            if (curCommandStr.Length == 0)
            {
                if (commandType != CommandType.NULL)
                    commandType = CommandType.NULL;
                return;
            }
        }
        else
        {
            return;
        }

        if (!EnumUtil.TryToEnum<CommandType>(curCommandStr.ToLower(), out var newCommandType)
            || newCommandType == commandType)
        {
            return;
        }

        //change command type start
        commandType = newCommandType;

        Debug.Log("[Command] Received : " + commandType.ToString());

        callbackByCmdType[commandType].Invoke();

        TextReadOrWriter.WriteText(commandTxtPath, string.Empty);
    }

    void OnCommand_start()
    {
        TextReadOrWriter.CreateOrWriteText(detectedTxtPath, string.Empty);

        if (detectedDeviceNameBuilder == null)
            detectedDeviceNameBuilder = new StringBuilder();
        else
            detectedDeviceNameBuilder.Clear();

        bleScanner.StartScan(isLoopScan: true);
    }

    void OnCommand_stop()
    {
        bleScanner.StopScan();
        if (detectedDeviceNameBuilder != null)
        {
            detectedDeviceNameBuilder.Clear();
            detectedDeviceNameBuilder = null;
        }
        hasValue = false;
    }

    void OnCommand_show()
    {
        WinSysHelper.ShowWindow();
    }
    void OnCommand_hide()
    {
        WinSysHelper.HideWindow();
    }
    void OnCommand_exit()
    {
        AppQuit();
    }
    void OnCommand_quit()
    {
        AppQuit();
    }

    void AppQuit()
    {
        Dispose();

        if (!Application.isEditor)
        {
            UnityEngine.Application.Quit();
        }
        else
        {
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlaying)
                UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }

    protected override void OnDispose()
    {
        if (!isVerified)
        {
            return;
        }
        fileChangedChecker.Dispose();
        bleScanner.Dispose();
        if (File.Exists(commandTxtPath))
            TextReadOrWriter.WriteText(commandTxtPath, string.Empty);
    }
}
