using CWJ;
using CWJ.Serializable;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class BleScannerProcessMngr : DisposableMonoBehaviour
{
    public enum CommandType
    {
        NULL = 0,
        //start,
        //stop,
        //show,
        //hide,
        quit,
        exit
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


    string separatorStr;
    const string Tag_System = "SYSTEM PATH AND CONFIGURATION";
    const string Tag_CommandTextFile = "Command Text File";
    const string Tag_DetectedTextFile = "Detected Text File";
    const string Tag_SeparatorChar = "Separator Char";
    
    [SerializeField]
    DictionaryVisualized<CommandType, UnityEvent> callbackByCmdType = new DictionaryVisualized<CommandType, UnityEvent>();

    static bool isVerified = false;


    private void Awake()
    {
        if (!Application.isEditor)
        {
            if (WinSysHelper.IsPreventProcessExecuted(isShowWhenOverlapped: false, hasDefaultErrorMsg: false))
            {
                UnityEngine.Application.Quit();
                return;
            }
            WinSysHelper.MinimizeMyWindow();
        }

        isVerified = true;
    }

    private void Start()
    {
        if (!isVerified)
        {
            return;
        }

        callbackByCmdType.AddCallbackInDictionaryByEnum(this, nameof(OnCommand_quit), enumStartValue: CommandType.quit, separatorChr: '_');

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


        separatorStr = ini[Tag_System][Tag_SeparatorChar].ToString();

        commandTxtPath = Path.Combine(exeFolder, ini[Tag_System][Tag_CommandTextFile].ToString());
        if (!File.Exists(commandTxtPath))
            TextReadOrWriter.CreateText(commandTxtPath);
        else if (!Application.isEditor)
            UpdateCommandByTxt(commandTxtPath);

        detectedTxtPath = Path.Combine(exeFolder, ini[Tag_System][Tag_DetectedTextFile].ToString());

        fileChangedChecker.fileChangedEvent.AddListener_New(UpdateCommandByTxt);
        fileChangedChecker.InitSystemWatcher(Path.GetDirectoryName(commandTxtPath), Path.GetFileName(commandTxtPath));

        bleScanner.deviceUpdateEvent.AddListener_New(OnDeviceUpdate);

        CO_LoopStart = StartCoroutine(IE_LoopStartAndStop());
    }

    Coroutine CO_LoopStart = null;
    IEnumerator IE_LoopStartAndStop()
    {
        var waitForSec = new WaitForSeconds(7.7f);
        do
        {
            OnCommand_start();
            yield return waitForSec;
            OnCommand_stop();
        } while (true);
    }

    Dictionary<string, DeviceCache> deviceCacheById = new Dictionary<string, DeviceCache>();

    bool isDeviceUpdated = false;
    void OnDeviceUpdate(DeviceCache device)
    {
        isDeviceUpdated = true;
        if (!deviceCacheById.TryGetValue(device.id, out var deviceCache))
            deviceCacheById.Add(device.id, device);
        else
            deviceCacheById[device.id] = device;
        isDeviceUpdated = false;
    }

    Coroutine CO_WriteBleDevice = null;
    IEnumerator DO_WriteBleDevice()
    {
        TextReadOrWriter.CreateOrWriteText(detectedTxtPath, string.Empty);
        yield return null;

        var waitForTime = new WaitForSeconds(0.77f);

        do
        {
            if (isDeviceUpdated)
                yield return null;

            var devices = deviceCacheById.Values;
            var deviceNames = devices.Where(device => device.CheckIsVerifiedAndConnectable()).Select(device => device.name);

            TextReadOrWriter.WriteText(detectedTxtPath, $"[{DateTime.Now.ToString("HH:mm:ss")}]" + string.Join(separatorStr, deviceNames));

            yield return waitForTime;

        } while (CO_WriteBleDevice != null);

        CO_WriteBleDevice = null;
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

    }

    void OnCommand_start()
    {
        deviceCacheById.Clear();
        bleScanner.StartScan(isLoopScan: true);

        if (CO_WriteBleDevice == null)
            CO_WriteBleDevice = StartCoroutine(DO_WriteBleDevice());
    }

    void OnCommand_stop()
    {
        if (CO_WriteBleDevice != null)
        {
            StopCoroutine(CO_WriteBleDevice);
            CO_WriteBleDevice = null;
        }

        bleScanner.StopScan();
    }

    void OnCommand_show()
    {
        WinSysHelper.ShowMyWindow();
    }
    void OnCommand_hide()
    {
        WinSysHelper.HideMyWindow();
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
        if (CO_LoopStart != null)
            StopCoroutine(CO_LoopStart);
        OnCommand_stop();
        fileChangedChecker.Dispose();
        bleScanner.Dispose();
        if (File.Exists(commandTxtPath))
            TextReadOrWriter.WriteText(commandTxtPath, string.Empty);
    }
}
